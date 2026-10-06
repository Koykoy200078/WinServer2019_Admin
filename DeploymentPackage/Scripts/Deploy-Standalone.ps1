<#
.SYNOPSIS
    Deploy PC Monitoring Client to standalone (non-domain) computers
.DESCRIPTION
    This script deploys the monitoring client to computers that are NOT joined to a domain.
    You need to manually specify the IP addresses or computer names of target PCs.
.PARAMETER ServerIP
    IP address of the monitoring server (this computer)
.PARAMETER ServerPort
    Port number for monitoring server (default: 8888)
.PARAMETER TargetPCs
    Array of computer names or IP addresses to deploy to
.PARAMETER Username
    Username for remote access (must have admin rights on target PCs)
.PARAMETER Password
    Password for the specified username
.EXAMPLE
    .\Deploy-Standalone.ps1 -ServerIP "192.168.2.45" -TargetPCs @("192.168.2.101", "192.168.2.102") -Username "Administrator"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$ServerIP,
    
    [int]$ServerPort = 8888,
    
    [Parameter(Mandatory=$true)]
    [string[]]$TargetPCs,
    
    [Parameter(Mandatory=$true)]
    [string]$Username,
    
    [Parameter(Mandatory=$false)]
    [System.Security.SecureString]$Password
)

# Colors for output
function Write-ColorOutput {
    param([string]$Message, [string]$Color = "White")
    Write-Host $Message -ForegroundColor $Color
}

Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  PC Monitoring Client Deployment" "Cyan"
Write-ColorOutput "  (Standalone/Non-Domain Mode)" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

# Get password if not provided
if (-not $Password) {
    $Password = Read-Host -AsSecureString "Enter password for $Username"
}

# Create credential object
$Credential = New-Object System.Management.Automation.PSCredential ($Username, $Password)

# Paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Candidate client paths
$candidatePaths = @(
    (Join-Path $ScriptDir "Client"),
    (Join-Path $ScriptDir "..\Client"),
    (Join-Path $ScriptDir "..\..\Client"),
    (Join-Path $ScriptDir "PCMonitorClient\bin\Release"),
    (Join-Path $ScriptDir "..\PCMonitorClient\bin\Release"),
    (Join-Path $ScriptDir "..\..\PCMonitorClient\bin\Release"),
    "F:\Sharing\Other\DeploymentPackage\Client",
    "F:\Sharing\PCMonitor",
    "\\$ServerIP\Sharing\PCMonitor",
    "\\$ServerIP\Sharing\Other\DeploymentPackage\Client",
    "C:\Program Files\PCMonitor",
    "C:\ProgramData\PCMonitor"
)

$ClientPath = $null
foreach ($cand in $candidatePaths) {
    if (Test-Path (Join-Path $cand "PCMonitorClient.exe")) {
        $ClientPath = (Resolve-Path $cand -ErrorAction SilentlyContinue).Path
        break
    }
}
if (-not $ClientPath) {
    foreach ($cand in $candidatePaths) {
        if (Test-Path $cand) {
            $ClientPath = (Resolve-Path $cand -ErrorAction SilentlyContinue).Path
            break
        }
    }
}

$LocalDeployPath = "C:\Temp\PCMonitorClient"

# Check if client files exist
if (-not $ClientPath -or -not (Test-Path (Join-Path $ClientPath "PCMonitorClient.exe"))) {
    Write-ColorOutput "❌ ERROR: Client files not found!" "Red"
    Write-ColorOutput "   Please build the PCMonitorClient project first." "Yellow"
    Write-ColorOutput "   Checked candidates:" "Yellow"
    $candidatePaths | ForEach-Object { Write-ColorOutput "   - $_" "Gray" }
    exit 1
}

# Create local temp directory
if (-not (Test-Path $LocalDeployPath)) {
    New-Item -ItemType Directory -Path $LocalDeployPath -Force | Out-Null
}

# Copy client files to temp
Write-ColorOutput "📦 Preparing client files..." "Yellow"
Copy-Item -Path "$ClientPath\*" -Destination $LocalDeployPath -Recurse -Force

# Update App.config with server IP and port
$configPath = Join-Path $LocalDeployPath "PCMonitorClient.exe.config"
if (Test-Path $configPath) {
    [xml]$config = Get-Content $configPath
    $config.configuration.appSettings.add | Where-Object { $_.key -eq "ServerIP" } | ForEach-Object { $_.value = $ServerIP }
    $config.configuration.appSettings.add | Where-Object { $_.key -eq "ServerPort" } | ForEach-Object { $_.value = $ServerPort.ToString() }
    $config.Save($configPath)
    Write-ColorOutput "✓ Updated configuration: Server=$ServerIP`:$ServerPort" "Green"
}

