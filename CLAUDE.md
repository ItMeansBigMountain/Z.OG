# CLAUDE.md

**Z.OG** — top-down mobile MMORPG looter shooter, open-world zombie apocalypse. Unity 6 URP.
Windows workstation is the primary Editor host; CI does headless builds.

> **Starting a session? Read [HANDOFF.md](HANDOFF.md) first.** It carries current status, what is
> blocked on the user, and gotchas that are not recoverable from the code or git history.

## Editor version

`ProjectSettings/ProjectVersion.txt` is the single source of truth (currently `6000.3.24f1`). It is
deliberately **not** gitignored. The setup script and both CI jobs read the version from it — never
hardcode a version string anywhere else. CI fails if `README.md` or `docs/LIVE_EDITOR_ONBOARDING.md`
quote a different one, so bump the docs when the Editor is upgraded.

The Editor is installed under a custom Unity Hub root on this machine
(`D:\Applications\unity\Editors`), not `C:\Program Files`.

## Opening the project

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Setup-LiveUnityMcp.ps1
```

Resolves the Editor, opens the project, waits for the MCP bridge on `127.0.0.1:8090`. Exits early if
the bridge is already up — a running Editor holds a lock on the project, so a second launch fails.

## MCP bridge

`com.gamelovers.mcp-unity`, configured in `ProjectSettings/McpUnitySettings.json`. These defaults are
enforced by CI and must not be loosened without a deliberate decision:

- `AllowRemoteConnections: false` (loopback only)
- `AllowPackageInstallation: false`
- `Port: 8090`

The bridge token under `Library/McpUnity/bridge-token` is secret and `Library/` is gitignored.

## Building

WebGL entry point is `BuildScript.PerformWebGLBuild` in `Assets/Editor/BuildScript.cs`, output to
`Builds/WebGL`. The CI build job is **skipped unless** `UNITY_LICENSE` or `UNITY_SERIAL` is set in
repository secrets — a green CI run does not necessarily mean the project compiled.

## Unity conventions for this repo

- Commit `.meta` files alongside every asset. A missing `.meta` makes Unity regenerate a new GUID and
  silently break references.
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, or `Builds/`.
- Scene and prefab files are YAML — prefer editing them through the Editor or the MCP bridge rather
  than by hand.
- `Logs/Editor.log` can be stale; check the modification date and the version banner at the top
  before trusting it for current-session diagnostics.

## The game

Top-down mobile MMORPG looter shooter in an open-world zombie apocalypse. The pillars, in the
order they matter:

1. **You don't know who anyone is.** No nameplates, no party UI, no friend markers on a minimap.
   You find your friends by physically finding them. Another player is unidentified until you get
   close enough to read their gear, and they may or may not be hostile. Every feature request that
   would reveal a player's identity or position at range works against the core of the game.
2. **Players are the threat; zombies are the pressure.** Infected are attracted by noise and
   dangerous in numbers, but the tension comes from other survivors.
3. **Permadeath and persistence.** Death costs the character and their gear. The world persists.
4. **Scarcity.** Loot is sparse and degrades; gear *is* the progression, not an XP bar.
5. **Survival needs.** Hunger, thirst, temperature, blood/health, infection.

### Monetization

Interstitial ads at two beats — after death, and on joining a match — removed by an ad-free IAP.
`com.unity.ads` and `com.unity.purchasing@5.4.3` are both in the manifest for this. Ad placement
is a design constraint on the death loop: deaths must be frequent enough to monetize but not so
punishing that an ad feels like salt in the wound.

### Three consequences that shape the architecture

**Vision must be restricted, or the game doesn't work.** A top-down camera shows the player
everything around them by default, which destroys the "is someone watching me?" tension the game
depends on. This needs a real fog-of-war / line-of-sight system — a view cone plus occlusion from
buildings and terrain, with unseen areas hidden rather than dimmed. Treat this as a core mechanic,
not a post-processing effect.

**Mobile is the target, not a port.** Touch-first controls, aggressive draw-distance and
occlusion budgets, and battery/thermal limits all constrain the open world's scale. Design for
phone-sized sessions rather than a PC world scaled down.

**The server must not tell clients what they cannot see.** In a game whose premise is not knowing
where other players are, sending every player's position to every client makes cheating trivial and
defeats the entire design. Networking has to be server-authoritative with distance/visibility-based
interest management (Netcode for GameObjects supports this). Decide this before building the
netcode layer, not after.

## Current state

Scaffold only. `Assets/Scenes/Main.unity` holds a Main Camera, Directional Light, and Ground plane.
`BuildScript.cs` is the only script — no gameplay systems yet. Packages for the intended direction
(Netcode for GameObjects, Input System, Cinemachine, Entities, AI Navigation, Addressables, Terrain)
are already in the manifest.
