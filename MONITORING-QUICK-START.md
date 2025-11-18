# 🚀 PC Monitoring System - QUICK START GUIDE

## What This Does

Monitor all 35 lab PCs in **REAL-TIME**:

- See what students are doing right now
- Track active windows/applications
- Monitor CPU and memory usage
- View activity history

## ⚡ 5-Minute Setup

### Step 1: Deploy Client to All PCs (One Command!)

```powershell
cd d:\Projects\WinServer2019_Admin
.\Deploy-MonitoringClient.ps1
```

This will automatically:

- ✅ Copy monitoring client to all PCs
- ✅ Set up auto-start on each PC
- ✅ Configure firewall
- ✅ Start clients immediately

### Step 2: Open Monitoring Dashboard

1. Run `d:\Projects\WinServer2019_Admin\bin\Release\WinServer2019.exe`
2. Login with your domain credentials
3. Click the **📊 Live Monitoring** button (green button near output console)
4. Click **Start Monitoring Server**

### Step 3: Watch Real-Time Activity!

Within 5-10 seconds, you'll see all connected PCs with:

- Current active window
- Running applications
- System resources
- Click any PC for detailed view!

## 📊 What You'll See

### Main Dashboard

```
┌─────────────────────────────────────────────────────────┐
│ PC Name   │ User      │ Active Window     │ CPU% │ Mem │
├─────────────────────────────────────────────────────────┤
│ PC-1      │ student1  │ Google Chrome...  │ 15%  │ 2GB │
│ PC-2      │ student2  │ Visual Studio...  │ 45%  │ 4GB │
│ PC-3      │ student3  │ Facebook - Moz... │ 8%   │ 1GB │
└─────────────────────────────────────────────────────────┘
```

### Detail View (Click any PC)

- 👤 Current user
- 🌐 IP Address
- 🖥️ Active window/process
- 💻 CPU & Memory usage
- 🔄 Top 10 running processes
- 📝 Last 20 activity changes (with timestamps!)

## 🎯 Common Use Cases

### Monitor During Exam

1. Start monitoring before exam
2. Watch for suspicious applications (browsers, chat apps)
3. Click any student PC for details
4. Check activity history for context

### Check What Students Are Doing

- Real-time view of active windows
- See if they're on-task
- Identify who needs help (stuck on same window)

### Find Problem PCs

- Sort by CPU/Memory usage
- Identify performance issues
- See which apps are causing problems

## 🛠️ Troubleshooting

### "No clients connecting"

```powershell
# Check if clients are running
Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
    Get-Process PCMonitorClient
}

# If not, start deployment script again
.\Deploy-MonitoringClient.ps1
```

### "Can't see what they're doing"

- Green highlight = Active (updated < 2 seconds ago)
- White = Connected but idle
- Not in list = Offline or not installed

### "Client not starting after reboot"

The scheduled task should auto-start. Verify:

```powershell
Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
    Get-ScheduledTask -TaskName "PCMonitorClient"
}
```

## ⚙️ Configuration

### Change Update Speed

Default: Updates every 2 seconds

To change: Edit `PCMonitorClient\MonitoringClient.cs` line 59:

```csharp
await Task.Delay(2000, token); // 2000 = 2 seconds
```

### Stop Monitoring on Specific PCs

```powershell
$pc = "PC-5.csitlab.local"
Invoke-Command -ComputerName $pc -ScriptBlock {
    Stop-Process -Name PCMonitorClient -Force
    Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false
}
```

### Uninstall from All PCs

```powershell
$targets = 1..35 | ForEach-Object { "PC-$_.csitlab.local" }

Invoke-Command -ComputerName $targets -ScriptBlock {
    Stop-Process -Name PCMonitorClient -Force -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item "C:\ProgramData\PCMonitor" -Recurse -Force -ErrorAction SilentlyContinue
}
```

## 🔐 Privacy & Security

**IMPORTANT**: This system monitors user activity

- ✅ Only works on your LAN (192.168.2.x)
- ✅ No internet data sent
- ✅ Data stored only in memory (no logs saved to disk)
- ⚠️ Ensure students/staff are informed about monitoring
- ⚠️ Follow your organization's privacy policies

## 📈 Performance Impact

**Server (Your PC)**:

- CPU: 2-5%
- RAM: 100-150 MB
- Network: ~35 KB/sec (all 35 clients)

**Clients (Student PCs)**:

- CPU: < 1%
- RAM: ~20-30 MB
- Network: ~1 KB/sec
- **Students won't notice it's running!**

## 💡 Pro Tips

1. **Keep monitoring window open in background** - It updates automatically
2. **Sort by active window** - Quickly find students on specific apps
3. **Watch activity log** - See when clients connect/disconnect
4. **Use with existing features** - Works alongside shutdown, restart, web blocking
5. **Check before web blocking** - See if students are actually on blocked sites

## 🆘 Quick Commands Reference

```powershell
# Deploy to all PCs
.\Deploy-MonitoringClient.ps1

# Check client status on one PC
Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
    Get-Process PCMonitorClient | Select ProcessName, CPU, WS, StartTime
}

# Restart client on one PC
Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock {
    Stop-Process -Name PCMonitorClient -Force
    Start-Process "C:\ProgramData\PCMonitor\PCMonitorClient.exe" -ArgumentList "192.168.2.45 8888" -WindowStyle Hidden
}

# Test server connectivity from client
Test-NetConnection -ComputerName 192.168.2.45 -Port 8888
```

## ✅ Success Checklist

- [ ] Deployed client to all PCs
- [ ] Server shows "Running" status
- [ ] Can see at least one PC in client list
- [ ] Green highlights appear when selecting PCs
- [ ] Can click PC and see details
- [ ] Activity log shows connection events

## 🎓 That's It!

You now have **real-time visibility** into all 35 lab PCs. Use it responsibly! 👍

For detailed documentation, see: `MONITORING-SYSTEM-README.md`
