# Application Flow Diagram

## Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    WinServer2019.exe                         │
│                      (Program.cs)                            │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Application Start
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                      LoginForm.cs                            │
│  ┌──────────────────────────────────────────────────┐      │
│  │  ┌────────────────────────────────────┐          │      │
│  │  │   Domain Auto-Detection             │          │      │
│  │  │   • Detect current domain           │          │      │
│  │  │   • Default: csitlab.local          │          │      │
│  │  └────────────────────────────────────┘          │      │
│  │                                                    │      │
│  │  ┌────────────────────────────────────┐          │      │
│  │  │   Credential Input                  │          │      │
│  │  │   • Username (or default)           │          │      │
│  │  │   • Password (or default)           │          │      │
│  │  │   • Domain                          │          │      │
│  │  └────────────────────────────────────┘          │      │
│  │                                                    │      │
│  │  ┌────────────────────────────────────┐          │      │
│  │  │   Active Directory Validation       │          │      │
│  │  │   • PrincipalContext                │          │      │
│  │  │   • ValidateCredentials()           │          │      │
│  │  └────────────────────────────────────┘          │      │
│  └──────────────────────────────────────────────────┘      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Login Successful (DialogResult.OK)
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                     MainActivity.cs                          │
│  ┌──────────────────────────────────────────────────┐      │
│  │              Constructor Receives:                │      │
│  │              • Username                           │      │
│  │              • Password                           │      │
│  │              • Domain                             │      │
│  └──────────────────────────────────────────────────┘      │
│                                                              │
│  ┌──────────────────────────────────────────────────┐      │
│  │         Initialize PowerShellExecutor             │      │
│  │         • Create runspace                         │      │
│  │         • Load PowerShell modules                 │      │
│  │         • Set credentials                         │      │
│  │         • Load block lists                        │      │
│  └──────────────────────────────────────────────────┘      │
│                                                              │
│  ┌──────────────────────────────────────────────────┐      │
│  │             Main User Interface                   │      │
│  │  ┌────────────────────────────────────────┐      │      │
│  │  │  Tab 1: PC Management                  │      │      │
│  │  │  • Get Status                          │      │      │
│  │  │  • Shutdown (Single/Range/All)         │      │      │
│  │  │  • Restart (Single/Range/All)          │      │      │
│  │  │  • Custom Command                      │      │      │
│  │  └────────────────────────────────────────┘      │      │
│  │                                                    │      │
│  │  ┌────────────────────────────────────────┐      │      │
│  │  │  Tab 2: Web Blocking                   │      │      │
│  │  │  • Block (Single/Range/All)            │      │      │
│  │  │  • Unblock (Single/Range/All)          │      │      │
│  │  │  • Deep Scan                           │      │      │
│  │  │  • View Block Lists                    │      │      │
│  │  │  • Block AI Sites Only                 │      │      │
│  │  └────────────────────────────────────────┘      │      │
│  │                                                    │      │
│  │  ┌────────────────────────────────────────┐      │      │
│  │  │  Tab 3: Utilities                      │      │      │
│  │  │  • Sync Time                           │      │      │
│  │  │  • Clean Backup Files                  │      │      │
│  │  │  • View Hosts Files                    │      │      │
│  │  │  • Export MySQL DB                     │      │      │
│  │  │  • Check Environment Variables         │      │      │
│  │  │  • Clean Temp Files                    │      │      │
│  │  └────────────────────────────────────────┘      │      │
│  │                                                    │      │
│  │  ┌────────────────────────────────────────┐      │      │
│  │  │  Output Console (RichTextBox)          │      │      │
│  │  │  • Real-time command output            │      │      │
│  │  │  • Color-coded messages                │      │      │
│  │  │  • Timestamped logs                    │      │      │
│  │  └────────────────────────────────────────┘      │      │
│  └──────────────────────────────────────────────────┘      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ User Action (Button Click)
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                PowerShellExecutor.cs                         │
│  ┌──────────────────────────────────────────────────┐      │
│  │         ExecuteCommand(string command)            │      │
│  │                                                    │      │
│  │  1. Create PowerShell instance                    │      │
│  │  2. Add script/command                            │      │
│  │  3. Execute with credentials                      │      │
│  │  4. Collect output                                │      │
│  │  5. Collect errors                                │      │
│  │  6. Return PowerShellExecutionResult              │      │
│  └──────────────────────────────────────────────────┘      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Commands executed via
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   PowerShell Scripts                         │
│                     (Scripts folder)                         │
│  ┌──────────────────────────────────────────────────┐      │
│  │              Main.ps1 (imported)                  │      │
│  │              • Domain configuration               │      │
│  │              • Credential setup                   │      │
│  │              • Variable initialization            │      │
│  └──────────────────────────────────────────────────┘      │
│                                                              │
│  ┌──────────────────────────────────────────────────┐      │
│  │            Functions/ (modules)                   │      │
│  │              • Helpers.ps1                        │      │
│  │              • PC-Management.ps1                  │      │
│  │              • Web-Blocking.ps1                   │      │
│  │              • Utilities.ps1                      │      │
│  │              • Lab-Monitoring.ps1                 │      │
│  └──────────────────────────────────────────────────┘      │
│                                                              │
│  ┌──────────────────────────────────────────────────┐      │
│  │            BlockLists/ (site lists)               │      │
│  │              • ai-sites.txt                       │      │
│  │              • gaming-sites.txt                   │      │
│  │              • social-media.txt                   │      │
│  │              • video-sites.txt                    │      │
│  │              • shopping-entertainment.txt         │      │
│  │              • search-engines.txt                 │      │
│  └──────────────────────────────────────────────────┘      │
└──────────────────────┬──────────────────────────────────────┘
                       │
                       │ Remote execution on
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              Domain PCs (PC-1 to PC-35)                      │
│                   csitlab.local                              │
│                                                              │
│  • Receive PowerShell commands via WinRM/PSRemoting         │
│  • Execute with provided credentials                        │
│  • Return results to server                                 │
└─────────────────────────────────────────────────────────────┘
```

## Data Flow

### Authentication Flow

```
User Input → LoginForm → PrincipalContext (AD) → Validation
                              ↓
                      Success: Open MainActivity
                      Failure: Show error, retry
