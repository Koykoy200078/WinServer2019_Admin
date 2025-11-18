# PC Monitoring System - Real-Time Client Activity Tracking

## Overview

This system allows you to monitor all client PCs in real-time from your server application. The system consists of:

1. **Server Application** (WinServer2019.exe) - Your main management tool with added monitoring dashboard
2. **Client Application** (PCMonitorClient.exe) - Silent background client that runs on each PC

## Features

### Server Side (Monitoring Dashboard)

- **Real-time monitoring** of all connected PCs
- **Live activity tracking**: See what window/application users are currently using
- **System resource monitoring**: CPU usage, memory usage per PC
- **Recent activities log**: Track last 20 window changes per client
- **Client details view**: Detailed information about selected PC
- **Connection status**: See which PCs are online/offline
- **Activity log**: Timestamped events from all clients

### Client Side (Silent Agent)

- **Completely silent** - No user interface or taskbar icon
- **Auto-starts** with Windows (via Group Policy or Startup folder)
- **Lightweight** - Minimal CPU and memory footprint
- **Automatic reconnection** - Reconnects if server goes offline
- **Real-time data** - Sends updates every 2 seconds

## Monitored Data

Each client sends the following data to the server:

- PC Name
- Username
- Active Window Title
- Active Process Name
- CPU Usage (%)
- Memory Usage (MB)
- IP Address
- Top 10 Running Processes
- Recent 20 Activity Changes
- Last Update Timestamp

## Installation Instructions

### Server Setup (192.168.2.45 - Your Admin PC)

1. **Build the updated server**:

   ```powershell
   cd d:\Projects\WinServer2019_Admin
   nuget restore
   MSBuild.exe WinServer2019.csproj /p:Configuration=Release /t:Rebuild
   ```

2. **Run WinServer2019.exe** and click the "📊 Live Monitoring" button

3. **Start the monitoring server** by clicking "Start Monitoring Server"
   - Server listens on port 8888
   - Ensure firewall allows inbound connections on port 8888

### Client Setup (PC-1 to PC-35)

#### Option 1: Group Policy Deployment (Recommended)

1. **Build the client**:

   ```powershell
   cd d:\Projects\WinServer2019_Admin\PCMonitorClient
   nuget restore
   MSBuild.exe PCMonitorClient.csproj /p:Configuration=Release /t:Rebuild
   ```

2. **Copy client to network share**:

   ```powershell
   xcopy /Y /I "bin\Release\*.*" "\\192.168.2.45\Sharing\PCMonitor\"
   ```

3. **Create Group Policy for automatic startup**:

   - Open Group Policy Management Console
   - Create new GPO: "PC Monitor Client Startup"
   - Edit GPO → User Configuration → Policies → Windows Settings → Scripts (Logon)
   - Add Script:
     ```
     Script Name: \\192.168.2.45\Sharing\PCMonitor\PCMonitorClient.exe
     Script Parameters: 192.168.2.45 8888
     ```
   - Link GPO to OU containing PC-1 to PC-35

4. **Force Group Policy Update** on all PCs:
   ```powershell
   $targets = 1..35 | ForEach-Object { "PC-$_.csitlab.local" }
   Invoke-Command -ComputerName $targets -ScriptBlock {
       gpupdate /force
   }
   ```

#### Option 2: PowerShell Mass Deployment

1. **Deploy to all PCs** using your existing script:

   ```powershell
   # Add to Main.ps1 or run directly
   $targets = 1..35 | ForEach-Object { "PC-$_.csitlab.local" }

   foreach ($pc in $targets) {
       try {
           # Copy client
           Copy-Item -Path "\\192.168.2.45\Sharing\PCMonitor\*" `
                     -Destination "\\$pc\C$\ProgramData\PCMonitor\" `
                     -Recurse -Force

           # Create startup task
           Invoke-Command -ComputerName $pc -ScriptBlock {
               $action = New-ScheduledTaskAction -Execute "C:\ProgramData\PCMonitor\PCMonitorClient.exe" `
                         -Argument "192.168.2.45 8888"
               $trigger = New-ScheduledTaskTrigger -AtLogOn
               $principal = New-ScheduledTaskPrincipal -GroupId "BUILTIN\Users"
               $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries

               Register-ScheduledTask -TaskName "PCMonitorClient" `
                                     -Action $action `
                                     -Trigger $trigger `
                                     -Principal $principal `
                                     -Settings $settings `
                                     -Force
           }

           Write-Host "✓ Deployed to $pc" -ForegroundColor Green
       }
       catch {
           Write-Host "✗ Failed on $pc : $_" -ForegroundColor Red
       }
   }
   ```

#### Option 3: Manual Installation (Single PC Test)

1. Copy `PCMonitorClient.exe` to target PC
2. Edit `App.config` to set server IP
3. Add to Startup folder:
   ```
   C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp\
   ```

## Firewall Configuration

### Server (192.168.2.45)

Allow inbound connections on port 8888:

```powershell
New-NetFirewallRule -DisplayName "PC Monitor Server" `
                    -Direction Inbound `
                    -LocalPort 8888 `
                    -Protocol TCP `
                    -Action Allow
