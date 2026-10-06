# ============================================
# PC Management System - Main Script
# ============================================
# Author: Christian Franc M. Carvajal (Koykoy200078)
# GitHub: https://github.com/Koykoy200078
# ============================================
# Domain: Auto-detected from current system
# Target PCs: PC-1 to PC-35
# ============================================

# Get script path
$script:scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:functionsPath = Join-Path $scriptPath "Functions"
$script:blockListsFolder = Join-Path $scriptPath "BlockLists"

# Auto-detect current domain
$defaultDomain = "csitlab.local"
$detectedDomain = ""

try {
    if (Get-Command Get-CimInstance -ErrorAction SilentlyContinue) {
        $detectedDomain = (Get-CimInstance -ClassName Win32_ComputerSystem).Domain
    } else {
        $detectedDomain = (Get-WmiObject -Class Win32_ComputerSystem).Domain
    }
    if ([string]::IsNullOrWhiteSpace($detectedDomain) -or $detectedDomain -eq "WORKGROUP") {
        $detectedDomain = "Not domain-joined (WORKGROUP)"
    }
} catch {
    $detectedDomain = "Unable to detect"
}

# Display detected domain with color coding
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "     DOMAIN CONFIGURATION                    " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Show detected domain
Write-Host "Detected Domain: " -NoNewline
if ($detectedDomain -eq $defaultDomain) {
    Write-Host "$detectedDomain" -ForegroundColor Green
    Write-Host "  ✓ Matches default domain" -ForegroundColor Green
} elseif ($detectedDomain -eq "Not domain-joined (WORKGROUP)" -or $detectedDomain -eq "Unable to detect") {
    Write-Host "$detectedDomain" -ForegroundColor Red
} else {
    Write-Host "$detectedDomain" -ForegroundColor Red
    Write-Host "  ✗ Different from default domain ($defaultDomain)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Default Domain: " -NoNewline
Write-Host "$defaultDomain" -ForegroundColor Cyan
Write-Host ""

# Allow user to change or confirm
Write-Host "Options:" -ForegroundColor Yellow
Write-Host "  1. Use detected domain: $detectedDomain"
Write-Host "  2. Use default domain: $defaultDomain"
Write-Host "  3. Enter custom domain"
Write-Host ""

$domainChoice = Read-Host "Select option (1-3) [Press Enter for detected domain]"

switch ($domainChoice) {
    "2" {
        $script:targetDomain = $defaultDomain
        Write-Host "Using default domain: $defaultDomain" -ForegroundColor Green
    }
    "3" {
        $script:targetDomain = Read-Host "Enter domain name"
        Write-Host "Using custom domain: $script:targetDomain" -ForegroundColor Cyan
    }
    default {
        if ($detectedDomain -eq "Not domain-joined (WORKGROUP)" -or $detectedDomain -eq "Unable to detect") {
            Write-Host "Cannot use detected domain. Using default: $defaultDomain" -ForegroundColor Yellow
            $script:targetDomain = $defaultDomain
        } else {
            $script:targetDomain = $detectedDomain
            Write-Host "Using detected domain: $script:targetDomain" -ForegroundColor Green
        }
    }
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Import all function modules
Write-Host "Loading modules..." -ForegroundColor Cyan
Import-Module (Join-Path $functionsPath "Helpers.ps1") -Force
Import-Module (Join-Path $functionsPath "PC-Management.ps1") -Force
Import-Module (Join-Path $functionsPath "Web-Blocking.ps1") -Force
Import-Module (Join-Path $functionsPath "Utilities.ps1") -Force
Import-Module (Join-Path $functionsPath "Lab-Monitoring.ps1") -Force
Write-Host "Modules loaded successfully!" -ForegroundColor Green
Write-Host ""

# Main Menu Function
# Main Menu Function
function Show-Menu {
    Clear-Host
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "     PC MANAGEMENT SYSTEM - MAIN MENU       " -ForegroundColor Cyan
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "Active Domain: " -NoNewline
    if ($script:targetDomain -eq "csitlab.local") {
        Write-Host "$script:targetDomain" -ForegroundColor Green
    } else {
        Write-Host "$script:targetDomain" -ForegroundColor Red
    }
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "█ PC MANAGEMENT" -ForegroundColor Green
    Write-Host "  1.  Get status of ALL PCs (PC-1 to PC-35)"
    Write-Host "  2.  Shutdown a single PC"
    Write-Host "  3.  Shutdown a range of PCs"
    Write-Host "  4.  Shutdown ALL PCs (PC-1 to PC-35)"
    Write-Host "  5.  Restart a single PC"
    Write-Host "  6.  Restart a range of PCs"
    Write-Host "  7.  Restart ALL PCs (PC-1 to PC-35)"
    Write-Host "  8.  Execute Custom PowerShell Command"
    Write-Host ""
    Write-Host "█ WEB & PROTOCOL BLOCKING (HOSTS + Firewall TCP/UDP + Anti-DoH)" -ForegroundColor Yellow
    Write-Host "  9.  Block ALL Categories (Single, Range, or ALL PCs)"
    Write-Host "  10. Block AI Sites ONLY (Single, Range, or ALL PCs)"
    Write-Host "  11. Block Social Media ONLY (Single, Range, or ALL PCs)"
    Write-Host "  12. Block AI + Social Media [Focus Mode] (Single, Range, or ALL PCs)"
    Write-Host "  13. Unblock Web Access / Restore All (Single, Range, or ALL PCs)"
    Write-Host "  14. Deep Scan & Security Audit (Audit all 35 PCs)"
    Write-Host "  15. View Block Lists & Category Statistics"
    Write-Host ""
    Write-Host "█ UTILITIES & LAB MAINTENANCE" -ForegroundColor Magenta
    Write-Host "  16. Sync Time/Date/Timezone to ALL PCs from Server"
    Write-Host "  17. Clean up backup hosts files on ALL PCs"
    Write-Host "  18. View all PC hosts files"
    Write-Host "  19. Export MySQL Database (Single, Range, or ALL PCs)"
    Write-Host "  20. Check/Fix Android & Java Environment Variables"
    Write-Host "  21. Clean Temporary Files (Single, Range, or ALL PCs)"
    Write-Host ""
    Write-Host "=============================================" -ForegroundColor Cyan
    Write-Host "Type 'clear' to clear screen | Type 'exit' to quit" -ForegroundColor DarkGray
    Write-Host "=============================================" -ForegroundColor Cyan
}

# Ask for credentials once (must have admin rights on all target PCs)
Write-Host "===== DOMAIN-SPECIFIC WEB BLOCKING SYSTEM =====" -ForegroundColor Cyan
Write-Host "Active Domain: " -NoNewline
if ($script:targetDomain -eq $defaultDomain) {
    Write-Host "$script:targetDomain" -ForegroundColor Green
} else {
    Write-Host "$script:targetDomain" -ForegroundColor Red
}
Write-Host ""
Write-Host "CREDENTIAL OPTIONS:" -ForegroundColor Yellow
Write-Host "  1. Use default credentials (Administrator)" -ForegroundColor Green
Write-Host "  2. Enter custom credentials" -ForegroundColor Cyan
Write-Host ""

$credChoice = Read-Host "Select option (1-2) [Press Enter for default]"

if ($credChoice -eq "2") {
    Write-Host ""
    Write-Host "Please enter admin credentials for domain PCs:" -ForegroundColor Yellow
    $script:cred = Get-Credential -Message "Enter domain credentials for $script:targetDomain"
    
    if ($null -eq $script:cred) {
        Write-Host "Credential input cancelled. Exiting..." -ForegroundColor Red
        exit
    }
} else {
    # Use default credentials
    Write-Host ""
    Write-Host "Using default credentials..." -ForegroundColor Green
    $defaultUsername = "Administrator"
    $defaultPassword = ConvertTo-SecureString "@csitlab123" -AsPlainText -Force
    $script:cred = New-Object System.Management.Automation.PSCredential($defaultUsername, $defaultPassword)
    Write-Host "  Username: $defaultUsername" -ForegroundColor Gray
}

Write-Host ""

# Credential validation loop
$credentialValid = $false
$attemptCount = 0
$maxAttempts = 3

while (-not $credentialValid -and $attemptCount -lt $maxAttempts) {
    $attemptCount++
    
    if ($attemptCount -gt 1) {
        Write-Host ""
        Write-Host "Attempt $attemptCount of $maxAttempts" -ForegroundColor Yellow
        Write-Host "Please enter admin credentials for domain PCs:" -ForegroundColor Yellow
        $script:cred = Get-Credential -Message "Enter domain credentials for $script:targetDomain"
        
        if ($null -eq $script:cred) {
            Write-Host "Credential input cancelled. Exiting..." -ForegroundColor Red
            exit
        }
        Write-Host ""
    }
    
    Write-Host "Validating credentials against domain..." -ForegroundColor Cyan
    
    # Test credentials by attempting to access a domain resource
    try {
        # Try to get domain information using the provided credentials
        $username = $script:cred.UserName
        $password = $script:cred.GetNetworkCredential().Password
        
        # Create DirectoryEntry to test authentication
        Add-Type -AssemblyName System.DirectoryServices.AccountManagement
        $contextType = [System.DirectoryServices.AccountManagement.ContextType]::Domain
        
        try {
            $principalContext = New-Object System.DirectoryServices.AccountManagement.PrincipalContext($contextType, $script:targetDomain)
            $credentialValid = $principalContext.ValidateCredentials($username, $password)
            $principalContext.Dispose()
        } catch {
            # If domain validation fails, try alternative method - test WinRM connection to localhost or a known PC
            try {
                $testResult = Invoke-Command -ComputerName $env:COMPUTERNAME -Credential $script:cred -ScriptBlock { $env:COMPUTERNAME } -ErrorAction Stop
                $credentialValid = $true
            } catch {
                $credentialValid = $false
            }
        }
        
        if ($credentialValid) {
            Write-Host "✓ Credentials validated successfully!" -ForegroundColor Green
            Write-Host "  Username: $username" -ForegroundColor Gray
            Write-Host "  Domain: $script:targetDomain" -ForegroundColor Gray
        } else {
            Write-Host "✗ Authentication failed - Invalid username or password" -ForegroundColor Red
            
            if ($attemptCount -lt $maxAttempts) {
                Write-Host "Please try again..." -ForegroundColor Yellow
            }
        }
        
    } catch {
        Write-Host "✗ Credential validation error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($attemptCount -lt $maxAttempts) {
            Write-Host "Please try again..." -ForegroundColor Yellow
        }
    }
}

if (-not $credentialValid) {
    Write-Host ""
    Write-Host "Failed to authenticate after $maxAttempts attempts." -ForegroundColor Red
    Write-Host "Please verify:" -ForegroundColor Yellow
    Write-Host "  1. Username format is correct (e.g., domain\username or username@domain.com)" -ForegroundColor Gray
    Write-Host "  2. Password is correct" -ForegroundColor Gray
    Write-Host "  3. Account has necessary permissions" -ForegroundColor Gray
    Write-Host "  4. Domain '$script:targetDomain' is accessible" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Exiting..." -ForegroundColor Red
    exit
}

Write-Host ""

# Load block lists from BlockLists folder
Write-Host "Loading block lists from: $blockListsFolder" -ForegroundColor Cyan

$script:blockedSites = @()
$script:blockListStats = @{}
$script:aiSitesOnly = @()
$script:socialSitesOnly = @()

# Check if BlockLists folder exists
if (Test-Path $blockListsFolder) {
    $blockFiles = Get-ChildItem -Path $blockListsFolder -Filter "*.txt" | Where-Object { 
        $_.Name -notin @("README.txt", "QUICK-REFERENCE.txt", "SITE-LIST.txt") 
    }
    
    foreach ($file in $blockFiles) {
        Write-Host "  Loading: $($file.Name)" -ForegroundColor Gray
        $sites = Get-Content $file.FullName | Where-Object { 
            $_ -notmatch "^#" -and $_ -notmatch "^\s*$" 
        }
        
        $blockListStats[$file.BaseName] = $sites.Count
        $blockedSites += $sites
        
        if ($file.BaseName -eq "ai-sites") {
            $script:aiSitesOnly = $sites
            Write-Host "    Added $($sites.Count) AI sites (available for AI-only blocking)" -ForegroundColor DarkCyan
        } elseif ($file.BaseName -eq "social-media") {
            $script:socialSitesOnly = $sites
            Write-Host "    Added $($sites.Count) Social Media sites (available for Social-only blocking)" -ForegroundColor DarkCyan
        } else {
            Write-Host "    Added $($sites.Count) sites from $($file.Name)" -ForegroundColor DarkGray
        }
    }
    
    Write-Host ""
    Write-Host "BLOCK LIST SUMMARY:" -ForegroundColor Yellow
    foreach ($category in $blockListStats.Keys) {
        Write-Host "  $category`: $($blockListStats[$category]) sites" -ForegroundColor White
    }
    Write-Host "  TOTAL SITES TO BLOCK: $($blockedSites.Count)" -ForegroundColor Green
} else {
    Write-Host "WARNING: BlockLists folder not found at $blockListsFolder" -ForegroundColor Red
    Write-Host "Creating default block list..." -ForegroundColor Yellow
    
    $blockedSites = @(
        "www.google.com", "google.com",
        "www.facebook.com", "facebook.com",
        "www.youtube.com", "youtube.com",
        "www.twitter.com", "twitter.com"
    )
}

Write-Host ""
Write-Host "IMPORTANT: Operations will only apply to PCs in the '$script:targetDomain' domain!" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to continue to main menu..." -ForegroundColor Cyan
$null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

        # Main program loop
do {
    Show-Menu
    $choice = Read-Host "Enter your choice (1-21, 'clear', or 'exit')"

    # Handle special commands
    if ($choice -eq 'exit') {
        Write-Host "Exiting..." -ForegroundColor Yellow
        break
    }
    
    if ($choice -eq 'clear') {
        continue  # Just clears and redraws menu
    }

    switch ($choice) {
        # ===== PC MANAGEMENT =====
        '1' {
            $targets = foreach ($i in 1..35) { "PC-$i" }
            Get-AllPCStatus -Targets $targets
        }
        '2' {
            $pc = Read-Host "Enter the PC name (e.g., PC-1)"
            $targets = @($pc)
            Invoke-PCShutdown -Targets $targets
        }
        '3' {
            $start = Read-Host "Enter start number (e.g., 5)"
            $end   = Read-Host "Enter end number (e.g., 10)"
            $targets = foreach ($i in $start..$end) { "PC-$i" }
            Invoke-PCShutdown -Targets $targets
        }
        '4' {
            $targets = foreach ($i in 1..35) { "PC-$i" }
            Invoke-PCShutdown -Targets $targets
        }
        '5' {
            $pc = Read-Host "Enter the PC name (e.g., PC-1)"
            $targets = @($pc)
            Invoke-PCRestart -Targets $targets
        }
        '6' {
            $start = Read-Host "Enter start number (e.g., 5)"
            $end   = Read-Host "Enter end number (e.g., 10)"
            $targets = foreach ($i in $start..$end) { "PC-$i" }
            Invoke-PCRestart -Targets $targets
        }
        '7' {
            $targets = foreach ($i in 1..35) { "PC-$i" }
            Invoke-PCRestart -Targets $targets
        }
        '8' {
            Write-Host ""
            Write-Host "Execute Custom PowerShell Command" -ForegroundColor Cyan
            Write-Host "Options:" -ForegroundColor Yellow
            Write-Host "  1. Execute on specific PC"
            Write-Host "  2. Execute on range of PCs"
            Write-Host "  3. Execute on ALL PCs (PC-1 to PC-35)"
            Write-Host ""
            
            $execChoice = Read-Host "Select option (1-3)"
            
            switch ($execChoice) {
                '1' {
                    $pc = Read-Host "Enter PC name (e.g., PC-1)"
                    Write-Host ""
                    Write-Host "Enter PowerShell command to execute:" -ForegroundColor Yellow
                    $command = Read-Host
                    
                    if (-not [string]::IsNullOrWhiteSpace($command)) {
                        $targets = @($pc)
                        Invoke-CustomPSCommand -Targets $targets -Command $command
                    } else {
                        Write-Host "No command provided." -ForegroundColor Red
                    }
                }
                '2' {
                    $start = Read-Host "Enter start number (e.g., 5)"
                    $end   = Read-Host "Enter end number (e.g., 10)"
                    Write-Host ""
                    Write-Host "Enter PowerShell command to execute:" -ForegroundColor Yellow
                    $command = Read-Host
                    
                    if (-not [string]::IsNullOrWhiteSpace($command)) {
                        $targets = foreach ($i in $start..$end) { "PC-$i" }
                        Invoke-CustomPSCommand -Targets $targets -Command $command
                    } else {
                        Write-Host "No command provided." -ForegroundColor Red
                    }
                }
                '3' {
                    Write-Host ""
                    Write-Host "Enter PowerShell command to execute:" -ForegroundColor Yellow
                    $command = Read-Host
                    
                    if (-not [string]::IsNullOrWhiteSpace($command)) {
                        $targets = foreach ($i in 1..35) { "PC-$i" }
                        Invoke-CustomPSCommand -Targets $targets -Command $command
                    } else {
                        Write-Host "No command provided." -ForegroundColor Red
                    }
                }
                default {
                    Write-Host "Invalid choice" -ForegroundColor Red
                }
            }
        }
        
        # ===== WEB & PROTOCOL BLOCKING =====
        '9' {
            $targets = Get-TargetSelection -ActionTitle "Block ALL Categories"
            if ($targets) { Invoke-WebBlocking -Targets $targets -BlockedSites $blockedSites -CategoryName "ALL CATEGORIES" }
        }
        '10' {
            $targets = Get-TargetSelection -ActionTitle "Block AI Sites ONLY"
            if ($targets) { Invoke-AIBlocking -Targets $targets -AISites $aiSitesOnly }
        }
        '11' {
            $targets = Get-TargetSelection -ActionTitle "Block Social Media ONLY"
            if ($targets) { Invoke-SocialMediaBlocking -Targets $targets -SocialSites $socialSitesOnly }
        }
        '12' {
            $targets = Get-TargetSelection -ActionTitle "Block AI + Social Media [Focus Mode]"
            if ($targets) { Invoke-FocusModeBlocking -Targets $targets -AISites $aiSitesOnly -SocialSites $socialSitesOnly }
        }
        '13' {
            $targets = Get-TargetSelection -ActionTitle "Unblock Web Access"
            if ($targets) { Invoke-WebUnblocking -Targets $targets }
        }
        '14' {
            Invoke-DeepScan
        }
        '15' {
            Show-BlockLists -BlockedSites $blockedSites -BlockListsFolder $blockListsFolder
        }
        
        # ===== UTILITIES & LAB MAINTENANCE =====
        '16' {
            Sync-TimeToAllPCs
        }
        '17' {
            Invoke-BackupCleanup
        }
        '18' {
            Show-AllHostsFiles
        }
        '19' {
            $targets = Get-TargetSelection -ActionTitle "Export MySQL Databases"
            if ($targets) {
                $desc = if ($targets.Count -eq 1) { "Single PC: $($targets[0])" } elseif ($targets.Count -ge 35) { "ALL PCs" } else { "Range of $($targets.Count) PCs" }
                Export-MySQLDatabases -Targets $targets -ExportType $desc -ScriptPath $scriptPath
            }
        }
        '20' {
            Test-AndroidJavaEnvironment
        }
        '21' {
            $targets = Get-TargetSelection -ActionTitle "Clean Temporary Files"
            if ($targets) { Clear-TempFiles -Targets $targets }
        }
        
        # ===== BACKWARD-COMPATIBLE ALIASES =====
        { $_ -in '22', '23' } {
            $targets = Get-TargetSelection -ActionTitle "Export MySQL Databases"
            if ($targets) {
                $desc = if ($targets.Count -eq 1) { "Single PC: $($targets[0])" } elseif ($targets.Count -ge 35) { "ALL PCs" } else { "Range of $($targets.Count) PCs" }
                Export-MySQLDatabases -Targets $targets -ExportType $desc -ScriptPath $scriptPath
            }
        }
        '24' {
            Test-AndroidJavaEnvironment
        }
        { $_ -in '25', '26', '27' } {
            $targets = Get-TargetSelection -ActionTitle "Clean Temporary Files"
            if ($targets) { Clear-TempFiles -Targets $targets }
        }
        
        default {
            Write-Host "Invalid choice. Try again..." -ForegroundColor Red
            Start-Sleep -Seconds 2
            continue
        }
    }

} while ($choice -ne 'exit')

Write-Host ""
Write-Host "Thank you for using PC Management System!" -ForegroundColor Cyan
Write-Host ""
