using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PCMonitorClient
{
    public class ActivityMonitor
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        private PerformanceCounter cpuCounter;
        private PerformanceCounter memCounter;
        private List<string> recentActivities;
        private const int MaxActivities = 20;

        public ActivityMonitor()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            memCounter = new PerformanceCounter("Memory", "Available MBytes");
            recentActivities = new List<string>();
        }

        public string GetActiveWindowTitle()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                StringBuilder sb = new StringBuilder(256);
                if (GetWindowText(handle, sb, 256) > 0)
                {
                    return sb.ToString();
                }
            }
            catch { }
            return "No Active Window";
        }

        public string GetActiveProcessName()
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                uint processId;
                GetWindowThreadProcessId(handle, out processId);
                
                if (processId != 0)
                {
                    var process = Process.GetProcessById((int)processId);
                    return process.ProcessName;
                }
            }
            catch { }
            return "Unknown";
        }

        public double GetCPUUsage()
        {
            try
            {
                return Math.Round(cpuCounter.NextValue(), 1);
            }
            catch
            {
                return 0;
            }
        }

        public double GetMemoryUsageMB()
        {
            try
            {
                var totalMemory = GetTotalMemoryMB();
                var availableMemory = memCounter.NextValue();
                return Math.Round(totalMemory - availableMemory, 0);
            }
            catch
            {
                return 0;
            }
        }

        private double GetTotalMemoryMB()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var totalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                        return Math.Round(totalBytes / (1024 * 1024), 0);
                    }
                }
            }
            catch { }
            return 0;
        }

        public List<string> GetTopProcesses(int count = 10)
        {
            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) || p.WorkingSet64 > 50 * 1024 * 1024)
                    .OrderByDescending(p => p.WorkingSet64)
                    .Take(count)
                    .Select(p => $"{p.ProcessName} ({FormatBytes(p.WorkingSet64)})")
                    .ToList();
                
                return processes;
            }
            catch
            {
                return new List<string>();
            }
        }

        public void LogActivity(string window, string process)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var activity = $"{timestamp} - {window} [{process}]";
            
            if (recentActivities.Count == 0 || !recentActivities[0].EndsWith($"[{process}]"))
            {
                recentActivities.Insert(0, activity);
                if (recentActivities.Count > MaxActivities)
                {
                    recentActivities.RemoveAt(recentActivities.Count - 1);
                }
            }
        }

        public List<string> GetRecentActivities()
        {
            return new List<string>(recentActivities);
        }

        public string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:F0} {sizes[order]}";
        }

        public byte[] CaptureScreenshot(int quality = 30, string resolution = "720p")
        {
            try
            {
                // Capture entire screen
                var bounds = Screen.PrimaryScreen.Bounds;
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }

                    // Calculate target resolution
                    int targetWidth, targetHeight;
                    switch (resolution.ToLower())
                    {
                        case "1080p":
                            targetWidth = 1920;
                            targetHeight = 1080;
                            break;
                        case "720p":
                            targetWidth = 1280;
                            targetHeight = 720;
                            break;
                        case "480p":
                            targetWidth = 854;
                            targetHeight = 480;
                            break;
                        default: // Original resolution, scale to 50%
                            targetWidth = bounds.Width / 2;
                            targetHeight = bounds.Height / 2;
                            break;
                    }

                    // Maintain aspect ratio
                    double aspectRatio = (double)bounds.Width / bounds.Height;
                    if (resolution != "original")
                    {
                        if (aspectRatio > (double)targetWidth / targetHeight)
                        {
                            targetHeight = (int)(targetWidth / aspectRatio);
                        }
                        else
                        {
                            targetWidth = (int)(targetHeight * aspectRatio);
                        }
                    }

                    using (var resized = new Bitmap(targetWidth, targetHeight))
                    {
                        using (var graphics = Graphics.FromImage(resized))
                        {
                            // Use high quality for better clarity at lower resolutions
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            graphics.DrawImage(bitmap, 0, 0, targetWidth, targetHeight);
                        }

                        // Compress as JPEG with specified quality
                        using (var ms = new MemoryStream())
                        {
                            var encoder = GetEncoder(ImageFormat.Jpeg);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                            resized.Save(ms, encoder, encoderParams);
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public void Dispose()
        {
            cpuCounter?.Dispose();
            memCounter?.Dispose();
        }
    }
}
