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
    public class ActivityMonitor : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private PerformanceCounter cpuCounter;
        private PerformanceCounter memCounter;
        private List<string> recentActivities;
        private const int MaxActivities = 20;

        public ActivityMonitor()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }
            catch { cpuCounter = null; }

            try
            {
                memCounter = new PerformanceCounter("Memory", "Available MBytes");
            }
            catch { memCounter = null; }

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
                    using (var process = Process.GetProcessById((int)processId))
                    {
                        return process.ProcessName;
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        public double GetCPUUsage()
        {
            try
            {
                if (cpuCounter != null)
                {
                    return Math.Round(cpuCounter.NextValue(), 1);
                }
            }
            catch { }
            return 0;
        }

        public double GetMemoryUsageMB()
        {
            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus) && memStatus.ullTotalPhys > 0)
                {
                    double usedBytes = (double)(memStatus.ullTotalPhys - memStatus.ullAvailPhys);
                    return Math.Round(usedBytes / (1024 * 1024), 0);
                }
            }
            catch { }

            try
            {
                var totalMemory = GetTotalMemoryMB();
                var availableMemory = memCounter != null ? memCounter.NextValue() : 0;
                return Math.Round(Math.Max(0, totalMemory - availableMemory), 0);
            }
            catch
            {
                return 0;
            }
        }

        private double? _cachedTotalMemoryMB;

        private double GetTotalMemoryMB()
        {
            if (_cachedTotalMemoryMB.HasValue)
            {
                return _cachedTotalMemoryMB.Value;
            }

            try
            {
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus) && memStatus.ullTotalPhys > 0)
                {
                    _cachedTotalMemoryMB = Math.Round((double)memStatus.ullTotalPhys / (1024 * 1024), 0);
                    return _cachedTotalMemoryMB.Value;
                }
            }
            catch { }

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        using (obj)
                        {
                            var totalBytes = Convert.ToDouble(obj["TotalPhysicalMemory"]);
                            _cachedTotalMemoryMB = Math.Round(totalBytes / (1024 * 1024), 0);
                            return _cachedTotalMemoryMB.Value;
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public List<string> GetTopProcesses(int count = 10)
        {
            Process[] allProcesses = null;
            try
            {
                allProcesses = Process.GetProcesses();
                var processes = allProcesses
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
            finally
            {
                if (allProcesses != null)
                {
                    foreach (var p in allProcesses)
                    {
                        try { p.Dispose(); } catch { }
                    }
                }
            }
        }

        public void LogActivity(string window, string process)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var activity = $"{timestamp} - {window} [{process}]";
            
            lock (recentActivities)
            {
                if (recentActivities.Count == 0 || !recentActivities[0].EndsWith($"[{process}]"))
                {
                    recentActivities.Insert(0, activity);
                    if (recentActivities.Count > MaxActivities)
                    {
                        recentActivities.RemoveAt(recentActivities.Count - 1);
                    }
                }
            }
        }

        public List<string> GetRecentActivities()
        {
            lock (recentActivities)
            {
                return new List<string>(recentActivities);
            }
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

        private static readonly ImageCodecInfo JpegEncoder = FindEncoder(ImageFormat.Jpeg);

        private static ImageCodecInfo FindEncoder(ImageFormat format)
        {
            try
            {
                var codecs = ImageCodecInfo.GetImageEncoders();
                foreach (var codec in codecs)
                {
                    if (codec.FormatID == format.Guid)
                    {
                        return codec;
                    }
                }
            }
            catch { }
            return null;
        }

        public byte[] CaptureScreenshot(int quality = 50, string resolution = "720p")
        {
            try
            {
                // Capture entire primary screen safely
                var primary = Screen.PrimaryScreen;
                Rectangle bounds = (primary != null && primary.Bounds.Width > 0 && primary.Bounds.Height > 0)
                    ? primary.Bounds
                    : (Screen.AllScreens.Length > 0 && Screen.AllScreens[0].Bounds.Width > 0 ? Screen.AllScreens[0].Bounds : new Rectangle(0, 0, 1920, 1080));

                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    bounds = new Rectangle(0, 0, 1920, 1080);
                }

                // IMPORTANT: Use Format24bppRgb. 
                // GDI+ BitBlt/CopyFromScreen does not touch the Alpha channel on Format32bppArgb bitmaps,
                // leaving Alpha at 0 (fully transparent) which causes DrawImage and JPEG save to render 100% black!
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb))
                {
                    using (var graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
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
                            targetWidth = Math.Max(1, bounds.Width / 2);
                            targetHeight = Math.Max(1, bounds.Height / 2);
                            break;
                    }

                    // Maintain aspect ratio
                    double aspectRatio = (double)bounds.Width / bounds.Height;
                    if (resolution != "original")
                    {
                        if (aspectRatio > (double)targetWidth / targetHeight)
                        {
                            targetHeight = Math.Max(1, (int)Math.Round(targetWidth / aspectRatio));
                        }
                        else
                        {
                            targetWidth = Math.Max(1, (int)Math.Round(targetHeight * aspectRatio));
                        }
                    }

                    using (var resized = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb))
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
                            if (JpegEncoder != null)
                            {
                                using (var encoderParams = new EncoderParameters(1))
                                {
                                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                                    resized.Save(ms, JpegEncoder, encoderParams);
                                }
                            }
                            else
                            {
                                resized.Save(ms, ImageFormat.Jpeg);
                            }
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback: If desktop is locked, in secure UAC switch, or display handle is temporarily unavailable,
                // render a diagnostic info banner so the server admin knows the workstation status instead of a black screen
                return CreateDiagnosticPlaceholder(ex.Message, quality);
            }
        }

        private byte[] CreateDiagnosticPlaceholder(string reason, int quality)
        {
            try
            {
                using (var bmp = new Bitmap(1280, 720, PixelFormat.Format24bppRgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(28, 28, 32));
                        using (var pen = new Pen(Color.FromArgb(70, 70, 80), 2))
                        {
                            g.DrawRectangle(pen, 20, 20, 1240, 680);
                        }

                        using (var brushTitle = new SolidBrush(Color.FromArgb(245, 245, 245)))
                        using (var brushSub = new SolidBrush(Color.FromArgb(170, 170, 180)))
                        using (var brushAcc = new SolidBrush(Color.FromArgb(255, 185, 50)))
                        using (var fontTitle = new Font("Segoe UI", 22, FontStyle.Bold))
                        using (var fontSub = new Font("Segoe UI", 12, FontStyle.Regular))
                        using (var fontSmall = new Font("Segoe UI", 9.5f, FontStyle.Regular))
                        {
                            g.DrawString("Desktop Screen Capture Paused", fontTitle, brushTitle, 50, 180);
                            g.DrawString($"PC: {Environment.MachineName} | User: {Environment.UserName} | Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", fontSub, brushAcc, 50, 235);
                            g.DrawString("Workstation may be locked, logging off, or running on a secure UAC desktop.", fontSub, brushSub, 50, 275);
                            g.DrawString($"Status: {reason}", fontSmall, brushSub, 50, 325);
                            g.DrawString("Live capture will automatically resume as soon as the user returns to the interactive desktop.", fontSmall, brushSub, 50, 360);
                        }
                    }

                    using (var ms = new MemoryStream())
                    {
                        if (JpegEncoder != null)
                        {
                            using (var ep = new EncoderParameters(1))
                            {
                                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                                bmp.Save(ms, JpegEncoder, ep);
                            }
                        }
                        else
                        {
                            bmp.Save(ms, ImageFormat.Jpeg);
                        }
                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            cpuCounter?.Dispose();
            memCounter?.Dispose();
        }
    }
}