```

### Command Execution Flow

```
Button Click → MainActivity Event Handler → PowerShellExecutor
                                                    ↓
                                    Create PowerShell Instance
                                                    ↓
                                    Load Script/Command
                                                    ↓
                                    Execute with Credentials
                                                    ↓
                              Remote PC via PSRemoting/WinRM
                                                    ↓
                              Collect Output & Errors
                                                    ↓
                              Return to MainActivity
                                                    ↓
                              Display in Output Console
```

## Component Relationships

```
Program.cs
    │
    ├─→ LoginForm.cs
    │       │
    │       ├─ System.DirectoryServices.AccountManagement
    │       └─ PrincipalContext (validates domain credentials)
    │
    └─→ MainActivity.cs
            │
            ├─→ PowerShellExecutor.cs
            │       │
            │       ├─ System.Management.Automation
            │       ├─ Runspace (persistent PowerShell environment)
            │       └─ PSCredential (secure credential handling)
            │
            └─→ PowerShell Scripts
                    │
                    ├─ Main.ps1 (configuration)
                    ├─ Functions/*.ps1 (operations)
                    └─ BlockLists/*.txt (site lists)
```

## Security Architecture

```
┌─────────────────────────────────────────┐
│         User Credentials                 │
│   (entered in LoginForm)                 │
└──────────────┬──────────────────────────┘
               │
               │ Validated against
               ▼
┌─────────────────────────────────────────┐
│      Active Directory (AD)               │
│      Domain: csitlab.local               │
└──────────────┬──────────────────────────┘
               │
               │ If valid, stored as
               ▼
┌─────────────────────────────────────────┐
│      PSCredential Object                 │
│   (SecureString password)                │
└──────────────┬──────────────────────────┘
               │
               │ Passed to
               ▼
┌─────────────────────────────────────────┐
│    PowerShell Runspace                   │
│   (executes with user context)           │
└──────────────┬──────────────────────────┘
               │
               │ Remote execution via
               ▼
┌─────────────────────────────────────────┐
│         WinRM/PSRemoting                 │
│    (encrypted communication)             │
└──────────────┬──────────────────────────┘
               │
               │ Executes on
               ▼
┌─────────────────────────────────────────┐
│      Target Domain PCs                   │
│      (PC-1 to PC-35)                     │
└─────────────────────────────────────────┘
```

## File Structure

```
WinServer2019_Admin/
│
├── Application Files
│   ├── LoginForm.cs              ← Login & authentication
│   ├── MainActivity.cs            ← Main interface & logic
│   ├── MainActivity.Designer.cs   ← UI design (auto-generated)
│   ├── PowerShellExecutor.cs      ← PowerShell integration
│   ├── Program.cs                 ← Entry point
│   └── WinServer2019.csproj       ← Project configuration
│
├── Configuration
│   ├── App.config                 ← Application settings
│   └── Properties/
│       ├── AssemblyInfo.cs
│       └── Resources.resx
│
├── Documentation
│   ├── README-UI.md               ← Full documentation
│   ├── SETUP-GUIDE.md             ← Setup instructions
│   └── APPLICATION-FLOW.md        ← This file
│
└── Scripts/                       ← PowerShell scripts
    ├── Main.ps1                   ← Main script config
    ├── Functions/                 ← Function modules
    │   ├── Helpers.ps1
    │   ├── PC-Management.ps1
    │   ├── Web-Blocking.ps1
    │   ├── Utilities.ps1
    │   └── Lab-Monitoring.ps1
    └── BlockLists/                ← Website blocklists
        ├── ai-sites.txt
        ├── gaming-sites.txt
        ├── social-media.txt
        └── ...
```

## Technology Stack

```
┌───────────────────────────────────────────────┐
│            User Interface Layer               │
│                                               │
│  • Windows Forms (.NET Framework 4.8.1)      │
│  • Custom UI Controls                         │
│  • TabControl, RichTextBox, Buttons          │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│          Business Logic Layer                 │
│                                               │
│  • C# Event Handlers                          │
│  • Input Validation                           │
│  • Error Handling                             │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│         Integration Layer                     │
│                                               │
│  • PowerShellExecutor Class                   │
│  • System.Management.Automation               │
│  • Runspace Management                        │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│         Scripting Layer                       │
│                                               │
│  • PowerShell 5.1+                            │
│  • Custom Modules                             │
│  • Remote Execution (PSRemoting)              │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│      Authentication & Directory Layer         │
│                                               │
│  • Active Directory (AD)                      │
│  • System.DirectoryServices                   │
│  • WinRM/Kerberos                            │
└──────────────────┬────────────────────────────┘
                   │
┌──────────────────▼────────────────────────────┐
│         Target Infrastructure                 │
│                                               │
│  • Windows Server 2019                        │
│  • Domain: csitlab.local                      │
│  • PCs: PC-1 to PC-35                         │
└───────────────────────────────────────────────┘
```

---

**Last Updated**: November 2025
**Version**: 1.0.0
**Author**: Christian Franc M. Carvajal (Koykoy200078)
