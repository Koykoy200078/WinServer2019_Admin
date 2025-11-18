using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

namespace WinServer2019
{
    public class PowerShellExecutionResult
    {
        public bool Success { get; set; }
        public List<string> Output { get; set; }
        public List<string> Errors { get; set; }

        public PowerShellExecutionResult()
        {
            Output = new List<string>();
            Errors = new List<string>();
            Success = true;
        }
    }

    public class PowerShellExecutor : IDisposable
    {
        private readonly string username;
        private readonly string password;
        private readonly string domain;
        private readonly string scriptPath;
        private Runspace runspace;

        public PowerShellExecutor(string user, string pass, string dom, string scriptsPath)
        {
            username = user;
            password = pass;
            domain = dom;
            scriptPath = scriptsPath;
            InitializeRunspace();
        }

        private void InitializeRunspace()
        {
            try
            {
                // Create initial session state
                InitialSessionState iss = InitialSessionState.CreateDefault();
                iss.ExecutionPolicy = Microsoft.PowerShell.ExecutionPolicy.Bypass;

                // Create and open runspace
                runspace = RunspaceFactory.CreateRunspace(iss);
                runspace.Open();

                // Set up script variables and load embedded functions
                using (PowerShell ps = PowerShell.Create())
                {
                    ps.Runspace = runspace;

                    // Set script variables
                    ps.AddScript($"$script:targetDomain = '{domain}'");
                    ps.AddScript($"$script:scriptPath = '{scriptPath}'");
                    
                    // Create credential object
                    ps.AddScript($@"
                        $securePassword = ConvertTo-SecureString '{password}' -AsPlainText -Force
                        $script:cred = New-Object System.Management.Automation.PSCredential('{username}', $securePassword)
                    ");

                    // Load embedded PowerShell functions
                    ps.AddScript(GetEmbeddedHelperFunctions());
                    ps.AddScript(GetEmbeddedPCManagementFunctions());
                    ps.AddScript(GetEmbeddedWebBlockingFunctions());
                    ps.AddScript(GetEmbeddedUtilityFunctions());

                    // Load block lists from embedded resources or files
                    ps.AddScript(GetBlockListsScript());

                    ps.Invoke();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize PowerShell runspace: {ex.Message}", ex);
            }
        }

        private string GetEmbeddedHelperFunctions()
        {
            return @"
function Test-DomainMembership {
    param(
        [string]$ComputerName,
        [string]$TargetDomain = $script:targetDomain
    )
    
    try {
        $result = Invoke-Command -ComputerName $ComputerName -Credential $script:cred -ScriptBlock {
            $domain = (Get-WmiObject -Class Win32_ComputerSystem).Domain
            return $domain
        } -ErrorAction Stop
        
        return $result -eq $TargetDomain
    }
    catch {
        return $false
    }
}

function Get-BlockingStatus {
    param([string]$ComputerName)
    
    try {
        $result = Invoke-Command -ComputerName $ComputerName -Credential $script:cred -ScriptBlock {
            try {
                $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""
                
                if (-not (Test-Path $hostsFile)) {
                    throw ""Hosts file not found""
                }
                
                $content = @()
                $retryCount = 0
                $maxRetries = 3
                
                while ($retryCount -lt $maxRetries) {
                    try {
                        [System.GC]::Collect()
                        [System.GC]::WaitForPendingFinalizers()
                        $content = Get-Content $hostsFile -Encoding UTF8 -ErrorAction Stop
                        break
                    }
                    catch {
                        $retryCount++
                        if ($retryCount -eq $maxRetries) {
                            throw ""Failed to read hosts file after $maxRetries attempts""
                        }
                        Start-Sleep -Milliseconds 200
                    }
                }
                
                $blockedCount = ($content | Where-Object { $_ -match ""BLOCKED BY ADMIN"" }).Count
                $totalLines = $content.Count
                $hasBlocks = ($content | Where-Object { $_ -match ""127\.0\.0\.1.*\.(com|net|org)"" }).Count -gt 0
                
                return [PSCustomObject]@{
                    Computer = $env:COMPUTERNAME
                    HasBlocks = $hasBlocks
                    BlockedEntries = $blockedCount
                    TotalHostsLines = $totalLines
                    Domain = (Get-WmiObject -Class Win32_ComputerSystem).Domain
                    Status = ""SUCCESS""
                }
            }
            catch {
                return [PSCustomObject]@{
                    Computer = $env:COMPUTERNAME
                    HasBlocks = ""ERROR""
                    BlockedEntries = ""N/A""
                    TotalHostsLines = ""N/A""
                    Domain = (Get-WmiObject -Class Win32_ComputerSystem).Domain
                    Status = ""ERROR""
                    ErrorMessage = $_.Exception.Message
                }
            }
        } -ErrorAction Stop
        
        return $result
    }
    catch {
        return [PSCustomObject]@{
            Computer = $ComputerName
            HasBlocks = ""ERROR""
            BlockedEntries = ""N/A""
            TotalHostsLines = ""N/A""
            Domain = ""N/A""
            Status = ""CONNECTION_ERROR""
            ErrorMessage = $_.Exception.Message
        }
    }
}
";
        }

        private string GetEmbeddedPCManagementFunctions()
        {
            return @"
function Get-AllPCStatus {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets
    )
    
    foreach ($pc in $Targets) {
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    $date = Get-Date
                    $tz   = (Get-TimeZone).Id

                    $srcLine = (w32tm /query /status | Select-String ""Source"").Line
                    $src = $srcLine -replace "".*Source:\s*"", """"

                    if ($src -match ""csitlab\.local"") {
                        $srcDisplay = ""Domain ($src)""
                    }
                    else {
                        $srcDisplay = $src
                    }

                    $primaryIP = Get-NetIPAddress -AddressFamily IPv4 | 
                        Where-Object { 
                            $_.IPAddress -notlike ""127.*"" -and 
                            $_.IPAddress -notlike ""169.254.*"" -and
                            $_.PrefixOrigin -ne ""WellKnown"" -and
                            $_.SuffixOrigin -ne ""Link""
                        } |
                        Sort-Object -Property InterfaceIndex |
                        Select-Object -First 1 -ExpandProperty IPAddress

                    if (-not $primaryIP) {
                        $primaryIP = ""No IP configured""
                    }

                    $dnsServers = Get-DnsClientServerAddress -AddressFamily IPv4 | 
                        Where-Object { 
                            $_.ServerAddresses.Count -gt 0 -and 
                            $_.InterfaceAlias -notlike ""*Loopback*"" -and
                            $_.InterfaceAlias -notlike ""*Virtual*""
                        } |
                        Select-Object -First 1 -ExpandProperty ServerAddresses

                    if ($dnsServers) {
                        $dnsDisplay = $dnsServers -join "", ""
                    } else {
                        $dnsDisplay = ""No DNS configured""
                    }

                    [PSCustomObject]@{
                        Computer = $env:COMPUTERNAME
                        IPAddress = $primaryIP
                        DNSServers = $dnsDisplay
                        DateTime = $date
                        TimeZone = $tz
                        Source   = $srcDisplay
                    }
                }

                Write-Host ""$($result.Computer) is ONLINE"" -ForegroundColor Green
                Write-Host ""   IP Address: $($result.IPAddress)"" -ForegroundColor Cyan
                Write-Host ""   DNS Servers: $($result.DNSServers)"" -ForegroundColor Cyan
                Write-Host ""   Date/Time : $($result.DateTime)""
                Write-Host ""   TimeZone  : $($result.TimeZone)""
                Write-Host ""   Source    : $($result.Source)""
            }
        }
        catch {
            Write-Host ""$pc is OFFLINE or unreachable via WinRM"" -ForegroundColor Red
        }
    }
}

function Invoke-PCShutdown {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets
    )
    
    foreach ($pc in $Targets) {
        Write-Host ""Checking $pc ..."" -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                Write-Host ""Shutting down $pc via WinRM ..."" -ForegroundColor Yellow
                Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    Stop-Computer -Force
                }
                Write-Host ""$pc shutdown command sent."" -ForegroundColor Green
            }
        }
        catch {
            Write-Host ""$pc is offline or unreachable via WinRM. Error: $_"" -ForegroundColor DarkGray
        }
    }
}

function Invoke-PCRestart {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets
    )
    
    foreach ($pc in $Targets) {
        Write-Host ""Checking $pc ..."" -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                Write-Host ""Restarting $pc via WinRM ..."" -ForegroundColor Yellow
                Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    Restart-Computer -Force
                }
                Write-Host ""$pc restart command sent."" -ForegroundColor Green
            }
        }
        catch {
            Write-Host ""$pc is offline or unreachable via WinRM. Error: $_"" -ForegroundColor DarkGray
        }
    }
}

function Invoke-DeepScan {
    Write-Host ""Starting Deep Scan of all PCs..."" -ForegroundColor Cyan
    Write-Host ""Checking blocking status and domain membership..."" -ForegroundColor Yellow
    Write-Host ""Target Domain: $script:targetDomain"" -ForegroundColor Cyan
    Write-Host """"
    
    $targets = foreach ($i in 1..35) { ""PC-$i.$script:targetDomain"" }
    $scanResults = @()
    
    foreach ($pc in $targets) {
        Write-Host ""Scanning $pc..."" -ForegroundColor Gray
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                
                if ($isDomainMember) {
                    $status = Get-BlockingStatus -ComputerName $pc
                    $scanResults += $status
                    
                    if ($status.HasBlocks) {
                        Write-Host ""  $pc`: ONLINE, Domain Member, Blocking ACTIVE ($($status.BlockedEntries) entries)"" -ForegroundColor Green
                    } else {
                        Write-Host ""  $pc`: ONLINE, Domain Member, Blocking INACTIVE"" -ForegroundColor Yellow
                    }
                } else {
                    Write-Host ""  $pc`: ONLINE, NOT in $script:targetDomain domain - SKIPPING"" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host ""  $pc`: OFFLINE or unreachable"" -ForegroundColor DarkGray
        }
    }
    
    Write-Host """"
    Write-Host ""===== DEEP SCAN SUMMARY ====="" -ForegroundColor Cyan
    $onlineDomainPCs = $scanResults | Where-Object { $_.Domain -eq $script:targetDomain }
    $blockedPCs = $onlineDomainPCs | Where-Object { $_.HasBlocks -eq $true }
    $unblockedPCs = $onlineDomainPCs | Where-Object { $_.HasBlocks -eq $false }
    
    Write-Host ""Total PCs scanned: 35"" -ForegroundColor White
    Write-Host ""Domain members online: $($onlineDomainPCs.Count)"" -ForegroundColor White
    Write-Host ""PCs with blocking active: $($blockedPCs.Count)"" -ForegroundColor Green
    Write-Host ""PCs with blocking inactive: $($unblockedPCs.Count)"" -ForegroundColor Yellow
}

function Invoke-CustomPSCommand {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [string]$Command
    )
    
    Write-Host ""===== EXECUTE CUSTOM POWERSHELL COMMAND ====="" -ForegroundColor Cyan
    Write-Host ""Command to execute: "" -ForegroundColor Yellow
    Write-Host ""  $Command"" -ForegroundColor White
    Write-Host """"
    Write-Host ""Target PCs: $($Targets.Count)"" -ForegroundColor Cyan
    Write-Host """"
    Write-Host ""Executing command on target PCs..."" -ForegroundColor Cyan
    Write-Host """"
    
    $successCount = 0
    $failCount = 0
    
    foreach ($pc in $Targets) {
        Write-Host ""Executing on $pc..."" -ForegroundColor Gray
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                
                if ($isDomainMember) {
                    $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList $Command -ScriptBlock {
                        param($cmd)
                        try {
                            $output = Invoke-Expression $cmd 2>&1 | Out-String
                            return @{
                                Computer = $env:COMPUTERNAME
                                Success = $true
                                Output = $output
                                Error = $null
                            }
                        }
                        catch {
                            return @{
                                Computer = $env:COMPUTERNAME
                                Success = $false
                                Output = $null
                                Error = $_.Exception.Message
                            }
                        }
                    } -ErrorAction Stop
                    
                    if ($result.Success) {
                        $successCount++
                        Write-Host ""  ✓ $($result.Computer) - SUCCESS"" -ForegroundColor Green
                        if (-not [string]::IsNullOrWhiteSpace($result.Output)) {
                            Write-Host ""    Output:"" -ForegroundColor Cyan
                            $outputLines = $result.Output -split ""`n""
                            foreach ($line in $outputLines) {
                                if (-not [string]::IsNullOrWhiteSpace($line)) {
                                    Write-Host ""      $line"" -ForegroundColor White
                                }
                            }
                        } else {
                            Write-Host ""    (No output)"" -ForegroundColor Gray
                        }
                        Write-Host """"
                    } else {
                        $failCount++
                        Write-Host ""  ✗ $($result.Computer) - FAILED"" -ForegroundColor Red
                        Write-Host ""    Error: $($result.Error)"" -ForegroundColor Red
                    }
                } else {
                    $failCount++
                    Write-Host ""  ✗ $pc - NOT in $script:targetDomain domain - SKIPPING"" -ForegroundColor Red
                }
            }
        }
        catch {
            $failCount++
            Write-Host ""  ✗ $pc - OFFLINE or unreachable"" -ForegroundColor DarkGray
        }
    }
    
    Write-Host """"
    Write-Host ""===== EXECUTION SUMMARY ====="" -ForegroundColor Cyan
    Write-Host ""Total PCs targeted: $($Targets.Count)"" -ForegroundColor White
    Write-Host ""Successful executions: $successCount"" -ForegroundColor Green
    Write-Host ""Failed executions: $failCount"" -ForegroundColor Red
    Write-Host """"
}
";
        }

        private string GetEmbeddedWebBlockingFunctions()
        {
            return @"
function Invoke-WebBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$BlockedSites
    )
    
    foreach ($pc in $Targets) {
        Write-Host ""Checking $pc ..."" -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                
                if (-not $isDomainMember) {
                    Write-Host ""$pc is not in $script:targetDomain domain - SKIPPING"" -ForegroundColor Red
                    continue
                }
                
                Write-Host ""Blocking web access on $pc..."" -ForegroundColor Yellow
                
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList (,$BlockedSites) -ScriptBlock {
                    param($sites)
                    
                    try {
                        $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""
                        
                        if (-not (Test-Path $hostsFile)) {
                            throw ""Hosts file not found""
                        }
                        
                        $backupName = ""$hostsFile.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')""
                        Copy-Item $hostsFile $backupName -Force
                        
                        $content = Get-Content $hostsFile -Encoding UTF8
                        $newContent = $content | Where-Object { 
                            $_ -notmatch ""BLOCKED BY ADMIN""
                        }
                        
                        $blockEntries = @()
                        $blockEntries += ""# BLOCKED BY ADMIN - $(Get-Date)""
                        $blockEntries += """"
                        
                        foreach ($site in $sites) {
                            $blockEntries += ""127.0.0.1 $site""
                            $blockEntries += ""0.0.0.0 $site""
                        }
                        
                        $blockEntries += """"
                        $finalContent = $newContent + $blockEntries
                        $finalContent | Out-File -FilePath $hostsFile -Encoding UTF8 -Force
                        
                        ipconfig /flushdns | Out-Null
                        
                        return @{ Success = $true; SitesBlocked = $sites.Count }
                    }
                    catch {
                        return @{ Success = $false; Message = $_.Exception.Message }
                    }
                } -ErrorAction Stop
                
                if ($result.Success) {
                    Write-Host ""$pc blocked successfully ($($result.SitesBlocked) sites)"" -ForegroundColor Green
                } else {
                    Write-Host ""$pc FAILED: $($result.Message)"" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host ""$pc is offline or unreachable"" -ForegroundColor DarkGray
        }
    }
}

function Invoke-WebUnblocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets
    )
    
    foreach ($pc in $Targets) {
        Write-Host ""Checking $pc ..."" -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                
                if (-not $isDomainMember) {
                    Write-Host ""$pc is not in $script:targetDomain domain - SKIPPING"" -ForegroundColor Red
                    continue
                }
                
                Write-Host ""Unblocking web access on $pc..."" -ForegroundColor Yellow
                
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    try {
                        $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""
                        
                        if (-not (Test-Path $hostsFile)) {
                            throw ""Hosts file not found""
                        }
                        
                        $content = Get-Content $hostsFile -Encoding UTF8
                        $newContent = $content | Where-Object { 
                            $_ -notmatch ""BLOCKED BY ADMIN"" -and 
                            $_ -notmatch ""127\.0\.0\.1\s+(?!localhost)"" -and
                            $_ -notmatch ""0\.0\.0\.0\s+""
                        }
                        
                        $newContent | Out-File -FilePath $hostsFile -Encoding UTF8 -Force
                        
                        ipconfig /flushdns | Out-Null
                        
                        return @{ Success = $true }
                    }
                    catch {
                        return @{ Success = $false; Message = $_.Exception.Message }
                    }
                } -ErrorAction Stop
                
                if ($result.Success) {
                    Write-Host ""$pc unblocked successfully"" -ForegroundColor Green
                } else {
                    Write-Host ""$pc FAILED: $($result.Message)"" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host ""$pc is offline or unreachable"" -ForegroundColor DarkGray
        }
    }
}

function Invoke-AIBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$AISites
    )
    
    foreach ($pc in $Targets) {
        Write-Host ""Blocking AI sites on $pc..."" -ForegroundColor Yellow
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList (,$AISites) -ScriptBlock {
                    param($sites)
                    try {
                        $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""
                        $content = Get-Content $hostsFile -Encoding UTF8
                        $blockEntries = @(""# AI SITES BLOCKED - $(Get-Date)"")
                        foreach ($site in $sites) {
                            $blockEntries += ""127.0.0.1 $site""
                        }
                        $finalContent = $content + $blockEntries
                        $finalContent | Out-File -FilePath $hostsFile -Encoding UTF8 -Force
                        ipconfig /flushdns | Out-Null
                        return @{ Success = $true }
                    } catch {
                        return @{ Success = $false }
                    }
                }
                Write-Host ""$pc AI sites blocked"" -ForegroundColor Green
            }
        } catch {
            Write-Host ""$pc is offline"" -ForegroundColor DarkGray
        }
    }
}

function Show-BlockLists {
    param(
        [Parameter(Mandatory=$true)]
        [array]$BlockedSites,
        [Parameter(Mandatory=$true)]
        [string]$BlockListsFolder
    )
    
    Write-Host ""===== BLOCK LISTS ====="" -ForegroundColor Cyan
    Write-Host ""Total sites: $($BlockedSites.Count)"" -ForegroundColor Green
    Write-Host ""Categories loaded from embedded lists"" -ForegroundColor Yellow
}
";
        }

        private string GetEmbeddedUtilityFunctions()
        {
            return @"
function Sync-TimeToAllPCs {
    Write-Host ""===== TIME SYNCHRONIZATION ====="" -ForegroundColor Cyan
    $serverTime = Get-Date
    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
    
    foreach ($pc in $targets) {
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList $serverTime -ScriptBlock {
                    param($targetTime)
                    Set-Date -Date $targetTime
                    w32tm /resync /force | Out-Null
                }
                Write-Host ""$pc time synced"" -ForegroundColor Green
            }
        } catch {
            Write-Host ""$pc offline"" -ForegroundColor DarkGray
        }
    }
}

function Invoke-BackupCleanup {
    Write-Host ""===== BACKUP CLEANUP ====="" -ForegroundColor Cyan
    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
    
    foreach ($pc in $targets) {
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    $hostsPath = ""$env:SystemRoot\System32\drivers\etc""
                    $backupFiles = Get-ChildItem -Path $hostsPath -Filter ""hosts.backup-*""
                    if ($backupFiles) {
                        $backupFiles | Remove-Item -Force
                        return $backupFiles.Count
                    }
                    return 0
                }
                Write-Host ""$pc cleaned $result backup files"" -ForegroundColor Green
            }
        } catch {
            Write-Host ""$pc offline"" -ForegroundColor DarkGray
        }
    }
}

function Show-AllHostsFiles {
    Write-Host ""===== HOSTS FILES ====="" -ForegroundColor Cyan
    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
    
    foreach ($pc in $targets) {
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""
                    $content = Get-Content $hostsFile
                    $blocked = ($content | Where-Object { $_ -match ""127\.0\.0\.1"" -and $_ -notmatch ""localhost"" }).Count
                    return @{ Lines = $content.Count; Blocked = $blocked }
                }
                Write-Host ""$pc - $($result.Lines) lines, $($result.Blocked) blocked"" -ForegroundColor Cyan
            }
        } catch {
            Write-Host ""$pc offline"" -ForegroundColor DarkGray
        }
    }
}

function Export-MySQLDatabases {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$false)]
        [string]$ExportType = ""ALL"",
        [Parameter(Mandatory=$true)]
        [string]$ScriptPath
    )
    
    Write-Host ""===== MYSQL EXPORT ====="" -ForegroundColor Cyan
    Write-Host ""Export functionality requires MySQL to be installed on target PCs"" -ForegroundColor Yellow
    Write-Host ""This feature exports databases from remote PCs"" -ForegroundColor Gray
}

function Test-AndroidJavaEnvironment {
    Write-Host ""===== ENVIRONMENT CHECK ====="" -ForegroundColor Cyan
    $targets = 1..35 | ForEach-Object { ""PC-$_.$script:targetDomain"" }
    
    foreach ($pc in $targets) {
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    $androidHome = [System.Environment]::GetEnvironmentVariable(""ANDROID_HOME"", ""Machine"")
                    $javaHome = [System.Environment]::GetEnvironmentVariable(""JAVA_HOME"", ""Machine"")
                    return @{ Android = $androidHome; Java = $javaHome }
                }
                $aStatus = if ($result.Android) { ""SET"" } else { ""NOT SET"" }
                $jStatus = if ($result.Java) { ""SET"" } else { ""NOT SET"" }
                Write-Host ""$pc - ANDROID_HOME: $aStatus, JAVA_HOME: $jStatus"" -ForegroundColor Cyan
            }
        } catch {
            Write-Host ""$pc offline"" -ForegroundColor DarkGray
        }
    }
}

function Clear-TempFiles {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets
    )
    
    Write-Host ""===== TEMP FILES CLEANUP ====="" -ForegroundColor Cyan
    
    foreach ($pc in $Targets) {
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    $cleaned = 0
                    try {
                        $temp1 = Get-ChildItem ""C:\Windows\Temp"" -Recurse -ErrorAction SilentlyContinue
                        $temp1 | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
                        $cleaned += $temp1.Count
                    } catch {}
                    return $cleaned
                }
                Write-Host ""$pc cleaned $result temp files"" -ForegroundColor Green
            }
        } catch {
            Write-Host ""$pc offline"" -ForegroundColor DarkGray
        }
    }
}
";
        }

        private string GetBlockListsScript()
        {
            return @"
# Initialize block lists with embedded data
$script:blockedSites = @(
    'facebook.com', 'www.facebook.com', 'fb.com',
    'youtube.com', 'www.youtube.com', 'youtu.be',
    'twitter.com', 'www.twitter.com', 'x.com',
    'instagram.com', 'www.instagram.com',
    'tiktok.com', 'www.tiktok.com',
    'reddit.com', 'www.reddit.com',
    'netflix.com', 'www.netflix.com',
    'twitch.tv', 'www.twitch.tv',
    'discord.com', 'www.discord.com',
    'snapchat.com', 'www.snapchat.com'
)

$script:aiSitesOnly = @(
    'openai.com', 'chat.openai.com', 'chatgpt.com',
    'claude.ai', 'anthropic.com',
    'gemini.google.com', 'bard.google.com',
    'copilot.microsoft.com', 'bing.com/chat',
    'perplexity.ai', 'you.com',
    'character.ai', 'poe.com',
    'midjourney.com', 'stability.ai',
    'huggingface.co', 'replicate.com'
)

$script:blockListStats = @{
    'social-media' = 10
    'video-sites' = 5
    'ai-sites' = 16
}

Write-Host 'Block lists loaded: $($script:blockedSites.Count) total sites' -ForegroundColor Green
";
        }

        public PowerShellExecutionResult ExecuteCommand(string command, Action<string> outputCallback = null, System.Threading.CancellationToken cancellationToken = default)
        {
            var result = new PowerShellExecutionResult();

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    result.Success = false;
                    result.Errors.Add("Execution cancelled before start");
                    return result;
                }

                using (PowerShell ps = PowerShell.Create())
                {
                    ps.Runspace = runspace;
                    ps.AddScript(command);

                    // Capture Write-Host output by subscribing to information stream with REAL-TIME callback
                    ps.Streams.Information.DataAdded += (sender, args) =>
                    {
                        var data = ps.Streams.Information[args.Index];
                        if (data != null)
                        {
                            string message = data.MessageData?.ToString() ?? "";
                            result.Output.Add(message);
                            outputCallback?.Invoke(message); // Real-time output
                        }
                    };

                    // Subscribe to other streams for real-time output
                    ps.Streams.Warning.DataAdded += (sender, args) =>
                    {
                        var data = ps.Streams.Warning[args.Index];
                        if (data != null)
                        {
                            string message = $"WARNING: {data.Message}";
                            result.Output.Add(message);
                            outputCallback?.Invoke(message);
                        }
                    };

                    ps.Streams.Error.DataAdded += (sender, args) =>
                    {
                        var data = ps.Streams.Error[args.Index];
                        if (data != null)
                        {
                            string message = $"ERROR: {data.ToString()}";
                            result.Errors.Add(message);
                            result.Success = false;
                            outputCallback?.Invoke(message);
                        }
                    };

                    ps.Streams.Verbose.DataAdded += (sender, args) =>
                    {
                        var data = ps.Streams.Verbose[args.Index];
                        if (data != null)
                        {
                            string message = $"VERBOSE: {data.Message}";
                            result.Output.Add(message);
                            outputCallback?.Invoke(message);
                        }
                    };

                    ps.Streams.Debug.DataAdded += (sender, args) =>
                    {
                        var data = ps.Streams.Debug[args.Index];
                        if (data != null)
                        {
                            string message = $"DEBUG: {data.Message}";
                            result.Output.Add(message);
                            outputCallback?.Invoke(message);
                        }
                    };

                    // Execute the command
                    Collection<PSObject> psOutput = ps.Invoke();

                    // Collect standard output (PSObjects returned from functions)
                    foreach (PSObject outputItem in psOutput)
                    {
                        if (outputItem != null)
                        {
                            string message = outputItem.ToString();
                            result.Output.Add(message);
                            outputCallback?.Invoke(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                string errorMsg = $"Exception: {ex.Message}";
                result.Errors.Add(errorMsg);
                outputCallback?.Invoke(errorMsg);
            }

            return result;
        }

        public PowerShellExecutionResult ExecuteScriptFile(string scriptFilePath, Dictionary<string, object> parameters = null)
        {
            var result = new PowerShellExecutionResult();

            try
            {
                if (!File.Exists(scriptFilePath))
                {
                    result.Success = false;
                    result.Errors.Add($"Script file not found: {scriptFilePath}");
                    return result;
                }

                using (PowerShell ps = PowerShell.Create())
                {
                    ps.Runspace = runspace;
                    ps.AddCommand(scriptFilePath);

                    // Add parameters if provided
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            ps.AddParameter(param.Key, param.Value);
                        }
                    }

                    // Execute the script
                    Collection<PSObject> psOutput = ps.Invoke();

                    // Collect output
                    foreach (PSObject outputItem in psOutput)
                    {
                        if (outputItem != null)
                        {
                            result.Output.Add(outputItem.ToString());
                        }
                    }

                    // Collect errors
                    if (ps.HadErrors)
                    {
                        result.Success = false;
                        foreach (ErrorRecord error in ps.Streams.Error)
                        {
                            result.Errors.Add(error.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Exception: {ex.Message}");
            }

            return result;
        }

        public void Dispose()
        {
            if (runspace != null)
            {
                runspace.Close();
                runspace.Dispose();
                runspace = null;
            }
        }
    }
}
