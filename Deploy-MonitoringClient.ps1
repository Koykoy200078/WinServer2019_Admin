# PC Monitoring System - Auto Deployment Script
# This script deploys the client monitoring software to all lab PCs

param(
    [string]$ServerIP = "192.168.2.45",
    [int]$ServerPort = 8888,
    [int]$StartPC = 1,
    [int]$EndPC = 35,
    [string]$Domain = "csitlab.local"
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "PC Monitoring System - Deployment" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Detect if running from deployment package or source
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($scriptDir -like "*DeploymentPackage*") {
    # Running from deployment package
    $clientPath = Join-Path (Split-Path -Parent $scriptDir) "Client"
} else {
    # Running from source (for development)
    $projectRoot = Split-Path -Parent $scriptDir
    $clientPath = Join-Path $projectRoot "PCMonitorClient\bin\Release"
}

$networkShare = "\\$ServerIP\Sharing\PCMonitor"
$targetFolder = "C:\ProgramData\PCMonitor"

# Verify client files exist
if (-not (Test-Path $clientPath)) {
    Write-Host "ERROR: Client files not found at: $clientPath" -ForegroundColor Red
    Write-Host "Please ensure the deployment package is complete." -ForegroundColor Yellow
    exit 1
}

Write-Host "Using client files from: $clientPath" -ForegroundColor Gray
Write-Host ""

# Step 1: Copy client to network share
Write-Host "[1/5] Copying client to network share..." -ForegroundColor Yellow
try {
    if (-not (Test-Path $networkShare)) {
        New-Item -Path $networkShare -ItemType Directory -Force | Out-Null
    }
    Copy-Item -Path "$clientPath\*" -Destination $networkShare -Recurse -Force
    Write-Host "  ✓ Client copied to $networkShare" -ForegroundColor Green
}
catch {
    Write-Host "  ✗ Failed to copy to network share: $_" -ForegroundColor Red
    exit 1
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

# Step 3: Deploy to all PCs
Write-Host "[3/5] Deploying to PCs $StartPC to $EndPC..." -ForegroundColor Yellow
$targets = $StartPC..$EndPC | ForEach-Object { "PC-$_.$Domain" }
$successCount = 0
$failCount = 0

foreach ($pc in $targets) {
    Write-Host "  Deploying to $pc..." -ForegroundColor Gray -NoNewline
    
    try {
        # Test connectivity by checking admin share access
        $remotePath = "\\$pc\C$\ProgramData\PCMonitor"
        $testPath = "\\$pc\C$"
        
        if (-not (Test-Path $testPath -ErrorAction SilentlyContinue)) {
            Write-Host " OFFLINE" -ForegroundColor DarkGray
            $failCount++
            continue
        }

        # Stop running client process via remote session
        try {
            Invoke-Command -ComputerName $pc -ScriptBlock {
                Get-Process -Name "PCMonitorClient" -ErrorAction SilentlyContinue | Stop-Process -Force
                Start-Sleep -Milliseconds 500
            } -ErrorAction SilentlyContinue
        } catch {
            # Ignore if process not running
        }

        # Create directory if doesn't exist
        if (-not (Test-Path $remotePath)) {
            try {
                New-Item -Path $remotePath -ItemType Directory -Force -ErrorAction Stop | Out-Null
            } catch {
                Write-Host " FAILED: Cannot create directory" -ForegroundColor Red
                $failCount++
                continue
            }
        }
        
        # Copy files with retry logic
        $retryCount = 0
        $maxRetries = 3
        $copySuccess = $false
        
        while (-not $copySuccess -and $retryCount -lt $maxRetries) {
            try {
                Copy-Item -Path "$networkShare\*" -Destination $remotePath -Recurse -Force -ErrorAction Stop
                $copySuccess = $true
            } catch {
                $retryCount++
                if ($retryCount -lt $maxRetries) {
                    Start-Sleep -Milliseconds 500
                } else {
                    Write-Host " FAILED: Cannot copy files after $maxRetries attempts" -ForegroundColor Red
                    $failCount++
                    continue
                }
            }
        }

        # Create scheduled task for auto-start
        Invoke-Command -ComputerName $pc -ScriptBlock {
            param($targetFolder, $serverIP, $serverPort)
            
            # Remove old task if exists
            Unregister-ScheduledTask -TaskName "PCMonitorClient" -Confirm:$false -ErrorAction SilentlyContinue
            
            # Create new task
            $action = New-ScheduledTaskAction -Execute "$targetFolder\PCMonitorClient.exe" `
                                             -Argument "$serverIP $serverPort"
            $trigger = New-ScheduledTaskTrigger -AtLogOn
            $principal = New-ScheduledTaskPrincipal -GroupId "BUILTIN\Users"
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
            
            # Start immediately
            Start-Process -FilePath "$targetFolder\PCMonitorClient.exe" `
                         -ArgumentList "$serverIP $serverPort" `
                         -WindowStyle Hidden
            
        } -ArgumentList $targetFolder, $ServerIP, $ServerPort -ErrorAction Stop

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
