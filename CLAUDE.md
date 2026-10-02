# CLAUDE.md

Lab-administration suite for the CSIT computer lab: domain `csitlab.local`, server `192.168.2.45`, client PCs `PC-1`…`PC-35`. Two .NET Framework WinForms apps + a PowerShell console toolkit. Default branch: `develop`. The GitHub repo is **public** — never commit secrets.

## Layout
- `WinServer2019.csproj` (net48; the only project in `WinServer2019.slnx`) — admin GUI that runs on the server.
  - `Program.cs` → `LoginForm` (AD check via `PrincipalContext`; domain auto-detect, fallback `csitlab.local`) → `MainActivity` (tabs PC Management / Web Blocking / Utilities = 27 actions, plus "📊 Live Monitoring").
  - `PowerShellExecutor.cs` — one persistent runspace per login (ExecutionPolicy Bypass); sets `$script:targetDomain`, `$script:cred`, `$script:scriptPath` and the block lists, then loads the embedded PS functions.
  - `MonitoringServer.cs` / `MonitoringForm.cs` / `ScreenViewerForm.cs` — TCP 8888 server, client list, live screen view, message/freeze commands.
- `PCMonitorClient/` (net481, **not in the .slnx**) — hidden agent on lab PCs: `MonitoringClient.cs` (TCP loop), `ActivityMonitor.cs` (window/CPU/RAM/screenshot), `MessageDisplayForm.cs` (fullscreen overlay + `BlockInput`).
- `Scripts/` — console toolkit: `Main.ps1` (27-option menu that mirrors the GUI) loads `Functions/*.ps1` via `Import-Module`; also build/deploy/diagnostic scripts and `BlockLists/*.txt`.
- `DeploymentPackage/` — output of `Build-All.ps1` (`Server/`, `Client/`, `Scripts/`); committed.
- The root `*.md`/`*.txt` guides are point-in-time change logs; many are stale or contradict each other. Trust the code.

