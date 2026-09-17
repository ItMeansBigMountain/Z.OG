# Z.OG

A top-down mobile MMORPG looter shooter set in an open-world zombie apocalypse: scarce loot,
survival pressure, and no way to tell friend from foe. You don't know where your friends are until you physically find them, and the stranger you
just spotted may or may not be hostile.

Monetized with interstitial ads between deaths and on match join, removable via an ad-free IAP.

The project runs a live MCP bridge so an AI agent can read the Editor console and edit scenes
directly in the open Editor. See [CLAUDE.md](CLAUDE.md) for design pillars and constraints.

## Project Info

- **Unity Version**: `6000.3.24f1` — the authority is [`ProjectSettings/ProjectVersion.txt`](ProjectSettings/ProjectVersion.txt); tooling and CI read it from there.
- **Render Pipeline**: Universal Render Pipeline 17.3.0
- **Editor bridge**: `com.gamelovers.mcp-unity` on `127.0.0.1:8090` (loopback only)
- **Primary Editor host**: Windows workstation. Headless builds run in CI.

## Current State

Early scaffold. `Assets/Scenes/Main.unity` contains a Main Camera, a Directional Light, and a Ground
plane. The only script is the WebGL build entry point in `Assets/Editor/BuildScript.cs`; no gameplay
systems exist yet.

Packages are already in place for the intended direction: Netcode for GameObjects, Input System,
Cinemachine, Entities, AI Navigation, Addressables, and Terrain.

## Structure

```
Z.OG/
├── Assets/
│   ├── Editor/BuildScript.cs   # WebGL build entry point (BuildScript.PerformWebGLBuild)
│   └── Scenes/Main.unity       # Main scene
├── Packages/manifest.json      # Pinned package versions
├── ProjectSettings/            # URP, graphics, Editor version, MCP bridge settings
├── docs/                       # Onboarding notes
├── scripts/                    # Windows setup automation
└── .github/workflows/          # CI: validation + WebGL build
```

## Prerequisites

- Unity Hub, signed in with a Unity account
- Unity `6000.3.24f1` installed, **with the WebGL module** if you intend to build for browser
- Git

## Quick Start

### 1. Clone

```powershell
git clone https://github.com/ItMeansBigMountain/Z.OG.git
cd Z.OG
```

### 2. Open the Editor with the MCP bridge

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Setup-LiveUnityMcp.ps1
```

The script reads the Editor version from `ProjectVersion.txt`, locates that Editor (including custom
Unity Hub install roots), verifies the MCP package is pinned in the manifest, opens the project, and
waits until the bridge is listening on `127.0.0.1:8090`. If the bridge is already up it exits
without launching a second Editor.

Override either value if you need to:

```powershell
.\scripts\Setup-LiveUnityMcp.ps1 -UnityVersion 6000.3.24f1
.\scripts\Setup-LiveUnityMcp.ps1 -EditorPath "D:\Applications\unity\Editors\6000.3.24f1\Editor\Unity.exe"
```

### 3. Verify the bridge

1. `Tools > MCP Unity > Server Window` reports the server running.
2. `Test-NetConnection 127.0.0.1 -Port 8090` succeeds.
3. From the MCP client, `get_console_logs` returns recent Editor console output.

Bridge settings live in [`ProjectSettings/McpUnitySettings.json`](ProjectSettings/McpUnitySettings.json).

## Security Boundary

- The bridge is loopback-only. `AllowRemoteConnections` stays `false`.
- Package installation through MCP stays disabled (`AllowPackageInstallation: false`).
- Never expose port 8090 to the internet; reach it from another host only through an authenticated,
  encrypted tunnel.
- The bridge token is generated under `Library/McpUnity/bridge-token`. `Library/` is Git-ignored —
  never commit or paste that token.

CI enforces the first three defaults on every push, so a change that loosens them fails the build.

## Building

### From the Editor

`File > Build Settings`, or call `BuildScript.PerformWebGLBuild` from the menu-less build path below.

### Headless

```powershell
& "D:\Applications\unity\Editors\6000.3.24f1\Editor\Unity.exe" `
  -batchmode `
  -projectPath . `
  -buildTarget WebGL `
  -executeMethod BuildScript.PerformWebGLBuild `
  -quit
```

Output lands in `Builds/WebGL`.

### In CI

[`.github/workflows/unity-build.yml`](.github/workflows/unity-build.yml) runs two jobs on every push
and PR to `main`:

- **validate** — parses the package manifest, asserts the MCP security defaults, syntax-checks the
  setup script, and fails if the docs quote a Unity version other than the one in
  `ProjectVersion.txt`.
- **build** — WebGL build via `game-ci/unity-builder`, using the version read from
  `ProjectVersion.txt`. **Skipped unless a Unity license is configured**: set either
  `UNITY_LICENSE` or `UNITY_SERIAL` (plus `UNITY_EMAIL` / `UNITY_PASSWORD`) in the repository
  secrets to enable it.

## Roadmap

- Top-down player controller on the Input System, with a Cinemachine follow camera
- Line-of-sight / fog of war — the core mechanic, not a visual effect: a top-down camera that shows
  everything around the player destroys the tension the game depends on
- Survival systems: health, hunger, thirst, stamina, temperature, infection
- Inventory and item definitions; loot scarcity and degradation
- Zombies: noise attraction, pathfinding, dangerous in groups
- Multiplayer via Netcode for GameObjects — server-authoritative, with visibility-based interest
  management so clients are never sent the positions of players they cannot see
- Content streaming via Addressables
