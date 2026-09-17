# Z.OG — session handoff

Read this before doing anything else. It records state that is **not** recoverable from the code or
git history, plus a set of gotchas that each cost real debugging time to find.

Last updated: 2026-09-16.

---

## 0. Where the project lives

**Canonical: `D:\Data\GameDev\UnityGames\Z.OG`**

It was copied here from `C:\Users\faree\OneDrive\Desktop\Code\Projects\unfinished\dayz-survival-unity`,
which is **abandoned** — do not edit it. The original is intentionally left in place as a fallback
until the new location is confirmed good.

**Why it moved:** every folder in the OneDrive copy carried reparse tag `0x9000e01a` (OneDrive
Files-On-Demand placeholders) while the OneDrive process was not running. Unity's atomic rename into
`Library\PackageCache` therefore failed with `EPERM: operation not permitted`, which is what broke
the `com.unity.purchasing` install. `Library/`, `Temp/`, `Logs/`, `UserSettings/` and the stale
`.csproj`/`.slnx` files were deliberately **not** copied — Unity regenerates them, and the old
`Library/` was the corrupted one.

**D: is exFAT.** Consequences:

- Git needs `git config --global --add safe.directory D:/Data/GameDev/UnityGames/Z.OG` (already done
  on this machine; a different machine or user account will need it again).
- exFAT has no journaling. For a Unity `Library/` that churns constantly this is a durability risk,
  not a correctness one. Worth raising with the user if the drive is ever repartitioned — NTFS would
  be the better home.

---

## 1. Status

### Done and verified

- **Package resolution fixed.** Five manifest entries were wrong *names*, not wrong versions:
  `com.unity.services.cloud-save` to `cloudsave@3.4.1`, `cloud-code` to `cloudcode@2.2.1`,
  `com.unity.profiling.cortex` to `com.unity.profiling.core@1.0.3`. Two were not real packages and
  were removed: `com.unity.animation@1.0.0` (discontinued DOTS preview, never reached 1.0.0) and
  `com.unity.render-pipelines.universal.shaders@17.0.0` (URP ships its own shaders).
- **MCP bridge works.** `com.gamelovers.mcp-unity@1.5.0`. Verified end-to-end with a JSON-RPC
  handshake — returns 34 tools (`create_scene`, `create_prefab`, `add_asset_to_scene`,
  `set_transform`, `create_material`, `update_component`, play-mode control). No mesh authoring.
- **IAP** upgraded 4.15.1 to 5.4.3 (4.x went unsupported June 2026). No IAP code exists yet, so the
  4-to-5 break cost nothing.
- **Version drift eliminated.** `ProjectSettings/ProjectVersion.txt` is now tracked (it was
  gitignored — the root cause) and the setup script plus both CI jobs read the version from it. CI
  fails if the docs quote a different version.