## Build & run
- MSBuild isn't on PATH. Use VS 2026 (v18): `& "C:\Program Files\Microsoft Visual Studio\18\Professional\MSBuild\Current\Bin\MSBuild.exe" WinServer2019.csproj -p:Configuration=Release`
- Client: same command with `PCMonitorClient\PCMonitorClient.csproj`.
- No restore needed: Newtonsoft.Json 13.0.3 is committed in the root `packages/` folder (both csproj HintPaths point there).
- Expected warning CS2002: `ScreenViewerForm.Designer.cs` is listed twice in the csproj.
- Post-build `xcopy`s `Scripts\` → `bin\<Config>\Scripts\`, which is tracked. Check `git diff bin/` after building.
- To check that it builds without touching tracked outputs, append `-p:OutputPath=<tmp>\bin\ -p:BaseIntermediateOutputPath=<tmp>\obj\ -p:IntermediateOutputPath=<tmp>\obj\Release\`.
- `pwsh -File Scripts\Build-All.ps1` — Release-rebuilds both projects, **deletes and regenerates `DeploymentPackage/`**, and copies to `\\192.168.2.45\Sharing\Other\DeploymentPackage` if that share is reachable. Only the copy in `Scripts/` resolves the repo root correctly.
- ClickOnce publish (VS Publish) → `\\192.168.2.45\Sharing\Other\`; manifests are signed with `WinServer2019_TemporaryKey.pfx`.
- No automated tests. Verification = build + manual run on the lab domain. GUI and script actions hit real PCs (shutdown, hosts-file edits) — never trigger them just to test.

## Gotchas
- **GUI ≠ `Scripts/Functions`**: GUI buttons run simplified PS copies embedded as C# strings in `PowerShellExecutor.cs` (`GetEmbedded*Functions()`, `GetBlockListsScript()`). Editing `Scripts/Functions/*.ps1` or `BlockLists/*.txt` only changes the console `Main.ps1`. Update both when behavior should match.
- Embedded PS is in `@"…"` strings (write `"` as `""`). `MainActivity` builds scripts with `$@"…"` (literal PS braces = `{{ }}`).
- User input is interpolated into PS strings; escape `'` → `''` the way `BtnCustomCommand_Click` does. (`InitializeRunspace` interpolates the password without escaping.)
- `Write-Host` output reaches the GUI through the Information stream; `ExecuteCommand` forwards every stream to `outputCallback` in real time.
- Each `MonitoringForm` creates its own `MonitoringServer` on port 8888: a second window can't bind, and closing the window stops the server.
- **`Scripts/` is an orphaned gitlink** (mode 160000, no `.gitmodules`): its files aren't versioned and edits never show in `git status`. Tracked copies live in `bin/Debug/Scripts/`, `bin/Release/Scripts/` and `DeploymentPackage/Server/Scripts/` (plus `DeploymentPackage/Scripts/` for the deploy scripts). They drift apart.
- **Run the repo's `.ps1` files with `pwsh` 7+**: they're UTF-8 without a BOM and use ✓/✗/█/emoji, so Windows PowerShell 5.1 (ANSI code page) fails to parse 12 of the 14 scripts. New scripts should be ASCII-only or UTF-8 *with* a BOM.
- `Deploy-MonitoringClient.ps1` only works from `DeploymentPackage/Scripts/`; run from the repo root, its client path resolves one folder too high.
- `.gitignore` covers caches only: `.vs/`, `obj/`, `bin/` (except `bin/<Config>/Scripts/`), and script runtime output (`Scripts/Reports/`, `Scripts/MySQL-Exports-*/`). `DeploymentPackage/` binaries, `packages/` and `*.csproj.user` are still tracked — stage source files explicitly, and ask before committing regenerated binaries.
- Hardcoded lab admin credentials exist (LoginForm's "default credentials" checkbox, `Main.ps1`, several docs). Don't copy them anywhere new.
- `Functions/Lab-Monitoring.ps1` (`Start-RealtimeMonitor`, `Get-StudentActivity`, `Start-BrowserSearchMonitor`) is imported by `Main.ps1` but has no menu entry. Guides that cite "option 25/26" are stale.

## Remote-management conventions
- Targets are FQDNs `PC-<n>.<domain>`; the range `1..35` is hardcoded in both C# and PS.
- Pattern: `Test-WSMan` → (often) `Test-DomainMembership` → `Invoke-Command -Credential $script:cred`. Offline PCs are reported as a line of output, not thrown as errors.
- Web blocking rewrites each PC's hosts file: `127.0.0.1`/`0.0.0.0 <site>` entries under `# BLOCKED BY ADMIN`, backup saved as `hosts.backup-yyyyMMdd-HHmmss`, then `ipconfig /flushdns`.

## Monitoring protocol
- Client → server: 4-byte little-endian length + UTF-8 JSON `ClientActivity` (≤10 MB, includes a 720p JPEG at quality 50), sent every 2 s; reconnects after 5 s.
- Server → client: plain `ACK` or a JSON `ServerCommand` (`CommandType` `message`|`freeze`, `MessageText`, `Duration` in s). This reply is **not length-prefixed** and the client does a single 4 KB read, so keep commands small or add framing on both ends.
- `ClientActivity`/`ServerCommand` are duplicated in `MonitoringServer.cs` and `PCMonitorClient/MonitoringClient.cs`; change both.
- Commands queue per PC name (the client's `Environment.MachineName`) and go out on that client's next update. The server drops clients that are silent for more than 30 s.
- `PCMonitorClient.exe [serverIP] [port]` — defaults `192.168.2.45 8888` are hardcoded in `Program.cs` (`App.config` appSettings are unused); a mutex keeps it single-instance.
- Deploy target: `C:\ProgramData\PCMonitor`, scheduled task `PCMonitorClient` (at logon, `BUILTIN\Users`), server firewall rule "PC Monitor Server" (TCP 8888 inbound).
- The `ScreenViewerForm` quality dropdown does nothing; the client always sends 720p.

## Conventions
- C# 7.3 (.NET Framework default): designer-backed forms, UI updates marshalled through `InvokeRequired`/`Invoke`, CRLF line endings.
- Commits: Conventional Commits with a scope, e.g. `feat(MessageDisplay): …`, `ci(deploy-monitoring): …`.
