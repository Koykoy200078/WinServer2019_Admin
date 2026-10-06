# Computer Laboratory Management & Web Blocking System

**Domain**: `csitlab.local`  
**Target Workstations**: `PC-1` through `PC-35` (`192.168.2.11` – `192.168.2.45`)  
**Server / Domain Controller**: `PC-36` (`192.168.2.45`)  

---

## 📁 Directory Structure

```
Scripts/
├── Main.ps1                  # Interactive Lab Management & Web Blocking Console
├── Build-All.ps1             # Solution builder (Server & Monitoring Client)
├── Deploy-Standalone.ps1     # Remote installation & deployment to workstations
├── Diagnose-Remote.ps1       # Workstation diagnostic & health checker
├── Check-Deployment.ps1      # Verifies client file installation & running processes
├── Sync-DomainTime.ps1       # Timezone & clock synchronization
├── Test-Client.ps1           # Rapid connection & responsiveness testing
├── BlockLists/               # Curated domain blocklists (2,070+ domains)
│   ├── ai-sites.txt          # AI models, chat interfaces, coding assistants & APIs (298)
│   ├── social-media.txt      # Social networks, messaging & streaming platforms (150)
│   ├── video-sites.txt       # Video streaming & entertainment (434)
│   ├── gaming-sites.txt      # Gaming platforms, game servers & web games (454)
│   ├── shopping-entertainment.txt # E-commerce & lifestyle (459)
│   ├── search-engines.txt    # Public search engines (276)
│   └── View-BlockLists.ps1   # Quick viewer tool
├── Functions/                # Modular engine functions
│   ├── Helpers.ps1           # Target selection, CimInstance queries, status auditing
│   ├── PC-Management.ps1     # Remote shutdown, restart, and custom PS commands
│   ├── Web-Blocking.ps1      # 4-layer blocking engine (HOSTS, Firewall, Anti-DoH, DNS)
│   ├── Utilities.ps1         # Time sync, MySQL dump, temp file cleaner, Java/Android
│   └── Lab-Monitoring.ps1    # Real-time lab monitoring utilities
└── docs/                     # Guides, reference manuals & archived legacy files
```

---

## 🛡️ Multi-Layer Web & Protocol Blocking

Websites and AI tools attempt to bypass traditional `hosts` file blocking via Secure DNS (DNS-over-HTTPS / DoH) and QUIC (UDP 443). The blocking engine enforces **4 distinct layers** of defense:

1. **Layer 1: Dual Sinkholing in HOSTS**
   - Redirects all target domains and their `www.` subdomains to both `0.0.0.0` and `127.0.0.1` for immediate connection refusal.
2. **Layer 2: Anti-DoH Browser Registry Policies**
   - Configures Edge (`HKLM:\SOFTWARE\Policies\Microsoft\Edge`), Chrome (`HKLM:\SOFTWARE\Policies\Google\Chrome`), and Firefox (`HKLM:\SOFTWARE\Policies\Mozilla\Firefox`) with `DnsOverHttpsMode = "off"`, preventing browsers from silently bypassing the system hosts file.
3. **Layer 3: Outbound Windows Firewall Rules**
   - Blocks outbound TCP and UDP on port 443 to known public DoH resolvers (`1.1.1.1`, `8.8.8.8`, `9.9.9.9`, `208.67.222.222`, etc.).
   - Blocks DNS-over-TLS (port 853) and QUIC protocol (UDP port 443) to force standard DNS resolution that honors the system hosts file.
4. **Layer 4: Cache Flush & Browser Enforcement**
   - Flushes client DNS cache (`Clear-DnsClientCache`) and gracefully terminates stray browser sessions (`msedge`, `chrome`, `firefox`) to ensure immediate effect.

---

## 🚀 Usage

Launch PowerShell (PowerShell 5.1 or PowerShell 7+ as Administrator):

```powershell
.\Main.ps1
```

### Menu Overview:
- **1–8: PC Management**: Status, remote shutdown, reboot (single, range, all), custom PowerShell commands.
- **9–15: Web & Protocol Blocking**:
  - `9`: Block ALL Categories (Single, Range, ALL)
  - `10`: Block AI Sites ONLY (Single, Range, ALL)
  - `11`: Block Social Media ONLY (Single, Range, ALL)
  - `12`: Block AI + Social Media [Focus Mode] (Single, Range, ALL)
  - `13`: Unblock Web Access / Restore All (Single, Range, ALL)
  - `14`: Deep Scan & Security Audit across all 35 workstations
  - `15`: View Block Lists & Category Statistics
- **16–21: Utilities & Maintenance**: Clock sync, hosts backup cleanup, MySQL export, Java/Android env check, temporary file cleaner.