# Configure firewall on server
Write-ColorOutput "`n🔥 Configuring firewall on server..." "Yellow"
try {
    $firewallRule = Get-NetFirewallRule -DisplayName "PC Monitor Server" -ErrorAction SilentlyContinue
    if (-not $firewallRule) {
        New-NetFirewallRule -DisplayName "PC Monitor Server" `
                           -Direction Inbound `
                           -Protocol TCP `
                           -LocalPort $ServerPort `
                           -Action Allow `
                           -Profile Any | Out-Null
        Write-ColorOutput "✓ Firewall rule created" "Green"
    } else {
        Write-ColorOutput "✓ Firewall rule already exists" "Green"
    }
} catch {
    Write-ColorOutput "⚠ Warning: Could not configure firewall: $_" "Yellow"
}

# Deploy to each PC
$successCount = 0
$failCount = 0
$results = @()

Write-ColorOutput "`n🚀 Deploying to $($TargetPCs.Count) computers..." "Cyan"
Write-ColorOutput ("=" * 60) "Cyan"

foreach ($pc in $TargetPCs) {
    Write-ColorOutput "`n[$pc] Processing..." "White"
    
    try {
        # Test connectivity
        Write-ColorOutput "  → Testing connection..." "Gray"
        $testResult = Test-Connection -ComputerName $pc -Count 1 -Quiet
        if (-not $testResult) {
            throw "Cannot reach computer"
        }
        
        # Enable PSRemoting on target (if not already enabled)
        Write-ColorOutput "  → Enabling PSRemoting..." "Gray"
        try {
            # For standalone PCs, we need to use TrustedHosts
            $currentTrustedHosts = (Get-Item WSMan:\localhost\Client\TrustedHosts).Value
            if ($currentTrustedHosts -notlike "*$pc*" -and $currentTrustedHosts -ne "*") {
                $newTrustedHosts = if ($currentTrustedHosts) { "$currentTrustedHosts,$pc" } else { $pc }
                Set-Item WSMan:\localhost\Client\TrustedHosts -Value $newTrustedHosts -Force
                Write-ColorOutput "  ✓ Added $pc to TrustedHosts" "Green"
            }
        } catch {
            Write-ColorOutput "  ⚠ Warning: TrustedHosts configuration: $_" "Yellow"
        }
        
        # Create remote session
        Write-ColorOutput "  → Creating remote session..." "Gray"
        $session = New-PSSession -ComputerName $pc -Credential $Credential -ErrorAction Stop
        
        # Copy files to remote PC
        Write-ColorOutput "  → Copying files..." "Gray"
        $remotePath = "C:\Program Files\PCMonitor"
        $legacyPath = "C:\ProgramData\PCMonitor"
        
        Invoke-Command -Session $session -ScriptBlock {
            param($path, $legacy)
            if (Test-Path $path) {
                # Stop existing client if running
                Get-Process -Name "PCMonitorClient" -ErrorAction SilentlyContinue | Stop-Process -Force
                Start-Sleep -Seconds 1
            }
            if (Test-Path $legacy) {
                # Clean up legacy ProgramData folder
                try {
                    Remove-Item -Path "$legacy\*" -Recurse -Force -ErrorAction SilentlyContinue
                    Remove-Item -Path $legacy -Recurse -Force -ErrorAction SilentlyContinue
                } catch { }
            }
            if (-not (Test-Path $path)) {
                New-Item -ItemType Directory -Path $path -Force | Out-Null
            }
        } -ArgumentList $remotePath, $legacyPath
        
        Copy-Item -Path "$LocalDeployPath\*" -Destination $remotePath -ToSession $session -Recurse -Force
        
        # Configure firewall on client
        Write-ColorOutput "  → Configuring firewall..." "Gray"
        Invoke-Command -Session $session -ScriptBlock {
            param($port)
            try {
                $rule = Get-NetFirewallRule -DisplayName "PC Monitor Client" -ErrorAction SilentlyContinue
                if (-not $rule) {
                    New-NetFirewallRule -DisplayName "PC Monitor Client" `
                                       -Direction Outbound `
                                       -Protocol TCP `
                                       -RemotePort $port `
                                       -Action Allow `
                                       -Profile Any | Out-Null
                }
            } catch { }
        } -ArgumentList $ServerPort
        
        # Configure permissions, logon task, and SYSTEM watchdog task
        Write-ColorOutput "  → Hardening permissions and configuring startup + watchdog tasks..." "Gray"
        Invoke-Command -Session $session -ScriptBlock {
            param($targetFolder, $serverIP, $serverPort)
            
            # 1. Lock down NTFS permissions (SYSTEM/Admins Full Control, Users Read & Execute only)
            try {
                & icacls.exe "$targetFolder" /inheritance:d /grant:r "NT AUTHORITY\SYSTEM:(OI)(CI)F" "BUILTIN\Administrators:(OI)(CI)F" "BUILTIN\Users:(OI)(CI)RX" *>$null
            } catch { }

            # 2. Configure Interactive Logon Task (Runs in user session with GUI/screen capture access)
            Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false -ErrorAction SilentlyContinue
            
            $action = New-ScheduledTaskAction -Execute "$targetFolder\PCMonitorClient.exe" `
                                             -Argument "$serverIP $serverPort" `
                                             -WorkingDirectory $targetFolder
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
                                  -Description "PC Activity Monitoring Client" `
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
            $wdTrigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) `
                                                 -RepetitionInterval (New-TimeSpan -Minutes 2) `
                                                 -RepetitionDuration ([TimeSpan]::MaxValue)
            $wdPrincipal = New-ScheduledTaskPrincipal -UserId "NT AUTHORITY\SYSTEM" -LogonType ServiceAccount -RunLevel Highest
            $wdSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
            
            Register-ScheduledTask -TaskName "PCMonitorWatchdog" `
                                  -Action $wdAction `
                                  -Trigger $wdTrigger `
                                  -Principal $wdPrincipal `
                                  -Settings $wdSettings `
                                  -Description "PC Monitor SYSTEM Anti-Tamper Watchdog" `
                                  -Force | Out-Null
        } -ArgumentList $remotePath, $ServerIP, $ServerPort
        
        # Start the client immediately if user logged in
        Write-ColorOutput "  → Starting client..." "Gray"
        Invoke-Command -Session $session -ScriptBlock {
            param($targetFolder)
            $explorer = Get-Process -Name 'explorer' -ErrorAction SilentlyContinue
            if ($explorer) {
                Start-ScheduledTask -TaskName "PCMonitorClient" -ErrorAction SilentlyContinue
            }
        } -ArgumentList $remotePath
        
        Remove-PSSession -Session $session
        
        Write-ColorOutput "  ✓ Successfully deployed!" "Green"
        $successCount++
        $results += [PSCustomObject]@{
            PC = $pc
            Status = "Success"
            Message = "Deployed and started"
        }
        
    } catch {
        Write-ColorOutput "  ❌ Failed: $_" "Red"
        $failCount++
        $results += [PSCustomObject]@{
            PC = $pc
            Status = "Failed"
            Message = $_.Exception.Message
        }
    }
}

# Summary
Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  Deployment Summary" "Cyan"
Write-ColorOutput "========================================" "Cyan"
Write-ColorOutput "✓ Successful: $successCount" "Green"
Write-ColorOutput "❌ Failed: $failCount" "Red"

if ($results | Where-Object { $_.Status -eq "Failed" }) {
    Write-ColorOutput "`nFailed Deployments:" "Yellow"
    $results | Where-Object { $_.Status -eq "Failed" } | Format-Table -AutoSize
}

# Instructions
Write-ColorOutput "`n📋 Next Steps:" "Cyan"
Write-ColorOutput "1. Run the server application: WinServer2019.exe" "White"
Write-ColorOutput "2. Click '📊 Live Monitoring' button" "White"
Write-ColorOutput "3. Click 'Start Monitoring Server'" "White"
Write-ColorOutput "4. Connected PCs should appear within 10 seconds" "White"
Write-ColorOutput "5. Double-click any PC to view their screen in real-time!" "White"

Write-ColorOutput "`n⚠ Troubleshooting:" "Yellow"
Write-ColorOutput "If PCs don't appear:" "White"
Write-ColorOutput "  • Check firewall rules on both server and clients" "Gray"
Write-ColorOutput "  • Verify server IP: $ServerIP" "Gray"
Write-ColorOutput "  • Check client is running: Get-Process -Name PCMonitorClient" "Gray"
Write-ColorOutput "  • Review scheduled task: Get-ScheduledTask -TaskName PCMonitorClient" "Gray"

Write-ColorOutput "`n✓ Deployment complete!`n" "Green"
