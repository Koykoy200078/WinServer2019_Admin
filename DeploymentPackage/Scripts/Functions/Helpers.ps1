# Helper Functions
# Common utility functions used across modules

function Test-DomainMembership {
    param(
        [string]$ComputerName,
        [string]$TargetDomain = $script:targetDomain
    )
    
    if ([string]::IsNullOrWhiteSpace($TargetDomain)) {
        $TargetDomain = "csitlab.local"
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
                $hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"
                
                if (-not (Test-Path $hostsFile)) {
                    throw "Hosts file not found"
                }
                
                # Read content with proper encoding and error handling
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
                            throw "Failed to read hosts file after $maxRetries attempts"
                        }
                        Start-Sleep -Milliseconds 200
                    }
                }
                
                $blockedCount = ($content | Where-Object { $_ -match "BLOCKED BY ADMIN" }).Count
                $totalLines = $content.Count
                $hasBlocks = ($content | Where-Object { $_ -match "(127\.0\.0\.1|0\.0\.0\.0).*\.(com|net|org|ai|io|me|tv|co)" }).Count -gt 0
                
                # Check firewall rules
                $fwRuleCount = 0
                try {
                    $fwRules = Get-NetFirewallRule -Name "ComLab-Block-*" -ErrorAction SilentlyContinue
                    if ($fwRules) { $fwRuleCount = @($fwRules).Count }
                } catch { }
                
                # Check DoH status
                $dohDisabled = $false
                try {
                    $edgeVal = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Edge" -Name "DnsOverHttpsMode" -ErrorAction SilentlyContinue).DnsOverHttpsMode
                    if ($edgeVal -eq "off") { $dohDisabled = $true }
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
                    Status          = "SUCCESS"
                }
            }
            catch {
                $errDomain = try {
                    if (Get-Command Get-CimInstance -ErrorAction SilentlyContinue) {
                        (Get-CimInstance -ClassName Win32_ComputerSystem).Domain
                    } else {
                        (Get-WmiObject -Class Win32_ComputerSystem).Domain
                    }
                } catch { "N/A" }
                return [PSCustomObject]@{
                    Computer        = $env:COMPUTERNAME
                    HasBlocks       = "ERROR"
                    BlockedEntries  = "N/A"
                    TotalHostsLines = "N/A"
                    FirewallRules   = 0
                    DoHDisabled     = $false
                    Domain          = $errDomain
                    Status          = "ERROR"
                    ErrorMessage    = $_.Exception.Message
                }
            }
        } -ErrorAction Stop
        
        return $result
    }
    catch {
        return [PSCustomObject]@{
            Computer        = $ComputerName
            HasBlocks       = "ERROR"
            BlockedEntries  = "N/A"
            TotalHostsLines = "N/A"
            FirewallRules   = 0
            DoHDisabled     = $false
            Domain          = "N/A"
            Status          = "CONNECTION_ERROR"
            ErrorMessage    = $_.Exception.Message
        }
    }
}

function Get-TargetSelection {
    param(
        [string]$ActionTitle = "Operation"
    )
    
    Write-Host ""
    Write-Host "Target Selection for: $ActionTitle" -ForegroundColor Yellow
    Write-Host "  1. Single PC (e.g. PC-1)"
    Write-Host "  2. Range of PCs (e.g. PC-1 to PC-10)"
    Write-Host "  3. ALL PCs (PC-1 to PC-35)"
    Write-Host "  0. Cancel"
    Write-Host ""
    
    $sel = Read-Host "Select target option (1-3, 0 to cancel)"
    switch ($sel) {
        '1' {
            $pc = Read-Host "Enter PC name (e.g., PC-1)"
            if ([string]::IsNullOrWhiteSpace($pc)) { return $null }
            $pc = $pc.Trim().ToUpper()
            if ($pc -notmatch "^PC-\d+$" -and $pc -match "^\d+$") { $pc = "PC-$pc" }
            return @($pc)
        }
        '2' {
            $start = Read-Host "Enter start PC number (e.g., 1)"
            $end   = Read-Host "Enter end PC number (e.g., 10)"
            $s = 0; $e = 0
            if ([int]::TryParse($start, [ref]$s) -and [int]::TryParse($end, [ref]$e) -and $s -gt 0 -and $e -ge $s) {
                return @($s..$e | ForEach-Object { "PC-$_" })
            } else {
                Write-Host "Invalid range numbers." -ForegroundColor Red
                return $null
            }
        }
        '3' {
            return @(1..35 | ForEach-Object { "PC-$_" })
        }
        default {
            Write-Host "Operation cancelled." -ForegroundColor DarkGray
            return $null
        }
    }
}
