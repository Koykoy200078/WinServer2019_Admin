# ==============================================================================
# Web & Protocol Blocking Functions (HOSTS, DNS, TCP/UDP Firewall, Anti-DoH)
# Computer Laboratory Management System
# ==============================================================================

function Get-SanitizedDomains {
    param([array]$RawList)

    $clean = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $RawList) {
        if ([string]::IsNullOrWhiteSpace($entry)) { continue }
        $line = $entry.Trim()
        if ($line.StartsWith("#")) { continue }

        # Remove http:// or https:// if present
        $line = $line -replace "^https?://", ""
        # Remove trailing slash and any URL path
        if ($line.Contains("/")) {
            $line = $line.Substring(0, $line.IndexOf("/"))
        }
        # Remove port if present
        if ($line.Contains(":")) {
            $line = $line.Substring(0, $line.IndexOf(":"))
        }
        # Remove leading wildcard *.
        $line = $line -replace "^\*\.", ""

        $line = $line.Trim().ToLower()
        # Must be a valid domain-like string (contain at least one dot)
        if ($line.Length -gt 3 -and $line.Contains(".") -and $line -match "^[a-z0-9\.\-]+$") {
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
        [string]$CategoryName = "ALL SITES"
    )

    $cleanSites = Get-SanitizedDomains -RawList $BlockedSites
    if ($cleanSites.Count -eq 0) {
        Write-Host "ERROR: No valid domains to block." -ForegroundColor Red
        return
    }

    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "  APPLYING MULTI-LAYER WEB & PROTOCOL BLOCKING: $CategoryName" -ForegroundColor Cyan
    Write-Host "  Unique domains to block: $($cleanSites.Count)" -ForegroundColor Green
    Write-Host "  Protections: HOSTS (0.0.0.0/127.0.0.1) + Anti-DoH + Firewall UDP/TCP" -ForegroundColor Yellow
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host ""

    foreach ($pc in $Targets) {
        Write-Host "Checking $pc ..." -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                if (-not $isDomainMember) {
                    Write-Host "$pc is not in $script:targetDomain domain - SKIPPING" -ForegroundColor Red
                    continue
                }

                Write-Host "$($pc): Applying $CategoryName blocking..." -ForegroundColor Yellow

                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList (,$cleanSites), $CategoryName -ScriptBlock {
                    param($sites, $category)

                    $results = @()
                    $hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"

                    try {
                        if (-not (Test-Path $hostsFile)) {
                            throw "Hosts file not found at $hostsFile"
                        }

                        # --- LAYER 1: HOSTS FILE SINKHOLING ---
                        $results += "=== 1. HOSTS FILE SINKHOLING ==="
                        $backupName = "$hostsFile.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
                        Copy-Item $hostsFile $backupName -Force -ErrorAction Stop
                        $results += "Backup created: $(Split-Path $backupName -Leaf)"

                        $content = @()
                        for ($i = 0; $i -lt 3; $i++) {
                            try {
                                [System.GC]::Collect()
                                [System.GC]::WaitForPendingFinalizers()
                                $content = Get-Content $hostsFile -Encoding UTF8 -ErrorAction Stop
                                break
                            } catch { Start-Sleep -Milliseconds 400 }
                        }

                        $markerStart = "# BLOCKED BY ADMIN - $category - START"
                        $markerEnd   = "# BLOCKED BY ADMIN - $category - END"

                        # Strip any existing blocks for this category or general admin blocks if ALL
                        $newContent = @()
                        $skip = $false
                        foreach ($line in $content) {
                            if ($line -like "*BLOCKED BY ADMIN - $category - START*" -or ($category -eq "ALL SITES" -and $line -like "*BLOCKED BY ADMIN*")) {
                                $skip = $true
                                continue
                            }
                            if ($skip -and ($line -like "*BLOCKED BY ADMIN - $category - END*" -or $line -like "*END BLOCKED BY ADMIN*")) {
                                $skip = $false
                                continue
                            }
                            if (-not $skip) {
                                $newContent += $line
                            }
                        }

                        # Prepare optimized block entries (both 0.0.0.0 and 127.0.0.1 for instant fail)
                        $blockEntries = @()
                        $blockEntries += $markerStart
                        $blockEntries += "# Total domains: $($sites.Count) | Applied: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
                        $blockEntries += ""

                        foreach ($site in $sites) {
                            $blockEntries += "0.0.0.0 $site"
                            $blockEntries += "127.0.0.1 $site"
                            if (-not $site.StartsWith("www.")) {
                                $blockEntries += "0.0.0.0 www.$site"
                                $blockEntries += "127.0.0.1 www.$site"
                            }
                        }

                        $blockEntries += ""
                        $blockEntries += $markerEnd

                        $finalContent = $newContent + $blockEntries
                        for ($i = 0; $i -lt 3; $i++) {
                            try {
                                [System.GC]::Collect()
                                [System.GC]::WaitForPendingFinalizers()
                                $finalContent | Out-File -FilePath $hostsFile -Encoding UTF8 -Force -ErrorAction Stop
                                break
                            } catch { Start-Sleep -Milliseconds 400 }
                        }
                        $results += "Hosts file updated ($($sites.Count) domains sinkholed)"

                        # --- LAYER 2: DISABLE BROWSER DNS-OVER-HTTPS (Anti-Bypass) ---
                        $results += "=== 2. BROWSER DOH ANTI-BYPASS ==="
                        try {
                            # Microsoft Edge policy
                            $edgePath = "HKLM:\SOFTWARE\Policies\Microsoft\Edge"
                            if (-not (Test-Path $edgePath)) { New-Item -Path $edgePath -Force | Out-Null }
                            Set-ItemProperty -Path $edgePath -Name "DnsOverHttpsMode" -Value "off" -Force
                            Set-ItemProperty -Path $edgePath -Name "BuiltInDnsClientEnabled" -Value 1 -Type DWord -Force

                            # Google Chrome policy
                            $chromePath = "HKLM:\SOFTWARE\Policies\Google\Chrome"
                            if (-not (Test-Path $chromePath)) { New-Item -Path $chromePath -Force | Out-Null }
                            Set-ItemProperty -Path $chromePath -Name "DnsOverHttpsMode" -Value "off" -Force
                            Set-ItemProperty -Path $chromePath -Name "BuiltInDnsClientEnabled" -Value 1 -Type DWord -Force

                            # Mozilla Firefox policy
                            $firefoxPath = "HKLM:\SOFTWARE\Policies\Mozilla\Firefox"
                            if (-not (Test-Path $firefoxPath)) { New-Item -Path $firefoxPath -Force | Out-Null }
                            Set-ItemProperty -Path $firefoxPath -Name "DNSOverHTTPS" -Value 0 -Type DWord -Force

                            $results += "Enforced Registry policies disabling DoH in Edge, Chrome, and Firefox"
                        } catch {
                            $results += "DoH policy warning: $($_.Exception.Message)"
                        }

                        # --- LAYER 3: WINDOWS FIREWALL OUTBOUND BLOCKING ---
                        $results += "=== 3. PROTOCOL & FIREWALL RULES ==="
                        try {
                            # Known Public DoH Resolvers (IPs commonly used to bypass local DNS)
                            $dohIPs = @(
                                "1.1.1.1", "1.0.0.1",              # Cloudflare
                                "8.8.8.8", "8.8.4.4",              # Google
                                "9.9.9.9", "149.112.112.112",      # Quad9
                                "208.67.222.222", "208.67.220.220",# OpenDNS
                                "94.140.14.14", "94.140.15.15"     # AdGuard
                            )

                            # Remove old ComLab rules first
                            Get-NetFirewallRule -Name "ComLab-Block-*" -ErrorAction SilentlyContinue | Remove-NetFirewallRule -ErrorAction SilentlyContinue

                            # Block Outbound TCP/UDP 853 (DNS-over-TLS)
                            New-NetFirewallRule -Name "ComLab-Block-DoT-853" `
                                                -DisplayName "ComLab: Block DNS-over-TLS (Port 853)" `
                                                -Direction Outbound `
                                                -Action Block `
                                                -Protocol TCP `
                                                -RemotePort 853 `
                                                -Profile Any `
                                                -ErrorAction SilentlyContinue | Out-Null

                            # Block Outbound DoH IPs on Port 443
                            New-NetFirewallRule -Name "ComLab-Block-DoH-Resolvers" `
                                                -DisplayName "ComLab: Block Public DoH Resolvers (TCP 443)" `
                                                -Direction Outbound `
                                                -Action Block `
                                                -Protocol TCP `
                                                -RemoteAddress $dohIPs `
                                                -RemotePort 443 `
                                                -Profile Any `
                                                -ErrorAction SilentlyContinue | Out-Null

                            # Block Outbound UDP Port 443 (QUIC / HTTP3) - stops QUIC bypasses
                            New-NetFirewallRule -Name "ComLab-Block-QUIC-UDP443" `
                                                -DisplayName "ComLab: Block QUIC Protocol (UDP 443)" `
                                                -Direction Outbound `
                                                -Action Block `
                                                -Protocol UDP `
                                                -RemotePort 443 `
                                                -Profile Any `
                                                -ErrorAction SilentlyContinue | Out-Null

                            $results += "Firewall rules deployed: Blocked DoH Resolvers (TCP 443), DoT (853), and QUIC (UDP 443)"
                        } catch {
                            $results += "Firewall warning: $($_.Exception.Message)"
                        }

                        # --- LAYER 4: DNS CACHE FLUSH & BROWSER CLEANUP ---
                        $results += "=== 4. CACHE FLUSH & ACTIVE SESSIONS ==="
                        try {
                            ipconfig /flushdns | Out-Null
                            Clear-DnsClientCache -ErrorAction SilentlyContinue
                            $results += "Flushed DNS client & resolver cache"

                            # Close open browser processes so existing socket pools disconnect
                            $browsers = @("chrome", "firefox", "msedge", "opera", "brave")
                            foreach ($b in $browsers) {
                                Get-Process -Name $b -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
                            }
                            $results += "Reset browser sockets"
                        } catch {
                            $results += "Cache reset warning: $($_.Exception.Message)"
                        }

                        return @{
                            Success = $true
                            SitesBlocked = $sites.Count
                            Details = $results
                        }
                    }
                    catch {
                        return @{
                            Success = $false
                            SitesBlocked = 0
                            Details = @("ERROR: $($_.Exception.Message)")
                        }
                    }
                } -ErrorAction Stop

                if ($result.Success) {
                    Write-Host "  [OK] $($pc): $CategoryName blocking active ($($result.SitesBlocked) domains)" -ForegroundColor Green
                    foreach ($line in $result.Details) {
                        if ($line.StartsWith("===")) {
                            Write-Host "       $line" -ForegroundColor DarkCyan
                        } else {
                            Write-Host "       $line" -ForegroundColor Gray
                        }
                    }
                } else {
                    Write-Host "  [FAIL] $($pc): $($result.Details -join ' | ')" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host "  [FAIL] $pc is offline or WinRM failed: $_" -ForegroundColor DarkGray
        }
    }

    Write-Host ""
    Write-Host "Blocking deployment completed." -ForegroundColor Green
    Pause
}

