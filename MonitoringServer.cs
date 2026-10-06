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
    public class ServerCommand
    {
        public string CommandType { get; set; } // "message", "freeze"
        public string MessageText { get; set; }
        public int Duration { get; set; } // Duration in seconds
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
        public ServerCommand PendingCommand { get; set; }
        public string ConnectionId { get; set; }
    }

    public class MonitoringServer : IDisposable
    {
        private TcpListener listener;
        private CancellationTokenSource cancellationTokenSource;
        private readonly ConcurrentDictionary<string, ClientActivity> clientActivities;
        private readonly ConcurrentDictionary<string, ConcurrentQueue<ServerCommand>> commandQueues;
        private readonly int port = 8888;
        private bool isRunning = false;

        public event Action<string> OnLogMessage;
        public event Action<ClientActivity> OnClientUpdate;
        public event Action<string> OnClientDisconnected;

        public MonitoringServer()
        {
            clientActivities = new ConcurrentDictionary<string, ClientActivity>(StringComparer.OrdinalIgnoreCase);
            commandQueues = new ConcurrentDictionary<string, ConcurrentQueue<ServerCommand>>(StringComparer.OrdinalIgnoreCase);
        }

        private void SafeLog(string msg)
        {
            try
            {
                OnLogMessage?.Invoke(msg);
            }
            catch { }
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

                SafeLog($"Monitoring server started on port {port}");

                // Start accepting clients
                Task.Run(() => AcceptClientsAsync(cancellationTokenSource.Token));

                // Start cleanup task for inactive clients
                Task.Run(() => CleanupInactiveClientsAsync(cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                SafeLog($"Error starting server: {ex.Message}");
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
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                listener?.Stop();
                listener = null;
                clientActivities.Clear();
                commandQueues.Clear();
                SafeLog("Monitoring server stopped");
            }
            catch (Exception ex)
            {
                SafeLog($"Error stopping server: {ex.Message}");
            }
        }

        private async Task AcceptClientsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested && isRunning)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    
                    // Optimize TCP settings for better performance
                    client.ReceiveBufferSize = 256 * 1024; // 256 KB
                    client.SendBufferSize = 256 * 1024; // 256 KB
                    client.NoDelay = true; // Disable Nagle's algorithm
                    
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
                        SafeLog($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }

        private async Task<int> ReadWithTimeoutAsync(NetworkStream stream, byte[] buffer, int offset, int count, int timeoutMs, CancellationToken token)
        {
            using (var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                timeoutCts.CancelAfter(timeoutMs);
                try
                {
                    return await stream.ReadAsync(buffer, offset, count, timeoutCts.Token);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    throw new TimeoutException($"Client read timed out after {timeoutMs / 1000} seconds of inactivity.");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
            string clientId = null;
            string connectionId = Guid.NewGuid().ToString();
            NetworkStream stream = null;

            try
            {
                stream = client.GetStream();

                while (!token.IsCancellationRequested && client.Connected)
                {
                    // Read message length first (4 bytes) with 30s timeout to prevent hanging async tasks
                    byte[] lengthBuffer = new byte[4];
                    int lengthBytesRead = 0;
                    while (lengthBytesRead < 4)
                    {
                        int read = await ReadWithTimeoutAsync(stream, lengthBuffer, lengthBytesRead, 4 - lengthBytesRead, 30000, token);
                        if (read == 0) return; // Connection closed
                        lengthBytesRead += read;
                    }

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    
                    // Validate message length (max 10 MB)
                    if (messageLength <= 0 || messageLength > 10 * 1024 * 1024)
                    {
                        SafeLog($"Invalid message length: {messageLength}");
                        break;
                    }

                    // Read the full message with 30s timeout
                    byte[] buffer = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        int bytesRead = await ReadWithTimeoutAsync(stream, buffer, totalBytesRead, messageLength - totalBytesRead, 30000, token);
                        if (bytesRead == 0) return; // Connection closed
                        totalBytesRead += bytesRead;
                    }

                    string jsonData = Encoding.UTF8.GetString(buffer, 0, messageLength);
                    var activity = JsonConvert.DeserializeObject<ClientActivity>(jsonData);

                    if (activity != null)
                    {
                        clientId = activity.PCName;
                        activity.ConnectionId = connectionId;
                        activity.LastUpdate = DateTime.Now;
                        activity.IsActive = true;

                        // Use verified physical socket endpoint for IP to eliminate virtual adapter / wrong NIC reports
                        if (client.Client.RemoteEndPoint is IPEndPoint ep)
                        {
                            activity.IPAddress = ep.Address.ToString();
                        }

                        clientActivities.AddOrUpdate(clientId, activity, (key, old) => activity);
                        try
                        {
                            OnClientUpdate?.Invoke(activity);
                        }
                        catch { }
                    }

                    // Check for pending commands and send them, otherwise send ACK
                    ServerCommand pendingCommand = null;
                    if (clientId != null)
                    {
                        if (commandQueues.TryGetValue(clientId, out var queue) && queue.TryDequeue(out pendingCommand))
                        {
                            // Dequeued
                        }
                        else
                        {
                            string shortId = clientId.Split('.')[0];
                            foreach (var kvp in commandQueues)
                            {
                                if (string.Equals(kvp.Key.Split('.')[0], shortId, StringComparison.OrdinalIgnoreCase) && kvp.Value.TryDequeue(out pendingCommand))
                                {
                                    break;
                                }
                            }
                        }
                    }

                    string response;
                    if (pendingCommand != null)
                    {
                        response = JsonConvert.SerializeObject(pendingCommand);
                    }
                    else
                    {
                        response = "ACK";
                    }

                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    byte[] combinedResponse = new byte[4 + responseData.Length];
                    Buffer.BlockCopy(BitConverter.GetBytes(responseData.Length), 0, combinedResponse, 0, 4);
                    Buffer.BlockCopy(responseData, 0, combinedResponse, 4, responseData.Length);
                    await stream.WriteAsync(combinedResponse, 0, combinedResponse.Length, token);
                    await stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    SafeLog($"Client error ({clientId ?? "unknown"}): {ex.Message}");
                }
            }
            finally
            {
                if (clientId != null)
                {
                    // Only remove if this connection is still the active one for this client
                    if (clientActivities.TryGetValue(clientId, out var current) && current.ConnectionId == connectionId)
                    {
                        if (clientActivities.TryRemove(clientId, out _))
                        {
                            try
                            {
                                OnClientDisconnected?.Invoke(clientId);
                            }
                            catch { }
                        }
                    }
                }
                try { stream?.Dispose(); } catch { }
                try { client?.Dispose(); } catch { }
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
                    var inactiveClients = clientActivities
                        .Where(c => (now - c.Value.LastUpdate).TotalSeconds > 30)
                        .Select(c => new { Key = c.Key, ConnectionId = c.Value.ConnectionId })
                        .ToList();

                    foreach (var inactive in inactiveClients)
                    {
                        if (clientActivities.TryGetValue(inactive.Key, out var current) && current.ConnectionId == inactive.ConnectionId)
                        {
                            if (clientActivities.TryRemove(inactive.Key, out _))
                            {
                                try
                                {
                                    OnClientDisconnected?.Invoke(inactive.Key);
                                }
                                catch { }
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    SafeLog($"Cleanup error: {ex.Message}");
                }
            }
        }

        public List<ClientActivity> GetConnectedClients()
        {
            return clientActivities.Values.ToList();
        }

        public ConcurrentDictionary<string, ClientActivity> GetConnectedClientsDictionary()
        {
            return clientActivities;
        }

        public void SendCommand(string pcName, ServerCommand command)
        {
            if (string.IsNullOrWhiteSpace(pcName)) return;

            string targetKey = pcName;
            string shortName = pcName.Split('.')[0];
            foreach (var key in clientActivities.Keys)
            {
                if (string.Equals(key.Split('.')[0], shortName, StringComparison.OrdinalIgnoreCase))
                {
                    targetKey = key;
                    break;
                }
            }

            var queue = commandQueues.GetOrAdd(targetKey, _ => new ConcurrentQueue<ServerCommand>());
            queue.Enqueue(command);
            SafeLog($"Command queued for {targetKey}: {command.CommandType}");
        }

        public ClientActivity GetClientActivity(string pcName)
        {
            if (string.IsNullOrWhiteSpace(pcName)) return null;
            if (clientActivities.TryGetValue(pcName, out var activity)) return activity;

            string shortName = pcName.Split('.')[0];
            foreach (var kvp in clientActivities)
            {
                if (string.Equals(kvp.Key.Split('.')[0], shortName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Value.IPAddress, pcName, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        public void Dispose()
        {
            Stop();
            commandQueues.Clear();
        }
    }
}