```

### Clients (PC-1 to PC-35)

Allow outbound connections (usually allowed by default):

```powershell
New-NetFirewallRule -DisplayName "PC Monitor Client" `
                    -Direction Outbound `
                    -RemoteAddress 192.168.2.45 `
                    -RemotePort 8888 `
                    -Protocol TCP `
                    -Action Allow
```

## Usage

### Starting Monitoring

1. Launch `WinServer2019.exe` on server (192.168.2.45)
2. Click "📊 Live Monitoring" button (green button near output console)
3. Click "Start Monitoring Server"
4. Wait for clients to connect (they connect automatically when running)

### Monitoring Dashboard

- **Client List**: Shows all connected PCs with their current activity
- **Green highlight**: Recently updated (< 2 seconds ago)
- **Select a PC**: Click to view detailed information in right panel
- **Activity Log**: Bottom panel shows all connection/disconnection events

### Client Information Displayed

When you select a PC from the list:

- User logged in
- IP address
- Current active window/application
- CPU and memory usage
- Top 10 running processes
- Last 20 window/application changes

## Troubleshooting

### Client Not Connecting

1. **Check if client is running**:

   ```powershell
   Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
       Get-Process PCMonitorClient -ErrorAction SilentlyContinue
   }
   ```

2. **Check firewall** on server:

   ```powershell
   Get-NetFirewallRule -DisplayName "PC Monitor Server"
   ```

3. **Test connectivity** from client:
   ```powershell
   Test-NetConnection -ComputerName 192.168.2.45 -Port 8888
   ```

### Stop/Remove Client

To stop monitoring on a PC:

```powershell
Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
    Stop-Process -Name PCMonitorClient -Force
    Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false
}
```

### Check Client Logs

The client runs silently but you can check if it's active:

```powershell
Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
    Get-Process | Where-Object { $_.ProcessName -eq 'PCMonitorClient' } |
    Select ProcessName, CPU, WS, StartTime
}
```

## Security Considerations

1. **Network Security**: The monitoring traffic is unencrypted. Ensure it runs only on your trusted LAN (192.168.2.x)

2. **Privacy**: This system captures user activity. Ensure compliance with:

   - School/organization policies
   - Student/staff privacy notices
   - Local regulations

3. **Access Control**: Only administrators should have access to the monitoring dashboard

4. **Data Retention**: Activity data is only stored in memory. No persistent logs are saved.

## Performance Impact

- **Client**: < 1% CPU, ~20-30 MB RAM
- **Server**: ~2-5% CPU with 35 clients, ~100-150 MB RAM
- **Network**: ~1 KB/second per client

## Advanced Configuration

### Change Update Frequency

Edit `MonitoringClient.cs` line 59:

```csharp
await Task.Delay(2000, token); // Change 2000 to desired milliseconds
```

### Change Server Port

1. Server: Edit `MonitoringServer.cs` line 29:

   ```csharp
   private readonly int port = 8888; // Change port number
   ```

2. Client: Pass new port as second argument:
   ```
   PCMonitorClient.exe 192.168.2.45 9999
   ```

### Increase Activity History

Edit `ActivityMonitor.cs` line 14:

```csharp
private const int MaxActivities = 20; // Increase for more history
```

## Integration with Existing Features

The monitoring system works alongside your existing features:

- PC Management (shutdown, restart)
- Web Blocking
- Utilities

You can see live activity while performing administrative tasks.

## Uninstalling

### Remove from all PCs:

```powershell
$targets = 1..35 | ForEach-Object { "PC-$_.csitlab.local" }

Invoke-Command -ComputerName $targets -ScriptBlock {
    Stop-Process -Name PCMonitorClient -Force -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item "C:\ProgramData\PCMonitor" -Recurse -Force -ErrorAction SilentlyContinue
}
```

## Support

For issues or questions:

1. Check the Activity Log in monitoring dashboard
2. Verify server is running and listening on port 8888
3. Ensure clients have network connectivity to server
4. Check firewall rules on both server and clients