function Invoke-WebUnblocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [string]$CategoryName = "ALL"
    )

    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "  RESTORING WEB & PROTOCOL ACCESS ($CategoryName)" -ForegroundColor Cyan
    Write-Host "  Actions: Clean HOSTS + Remove Firewall Rules + Restore DNS" -ForegroundColor Yellow
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host ""

    foreach ($pc in $Targets) {
        Write-Host "Checking $pc ..." -ForegroundColor Cyan
        try {
            if (Test-WSMan -ComputerName $pc -ErrorAction Stop) {
                $isDomainMember = Test-DomainMembership -ComputerName $pc
                if (-not $isDomainMember) {
                    Write-Host "$pc is not in $script:targetDomain domain - SKIPPING" -ForegroundColor Red
                    continue
                }

                Write-Host "$($pc): Unblocking web access..." -ForegroundColor Yellow

                $result = Invoke-Command -ComputerName $pc -Credential $script:cred -ArgumentList $CategoryName -ScriptBlock {
                    param($cat)

                    $results = @()
                    $hostsFile = "$env:SystemRoot\System32\drivers\etc\hosts"

                    try {
                        if (-not (Test-Path $hostsFile)) {
                            throw "Hosts file not found at $hostsFile"
                        }

                        # --- 1. RESTORE HOSTS FILE ---
                        $results += "=== 1. HOSTS FILE CLEANUP ==="
                        $content = @()
                        for ($i = 0; $i -lt 3; $i++) {
                            try {
                                [System.GC]::Collect()
                                [System.GC]::WaitForPendingFinalizers()
                                $content = Get-Content $hostsFile -Encoding UTF8 -ErrorAction Stop
                                break
                            } catch { Start-Sleep -Milliseconds 400 }
                        }

                        $originalCount = $content.Count
                        $newContent = $content | Where-Object {
                            $_ -notmatch "BLOCKED BY ADMIN" -and
                            $_ -notmatch "^(0\.0\.0\.0|127\.0\.0\.1)\s+(www\.)?[a-zA-Z0-9\-]+\.[a-zA-Z0-9\-\.]+" -or
                            $_ -match "^127\.0\.0\.1\s+localhost"
                        }

                        $removedEntries = $originalCount - $newContent.Count
                        for ($i = 0; $i -lt 3; $i++) {
                            try {
                                [System.GC]::Collect()
                                [System.GC]::WaitForPendingFinalizers()
                                $newContent | Out-File -FilePath $hostsFile -Encoding UTF8 -Force -ErrorAction Stop
                                break
                            } catch { Start-Sleep -Milliseconds 400 }
                        }
                        $results += "Cleaned hosts file: $removedEntries block entries removed"

                        # Clean old backup files
                        try {
                            $oldBackups = Get-ChildItem "$env:SystemRoot\System32\drivers\etc" -Filter "hosts.backup-*" -ErrorAction SilentlyContinue
                            if ($oldBackups) {
                                $count = $oldBackups.Count
                                $oldBackups | Remove-Item -Force -ErrorAction SilentlyContinue
                                $results += "Removed $count old backup hosts files"
                            }
                        } catch { }

                        # --- 2. REMOVE FIREWALL RULES ---
                        $results += "=== 2. FIREWALL RULES RESTORATION ==="
                        try {
                            $fwRules = Get-NetFirewallRule -Name "ComLab-Block-*" -ErrorAction SilentlyContinue
                            if ($fwRules) {
                                $ruleCount = @($fwRules).Count
                                $fwRules | Remove-NetFirewallRule -ErrorAction SilentlyContinue
                                $results += "Removed $ruleCount ComLab outbound block firewall rules"
                            } else {
                                $results += "No active ComLab firewall block rules found"
                            }
                        } catch {
                            $results += "Firewall restore warning: $($_.Exception.Message)"
                        }

                        # --- 3. RESTORE REGISTRY DOH SETTINGS ---
                        $results += "=== 3. RESTORE BROWSER DNS SETTINGS ==="
                        try {
                            Remove-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\Edge" -Name "DnsOverHttpsMode" -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Google\Chrome" -Name "DnsOverHttpsMode" -ErrorAction SilentlyContinue
                            Remove-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Mozilla\Firefox" -Name "DNSOverHTTPS" -ErrorAction SilentlyContinue
                            $results += "Restored default browser DNS settings"
                        } catch { }

                        # --- 4. FLUSH DNS ---
                        $results += "=== 4. FLUSH DNS CACHE ==="
                        try {
                            ipconfig /flushdns | Out-Null
                            Clear-DnsClientCache -ErrorAction SilentlyContinue
                            $results += "DNS cache cleared"
                        } catch { }

                        return @{
                            Success = $true
                            EntriesRemoved = $removedEntries
                            Details = $results
                        }
                    } catch {
                        return @{
                            Success = $false
                            EntriesRemoved = 0
                            Details = @("ERROR: $($_.Exception.Message)")
                        }
                    }
                } -ErrorAction Stop

                if ($result.Success) {
                    Write-Host "  [OK] $($pc): Web & protocol access unblocked ($($result.EntriesRemoved) entries removed)" -ForegroundColor Green
                    foreach ($line in $result.Details) {
                        Write-Host "       $line" -ForegroundColor Gray
                    }
                } else {
                    Write-Host "  [FAIL] $($pc): $($result.Details -join ' | ')" -ForegroundColor Red
                }
            }
        }
        catch {
            Write-Host "  [FAIL] $pc is offline or WinRM failed: $_" -ForegroundColor DarkGray
        }
    }

    Write-Host ""
    Write-Host "Unblocking completed." -ForegroundColor Green
    Pause
}

