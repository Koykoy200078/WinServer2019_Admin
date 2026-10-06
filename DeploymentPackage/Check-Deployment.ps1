# Deployment Verification Script
param(
    [string]$PCName = "192.168.2.11",
    [pscredential]$Credential = $null
)

Write-Host "`n=======================================" -ForegroundColor Cyan
Write-Host "  Deployment Verification" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "Target: $PCName`n" -ForegroundColor Yellow

# Check 1: File Share Access & Binary Verification
Write-Host "[1/4] Checking client installation files..." -ForegroundColor Yellow
$sharePath = "\\$PCName\C$\ProgramData\PCMonitor"
try {
    if (Test-Path $sharePath) {
        $files = Get-ChildItem $sharePath -ErrorAction Stop
        Write-Host "  [OK] Installation directory found at: $sharePath" -ForegroundColor Green
        $files | Select-Object Name, @{N="Size (KB)";E={[math]::Round($_.Length/1KB,2)}}, LastWriteTime | Format-Table -AutoSize
    } else {
        Write-Host "  [FAIL] Folder does not exist: $sharePath" -ForegroundColor Red
    }
} catch {
    Write-Host "  [FAIL] Cannot access $sharePath : $_" -ForegroundColor Red
}

# Check 2: Network Connectivity (Ping)
Write-Host "[2/4] Testing network ping..." -ForegroundColor Yellow
try {
    $ping = Test-Connection -ComputerName $PCName -Count 1 -Quiet
    if ($ping) {
        Write-Host "  [OK] PC is reachable via ping" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Ping failed (machine may have ICMP blocked or is offline)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [WARN] Ping error: $_" -ForegroundColor Yellow
}

# Check 3: Running Process (PowerShell 5.1 & PowerShell 7+ compatible)
Write-Host "`n[3/4] Checking running PCMonitorClient process..." -ForegroundColor Yellow
$procFound = $false
$procs = @()

# Method A: CIM Session via DCOM (Works in PowerShell 7+ and PowerShell 5.1)
try {
    $sessionOpt = New-CimSessionOption -Protocol Dcom
    $sessionParams = @{
        ComputerName        = $PCName
        SessionOption       = $sessionOpt
        OperationTimeoutSec = 8
    }
    if ($Credential) {
        $sessionParams["Credential"] = $Credential
    }
    $cimSession = New-CimSession @sessionParams -ErrorAction Stop
    $procs = Get-CimInstance -CimSession $cimSession -ClassName Win32_Process -Filter "Name = 'PCMonitorClient.exe'" -ErrorAction Stop
    Remove-CimSession -CimSession $cimSession -ErrorAction SilentlyContinue
} catch {
    # Method B: Legacy Get-WmiObject (Windows PowerShell 5.1)
    if (Get-Command Get-WmiObject -ErrorAction SilentlyContinue) {
        try {
            $wmiParams = @{
                Class        = "Win32_Process"
                Filter       = "Name = 'PCMonitorClient.exe'"
                ComputerName = $PCName
            }
            if ($Credential) { $wmiParams["Credential"] = $Credential }
            $procs = Get-WmiObject @wmiParams -ErrorAction Stop
        } catch { }
    }
}

# Method C: tasklist.exe fallback
if (-not $procs) {
    try {
        $tlArgs = @("/S", $PCName, "/FI", "IMAGENAME eq PCMonitorClient.exe", "/FO", "CSV", "/NH")
        if ($Credential) {
            $tlArgs += @("/U", $Credential.UserName, "/P", $Credential.GetNetworkCredential().Password)
        }
        $tlOut = & tasklist.exe @tlArgs 2>$null
        if ($tlOut -and $tlOut -like "*PCMonitorClient*") {
            Write-Host "  [OK] PCMonitorClient.exe is currently RUNNING! (verified via tasklist)" -ForegroundColor Green
            Write-Host "       $tlOut" -ForegroundColor Gray
            $procFound = $true
        }
    } catch { }
}

if ($procs) {
    Write-Host "  [OK] PCMonitorClient.exe is currently RUNNING!" -ForegroundColor Green
    $procs | Select-Object ProcessId, CommandLine, @{N="WorkingSet (MB)";E={[math]::Round($_.WorkingSetSize/1MB,2)}} | Format-Table -AutoSize
    $procFound = $true
} elseif (-not $procFound) {
    Write-Host "  [FAIL] PCMonitorClient.exe is NOT running" -ForegroundColor Red
}

# Check 4: Auto-Start Configuration (Scheduled Task)
Write-Host "[4/4] Verifying scheduled task auto-start configuration..." -ForegroundColor Yellow
$taskPath = "\\$PCName\C$\Windows\System32\Tasks\PCMonitorClient"
try {
    if (Test-Path $taskPath) {
        Write-Host "  [OK] Scheduled Task 'PCMonitorClient' is registered for Logon auto-start!" -ForegroundColor Green
        $taskXml = [xml](Get-Content $taskPath -Raw)
        $cmd = $taskXml.Task.Actions.Exec.Command
        $args = $taskXml.Task.Actions.Exec.Arguments
        Write-Host "       Command:   $cmd" -ForegroundColor Gray
        Write-Host "       Arguments: $args" -ForegroundColor Gray
    } else {
        Write-Host "  [WARN] Scheduled Task file not found at $taskPath" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [WARN] Could not inspect task file: $_" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=======================================" -ForegroundColor Cyan
Write-Host "  Verification Summary" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
if ($procFound) {
    Write-Host "STATUS: SUCCESS! $PCName is fully deployed and the monitoring client is active." -ForegroundColor Green
} else {
    Write-Host "STATUS: ATTENTION NEEDED - Client binary is deployed but process is not running." -ForegroundColor Yellow
}
Write-Host ""