- **Prototype scripts written** (see section 3). Not yet wired into a scene.
- **All "DayZ" references scrubbed** from docs, `projectName`, `metroPackageName`, VS Code settings.
- **GitHub repo renamed** to `Z.OG` (https://github.com/ItMeansBigMountain/Z.OG). The `origin`
  remote in this copy is updated and verified reachable. `gh` 2.101.0 is authenticated as
  `ItMeansBigMountain`, installed at `C:/Program Files/GitHub CLI/gh.exe` — **not on PATH**, call it
  by full path. Note the repo is **public**.

### Blocked on the user

| Item | Blocker |
|---|---|
| Wire UGS cloud services | Service account key. `ugs` CLI v1.9.0 is installed; `ugs status` shows no credentials. Never ask them to paste the secret — they run `ugs login` themselves. |
| Build target | CI still builds **WebGL**, but the game is now targeting **mobile**. Unresolved: switch to Android, or keep WebGL for playtests? |

### Not done

- **Nothing is committed.** The whole session's work is uncommitted in the working tree.
- Scene/prefab wiring for the prototype (needs Unity open + MCP).

---

## 2. Gotchas that will waste your time again

1. **The MCP bridge binds IPv6 only.** It listens on `[::1]:8090`, *not* `127.0.0.1:8090`.
   `Test-NetConnection -ComputerName 127.0.0.1 -Port 8090` returns **False while the bridge is
   healthy**. Probe `localhost` instead. This already caused one wrong "the server is down"
   conclusion; `scripts/Setup-LiveUnityMcp.ps1` was fixed to probe `localhost`.

2. **Do not let `com.unity.ai.assistant` auto-upgrade.** It must stay at **`1.6.0-pre.1`**. Unity's
   Package Manager upgraded it to `2.19.0-pre.2` mid-session; that version declares a 6000.0 minimum
   but is built against newer SRP APIs than 6000.3 ships, and fails with
   `error CS0234: The type or namespace name 'Srp' does not exist`. **One compile error blocks every
   assembly in the project, including the MCP bridge** — so this presents as "the MCP server is
   dead" rather than as a package problem. Unity publishes this package per editor stream, so
   1.6.0-pre.1 is *newer* than 2.19.0-pre.2 for 6000.3 despite the lower number.

3. **`.mcp.json` embeds a PackageCache path** containing the hash
   `com.gamelovers.mcp-unity@382a43a30f4d`. The hash derives from the pinned git revision, so it is
   stable across re-resolves — but `Server~/build/` is an **npm build artifact that is not in git**.
   After Unity re-resolves packages in the new location it must be rebuilt: run `npm install` then
   `npm run build` inside `Library/PackageCache/com.gamelovers.mcp-unity@382a43a30f4d/Server~`.
   Alternatively use `Tools > MCP Unity > Server Window > Configure Claude Code (Project)`, which
   regenerates `.mcp.json` correctly, including `MCP_UNITY_AUTH_TOKEN_PATH`. The bridge's token
   resolution is **strict with no fallback**, so a hand-written config missing the token path fails.

4. **UPM git dependencies need a full 40-char SHA**, a tag, or a branch. The original manifest pinned
   `#382a43a` (abbreviated) *and* `?path=/Editor` when `package.json` is at the repo root — two
   independent bugs in one line. Now `...mcp-unity.git#1.5.0`.

5. **`Logs/Editor.log` inside the project is stale.** The live log is
   `C:\Users\faree\AppData\Local\Unity\Editor\Editor.log`. The in-project one had a Sep 8 crash from
   an editor install (`6000.6.0f1`) that no longer exists, which reads alarmingly and is irrelevant.

6. **`grep -c` returning 0 exits non-zero** and will silently truncate a `&&` command chain. Bit me
   once while checking compile errors.

---

## 3. The prototype

All under `Assets/Scripts/Runtime/` (asmdef `ZOG.Runtime`, references `Unity.Netcode.Runtime` and
`Unity.InputSystem`). `activeInputHandler: 2` (both backends), so `Keyboard.current` works without an
InputActions asset.

| File | What it does |
|---|---|
| `Player/PlayerController.cs` | Server-authoritative movement. Owner samples WASD and sends it to the server **only when input changes**; server clamps and applies it. |
| `Networking/ProximityInterest.cs` | Server-side visibility filter via `NetworkShow`/`NetworkHide` plus `CheckObjectVisibility`. Clients only receive players within `visibilityRadius` (40m). |
| `Networking/ConnectionMenu.cs` | Throwaway IMGUI: Host / Client / Dedicated server. Delete once there is a real front end. |
| `Player/TopDownCamera.cs` | Follows the local player. Placeholder for a Cinemachine rig. |

**`ProximityInterest` is not an optimization — it is the game's core pillar in code.** Hiding other
players in the renderer still ships their positions to every client, so any modified client can draw
them, which destroys "you don't know where anyone is." It also keeps per-client bandwidth scaling
with players *nearby* rather than players *total*, which is the answer to "one server, scalable."

### Still to wire (needs Unity + MCP)

1. A `NetworkManager` GameObject with `UnityTransport`, and a `ConnectionMenu` component.
2. A player prefab: `NetworkObject` + `NetworkTransform` (server authority) + `CharacterController` +
   `PlayerController` + `ProximityInterest`. Register it as the Player Prefab on `NetworkManager`.
3. `TopDownCamera` on the Main Camera in `Assets/Scenes/Main.unity`.
4. Verify with `Window > Multiplayer > Multiplayer Play Mode` (`com.unity.multiplayer.playmode@2.0.2`
   is in the manifest): activate 2 virtual players, one Host and one Client. For the shape that
   matches production, use **Dedicated server** in one instance and Client in the others.

---

## 4. Next actions, in order

1. Open `D:\Data\GameDev\UnityGames\Z.OG` in Unity Hub and let it rebuild `Library/`. Watch for the
   `ai.assistant` version (gotcha 2) and confirm `com.unity.purchasing` installs cleanly this time.
2. Rebuild the MCP Node bridge (gotcha 3), confirm `localhost:8090` (gotcha 1), restart Claude Code
   from the new folder so `.mcp.json` loads.
3. Wire the prototype scene (section 3).
4. Commit everything — it has never been committed. Suggest one commit for the infrastructure fixes
   and a second for the prototype.
5. Push once committed — `origin` is already correct and reachable.

---

## 5. Open questions for the user

- Build target: Android now, or keep WebGL for quick playtests?
- Repo is public. Intentional for a commercial project, or should it go private?
- Which UGS services to wire first? Recommendation was **Game Server Hosting** and **Vivox**
  (proximity voice is arguably the most on-vision service on the list), and to avoid **Relay** and
  **Distributed Authority** — both hand authority or full world state to a client, which is
  unfixable from a cheating standpoint for a game built on hidden information.
