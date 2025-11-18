# PC Management System - Windows Forms Application

## Overview

This is a Windows Forms application built with C# .NET Framework 4.8.1 that provides a graphical user interface for managing PCs in the **csitlab.local** domain (PC-1 to PC-35).

The application wraps the PowerShell scripts from the `Scripts` folder and provides an intuitive interface for:

- PC Management (shutdown, restart, status checks)
- Web Blocking (DNS/hosts file management)
- System Utilities (time sync, temp cleanup, MySQL export)

## Features

### 🔐 Login System

- **Domain credential verification** using Active Directory
- Auto-detects current domain (defaults to csitlab.local)
- Option to use default credentials or custom credentials
- Validates credentials before allowing access to main interface

### 🖥️ PC Management Tab

- Get status of all PCs (PC-1 to PC-35)
- Shutdown single PC, range of PCs, or all PCs
- Restart single PC, range of PCs, or all PCs
- Execute custom PowerShell commands

### 🌐 Web Blocking Tab

- Block/Unblock web access on single PC, range, or all PCs
- Deep scan to check blocking status across all PCs
- View current block lists
- Block AI sites only on all PCs

### 🛠️ Utilities Tab

- Sync time/date/timezone to all PCs
- Clean backup hosts files
- View all PC hosts files
- Export MySQL databases from PCs
- Check/Fix Android & Java environment variables
- Clean temporary files on PCs

### 📊 Real-time Output Console

- Black console-style output window
- Color-coded messages (green for success, red for errors, yellow for warnings)
- Timestamped log entries
- Auto-scrolling output

## Requirements

### System Requirements

- Windows Server 2019 or Windows 10/11
- .NET Framework 4.8.1
- PowerShell 5.1 or higher
- Administrator privileges on domain PCs

### NuGet Packages

The application uses the following assemblies:

- `System.Management.Automation` (PowerShell SDK)
- `System.DirectoryServices.AccountManagement` (Active Directory)
- `System.Windows.Forms`

## Installation

1. **Clone or extract the project** to your desired location
2. **Ensure the Scripts folder structure is intact**:

   ```
   WinServer2019_Admin/
   ├── Scripts/
   │   ├── Main.ps1
   │   ├── Functions/
   │   │   ├── Helpers.ps1
   │   │   ├── PC-Management.ps1
   │   │   ├── Web-Blocking.ps1
   │   │   ├── Utilities.ps1
   │   │   └── Lab-Monitoring.ps1
   │   └── BlockLists/
   │       ├── ai-sites.txt
   │       ├── gaming-sites.txt
   │       ├── social-media.txt
   │       └── ...
   ```

3. **Build the project** in Visual Studio or using MSBuild:

   ```powershell
   msbuild WinServer2019.csproj /p:Configuration=Release
   ```

4. **Copy the Scripts folder** to the same directory as the compiled executable:
   ```
   bin/Release/
   ├── WinServer2019.exe
   └── Scripts/
   ```

## Usage

### First Launch

1. Run `WinServer2019.exe`
2. The **Login Form** will appear
3. Enter your domain credentials or use default credentials
4. The system will auto-detect your domain (or use csitlab.local as default)
5. Click **Login** to authenticate

### Default Credentials

- **Username**: Administrator
- **Password**: @csitlab123
- **Domain**: csitlab.local

### Main Interface

After successful login, you'll see:

- Three tabs: **PC Management**, **Web Blocking**, **Utilities**
- A real-time output console on the right
- Status bar at the bottom showing current operation status
- Domain and username displayed at the top

### Example Operations

#### Shutdown a Range of PCs

1. Go to **PC Management** tab
2. Click **Shutdown Range of PCs**
3. Enter start PC number (e.g., 1)
4. Enter end PC number (e.g., 10)
5. Monitor progress in the output console

#### Block Web Access on All PCs

1. Go to **Web Blocking** tab
2. Click **Block Web/DNS - ALL PCs**
3. Confirm the action
4. Watch the console for real-time progress

## Architecture

### Components

#### `LoginForm.cs`

- Handles domain credential authentication
- Auto-detects current domain
- Validates credentials using `PrincipalContext`
- Provides option for default or custom credentials

#### `MainActivity.cs`

- Main application window with tabbed interface
- Event handlers for all operations
- Real-time output console
- Status bar for operation feedback
- Integration with PowerShell executor

#### `PowerShellExecutor.cs`

- Manages PowerShell runspace
- Loads and executes PowerShell scripts
- Handles credential passing to remote PCs
- Collects output, errors, and warnings
- Provides async execution capabilities

#### `Program.cs`

- Application entry point
- Handles login flow
- Passes credentials to main activity

### PowerShell Integration

The application creates a persistent PowerShell runspace that:

1. Imports all function modules from `Scripts/Functions/`
2. Loads block lists from `Scripts/BlockLists/`
3. Sets up domain credentials
4. Maintains script variables across commands

## Configuration

### Domain Configuration

To change the default domain, edit in `LoginForm.cs`:

```csharp
txtDomain.Text = "yourdomain.local"; // Change default domain
```

### Default Credentials

To change default credentials, edit in `LoginForm.cs`:

```csharp
txtUsername.Text = "YourAdmin";
txtPassword.Text = "YourPassword";
```

### PowerShell Script Path

The application expects scripts in a `Scripts` folder relative to the executable. To change this, modify in `MainActivity.cs`:

```csharp
scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
```

## Troubleshooting

### "System.Management.Automation not found"

- Install PowerShell SDK or reference the DLL from:
  `C:\Program Files (x86)\Reference Assemblies\Microsoft\WindowsPowerShell\3.0\System.Management.Automation.dll`

### "Invalid credentials or domain not accessible"

- Verify domain name is correct
- Ensure account has admin rights on target PCs
- Check network connectivity to domain controller

### "Scripts folder not found"

- Ensure the `Scripts` folder is in the same directory as the executable
- Check that all PowerShell scripts are present

### PowerShell execution errors

- Verify PowerShell execution policy: `Set-ExecutionPolicy Bypass -Scope Process`
- Check that function modules are properly formatted
- Ensure target PCs are online and accessible

## Security Notes

⚠️ **Important Security Considerations**:

- Store credentials securely (consider using Windows Credential Manager)
- Use least-privilege accounts when possible
- Audit all operations for compliance
- Encrypted credentials are recommended for production use

## Author

**Christian Franc M. Carvajal (Koykoy200078)**

- GitHub: https://github.com/Koykoy200078

## License

This project is for educational and administrative purposes within the csitlab.local domain environment.

## Version History

- **v1.0.0** - Initial release with login system and full UI implementation
  - Domain credential verification
  - PC Management features
  - Web Blocking features
  - System Utilities
  - Real-time console output
