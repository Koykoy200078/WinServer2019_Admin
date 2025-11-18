# Publishing WinServer2019 Admin Tool

## Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.8
- Windows 10/11 or Windows Server 2019/2022

## How to Publish

### Method 1: Using Visual Studio Publish Wizard

1. **Open the project** in Visual Studio
2. **Right-click** on the `WinServer2019` project in Solution Explorer
3. **Select "Publish"**
4. **Choose a publish profile:**
   - **Folder**: Publish to a local folder or network share
   - **ClickOnce**: Create an installer that can auto-update

#### Folder Publish (Recommended for Portable)

1. Select **Folder** target
2. Choose output location (e.g., `C:\Publish\WinServer2019`)
3. Click **Publish**
4. Files will be generated in the specified folder
5. Copy the entire folder to any Windows PC
6. Run `WinServer2019.exe` directly

#### ClickOnce Publish (For Auto-Updates)

1. Select **ClickOnce** (current configuration: `\\192.168.2.45\Sharing\Other\`)
2. Configure:
   - **Installation URL**: Network share or web server
   - **Update settings**: Check for updates on startup
   - **Prerequisites**: .NET Framework 4.8
3. Click **Publish**
4. Users can install from the published location

### Method 2: Manual Build

1. Open **Developer Command Prompt for VS**
2. Navigate to project directory:
   ```cmd
   cd d:\Projects\WinServer2019_Admin
   ```
3. Build in Release mode:
   ```cmd
   msbuild WinServer2019.csproj /p:Configuration=Release /p:Platform=x64
   ```
4. Output will be in: `bin\Release\`

## Making it Portable

### Current Status: ✅ ALREADY PORTABLE

The application is now **fully self-contained** with:

✅ **All PowerShell scripts embedded** in C# code (no external .ps1 files needed)
✅ **PowerShell SDK bundled** via System.Management.Automation.dll
✅ **No PowerShell installation required** on target PCs
✅ **All functions built-in**: PC Management, Web Blocking, Utilities

### What Gets Bundled

When you publish, these files are included:

- `WinServer2019.exe` (main application)
- `System.Management.Automation.dll` (PowerShell runtime)
- `System.DirectoryServices.AccountManagement.dll` (AD authentication)
- `WinServer2019.exe.config` (app configuration)
- Dependencies (automatically included by Visual Studio)

### Distribution

#### Option A: Single Folder

1. Publish to folder
2. Zip the entire output folder
3. Extract on target PC
4. Run `WinServer2019.exe`

#### Option B: Network Share

1. Publish to network location: `\\192.168.2.45\Sharing\Other\`
2. Users double-click `setup.exe` to install
3. Application auto-updates from network share

#### Option C: USB Drive

1. Copy published folder to USB drive
2. Plug into any Windows PC
3. Run directly from USB (no installation needed)

## Requirements on Target PCs

### Minimum Requirements:

- ✅ Windows 10/11 or Windows Server 2019/2022
- ✅ .NET Framework 4.8 (usually pre-installed)
- ✅ Domain membership (for remote management)
- ✅ WinRM enabled on target PCs
- ❌ PowerShell installation NOT REQUIRED (bundled in app)

### Recommended Configuration:

- Windows Server 2019/2022 (for running the admin tool)
- Domain Admin credentials
- Network access to target PCs (PC-1 through PC-35)

## Application Features

### Embedded PowerShell Functions:

- ✅ Get PC Status (IP, DNS, Time, Domain)
- ✅ Shutdown/Restart PCs (Single/Range/All)
- ✅ Block/Unblock Web Access
- ✅ AI Sites Blocking
- ✅ Deep Scan (Blocking Status)
- ✅ Time Synchronization
- ✅ Backup Hosts Files Cleanup
- ✅ View All Hosts Files
- ✅ MySQL Database Export
- ✅ Environment Variables Check
- ✅ Temporary Files Cleanup

### Built-in Block Lists:

- Social Media (Facebook, Twitter, Instagram, TikTok, etc.)
- Video Sites (YouTube, Netflix, Twitch, etc.)
- AI Sites (ChatGPT, Claude, Gemini, Copilot, etc.)
- Total: ~31 sites embedded in application

## Testing the Published App

1. **Publish the application**
2. **Copy to a clean test PC** (without development tools)
3. **Run as Administrator**
4. **Login with domain credentials**:
   - Domain: `csitlab.local`
   - Username: `Administrator`
   - Password: [your domain password]
5. **Test basic functions**:
   - Get Status of all PCs
   - Block/Unblock web access
   - Sync time

## Troubleshooting

### Issue: "Application requires .NET Framework 4.8"

**Solution**: Install .NET Framework 4.8 from Microsoft website

### Issue: "Access Denied" errors

**Solution**: Run as Administrator with Domain Admin credentials

### Issue: "Cannot connect to remote PCs"

**Solution**:

- Verify WinRM is enabled: `winrm quickconfig`
- Check firewall rules
- Verify domain membership
- Test with: `Test-WSMan PC-1.csitlab.local`

### Issue: "Functions not found"

**Solution**: This should NOT happen anymore (all functions are embedded)

- But if it does, rebuild the project in Release mode
- Verify `PowerShellExecutor.cs` has all embedded function methods

## Security Notes

⚠️ **Important Security Considerations:**

1. **Credentials in Memory**: User credentials are stored in memory during execution
2. **Domain Admin Rights**: Application should be run with Domain Admin account
3. **Network Share**: If using ClickOnce from network share, ensure share is secure
4. **Code Signing**: Consider signing the executable for production deployment
5. **Audit Logging**: Application actions are not logged by default

## Version Information

- **Current Version**: 1.0.0
- **Target Framework**: .NET Framework 4.8
- **Platform**: x64
- **PowerShell Version**: Embedded (SDK)

## Support

For issues or questions:

1. Check event logs on admin PC
2. Verify WinRM connectivity to target PCs
3. Test PowerShell remoting manually
4. Check domain group policies

---

**Last Updated**: November 18, 2025
**Built by**: Koykoy200078
**Repository**: WinServer2019_Admin
