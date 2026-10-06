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
        private string screenshotResolution = "720p";
        private int screenshotQuality = 50;
        private static readonly object _messageSync = new object();
        private static MessageDisplayForm _currentMessageForm = null;

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
                Disconnect();
                client = new TcpClient();
                // Increase buffer sizes for faster transfer
                client.ReceiveBufferSize = 256 * 1024; // 256 KB
                client.SendBufferSize = 256 * 1024; // 256 KB
                client.NoDelay = true; // Disable Nagle's algorithm for lower latency
                
                var connectTask = client.ConnectAsync(serverIP, serverPort);
                if (await Task.WhenAny(connectTask, Task.Delay(5000)) == connectTask)
                {
                    await connectTask;
                    stream = client.GetStream();
                }
                else
                {
                    Disconnect();
                }
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

                // Capture screenshot
                byte[] screenshot = monitor.CaptureScreenshot(screenshotQuality, screenshotResolution);

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

                // Wait for length-prefixed server response with 10s timeout to prevent hanging
                using (var readCts = new CancellationTokenSource(10000))
                {
                    byte[] lengthBuffer = new byte[4];
                    int lengthRead = 0;
                    while (lengthRead < 4)
                    {
                        int read = await stream.ReadAsync(lengthBuffer, lengthRead, 4 - lengthRead, readCts.Token);
                        if (read == 0) throw new System.IO.IOException("Server closed connection");
                        lengthRead += read;
                    }

                    int responseLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (responseLength > 0 && responseLength < 10 * 1024 * 1024)
                    {
                        byte[] responseBuffer = new byte[responseLength];
                        int totalRead = 0;
                        while (totalRead < responseLength)
                        {
                            int read = await stream.ReadAsync(responseBuffer, totalRead, responseLength - totalRead, readCts.Token);
                            if (read == 0) throw new System.IO.IOException("Server closed connection");
                            totalRead += read;
                        }

                        string response = Encoding.UTF8.GetString(responseBuffer, 0, responseLength);
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
                if (command.CommandType == "quality" || command.CommandType == "set_quality")
                {
                    if (!string.IsNullOrEmpty(command.MessageText))
                    {
                        string q = command.MessageText.ToLower();
                        if (q.Contains("480"))
                        {
                            screenshotResolution = "480p";
                            screenshotQuality = 40;
                        }
                        else if (q.Contains("1080"))
                        {
                            screenshotResolution = "1080p";
                            screenshotQuality = 60;
                        }
                        else
                        {
                            screenshotResolution = "720p";
                            screenshotQuality = 50;
                        }
                    }
                    return;
                }

                if (command.CommandType == "message" || command.CommandType == "freeze")
                {
                    lock (_messageSync)
                    {
                        if (_currentMessageForm != null && !_currentMessageForm.IsDisposed)
                        {
                            try
                            {
                                _currentMessageForm.Invoke(new Action(() => _currentMessageForm.Close()));
                            }
                            catch { }
                        }
                    }

                    // Run on a separate thread to avoid blocking the monitoring loop
                    System.Threading.Thread messageThread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            bool isFreeze = command.CommandType == "freeze";
                            using (var messageForm = new MessageDisplayForm(command.MessageText, command.Duration, isFreeze))
                            {
                                lock (_messageSync)
                                {
                                    _currentMessageForm = messageForm;
                                }
                                try
                                {
                                    messageForm.ShowDialog();
                                }
                                finally
                                {
                                    lock (_messageSync)
                                    {
                                        if (_currentMessageForm == messageForm)
                                        {
                                            _currentMessageForm = null;
                                        }
                                    }
                                }
                            }
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
