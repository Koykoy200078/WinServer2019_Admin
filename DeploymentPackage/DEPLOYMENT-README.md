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

- **Real-time screen capture** every 1-2 seconds
- **Quality selector:** 480p (Fast), 720p (Balanced), 1080p (High Quality)
- **Default:** 720p @ 50% JPEG quality for optimal balance
- **Optimized network:** 256 KB buffers, TCP_NODELAY enabled
- **High-quality scaling:** Sharp, clear images with proper interpolation
- **Real-time stats:** Shows resolution, transfer size, and FPS
- See exactly what users are doing!

## 🔧 Configuration

Default settings:
- Server Port: **8888**
- Update Interval: **2 seconds**
- Screenshot Quality: **50%** (720p balanced mode)
- Network Buffer: **256 KB** (optimized for speed)

## ⚡ Performance

Expected bandwidth per client:
- **480p:**  ~15-25 KB/frame  (Fast, low bandwidth)
- **720p:**  ~30-50 KB/frame  (Balanced) ⭐ Default
- **1080p:** ~60-100 KB/frame (High quality)

With 35 clients @ 720p: ~2-3 MB/sec total bandwidth

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
2. Check network bandwidth (720p uses ~30-50KB/sec per client)
3. Verify no antivirus blocking screen capture
4. Try lower quality (480p) if network is slow

**Pixelated or blurry screen?**
1. Client defaults to 720p @ 50% quality (balanced)
2. Quality selector in screen viewer (top bar)
3. Higher quality = more bandwidth needed
4. Ensure TCP_NODELAY is working (check firewall)

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
