# PC Monitoring System - Auto Deployment Script
# This script deploys the client monitoring software to all lab PCs

param(
    [string]$ServerIP = "192.168.2.45",
    [int]$ServerPort = 8888,
    [int]$StartPC = 1,
    [int]$EndPC = 35,
    [string]$Domain = "csitlab.local",
    [string]$PCName = "",
    [PSCredential]$Credential = $null
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "PC Monitoring System - Deployment" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Detect if running from deployment package or source
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Multi-path search for client binaries
$candidatePaths = @(
    (Join-Path $scriptDir "Client"),
    (Join-Path $scriptDir "..\Client"),
    (Join-Path $scriptDir "..\..\Client"),
    (Join-Path $scriptDir "PCMonitorClient\bin\Release"),
    (Join-Path $scriptDir "..\PCMonitorClient\bin\Release"),
    (Join-Path $scriptDir "..\..\PCMonitorClient\bin\Release"),
    "F:\Sharing\Other\DeploymentPackage\Client",
    "F:\Sharing\PCMonitor",
    "\\$ServerIP\Sharing\PCMonitor",
    "\\$ServerIP\Sharing\Other\DeploymentPackage\Client",
    "C:\ProgramData\PCMonitor"
)

$clientPath = $null
foreach ($cand in $candidatePaths) {
    if (Test-Path (Join-Path $cand "PCMonitorClient.exe")) {
        $clientPath = (Resolve-Path $cand -ErrorAction SilentlyContinue).Path
        break
    }
}
if (-not $clientPath) {
    foreach ($cand in $candidatePaths) {
        if (Test-Path $cand) {
            $clientPath = (Resolve-Path $cand -ErrorAction SilentlyContinue).Path
            break
        }
    }
}

$networkShare = "\\$ServerIP\Sharing\PCMonitor"
if (-not (Test-Path $networkShare -ErrorAction SilentlyContinue) -and (Test-Path "F:\Sharing\PCMonitor")) {
    $networkShare = "F:\Sharing\PCMonitor"
}
$targetFolder = "C:\Program Files\PCMonitor"

# Verify client files exist
if (-not $clientPath -or -not (Test-Path $clientPath)) {
    Write-Host "ERROR: Client files not found! Checked candidate locations:" -ForegroundColor Red
    $candidatePaths | ForEach-Object { Write-Host "  - $_" -ForegroundColor DarkGray }
    Write-Host "Please build the project first or verify deployment files." -ForegroundColor Yellow
    exit 1
}

Write-Host "Using client files from: $clientPath" -ForegroundColor Gray
Write-Host "Target installation folder: $targetFolder (Protected System Location)" -ForegroundColor Gray
Write-Host ""

# Step 1: Copy client to network share
Write-Host "[1/5] Copying client to network share..." -ForegroundColor Yellow
try {
    if (-not (Test-Path $networkShare -ErrorAction SilentlyContinue)) {
        New-Item -Path $networkShare -ItemType Directory -Force | Out-Null
    }
    $resolvedClient = (Resolve-Path $clientPath -ErrorAction SilentlyContinue).Path
    $resolvedShare = (Resolve-Path $networkShare -ErrorAction SilentlyContinue).Path
    if ($resolvedClient -and $resolvedShare -and ($resolvedClient -eq $resolvedShare)) {
        Write-Host "  ✓ Client files already up to date on $networkShare" -ForegroundColor Green
    } else {
        Copy-Item -Path "$clientPath\*" -Destination $networkShare -Recurse -Force
        Write-Host "  ✓ Client copied to $networkShare" -ForegroundColor Green
    }
}
catch {
    Write-Host "  ⚠ Warning: Could not update network share: $_" -ForegroundColor Yellow
    Write-Host "    Will deploy directly from $clientPath" -ForegroundColor Gray
}

# Step 2: Configure firewall on server
Write-Host "[2/5] Configuring firewall on server..." -ForegroundColor Yellow
try {
    $rule = Get-NetFirewallRule -DisplayName "PC Monitor Server" -ErrorAction SilentlyContinue
    if (-not $rule) {
        New-NetFirewallRule -DisplayName "PC Monitor Server" `
                            -Direction Inbound `
                            -LocalPort $ServerPort `
                            -Protocol TCP `
                            -Action Allow | Out-Null
        Write-Host "  ✓ Firewall rule created" -ForegroundColor Green
    }
    else {
        Write-Host "  ✓ Firewall rule already exists" -ForegroundColor Green
    }
}
catch {
    Write-Host "  ⚠ Warning: Could not configure firewall: $_" -ForegroundColor Yellow
}

# Step 3: Deploy to target PCs
if ($PCName) {
    if ($PCName -match '^\d+$') {
        $targets = @("PC-$PCName.$Domain")
    } elseif ($PCName -notmatch '\.' -and $PCName -match '^(?i)PC-\d+') {
        $targets = @("$PCName.$Domain")
    } else {
        $targets = @($PCName)
    }
    Write-Host "[3/5] Deploying to target: $($targets -join ', ')..." -ForegroundColor Yellow
} else {
    $targets = $StartPC..$EndPC | ForEach-Object { "PC-$_.$Domain" }
    Write-Host "[3/5] Deploying to PCs $StartPC to $EndPC..." -ForegroundColor Yellow
}

$successCount = 0
$failCount = 0

foreach ($pc in $targets) {
    Write-Host "  Deploying to $pc..." -ForegroundColor Gray -NoNewline
    
    try {
        # Test connectivity by checking admin share access
        $remotePath = "\\$pc\C$\Program Files\PCMonitor"
        $legacyPath = "\\$pc\C$\ProgramData\PCMonitor"
        $testPath = "\\$pc\C$"
        
        if (-not (Test-Path $testPath -ErrorAction SilentlyContinue)) {
            Write-Host " OFFLINE (Cannot reach $testPath)" -ForegroundColor DarkGray
            $failCount++
            continue
        }

        # Build Invoke-Command parameters
        $invokeBaseParams = @{
            ComputerName = $pc
        }
        if ($Credential) {
            $invokeBaseParams["Credential"] = $Credential
        }

        # Stop running client process via remote session
        try {
            $stopParams = $invokeBaseParams.Clone()
            $stopParams["ScriptBlock"] = {
                Get-Process -Name "PCMonitorClient" -ErrorAction SilentlyContinue | Stop-Process -Force
                Start-Sleep -Milliseconds 500
            }
            $stopParams["ErrorAction"] = "SilentlyContinue"
            Invoke-Command @stopParams
        } catch {
            # Ignore if process not running
        }

        # Clean legacy ProgramData folder if present
        if (Test-Path $legacyPath) {
            try {
                Remove-Item -Path "$legacyPath\*" -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item -Path $legacyPath -Recurse -Force -ErrorAction SilentlyContinue
            } catch { }
        }

        # Create system directory if doesn't exist
        if (-not (Test-Path $remotePath)) {
            try {
                New-Item -Path $remotePath -ItemType Directory -Force -ErrorAction Stop | Out-Null
            } catch {
                Write-Host " FAILED: Cannot create directory $remotePath" -ForegroundColor Red
                $failCount++
                continue
            }
        }
        
        # Copy files with retry logic
        $retryCount = 0
        $maxRetries = 3
        $copySuccess = $false
        $sourceToUse = if (Test-Path "$networkShare\PCMonitorClient.exe" -ErrorAction SilentlyContinue) { "$networkShare\*" } else { "$clientPath\*" }
        
        while (-not $copySuccess -and $retryCount -lt $maxRetries) {
            try {
                Copy-Item -Path $sourceToUse -Destination $remotePath -Recurse -Force -ErrorAction Stop
                $copySuccess = $true
            } catch {
                $retryCount++
                if ($retryCount -lt $maxRetries) {
                    Start-Sleep -Milliseconds 500
                } else {
                    Write-Host " FAILED: Cannot copy files after $maxRetries attempts: $_" -ForegroundColor Red
                    $failCount++
                    continue
                }
            }
        }

        # Configure system permissions, logon task, and SYSTEM watchdog task
        $taskParams = $invokeBaseParams.Clone()
        $taskParams["ScriptBlock"] = {
            param($targetFolder, $serverIP, $serverPort)
            
            # 1. Lock down NTFS permissions (SYSTEM/Admins Full Control, Users Read & Execute)
            try {
                & icacls.exe "$targetFolder" /inheritance:d /grant:r "NT AUTHORITY\SYSTEM:(OI)(CI)F" "BUILTIN\Administrators:(OI)(CI)F" "BUILTIN\Users:(OI)(CI)RX" *>$null
            } catch { }

            # 2. Configure User Session Task (Runs at Logon with full GUI / screen capture access)
            Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false -ErrorAction SilentlyContinue
            
            $action = New-ScheduledTaskAction -Execute "$targetFolder\PCMonitorClient.exe" `
                                             -Argument "$serverIP $serverPort"
            $trigger = New-ScheduledTaskTrigger -AtLogOn
            $principal = New-ScheduledTaskPrincipal -GroupId "BUILTIN\Users" -RunLevel Highest
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries `
                                                     -DontStopIfGoingOnBatteries `
                                                     -StartWhenAvailable `
                                                     -RunOnlyIfNetworkAvailable
            
            Register-ScheduledTask -TaskName "PCMonitorClient" `
                                  -Action $action `
                                  -Trigger $trigger `
                                  -Principal $principal `
                                  -Settings $settings `
                                  -Force | Out-Null

            # 3. Configure SYSTEM Watchdog Task (Checks process every 2 mins and restarts if killed)
            $watchdogScript = @'
$proc = Get-Process -Name 'PCMonitorClient' -ErrorAction SilentlyContinue
if (-not $proc) {
    $explorer = Get-Process -Name 'explorer' -ErrorAction SilentlyContinue
    if ($explorer) {
        Start-ScheduledTask -TaskName 'PCMonitorClient' -ErrorAction SilentlyContinue
    }
}
'@
            $watchdogPath = "$targetFolder\Watchdog.ps1"
            Set-Content -Path $watchdogPath -Value $watchdogScript -Encoding UTF8 -Force

            Unregister-ScheduledTask -TaskName "PCMonitorWatchdog" -Confirm:$false -ErrorAction SilentlyContinue
            
            $wdAction = New-ScheduledTaskAction -Execute "powershell.exe" `
                                               -Argument "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$watchdogPath`""
            $wdTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes 2)
            $wdPrincipal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -LogonType ServiceAccount -RunLevel Highest
            $wdSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries `
                                                       -DontStopIfGoingOnBatteries `
                                                       -StartWhenAvailable
            
            Register-ScheduledTask -TaskName "PCMonitorWatchdog" `
                                  -Action $wdAction `
                                  -Trigger $wdTrigger `
                                  -Principal $wdPrincipal `
                                  -Settings $wdSettings `
                                  -Force | Out-Null
            
            # 4. Start immediately
            Start-ScheduledTask -TaskName "PCMonitorClient" -ErrorAction SilentlyContinue
            Start-Process -FilePath "$targetFolder\PCMonitorClient.exe" `
                         -ArgumentList "$serverIP $serverPort" `
                         -WindowStyle Hidden -ErrorAction SilentlyContinue
            
        }
        $taskParams["ArgumentList"] = @($targetFolder, $ServerIP, $ServerPort)
        $taskParams["ErrorAction"] = "Stop"
        Invoke-Command @taskParams

        Write-Host " SUCCESS" -ForegroundColor Green
        $successCount++
    }
    catch {
        Write-Host " FAILED: $_" -ForegroundColor Red
        $failCount++
    }
}

# Step 4: Summary
Write-Host ""
Write-Host "[4/5] Deployment Summary:" -ForegroundColor Yellow
Write-Host "  Total PCs targeted: $($targets.Count)" -ForegroundColor White
Write-Host "  Successful: $successCount" -ForegroundColor Green
Write-Host "  Failed: $failCount" -ForegroundColor Red

# Step 5: Instructions
Write-Host ""
Write-Host "[5/5] Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Run WinServer2019.exe on this PC" -ForegroundColor Cyan
Write-Host "  2. Click the '📊 Live Monitoring' button (green button)" -ForegroundColor Cyan
Write-Host "  3. Click 'Start Monitoring Server'" -ForegroundColor Cyan
Write-Host "  4. Wait for clients to connect (they should appear within 5-10 seconds)" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Server will listen on: ${ServerIP}:${ServerPort}" -ForegroundColor White
Write-Host ""
Write-Host "✓ Deployment Complete!" -ForegroundColor Green
Write-Host ""

# Test instruction
Write-Host "To verify client is running on a PC:" -ForegroundColor Yellow
Write-Host '  Invoke-Command -ComputerName PC-1.csitlab.local -ScriptBlock { Get-Process PCMonitorClient }' -ForegroundColor Gray
Write-Host ""
