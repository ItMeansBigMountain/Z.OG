# Live Unity Editor onboarding

The primary Unity Editor runs on the Windows workstation so the operator can watch agent-driven
changes immediately. The VPS remains the automation/build host.

## Windows bootstrap

1. Install Unity Hub and sign in.
2. Install the Editor version recorded in `ProjectSettings/ProjectVersion.txt` — currently `6000.3.24f1`.
3. Clone this repository.
4. In PowerShell from the repository root, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Setup-LiveUnityMcp.ps1
```

The script reads the Editor version from `ProjectVersion.txt`, resolves that Editor from the Unity
Hub install root (including a custom root set in Hub preferences), validates the pinned MCP package,
opens the project, and waits until the Editor bridge listens on `127.0.0.1:8090`. Pass
`-EditorPath` to point at a specific `Unity.exe`, or `-UnityVersion` to override the version.

If the bridge is already listening, the script reports that and exits without launching a second
Editor — an Editor that already has the project open holds a lock on it.

## Security boundary

- The bridge remains loopback-only by default.
- Package installation through MCP remains disabled.
- Do not expose port 8090 directly to the internet.
- Connect the VPS through an authenticated encrypted tunnel only.
- The bridge authentication token is generated under `Library/McpUnity/bridge-token`; `Library/` is Git-ignored and the token must never be committed or pasted into chat.

## Verification

Successful onboarding requires all of the following:

1. Unity Editor visibly opens the project on Windows.
2. `Tools > MCP Unity > Server Window` reports the server running.
3. Windows reports `127.0.0.1:8090` listening.
4. The MCP client discovers the Unity tools through the encrypted tunnel.
5. The client invokes `get_console_logs` and performs a reversible scene edit that appears in the open Editor.
