# PC Monitoring System - Deployment Package

## 🎯 Quick Start

### For Domain Networks:
```powershell
# Navigate to Scripts folder
cd Scripts

# Deploy to PCs 1-35 in domain
.\Deploy-MonitoringClient.ps1 -ServerIP "192.168.2.45" -StartPC 1 -EndPC 35
```

### For Standalone (Non-Domain) Networks:
```powershell
# Navigate to Scripts folder
cd Scripts

# Create list of target PCs
$targetPCs = @(
    "192.168.2.101",
    "192.168.2.102",
    "192.168.2.103"
)

# Deploy to standalone PCs
.\Deploy-Standalone.ps1 -ServerIP "192.168.2.45" -TargetPCs $targetPCs -Username "Administrator"
```

## 📁 Package Contents

- **Server/** - Server application (WinServer2019.exe)
- **Client/** - Silent monitoring client (PCMonitorClient.exe)
- **Scripts/** - Deployment automation scripts

## 🚀 Usage

1. **Start the Server:**
   - Run Server\WinServer2019.exe
   - Click **📊 Live Monitoring** button
   - Click **Start Monitoring Server**

2. **Deploy Clients:**
   - Use deployment script (see Quick Start above)
   - Clients will auto-start on PC boot

3. **View Screens:**
   - Connected PCs appear in dashboard
   - **Double-click any PC** to view their screen in real-time!
   - Or click the "👁 View" button

## 🎥 Screen Viewing Features

- **Real-time screen capture** every 1 second
- **Compressed images** (30% quality) to minimize bandwidth
- **Resized display** (50% scale) for performance
- See exactly what users are doing!

## 🔧 Configuration

Default settings:
- Server Port: **8888**
- Update Interval: **2 seconds**
- Screenshot Quality: **30%** (adjustable in code)

## 🔥 Firewall Rules

Automatically configured by deployment script:
- **Server:** Inbound TCP port 8888
- **Clients:** Outbound TCP to server

## ⚠️ Requirements

- **Server:** Windows Server 2016+ or Windows 10+
- **Clients:** Windows 10+ with .NET Framework 4.8.1
- **Network:** TCP port 8888 open between server and clients
- **Permissions:** Administrator rights on all machines

## 📊 What's Monitored

- Active window title and application
- CPU and memory usage
- Running processes
- Activity history (last 20 actions)
- **Real-time screen view** 🎥

## 🛠️ Troubleshooting

**Clients not connecting?**
1. Check firewall rules on both server and clients
2. Verify server IP is correct in client config
3. Test connectivity: Test-NetConnection -ComputerName <ServerIP> -Port 8888

**Screen not showing?**
1. Ensure client has latest version
2. Check network bandwidth (screenshots are compressed but still use ~50-100KB/sec)
3. Verify no antivirus blocking screen capture

**Client not starting?**
1. Check scheduled task: Get-ScheduledTask -TaskName "PCMonitorClient"
2. Run manually to see errors: C:\ProgramData\PCMonitor\PCMonitorClient.exe
3. Check event viewer for application errors

## 📝 Uninstall

To remove from a client:
```powershell
Invoke-Command -ComputerName PC-1 -ScriptBlock {
    # Stop process
    Get-Process -Name "PCMonitorClient" -ErrorAction SilentlyContinue | Stop-Process -Force
    
    # Remove scheduled task
    Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false
    
    # Remove files
    Remove-Item "C:\ProgramData\PCMonitor" -Recurse -Force
}
```

## 🔒 Privacy Notice

This system captures and transmits:
- Application usage data
- System resource metrics
- **Screen images in real-time**

Ensure compliance with your organization's policies and inform users as required.

---
Built with ❤️ for Windows Server 2019 Lab Management
