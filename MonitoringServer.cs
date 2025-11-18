using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WinServer2019
{
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

    public class MonitoringServer : IDisposable
    {
        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private ConcurrentDictionary<string, ClientActivity> connectedClients;
        private readonly int port = 8888;
        private bool isRunning = false;

        public event Action<string> OnLogMessage;
        public event Action<ClientActivity> OnClientUpdate;
        public event Action<string> OnClientDisconnected;

        public MonitoringServer()
        {
            connectedClients = new ConcurrentDictionary<string, ClientActivity>();
        }

        public void Start()
        {
            if (isRunning) return;

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                isRunning = true;

                OnLogMessage?.Invoke($"Monitoring server started on port {port}");

                // Start accepting clients
                Task.Run(() => AcceptClientsAsync(cancellationTokenSource.Token));

                // Start cleanup task for inactive clients
                Task.Run(() => CleanupInactiveClientsAsync(cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error starting server: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            if (!isRunning) return;

            try
            {
                isRunning = false;
                cancellationTokenSource?.Cancel();
                listener?.Stop();
                connectedClients.Clear();
                OnLogMessage?.Invoke("Monitoring server stopped");
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"Error stopping server: {ex.Message}");
            }
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClientAsync(client, token), token);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!token.IsCancellationRequested)
                    {
                        OnLogMessage?.Invoke($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string clientId = null;
            NetworkStream stream = null;

            try
            {
                stream = client.GetStream();

                while (!token.IsCancellationRequested && client.Connected)
                {
                    // Read message length first (4 bytes)
                    byte[] lengthBuffer = new byte[4];
                    int lengthBytesRead = 0;
                    while (lengthBytesRead < 4)
                    {
                        int read = await stream.ReadAsync(lengthBuffer, lengthBytesRead, 4 - lengthBytesRead, token);
                        if (read == 0) return; // Connection closed
                        lengthBytesRead += read;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    
                    // Validate message length (max 10 MB)
                    if (messageLength <= 0 || messageLength > 10 * 1024 * 1024)
                    {
                        OnLogMessage?.Invoke($"Invalid message length: {messageLength}");
                        break;
                    }

                    // Read the full message
                    byte[] buffer = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        int bytesRead = await stream.ReadAsync(buffer, totalBytesRead, messageLength - totalBytesRead, token);
                        if (bytesRead == 0) return; // Connection closed
                        totalBytesRead += bytesRead;
                    }

                    string jsonData = Encoding.UTF8.GetString(buffer, 0, messageLength);
                    var activity = JsonConvert.DeserializeObject<ClientActivity>(jsonData);

                    if (activity != null)
                    {
                        clientId = activity.PCName;
                        activity.LastUpdate = DateTime.Now;
                        activity.IsActive = true;

                        connectedClients.AddOrUpdate(clientId, activity, (key, old) => activity);
                        OnClientUpdate?.Invoke(activity);
                    }

                    // Send acknowledgment
                    byte[] ack = Encoding.UTF8.GetBytes("ACK");
                    await stream.WriteAsync(ack, 0, ack.Length, token);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    OnLogMessage?.Invoke($"Client error ({clientId}): {ex.Message}");
                }
            }
            finally
            {
                if (clientId != null)
                {
                    connectedClients.TryRemove(clientId, out _);
                    OnClientDisconnected?.Invoke(clientId);
                }
                stream?.Dispose();
                client?.Dispose();
            }
        }

        private async Task CleanupInactiveClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isRunning)
            {
                try
                {
                    await Task.Delay(5000, token);

                    var now = DateTime.Now;
                    var inactiveClients = connectedClients
                        .Where(c => (now - c.Value.LastUpdate).TotalSeconds > 30)
                        .Select(c => c.Key)
                        .ToList();

                    foreach (var clientId in inactiveClients)
                    {
                        if (connectedClients.TryRemove(clientId, out _))
                        {
                            OnClientDisconnected?.Invoke(clientId);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnLogMessage?.Invoke($"Cleanup error: {ex.Message}");
                }
            }
        }

        public List<ClientActivity> GetConnectedClients()
        {
            return connectedClients.Values.ToList();
        }

        public ConcurrentDictionary<string, ClientActivity> GetConnectedClientsDictionary()
        {
            return connectedClients;
        }

        public ClientActivity GetClientActivity(string pcName)
        {
            connectedClients.TryGetValue(pcName, out var activity);
            return activity;
        }

        public void Dispose()
        {
            Stop();
            cancellationTokenSource?.Dispose();
        }
    }
}
