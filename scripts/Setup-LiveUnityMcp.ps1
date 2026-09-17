param(
    [string]$ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")),
    [string]$UnityVersion,
    [string]$EditorPath
)

$ErrorActionPreference = "Stop"

$manifest = Join-Path $ProjectPath "Packages\manifest.json"
$settings = Join-Path $ProjectPath "ProjectSettings\McpUnitySettings.json"
$versionFile = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"

if (-not (Test-Path $manifest)) { throw "Not a Unity project: $ProjectPath" }
if (-not (Test-Path $settings)) { throw "Missing MCP settings: $settings" }

$dependency = Select-String -Path $manifest -SimpleMatch '"com.gamelovers.mcp-unity"'
if (-not $dependency) { throw "The pinned MCP Unity package is missing from Packages/manifest.json." }

# ProjectVersion.txt is the single source of truth for the Editor version, so this script
# cannot drift from the project the way a hardcoded version string did.
if (-not $UnityVersion) {
    if (-not (Test-Path $versionFile)) { throw "Missing $versionFile; pass -UnityVersion explicitly." }
    $versionMatch = Select-String -Path $versionFile -Pattern '^m_EditorVersion:\s*(\S+)\s*$'
    if (-not $versionMatch) { throw "Could not read m_EditorVersion from $versionFile." }
    $UnityVersion = $versionMatch.Matches[0].Groups[1].Value
}

# Unity Hub installs to a custom root on some machines; check that before the default location.
if (-not $EditorPath) {
    $roots = New-Object System.Collections.Generic.List[string]
    $secondaryInstallPath = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path $secondaryInstallPath) {
        try { $customRoot = (Get-Content $secondaryInstallPath -Raw) | ConvertFrom-Json } catch { $customRoot = $null }
        if (($customRoot -is [string]) -and $customRoot) { $roots.Add($customRoot) }
    }
    $roots.Add((Join-Path ${env:ProgramFiles} "Unity\Hub\Editor"))

    foreach ($root in $roots) {
        $candidate = Join-Path $root "$UnityVersion\Editor\Unity.exe"
        if (Test-Path $candidate) {
            $EditorPath = $candidate
            break
        }
    }
}

if ((-not $EditorPath) -or (-not (Test-Path $EditorPath))) {
    throw "Unity $UnityVersion was not found. Install that exact version in Unity Hub, or pass -EditorPath pointing at Unity.exe."
}

$mcpPort = ((Get-Content $settings -Raw) | ConvertFrom-Json).Port
if (-not $mcpPort) { $mcpPort = 8090 }

function Test-McpPort {
    param([int]$Port)
    # The bridge may bind IPv6-only ([::1]); "localhost" covers both stacks, 127.0.0.1 does not.
    return (Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue)
}

# An Editor that is already open holds a lock on the project; launching a second one just fails.
if (Test-McpPort -Port $mcpPort) {
    Write-Host "MCP Unity is already listening on 127.0.0.1:$mcpPort. Nothing to do."
    return
}

Write-Host "Opening Unity $UnityVersion from $EditorPath with MCP bound to localhost:$mcpPort..."
Start-Process -FilePath $EditorPath -ArgumentList @("-projectPath", $ProjectPath)

$deadline = (Get-Date).AddMinutes(5)
do {
    Start-Sleep -Seconds 3
    $listening = Test-McpPort -Port $mcpPort
} until ($listening -or (Get-Date) -ge $deadline)

if (-not $listening) {
    throw "Unity opened, but MCP did not listen on localhost:$mcpPort within five minutes. Check Tools > MCP Unity > Server Window and the Console."
}

Write-Host "MCP Unity is listening locally on 127.0.0.1:$mcpPort."
Write-Host "Keep Allow Remote Connections disabled; remote connectivity must use an encrypted tunnel."