function Invoke-AIBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$AISites
    )
    Invoke-WebBlocking -Targets $Targets -BlockedSites $AISites -CategoryName "AI SITES ONLY"
}

function Invoke-SocialMediaBlocking {
    param(
        [Parameter(Mandatory=$true)]
        [array]$Targets,
        [Parameter(Mandatory=$true)]
        [array]$SocialSites
    )
    Invoke-WebBlocking -Targets $Targets -BlockedSites $SocialSites -CategoryName "SOCIAL MEDIA ONLY"
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
    Invoke-WebBlocking -Targets $Targets -BlockedSites $combined -CategoryName "AI + SOCIAL MEDIA (FOCUS MODE)"
}

function Invoke-DeepScan {
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "  DEEP SCAN: LAB BLOCKING & SECURITY AUDIT" -ForegroundColor Cyan
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host ""

    $targets = foreach ($i in 1..35) { "PC-$i" }
    $reports = @()

    foreach ($pc in $targets) {
        Write-Host "Auditing $pc..." -ForegroundColor Gray -NoNewline
        $status = Get-BlockingStatus -ComputerName $pc

        if ($status.Status -eq "SUCCESS") {
            $stateDesc = if ($status.HasBlocks) { "BLOCKED ($($status.BlockedEntries) rules)" } else { "UNBLOCKED" }
            $fwDesc = if ($status.FirewallRules -gt 0) { "ACTIVE ($($status.FirewallRules))" } else { "None" }
            $dohDesc = if ($status.DoHDisabled) { "ENFORCED" } else { "Default" }

            $statusColor = if ($status.HasBlocks) { "Yellow" } else { "Green" }
            Write-Host " [$stateDesc | FW: $fwDesc | DoH: $dohDesc]" -ForegroundColor $statusColor

            $reports += [PSCustomObject]@{
                PC           = $pc
                Status       = "ONLINE"
                WebBlocking  = $stateDesc
                Firewall     = $fwDesc
                AntiDoH      = $dohDesc
                TotalLines   = $status.TotalHostsLines
            }
        } else {
            Write-Host " [OFFLINE]" -ForegroundColor DarkGray
            $reports += [PSCustomObject]@{
                PC           = $pc
                Status       = "OFFLINE"
                WebBlocking  = "N/A"
                Firewall     = "N/A"
                AntiDoH      = "N/A"
                TotalLines   = "N/A"
            }
        }
    }

    Write-Host ""
    Write-Host "AUDIT SUMMARY TABLE:" -ForegroundColor Yellow
    $reports | Format-Table -AutoSize
    Pause
}

