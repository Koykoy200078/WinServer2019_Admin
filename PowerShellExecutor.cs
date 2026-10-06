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
                    ps.AddScript($"$script:targetDomain = '{domain.Replace("'", "''")}'");
                    ps.AddScript($"$script:scriptPath = '{scriptPath.Replace("'", "''")}'");
                    ps.AddScript($"$script:blockListsFolder = Join-Path '{scriptPath.Replace("'", "''")}' 'BlockLists'");
                    
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
    
    if ([string]::IsNullOrWhiteSpace($TargetDomain)) {
        $TargetDomain = ""csitlab.local""
    }
    
    try {
        $result = Invoke-Command -ComputerName $ComputerName -Credential $script:cred -ScriptBlock {
            if (Get-Command Get-CimInstance -ErrorAction SilentlyContinue) {
                (Get-CimInstance -ClassName Win32_ComputerSystem).Domain
            } else {
                (Get-WmiObject -Class Win32_ComputerSystem).Domain
            }
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
                $hasBlocks = ($content | Where-Object { $_ -match ""(127\.0\.0\.1|0\.0\.0\.0).*\.(com|net|org|ai|io|me|tv|co)"" }).Count -gt 0
                
                $fwRuleCount = 0
                try {
                    $fwRules = Get-NetFirewallRule -Name ""ComLab-Block-*"" -ErrorAction SilentlyContinue
                    if ($fwRules) { $fwRuleCount = @($fwRules).Count }
                } catch { }

                $dohDisabled = $false
                try {
                    $edgeVal = (Get-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Edge"" -Name ""DnsOverHttpsMode"" -ErrorAction SilentlyContinue).DnsOverHttpsMode
                    if ($edgeVal -eq ""off"") { $dohDisabled = $true }
                } catch { }

                $curDomain = if (Get-Command Get-CimInstance -ErrorAction SilentlyContinue) {
                    (Get-CimInstance -ClassName Win32_ComputerSystem).Domain
                } else {
                    (Get-WmiObject -Class Win32_ComputerSystem).Domain
                }

                return [PSCustomObject]@{
                    Computer        = $env:COMPUTERNAME
                    HasBlocks       = $hasBlocks
                    BlockedEntries  = $blockedCount
                    TotalHostsLines = $totalLines
                    FirewallRules   = $fwRuleCount
                    DoHDisabled     = $dohDisabled
                    Domain          = $curDomain
                    Status          = ""SUCCESS""
                }
            }
            catch {
                $errDomain = try {
                    if (Get-Command Get-CimInstance -ErrorAction SilentlyContinue) {
                        (Get-CimInstance -ClassName Win32_ComputerSystem).Domain
                    } else {
                        (Get-WmiObject -Class Win32_ComputerSystem).Domain
                    }
                } catch { ""N/A"" }
                return [PSCustomObject]@{
                    Computer        = $env:COMPUTERNAME
                    HasBlocks       = ""ERROR""
                    BlockedEntries  = ""N/A""
                    TotalHostsLines = ""N/A""
                    FirewallRules   = 0
                    DoHDisabled     = $false
                    Domain          = $errDomain
                    Status          = ""ERROR""
                    ErrorMessage    = $_.Exception.Message
                }
            }
        } -ErrorAction Stop
        
        return $result
    }
    catch {
        return [PSCustomObject]@{
            Computer        = $ComputerName
            HasBlocks       = ""ERROR""
            BlockedEntries  = ""N/A""
            TotalHostsLines = ""N/A""
            FirewallRules   = 0
            DoHDisabled     = $false
            Domain          = ""N/A""
            Status          = ""CONNECTION_ERROR""
            ErrorMessage    = $_.Exception.Message
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
function Get-SanitizedDomains {
    param([array]$RawList)

    $clean = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $RawList) {
        if ([string]::IsNullOrWhiteSpace($entry)) { continue }
        $line = $entry.Trim()
        if ($line.StartsWith(""#"")) { continue }

        $line = $line -replace ""^https?://"", """"
        if ($line.Contains(""/"")) {
            $line = $line.Substring(0, $line.IndexOf(""/""))
        }
        if ($line.Contains("":"")) {
            $line = $line.Substring(0, $line.IndexOf("":""))
        }
        $line = $line -replace ""^\*\."", """"

        $line = $line.Trim().ToLower()
        if ($line.Length -gt 3 -and $line.Contains(""."") -and $line -match ""^[a-z0-9\.\-]+$"") {
            [void]$clean.Add($line)
        }
    }
    return [string[]]$clean
}

function Invoke-WebBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$BlockedSites,
        [string]$CategoryName = ""ALL SITES""
    )

    $cleanSites = Get-SanitizedDomains -RawList $BlockedSites
    if ($cleanSites.Count -eq 0) {
        Write-Host ""ERROR: No valid domains to block."" -ForegroundColor Red
        return
    }

    Write-Host """"
    Write-Host ""========================================================="" -ForegroundColor Cyan
    Write-Host ""  APPLYING MULTI-LAYER WEB & PROTOCOL BLOCKING: $CategoryName"" -ForegroundColor Cyan
    Write-Host ""  Unique domains: $($cleanSites.Count)"" -ForegroundColor Green
    Write-Host ""  Layers: HOSTS Sinkhole + Anti-DoH + Firewall TCP/UDP"" -ForegroundColor Yellow
    Write-Host ""========================================================="" -ForegroundColor Cyan
    Write-Host """"

    foreach ($pc in $Targets) {
        Write-Host ""Checking $pc ..."" -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                if (-not $isDomainMember) {
                    Write-Host ""$pc is not in $script:targetDomain domain - SKIPPING"" -ForegroundColor Red
                    continue
                }

                Write-Host ""$($pc): Applying $CategoryName blocking..."" -ForegroundColor Yellow

                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList (,$cleanSites), $CategoryName -ScriptBlock {
                    param($sites, $category)

                    $results = @()
                    $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""

                    try {
                        if (-not (Test-Path $hostsFile)) {
                            throw ""Hosts file not found at $hostsFile""
                        }

                        # --- LAYER 1: HOSTS SINKHOLING ---
                        $backupName = ""$hostsFile.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')""
                        Copy-Item $hostsFile $backupName -Force -ErrorAction Stop

                        $content = @()
                        for ($i = 0; $i -lt 3; $i++) {
                            try {
                                [System.GC]::Collect()
                                [System.GC]::WaitForPendingFinalizers()
                                $content = Get-Content $hostsFile -Encoding UTF8 -ErrorAction Stop
                                break
                            } catch { Start-Sleep -Milliseconds 400 }
                        }

                        $markerStart = ""# BLOCKED BY ADMIN - $category - START""
                        $markerEnd   = ""# BLOCKED BY ADMIN - $category - END""

                        $newContent = @()
                        $skip = $false
                        foreach ($line in $content) {
                            if ($line -like ""*BLOCKED BY ADMIN - $category - START*"" -or ($category -eq ""ALL SITES"" -and $line -like ""*BLOCKED BY ADMIN*"")) {
                                $skip = $true
                                continue
                            }
                            if ($skip -and ($line -like ""*BLOCKED BY ADMIN - $category - END*"" -or $line -like ""*END BLOCKED BY ADMIN*"")) {
                                $skip = $false
                                continue
                            }
                            if (-not $skip) {
                                $newContent += $line
                            }
                        }

                        $blockEntries = @()
                        $blockEntries += """"
                        $blockEntries += $markerStart
                        $blockEntries += ""# Applied on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') for $($sites.Count) domains""

                        $emitted = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
                        foreach ($site in $sites) {
                            if ($emitted.Add($site)) {
                                $blockEntries += ""0.0.0.0 $site""
                                $blockEntries += ""127.0.0.1 $site""
                            }
                            if (-not $site.StartsWith(""www."") -and -not $site.StartsWith(""api."")) {
                                $wwwSite = ""www.$site""
                                if ($emitted.Add($wwwSite)) {
                                    $blockEntries += ""0.0.0.0 $wwwSite""
                                    $blockEntries += ""127.0.0.1 $wwwSite""
                                }
                            }
                        }

                        $blockEntries += $markerEnd
                        $blockEntries += """"

                        $finalContent = $newContent + $blockEntries
                        $finalContent | Set-Content -Path $hostsFile -Encoding UTF8 -Force

                        # --- LAYER 2: ANTI-DOH BROWSER POLICY ---
                        $browserPolicies = @(
                            ""HKLM:\SOFTWARE\Policies\Microsoft\Edge"",
                            ""HKLM:\SOFTWARE\Policies\Google\Chrome"",
                            ""HKLM:\SOFTWARE\Policies\Mozilla\Firefox""
                        )
                        foreach ($p in $browserPolicies) {
                            try {
                                if (-not (Test-Path $p)) { New-Item -Path $p -Force | Out-Null }
                                Set-ItemProperty -Path $p -Name ""DnsOverHttpsMode"" -Value ""off"" -Type String -Force
                            } catch { }
                        }

                        # --- LAYER 3: WINDOWS FIREWALL OUTBOUND BLOCKING ---
                        $dohIPs = @(
                            ""1.1.1.1"", ""1.0.0.1"",
                            ""8.8.8.8"", ""8.8.4.4"",
                            ""9.9.9.9"", ""149.112.112.112"",
                            ""208.67.222.222"", ""208.67.220.220""
                        )

                        try {
                            Get-NetFirewallRule -Name ""ComLab-Block-*"" -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue

                            New-NetFirewallRule -DisplayName ""ComLab - Block Public DoH TCP 443"" `
                                -Name ""ComLab-Block-DoH-TCP"" -Direction Outbound -Action Block `
                                -RemoteAddress $dohIPs -Protocol TCP -RemotePort 443 -Enabled True -ErrorAction SilentlyContinue | Out-Null

                            New-NetFirewallRule -DisplayName ""ComLab - Block Public DoH UDP 443"" `
                                -Name ""ComLab-Block-DoH-UDP"" -Direction Outbound -Action Block `
                                -RemoteAddress $dohIPs -Protocol UDP -RemotePort 443 -Enabled True -ErrorAction SilentlyContinue | Out-Null

                            New-NetFirewallRule -DisplayName ""ComLab - Block DNS over TLS 853"" `
                                -Name ""ComLab-Block-DoT"" -Direction Outbound -Action Block `
                                -Protocol TCP -RemotePort 853 -Enabled True -ErrorAction SilentlyContinue | Out-Null

                            New-NetFirewallRule -DisplayName ""ComLab - Block QUIC UDP 443"" `
                                -Name ""ComLab-Block-QUIC"" -Direction Outbound -Action Block `
                                -Protocol UDP -RemotePort 443 -Enabled True -ErrorAction SilentlyContinue | Out-Null
                        } catch { }

                        # --- LAYER 4: DNS FLUSH & BROWSER CLEANUP ---
                        Clear-DnsClientCache -ErrorAction SilentlyContinue
                        ipconfig /flushdns | Out-Null

                        Get-Process -Name msedge, chrome, firefox -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

                        return @{
                            Success = $true
                            Computer = $env:COMPUTERNAME
                            SitesBlocked = $sites.Count
                            TotalEntries = $blockEntries.Count
                        }
                    }
                    catch {
                        return @{
                            Success = $false
                            Computer = $env:COMPUTERNAME
                            Message = $_.Exception.Message
                        }
                    }
                } -ErrorAction Stop

                if ($result.Success) {
                    Write-Host ""  [OK] $pc blocked successfully ($($result.SitesBlocked) domains, $($result.TotalEntries) entries)"" -ForegroundColor Green
                } else {
                    Write-Host ""  [FAIL] $pc FAILED: $($result.Message)"" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host ""  [FAIL] $pc is offline or WinRM failed: $_"" -ForegroundColor DarkGray
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

                Write-Host ""$($pc): Unblocking web access and removing protocol restrictions..."" -ForegroundColor Yellow

                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ScriptBlock {
                    try {
                        $hostsFile = ""$env:SystemRoot\System32\drivers\etc\hosts""
                        if (-not (Test-Path $hostsFile)) { throw ""Hosts file not found"" }

                        $content = Get-Content $hostsFile -Encoding UTF8
                        $newContent = @()
                        $skip = $false

                        foreach ($line in $content) {
                            if ($line -like ""*BLOCKED BY ADMIN*START*"") { $skip = $true; continue }
                            if ($skip -and $line -like ""*BLOCKED BY ADMIN*END*"") { $skip = $false; continue }
                            if ($skip) { continue }
                            if ($line -match ""BLOCKED BY ADMIN"") { continue }
                            if ($line -match ""(127\.0\.0\.1|0\.0\.0\.0)\s+(?!localhost)(?!broadcasthost)"") { continue }
                            $newContent += $line
                        }

                        $newContent | Set-Content -Path $hostsFile -Encoding UTF8 -Force

                        Get-NetFirewallRule -Name ""ComLab-Block-*"" -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue

                        Remove-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Microsoft\Edge"" -Name ""DnsOverHttpsMode"" -ErrorAction SilentlyContinue
                        Remove-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Google\Chrome"" -Name ""DnsOverHttpsMode"" -ErrorAction SilentlyContinue
                        Remove-ItemProperty -Path ""HKLM:\SOFTWARE\Policies\Mozilla\Firefox"" -Name ""DNSOverHTTPS"" -ErrorAction SilentlyContinue

                        Clear-DnsClientCache -ErrorAction SilentlyContinue
                        ipconfig /flushdns | Out-Null

                        return @{ Success = $true; Computer = $env:COMPUTERNAME }
                    }
                    catch {
                        return @{ Success = $false; Computer = $env:COMPUTERNAME; Message = $_.Exception.Message }
                    }
                } -ErrorAction Stop

                if ($result.Success) {
                    Write-Host ""  [OK] $pc unblocked successfully (HOSTS & Firewall restored)"" -ForegroundColor Green
                } else {
                    Write-Host ""  [FAIL] $pc FAILED: $($result.Message)"" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host ""  [FAIL] $pc is offline or WinRM failed: $_"" -ForegroundColor DarkGray
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
    Invoke-WebBlocking -Targets $Targets -BlockedSites $AISites -CategoryName ""AI SITES ONLY""
}

function Invoke-SocialMediaBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$SocialSites
    )
    Invoke-WebBlocking -Targets $Targets -BlockedSites $SocialSites -CategoryName ""SOCIAL MEDIA ONLY""
}

function Invoke-FocusModeBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$AISites,
        [Parameter(Mandatory=$true)]
        [array]$SocialSites
    )
    $combined = @($AISites) + @($SocialSites)
    Invoke-WebBlocking -Targets $Targets -BlockedSites $combined -CategoryName ""AI + SOCIAL MEDIA (FOCUS MODE)""
}

function Invoke-DeepScan {
    Write-Host """"
    Write-Host ""========================================================="" -ForegroundColor Cyan
    Write-Host ""  DEEP SCAN: LAB BLOCKING & SECURITY AUDIT"" -ForegroundColor Cyan
    Write-Host ""========================================================="" -ForegroundColor Cyan
    Write-Host """"

    $targets = foreach ($i in 1..35) { ""PC-$i"" }
    $reports = @()

    foreach ($pc in $targets) {
        Write-Host ""Auditing $pc... "" -ForegroundColor Gray -NoNewline
        $status = Get-BlockingStatus -ComputerName $pc

        if ($status.Status -eq ""SUCCESS"") {
            $stateDesc = if ($status.HasBlocks) { ""BLOCKED ($($status.BlockedEntries) entries)"" } else { ""UNBLOCKED"" }
            $fwDesc = if ($status.FirewallRules -gt 0) { ""ACTIVE ($($status.FirewallRules))"" } else { ""None"" }
            $dohDesc = if ($status.DoHDisabled) { ""ENFORCED"" } else { ""Default"" }

            Write-Host ""[$stateDesc | FW: $fwDesc | DoH: $dohDesc]"" -ForegroundColor ($status.HasBlocks ? ""Yellow"" : ""Green"")

            $reports += [PSCustomObject]@{
                PC           = $pc
                Status       = ""ONLINE""
                WebBlocking  = $stateDesc
                Firewall     = $fwDesc
                AntiDoH      = $dohDesc
                TotalLines   = $status.TotalHostsLines
            }
        } else {
            Write-Host ""[OFFLINE]"" -ForegroundColor DarkGray
            $reports += [PSCustomObject]@{
                PC           = $pc
                Status       = ""OFFLINE""
                WebBlocking  = ""N/A""
                Firewall     = ""N/A""
                AntiDoH      = ""N/A""
                TotalLines   = ""N/A""
            }
        }
    }

    Write-Host """"
    Write-Host ""AUDIT SUMMARY TABLE:"" -ForegroundColor Yellow
    $reports | Format-Table -AutoSize
}

function Show-BlockLists {
    param(
        [Parameter(Mandatory=$true)]
        [array]$BlockedSites,
        [Parameter(Mandatory=$false)]
        [string]$BlockListsFolder
    )
    
    Write-Host """"
    Write-Host ""========================================================="" -ForegroundColor Cyan
    Write-Host ""               CURRENT BLOCK LIST SUMMARY                 "" -ForegroundColor Cyan
    Write-Host ""========================================================="" -ForegroundColor Cyan
    Write-Host ""Total unique active sites across all files: $($BlockedSites.Count)"" -ForegroundColor Green
    Write-Host """"
    
    $folderToUse = if ($BlockListsFolder -and (Test-Path $BlockListsFolder)) {
        $BlockListsFolder
    } elseif ($script:blockListsFolder -and (Test-Path $script:blockListsFolder)) {
        $script:blockListsFolder
    } else {
        $null
    }

    if ($folderToUse) {
        $blockFiles = Get-ChildItem -Path $folderToUse -Filter ""*.txt"" | Where-Object { 
            $_.Name -notin @(""README.txt"", ""QUICK-REFERENCE.txt"", ""SITE-LIST.txt"") 
        }
        
        foreach ($file in $blockFiles) {
            $raw = Get-Content $file.FullName
            $sites = Get-SanitizedDomains -RawList $raw
            
            $categoryName = switch ($file.BaseName) {
                ""ai-sites""                { ""Artificial Intelligence (AI & ChatBots)"" }
                ""social-media""            { ""Social Media & Messaging Platforms"" }
                ""video-sites""             { ""Video Streaming & Entertainment"" }
                ""gaming-sites""            { ""Online Gaming Platforms"" }
                ""shopping-entertainment""  { ""Shopping & Lifestyle"" }
                ""search-engines""          { ""Search Engines"" }
                default                   { $file.BaseName }
            }
            
            Write-Host ""█ $categoryName ($($sites.Count) domains)"" -ForegroundColor Yellow
            Write-Host ""  File: $($file.Name)"" -ForegroundColor DarkGray
            $preview = $sites | Select-Object -First 4
            foreach ($s in $preview) { Write-Host ""    • $s"" -ForegroundColor White }
            if ($sites.Count -gt 4) { Write-Host ""    ... and $($sites.Count - 4) more"" -ForegroundColor Gray }
            Write-Host """"
        }
    } else {
        Write-Host ""Loaded from embedded memory lists:"" -ForegroundColor Yellow
        Write-Host ""  Total Blocked Sites: $($BlockedSites.Count)"" -ForegroundColor White
        Write-Host ""  AI Sites Only: $($script:aiSitesOnly.Count)"" -ForegroundColor White
        Write-Host ""  Social Media Only: $($script:socialSitesOnly.Count)"" -ForegroundColor White
    }
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
# Initialize block lists with dynamic loading from disk, fallback to embedded
$script:blockedSites = @()
$script:blockListStats = @{}
$script:aiSitesOnly = @()
$script:socialSitesOnly = @()

$folderToSearch = if ($script:blockListsFolder -and (Test-Path $script:blockListsFolder)) {
    $script:blockListsFolder
} elseif ($script:scriptPath -and (Test-Path (Join-Path $script:scriptPath 'BlockLists'))) {
    Join-Path $script:scriptPath 'BlockLists'
} else {
    $null
}

if ($folderToSearch -and (Test-Path $folderToSearch)) {
    $blockFiles = Get-ChildItem -Path $folderToSearch -Filter '*.txt' | Where-Object { 
        $_.Name -notin @('README.txt', 'QUICK-REFERENCE.txt', 'SITE-LIST.txt') 
    }
    
    foreach ($file in $blockFiles) {
        $sites = Get-Content $file.FullName | Where-Object { 
            $_ -notmatch '^#' -and $_ -notmatch '^\s*$' 
        }
        
        $script:blockListStats[$file.BaseName] = $sites.Count
        $script:blockedSites += $sites
        
        if ($file.BaseName -eq 'ai-sites') {
            $script:aiSitesOnly = $sites
        } elseif ($file.BaseName -eq 'social-media') {
            $script:socialSitesOnly = $sites
        }
    }
    Write-Host ""Block lists loaded from disk ($folderToSearch): $($script:blockedSites.Count) total sites"" -ForegroundColor Green
    Write-Host ""  AI Sites: $($script:aiSitesOnly.Count) | Social Media: $($script:socialSitesOnly.Count)"" -ForegroundColor Cyan
} else {
    # Fallback to embedded lists
    $script:blockedSites = @(
        'facebook.com', 'www.facebook.com', 'fb.com', 'm.facebook.com',
        'youtube.com', 'www.youtube.com', 'youtu.be',
        'twitter.com', 'www.twitter.com', 'x.com',
        'instagram.com', 'www.instagram.com',
        'tiktok.com', 'www.tiktok.com',
        'reddit.com', 'www.reddit.com',
        'netflix.com', 'www.netflix.com',
        'twitch.tv', 'www.twitch.tv',
        'discord.com', 'www.discord.com',
        'snapchat.com', 'www.snapchat.com',
        'openai.com', 'chatgpt.com', 'claude.ai', 'gemini.google.com', 'deepseek.com'
    )
    $script:aiSitesOnly = @(
        'openai.com', 'chatgpt.com', 'chat.openai.com', 'api.openai.com', 'sora.com',
        'claude.ai', 'anthropic.com',
        'gemini.google.com', 'bard.google.com',
        'deepseek.com', 'api.deepseek.com',
        'copilot.microsoft.com',
        'perplexity.ai', 'grok.com', 'x.ai', 'character.ai', 'poe.com',
        'midjourney.com', 'stability.ai', 'huggingface.co', 'cursor.com', 'v0.dev'
    )
    $script:socialSitesOnly = @(
        'facebook.com', 'www.facebook.com', 'instagram.com', 'threads.net',
        'tiktok.com', 'x.com', 'twitter.com', 'discord.com', 'reddit.com'
    )
    $script:blockListStats = @{
        'social-media' = $script:socialSitesOnly.Count
        'ai-sites' = $script:aiSitesOnly.Count
    }
    Write-Host ""Block lists loaded from embedded fallback: $($script:blockedSites.Count) total sites"" -ForegroundColor Yellow
}
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
