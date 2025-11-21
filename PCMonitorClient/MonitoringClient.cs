using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PCMonitorClient
{
    public class ServerCommand
    {
        public string CommandType { get; set; }
        public string MessageText { get; set; }
        public int Duration { get; set; }
    }

    public class ClientActivity
    {
        public string PCName { get; set; }
        public string Username { get; set; }
        public string ActiveWindow { get; set; }
        public string ActiveProcess { get; set; }
        public DateTime LastUpdate { get; set; }
        public string IPAddress { get; set; }
        public List<string> RunningProcesses { get; set; }
        public double CPUUsage { get; set; }
        public double MemoryUsageMB { get; set; }
        public List<string> RecentActivities { get; set; }
        public bool IsActive { get; set; }
        public byte[] ScreenshotData { get; set; }
    }

    public class MonitoringClient : IDisposable
    {
        private readonly string serverIP;
        private readonly int serverPort;
        private TcpClient client;
        private NetworkStream stream;
        private ActivityMonitor monitor;
        private CancellationTokenSource cancellationTokenSource;
        private bool isRunning = false;

        public MonitoringClient(string serverIp, int port)
        {
            serverIP = serverIp;
            serverPort = port;
            monitor = new ActivityMonitor();
        }

        public void Start()
        {
            if (isRunning) return;

            isRunning = true;
            cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => RunMonitoringLoopAsync(cancellationTokenSource.Token));
        }

        public void Stop()
        {
            isRunning = false;
            cancellationTokenSource?.Cancel();
            Disconnect();
        }

        private async Task RunMonitoringLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isRunning)
            {
                try
                {
                    if (client == null || !client.Connected)
                    {
                        await ConnectToServerAsync();
                    }

                    if (client != null && client.Connected)
                    {
                        await SendActivityDataAsync();
                    }

                    await Task.Delay(2000, token); // Send updates every 2 seconds
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    Disconnect();
                    await Task.Delay(5000, token); // Wait 5 seconds before retry
                }
            }
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                client = new TcpClient();
                // Increase buffer sizes for faster transfer
                client.ReceiveBufferSize = 256 * 1024; // 256 KB
                client.SendBufferSize = 256 * 1024; // 256 KB
                client.NoDelay = true; // Disable Nagle's algorithm for lower latency
                
                await client.ConnectAsync(serverIP, serverPort);
                stream = client.GetStream();
                stream.ReadTimeout = 10000; // 10 second timeout
                stream.WriteTimeout = 10000;
            }
            catch
            {
                Disconnect();
            }
        }

        private async Task SendActivityDataAsync()
        {
            try
            {
                var activeWindow = monitor.GetActiveWindowTitle();
                var activeProcess = monitor.GetActiveProcessName();
                monitor.LogActivity(activeWindow, activeProcess);

                // Capture screenshot (50% quality, 720p for better clarity and speed)
                byte[] screenshot = monitor.CaptureScreenshot(50, "720p");

                var activity = new ClientActivity
                {
                    PCName = Environment.MachineName,
                    Username = Environment.UserName,
                    ActiveWindow = activeWindow,
                    ActiveProcess = activeProcess,
                    LastUpdate = DateTime.Now,
                    IPAddress = monitor.GetLocalIPAddress(),
                    RunningProcesses = monitor.GetTopProcesses(10),
                    CPUUsage = monitor.GetCPUUsage(),
                    MemoryUsageMB = monitor.GetMemoryUsageMB(),
                    RecentActivities = monitor.GetRecentActivities(),
                    IsActive = true,
                    ScreenshotData = screenshot
                };

                string jsonData = JsonConvert.SerializeObject(activity);
                byte[] data = Encoding.UTF8.GetBytes(jsonData);

                // Send message length first (4 bytes)
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);
                await stream.WriteAsync(lengthPrefix, 0, 4);

                // Send actual data
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();

                // Wait for server response (command or ACK)
                byte[] buffer = new byte[4096];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // Check if response is a command (not just ACK)
                if (response != "ACK")
                {
                    try
                    {
                        var command = JsonConvert.DeserializeObject<ServerCommand>(response);
                        if (command != null)
                        {
                            HandleCommand(command);
                        }
                    }
                    catch { } // Ignore JSON parsing errors
                }
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        private void HandleCommand(ServerCommand command)
        {
            try
            {
                if (command.CommandType == "message" || command.CommandType == "freeze")
                {
                    // Run on a separate thread to avoid blocking the monitoring loop
                    System.Threading.Thread messageThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            var messageForm = new MessageDisplayForm(command.MessageText, command.Duration);
                            messageForm.ShowDialog();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error showing message: {ex.Message}");
                        }
                    });
                    messageThread.SetApartmentState(System.Threading.ApartmentState.STA);
                    messageThread.IsBackground = true;
                    messageThread.Start();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the client
                System.Diagnostics.Debug.WriteLine($"Error handling command: {ex.Message}");
            }
        }

        private void Disconnect()
        {
            try
            {
                stream?.Dispose();
                client?.Close();
                client?.Dispose();
            }
            catch { }
            finally
            {
                stream = null;
                client = null;
            }
        }

        public void Dispose()
        {
            Stop();
            monitor?.Dispose();
            cancellationTokenSource?.Dispose();
        }
    }
}