function Show-BlockLists {
    param(
        [Parameter(Mandatory=$true)]
        [array]$BlockedSites,
        [Parameter(Mandatory=$true)]
        [string]$BlockListsFolder
    )
    
    Clear-Host
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "               CURRENT BLOCK LIST SUMMARY                 " -ForegroundColor Cyan
    Write-Host "==========================================================" -ForegroundColor Cyan
    Write-Host "Total unique active sites across all files: $($BlockedSites.Count)" -ForegroundColor Green
    Write-Host ""
    
    $blockFiles = Get-ChildItem -Path $BlockListsFolder -Filter "*.txt" | Where-Object { 
        $_.Name -notin @("README.txt", "QUICK-REFERENCE.txt", "SITE-LIST.txt") 
    }
    
    foreach ($file in $blockFiles) {
        $raw = Get-Content $file.FullName
        $sites = Get-SanitizedDomains -RawList $raw
        
        $categoryName = switch ($file.BaseName) {
            "ai-sites"                { "Artificial Intelligence (AI & ChatBots)" }
            "social-media"            { "Social Media & Messaging Platforms" }
            "video-sites"             { "Video Streaming & Entertainment" }
            "gaming-sites"            { "Online Gaming Platforms" }
            "shopping-entertainment"  { "Shopping & Lifestyle" }
            "search-engines"          { "Search Engines" }
            default                   { $file.BaseName }
        }
        
        Write-Host "█ $categoryName ($($sites.Count) domains)" -ForegroundColor Yellow
        Write-Host "  File: $($file.Name)" -ForegroundColor DarkGray
        
        $preview = $sites | Select-Object -First 6
        foreach ($site in $preview) {
            Write-Host "    • $site" -ForegroundColor White
        }
        if ($sites.Count -gt 6) {
            Write-Host "    ... and $($sites.Count - 6) more domains" -ForegroundColor Gray
        }
        Write-Host ""
    }
    
    Write-Host "Press any key to return to menu..."
    $null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
}
