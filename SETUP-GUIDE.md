# Quick Setup Guide - PC Management System UI

## Step-by-Step Setup

### 1. Install PowerShell SDK (if not already installed)

You may need to install the PowerShell SDK NuGet package. Run this in the Package Manager Console:

```powershell
Install-Package System.Management.Automation -Version 7.4.0
```

Or manually add the reference to the DLL:

- Right-click on References in Solution Explorer
- Add Reference → Browse
- Navigate to: `C:\Program Files (x86)\Reference Assemblies\Microsoft\WindowsPowerShell\3.0\`
- Select `System.Management.Automation.dll`

### 2. Build the Project

In Visual Studio:

1. Open `WinServer2019.slnx` or `WinServer2019.csproj`
2. Build → Build Solution (or press Ctrl+Shift+B)

Or use command line:

```powershell
cd "d:\Projects\WinServer2019_Admin"
msbuild WinServer2019.csproj /p:Configuration=Release
```

### 3. Copy Scripts Folder

After building, copy the Scripts folder to the output directory:

```powershell
# For Debug build
Copy-Item -Path "Scripts" -Destination "bin\Debug\" -Recurse -Force

# For Release build
Copy-Item -Path "Scripts" -Destination "bin\Release\" -Recurse -Force
```

### 4. Run the Application

```powershell
# Debug version
.\bin\Debug\WinServer2019.exe

# Release version
.\bin\Release\WinServer2019.exe
```

## First Time Login

When you first run the application:

1. **Login Screen appears**

   - Detected Domain: Shows your current domain or "csitlab.local"
   - You can choose:
     - Use default credentials (Administrator / @csitlab123)
     - Enter custom credentials

2. **Domain Options**

   - If your machine is domain-joined, it will auto-detect
   - If not joined, it defaults to "csitlab.local"
   - You can manually enter a different domain

3. **Authentication**
   - Click "Login" to authenticate
   - The system validates credentials against Active Directory
   - If successful, the main interface opens

## Testing Without Domain

If you want to test the UI without connecting to an actual domain:

1. **Modify LoginForm.cs** (for testing only):

```csharp
private bool ValidateCredentials(string username, string password, string domain)
{
    // TESTING ONLY - Comment this line for production
    return true; // Skip domain validation

    // Original code below
    try
    {
        using (PrincipalContext context = new PrincipalContext(ContextType.Domain, domain, username, password))
        {
            return context.ValidateCredentials(username, password);
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
        return false;
    }
}
```

⚠️ **Remember to remove the `return true;` line in production!**

## Common Issues and Solutions

### Issue 1: PowerShell Scripts Not Found

**Error**: "Script file not found: Scripts\Main.ps1"

**Solution**:

```powershell
# Ensure Scripts folder is in the same directory as the .exe
# Copy it manually or use post-build event
```

**Add to project** (WinServer2019.csproj):

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="xcopy /Y /E /I &quot;$(ProjectDir)Scripts&quot; &quot;$(TargetDir)Scripts&quot;" />
</Target>
```

### Issue 2: PowerShell Execution Policy

**Error**: Execution policy prevents script loading

**Solution**:

```powershell
Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass -Force
```

### Issue 3: Missing System.Management.Automation

**Error**: "Could not load file or assembly 'System.Management.Automation'"

**Solution**: Install via NuGet or add DLL reference:

```powershell
# Option 1: NuGet Package Manager Console
Install-Package System.Management.Automation

# Option 2: Manual DLL reference
# Add reference to: C:\Program Files (x86)\Reference Assemblies\Microsoft\WindowsPowerShell\3.0\System.Management.Automation.dll
```

### Issue 4: DirectoryServices Not Found

**Error**: "The type or namespace name 'DirectoryServices' does not exist"

**Solution**: The reference should already be added, but if not:

```xml
<Reference Include="System.DirectoryServices" />
<Reference Include="System.DirectoryServices.AccountManagement" />
```

## Deployment

### For Production Deployment:

1. **Build in Release mode**

```powershell
msbuild WinServer2019.csproj /p:Configuration=Release
```

2. **Create deployment package**

```powershell
New-Item -Path "Deploy" -ItemType Directory -Force
Copy-Item "bin\Release\WinServer2019.exe" "Deploy\"
Copy-Item "bin\Release\*.dll" "Deploy\"
Copy-Item "Scripts" "Deploy\Scripts" -Recurse
```

3. **Create installer** (optional)
   - Use tools like Inno Setup or WiX
   - Include Scripts folder in installer
   - Set proper permissions

### Minimum Files Required:

```
Deploy/
├── WinServer2019.exe
├── System.Management.Automation.dll (if not in GAC)
└── Scripts/
    ├── Main.ps1
    ├── Functions/
    │   └── [all .ps1 files]
    └── BlockLists/
        └── [all .txt files]
```

## Running on Server

### Windows Server 2019 Setup:

1. **Enable .NET Framework 4.8.1** (if not installed)

```powershell
# Download and install .NET Framework 4.8.1
# https://dotnet.microsoft.com/download/dotnet-framework/net481
```

2. **Configure PowerShell Remoting** (if not enabled)

```powershell
Enable-PSRemoting -Force
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "PC-*" -Force
```

3. **Set Execution Policy**

```powershell
Set-ExecutionPolicy RemoteSigned -Force
```

4. **Run as Administrator**
   - Right-click `WinServer2019.exe`
   - Select "Run as administrator"

## Testing Checklist

Before deploying to production:

- [ ] Login form appears and detects domain correctly
- [ ] Authentication works with valid credentials
- [ ] Authentication rejects invalid credentials
- [ ] Main interface loads after successful login
- [ ] All three tabs are visible and accessible
- [ ] Output console displays messages
- [ ] Status bar updates during operations
- [ ] PowerShell scripts load without errors
- [ ] Can execute at least one test command
- [ ] Application closes properly
- [ ] Scripts folder is accessible

## Support

For issues or questions:

- Check the main README-UI.md for detailed documentation
- Review PowerShell script logs in Scripts folder
- Check Windows Event Viewer for system errors
- Contact: Christian Franc M. Carvajal (Koykoy200078)
- GitHub: https://github.com/Koykoy200078

---

**Last Updated**: November 2025
**Version**: 1.0.0
