# Gorilla Tag Compatibility Audit — `maz3n-menu`

| | |
|---|---|
| **Audit date** | 2026-08-09 |
| **Repository** | `owenobriencodes/maz3n-menu` @ `f8d79358` |
| **Menu identity** | ii's Stupid Menu **v8.2.4**, `BuildTimestamp` **2026-02-13T18:23:06Z** |
| **Fork lineage** | `iiDk-the-actual/iis.Stupid.Menu` → `nxssubzero/Gorilla` → this fork |
| **Question** | Will this codebase work with the current version of Gorilla Tag? |
| **Verdict** | **No.** It will not compile against current game assemblies, and if forced to load it will fail before the menu initializes. |

---

## 1. Executive summary

This fork was last built on **2026-02-13**. Gorilla Tag has shipped **12 updates** since that date, the most recent being the Pirates Update on **2026-08-07**. Over that window the game's managed assemblies drifted substantially, and at least one class that this menu hard-references was renamed.

There are three independent reasons the current tree is non-functional:

1. **A blocking compile break.** `GorillaNot` — the class this menu patches in 11 places — has been renamed to `MonkeAgent`. Every `[HarmonyPatch(typeof(GorillaNot), …)]` fails to resolve.
2. **A total-failure runtime path.** Because of how `PatchHandler.PatchAll()` filters types, a *missing type* (as opposed to a missing method) throws out of the patch loop entirely. The menu never initializes. There is no partial-degradation fallback for this case.
3. **Dead external infrastructure.** The upstream repository has been deleted (HTTP 404), taking every remote resource this menu downloads at runtime with it. The upstream project is formally discontinued.

The good news is that the *platform* assumptions are all still correct — this is API drift inside `Assembly-CSharp`, which is the tractable kind of breakage. See §5 and §6.

---

## 2. Subsystem status at a glance

| Subsystem | Status | Notes |
|---|---|---|
| Build (compile) | **Broken** | `GorillaNot` unresolved; other removed members below |
| Plugin load / menu init | **Broken** | Fails in `PatchHandler.PatchAll()` before UI is created |
| BepInEx 5 plugin model | OK | Still the current loader for GT on PC |
| Mono / `netstandard2.1` target | OK | GT PC is still Mono, not IL2CPP |
| Photon Fusion + PUN references | OK (drifted) | Both still ship; DLLs need refreshing |
| Photon event codes / `RaiseEvent` | **Broken** | Hardcoded event bytecodes changed |
| Voice (`Speaker`, `Recorder`) | **At risk** | Voice pipeline changed in the 7/10 update |
| Anti-report / RPC-limit patches | **Broken** | Class renamed; new `CallLimiter` unhandled |
| Telemetry suppression patches | **Broken** | `GorillaTelemetry` methods relocated |
| Virtual Stump / `BuilderTable` | **At risk** | Heavily changed across 6/12 and 7/24 updates |
| Movement / gravity mods | **At risk** | Gravity Zones (7/10) not modelled anywhere in this tree |
| Remote resources (sounds, icons) | **Broken** | Upstream repo 404 |
| Friends system / server API | **Broken** | Backend decommissioned |
| Server kill-switch | Inert (benign) | `min-version` lowered to `0.0.0` upstream |

---

## 3. Baseline — what this fork actually is

From `PluginInfo.cs`:

```csharp
public const string Version         = "8.2.4";
public const string BuildTimestamp  = "2026-02-13T18:23:06Z";
public const string ServerResourcePath =
    "https://raw.githubusercontent.com/iiDk-the-actual/iis.Stupid.Menu/master/Resources/Server";
public const string ServerAPI = "https://iidk.online";
```

Scale of coupling to the game, measured across 218 C# source files:

| Metric | Count |
|---|---|
| Distinct game **types** patched via Harmony | **81** |
| Distinct game **methods** patched via Harmony | **146** |
| `VRRig` references | 2,608 |
| `GorillaTagger` references | 1,292 |
| `NetworkSystem` references | 315 |
| `GorillaParent` references | 259 |
| `CosmeticsController` references | 134 |

Every one of those patches is **compile-time bound** through `typeof(…)` / `nameof(…)`. That is a double-edged property: it means the game's API drift cannot fail silently at build time, but it also means the build breaks loudly on any rename.

The project compiles against a **vendored snapshot of the game's own assemblies** in `References/Managed/` (`Assembly-CSharp.dll`, 6.27 MB). That snapshot is the Feb-2026 build. Refreshing it is the central maintenance task (§6).

---

## 4. Evidence

### 4.1 Game update cadence since the fork point

Retrieved from the Steam news API (app `1533390`). Twelve updates shipped after this menu's last build:

| Date | Update | Systems likely touched |
|---|---|---|
| 2026-08-07 | Pirates Update | Interactive cosmetics (Ghost Medallion), live events |
| 2026-07-24 | Monke Gotta Swing | VStump size changers, Alarm Clocks, VIM gravity mode, Photo Booth |
| 2026-07-13 | Small Bug Fix | Rig fixes near teleport artifacts (City, Space) |
| 2026-07-10 | Space Map Update | **Gravity Zones**, **HQ voice chat for VIMs**, new map |
| 2026-06-26 | Aliens | VIM feature graduation, outfit/block slots |
| 2026-06-12 | Creator Fest | VStump save support + PC parity |
| 2026-05-29 | Summer Fun / Pride | Cosmetics, misc features |
| 2026-05-15 | Monktoons & Friends | Cosmetics |
| 2026-05-01 | Steam GTFC | Fan Club / subscription surface |
| 2026-04-17 | April Rains | — |
| 2026-04-03 | Spring Update | — |
| 2026-03-20 | 3/20 Update | — |

Two of these are structurally significant for a mod menu: **Gravity Zones** (7/10) changes player physics assumptions, and **HQ voice chat** (7/10) changes the voice pipeline this menu patches.

### 4.2 The blocking break — `GorillaNot` → `MonkeAgent`

Established by diffing this fork's Harmony patch set against the actively-maintained community fork ([`Seralyth/Seralyth-Menu`](https://github.com/Seralyth/Seralyth-Menu), v5.0.1, pushed 2026-08-09). The rename is one-to-one across all eleven methods:

| This fork (v8.2.4) | Current game |
|---|---|
| `GorillaNot.CheckReports` | `MonkeAgent.CheckReports` |
| `GorillaNot.CloseInvalidRoom` | `MonkeAgent.CloseInvalidRoom` |
| `GorillaNot.DispatchReport` | `MonkeAgent.DispatchReport` |
| `GorillaNot.GetRPCCallTracker` | `MonkeAgent.GetRPCCallTracker` |
| `GorillaNot.IncrementRPCCall` | `MonkeAgent.IncrementRPCCall` |
| `GorillaNot.IncrementRPCCallLocal` | `MonkeAgent.IncrementRPCCallLocal` |
| `GorillaNot.LogErrorCount` | `MonkeAgent.LogErrorCount` |
| `GorillaNot.QuitDelay` | `MonkeAgent.QuitDelay` |
| `GorillaNot.SendReport` | `MonkeAgent.SendReport` |
| `GorillaNot.ShouldDisconnectFromRoom` | `MonkeAgent.ShouldDisconnectFromRoom` |

`GorillaNot` appears **27 times** across this tree. The patch attributes live at `Patches/Safety/IncrementRPCPatches.cs:36` and `:43`.

### 4.3 Why this is a total failure, not a degraded one

`Patches/PatchHandler.cs:38-51`:

```csharp
foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
             .Where(t => t.IsClass && t.GetCustomAttribute<HarmonyPatch>() != null))
{
    try
    {
        instance.CreateClassProcessor(type).Patch();
    }
    catch (Exception ex)
    {
        PatchErrors++;
        LogManager.LogError($"Failed to patch {type.FullName}: {ex}");
    }
}
```

The `try`/`catch` covers only the *application* of a patch. The `.Where(…)` predicate is evaluated lazily **during** `foreach` enumeration, which places it **outside** the `try` block. Instantiating the `HarmonyPatch` attribute forces resolution of `typeof(GorillaNot)`; when that type is absent, the resulting `TypeLoadException` propagates straight out of `PatchAll()`.

The call chain is `Plugin.Awake()` → `GorillaTagger.OnPlayerSpawned(LoadMenu)` → `LoadMenu()` → `PatchHandler.PatchAll()`. Nothing in that chain catches it, and the `GameObject` that hosts `UI`, `CoroutineManager`, and `NotificationManager` is only created *after* `PatchAll()` returns.

**Consequence:** a missing *method* costs one logged line and one dead feature. A missing *type* costs the entire menu, silently, with no in-game indication. `GorillaNot` is a missing type.

> This is worth hardening regardless of the rename — see Next Step 5 in §6.

### 4.4 Other game API this fork patches that no longer exists

| Removed / relocated member | Where it went |
|---|---|
| `GorillaGameManager.ValidGameMode` | Removed |
| `GorillaTelemetry.EnqueueTelemetryEventPlayFab` | → `PlayFabEventsAPI.WriteTelemetryEvents` |
| `GorillaTelemetry.FlushPlayFabTelemetry` | → `PlayFabEventsAPI.WriteTelemetryEvents` |
| `GrowingSnowballThrowable.PerformSnowballThrowAuthority` | Removed |
| `RoomSystem.SearchForShuttle` | Removed |

### 4.5 New game API this fork has no awareness of

| New type / member | Significance |
|---|---|
| **`CallLimiter.Reset`** | New in-game RPC/call throttle. The maintained fork patches `Reset` with a postfix calling `__instance.Remove()`. Without this, anything issuing frequent RPCs is rate-limited. |
| `LoadBalancingClient.OpRaiseEvent` | Photon event codes changed; the maintained fork carries a commit titled *"Migrate hardcoded bytecodes in Photon RaiseEvents and event code checks"*. Any hardcoded event byte in this tree is suspect. |
| `PhotonNetwork.OnEvent` | Same migration. |
| `NetworkSystemPUN.SetupVoice` | Matches the 7/10 HQ-voice-chat change. |
| `NetworkSystemPUN.ConnectToRoom` | New patch point. |
| `GTSignalRelay` | New networking relay type. |
| `MonkeAgent` | The `GorillaNot` replacement (§4.2). |
| `PlayFabEventsAPI.WriteTelemetryEvents` | Telemetry relocation. |
| `AprilFoolsGravityFX.Start` | Added by the 4/03-era update. |
| `HttpClient`, `WebClient`, `WebRequest`, `UnityWebRequest`, `Process` | The maintained fork added patches on these BCL types — an outbound-request interception layer that did not exist in Feb. |

Additionally: **nothing in this tree references Gravity Zones**, the player-physics system introduced on 2026-07-10. Movement mods that assume a single global gravity vector should be re-validated against it.

### 4.6 Vendored reference drift

Comparing `Seralyth/Seralyth-Menu` from this fork's merge-base (`5ebecbb8`, 2026-02-16) to its current `HEAD`:

- **551 commits ahead**
- **300 files changed** (280 modified, 18 added, 1 removed, 1 renamed)
- **93 files under `Patches/` modified**
- **65 DLLs under `References/Managed/` changed**, including `Assembly-CSharp.dll` and `Assembly-CSharp-firstpass.dll`

The maintained fork carries four separate `Update references` commits (2026-05-29, 07-10, 07-12, 07-14) — confirming that refreshing the vendored snapshot is recurring, per-update maintenance rather than a one-time fix.

Notable reference changes beyond `Assembly-CSharp`:

- `PhotonVoice.dll`, `PhotonVoice.API.dll`, `PhotonVoice.PUN.dll`, `PhotonVoice.Fusion.dll` — all modified (corroborates the voice-pipeline change)
- `PhotonRealtime.dll`, `PhotonUnityNetworking.dll` — modified
- `Fusion.Unity.dll` — modified
- `PlayFab.dll` — modified
- `K4os.Compression.LZ4.dll` — **newly added** to the game
- `BakeryAPV.dll`, `BakeryECS.dll` — **newly added** to the game
- `GT_CustomMapSupportRuntime.dll` — modified (relevant if you touch custom map support)

### 4.7 Dead external infrastructure

| Endpoint | Result | Impact |
|---|---|---|
| `github.com/iiDk-the-actual/iis.Stupid.Menu` | **404** — repository deleted | — |
| `raw.githubusercontent.com/iiDk-the-actual/…/Resources/Server/*` | **404** | Every `ServerResourcePath` fetch fails: menu/notification/achievement audio, achievement icons, `PluginLibrary.txt`, soundboard `SoundLibrary.txt`, VStump ad video. ~25 call sites across `Managers/`, `Menu/`, `Mods/`, `Patches/`. |
| `https://iidk.online/` (bare) | HTTP **500** | — |
| `https://iidk.online/getfriends` | HTTP **500** | Friends system non-functional |
| `https://iidk.online/serverdata` | HTTP **200** — still served | See below |

`/serverdata` still responds, and its payload is the upstream project's own epitaph:

```json
{
  "menu-version": "8.3.0",
  "min-version": "0.0.0",
  "min-console-version": "0.0.0",
  "motd": "This menu has been discontinued. It will no longer be receiving updates,
           please switch to a community instance at crimsoncauldron.dev/instances.",
  "detected-mods": []
}
```

Two operational consequences:

- `min-version` has been lowered to `0.0.0`, so the server-driven kill-switch in `ServerData.cs:204` will **not** fire. The menu will not self-disable.
- `detected-mods` is now **empty**, so the auto-disable safety list at `ServerData.cs:~290` is inert — no mod will ever be flagged and disabled by it again.

Failure handling here is sound: `LoadServerData()` logs and `yield break`s on a failed request (`ServerData.cs:176-180`), so a dead backend does not block startup. What is lost is the MOTD, Discord link, admin panel, Patreon tiers, polls, and the detected-mods list.

Note also that this fork is at **8.2.4** while the final upstream release was **8.3.0** — it is one release behind even the discontinued upstream.

---

## 5. What is still compatible

Verified against a Gorilla Tag modding template updated 2026-07-21 ([`itsreallyhex/HexTemplate`](https://github.com/itsreallyhex/HexTemplate)), which references a live current install:

- **Gorilla Tag on PC is still Mono**, not IL2CPP. The `netstandard2.1` target in `iiMenu.csproj` remains correct.
- **BepInEx 5 is still the loader** — `BepInEx/core/BepInEx.dll` and `0Harmony.dll` are still the reference paths. The `BaseUnityPlugin` + `[BepInPlugin]` model in `Plugin.cs` is unchanged.
- **Both Fusion and PUN still ship** side by side (`Fusion.Runtime.dll`, `PhotonUnityNetworking.dll`, `PhotonRealtime.dll`), so the dual-stack assumptions throughout this tree hold.
- `GTPlayer`, `VRRig`, `GorillaTagger`, `NetworkSystem`, `GorillaParent`, `CosmeticsController` all still exist under those names — the bulk of the 2,600+ `VRRig` call sites should survive a reference refresh.
- `BepInEx.AssemblyPublicizer.MSBuild` 0.4.3 and the `Publicize="true"` reference attributes still work as configured.

> Some community write-ups claim BepInEx is deprecated for Gorilla Tag in favour of an "official modding platform." That is **not** supported by the evidence here — current templates and the actively-maintained menu fork both still target BepInEx 5. Treat those claims as inaccurate.

**Bottom line:** the loader, target framework, networking stack, and core type names are intact. The breakage is confined to `Assembly-CSharp` API drift plus dead remote infrastructure.

---

## 6. Next steps

Two viable paths. Read §6.1 and §6.2, then §6.3 for the recommendation.

### 6.1 Path A — refresh references and fix forward on this fork

Keeps your fork's identity, branding, and the blue-shade UI changes in `f8d79358`. You absorb roughly six months of API drift yourself.

**Step 1 — Obtain a current game install.**
You need a machine with Gorilla Tag installed (Windows/Steam). This audit could not complete a reference diff directly because no current `Assembly-CSharp.dll` was available locally — `Gorilla-Tag-files/` contains only audio, Blender, and cosmetic assets.

**Step 2 — Refresh the vendored references.**

```bat
robocopy "%GT%\Gorilla Tag_Data\Managed" "References\Managed" *.dll /XO
robocopy "%GT%\BepInEx\core"             "References\core"    *.dll /XO
```

Then add the three new assemblies to `iiMenu.csproj` if you need them:
`K4os.Compression.LZ4.dll`, `BakeryAPV.dll`, `BakeryECS.dll`.

Also update `<GamePath>` in `Directory.Build.props` — it is currently hardcoded to `D:\SteamLibrary\steamapps\common\Gorilla Tag`. The maintained fork uses a conditional fallback worth copying:

```xml
<GamePath Condition="Exists('D:\SteamLibrary\steamapps\common\Gorilla Tag')">D:\SteamLibrary\steamapps\common\Gorilla Tag</GamePath>
<GamePath Condition="!Exists('D:\SteamLibrary\steamapps\common\Gorilla Tag') and Exists('C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag')">C:\Program Files (x86)\Steam\steamapps\common\Gorilla Tag</GamePath>
```

**Step 3 — Build and let the compiler enumerate the drift.**

```bat
dotnet build -c Release 2>&1 | findstr /C:"error CS"
```

Because every patch is bound through `typeof`/`nameof`, the compiler produces an exhaustive list of renamed and removed members. This is your real work queue — §4.4 and §4.5 are a lower bound, not the complete set.

**Step 4 — Apply the known fixes.**

- Rename `GorillaNot` → `MonkeAgent` throughout (27 sites; the method names are unchanged, so a type-name-only replace is safe).
- Repoint `GorillaTelemetry.EnqueueTelemetryEventPlayFab` / `FlushPlayFabTelemetry` to `PlayFabEventsAPI.WriteTelemetryEvents`.
- Remove or re-target the patches on `GorillaGameManager.ValidGameMode`, `GrowingSnowballThrowable.PerformSnowballThrowAuthority`, `RoomSystem.SearchForShuttle`.
- Add a `CallLimiter.Reset` postfix (see §4.5) or accept in-game rate limiting.
- Audit every hardcoded Photon event byte against the current `LoadBalancingClient.OpRaiseEvent` / `PhotonNetwork.OnEvent` signatures.
- Re-validate voice patches (`Speaker.OnAudioFrame`, `Recorder`) against the post-7/10 pipeline and `NetworkSystemPUN.SetupVoice`.
- Re-validate `BuilderTable` / `BuilderPiecePrivatePlot` patches against the current Virtual Stump.
- Re-validate `SubscriptionManager` patches against the current VIM system.
- Re-validate movement mods against Gravity Zones.

**Step 5 — Harden `PatchHandler.PatchAll()` so this class of failure degrades gracefully.**
Move the attribute probe inside the guarded region so a missing type costs one patch, not the whole menu:

```csharp
foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
{
    try
    {
        if (!type.IsClass || type.GetCustomAttribute<HarmonyPatch>() == null)
            continue;

        instance.CreateClassProcessor(type).Patch();
    }
    catch (Exception ex)
    {
        PatchErrors++;
        LogManager.LogError($"Failed to patch {type.FullName}: {ex}");
    }
}
```

Consider also wrapping the `GetTypes()` call itself to tolerate `ReflectionTypeLoadException` and continue with `ex.Types.Where(t => t != null)`. Do this step **first** — it makes every subsequent iteration diagnosable, because the menu will load and log rather than vanish.

**Step 6 — Deal with the dead remote resources.**
Every `ServerResourcePath` URL is a 404. Either:
- vendor the assets locally and load from `PluginInfo.BaseDirectory`, or
- repoint `ServerResourcePath` at your own fork's `Resources/Server` path (this repo does contain a `Resources/` tree), or
- repoint at a live community instance.

Similarly decide what to do with `ServerAPI` (`iidk.online`): stand up your own, or gate the Friends system and telemetry behind a feature flag so the failure path is clean.

### 6.2 Path B — rebase onto the maintained community fork

[`Seralyth/Seralyth-Menu`](https://github.com/Seralyth/Seralyth-Menu) is the successor the upstream author points to. It is at v5.0.1, was pushed on 2026-08-09, and has already absorbed all twelve game updates.

What you inherit:
- Current references and all 93 updated patch files
- Live infrastructure: `ServerResourcePath` → `raw.githubusercontent.com/Seralyth/Seralyth-Menu/master/Resources/Server`, `ServerAPI` → `menu.seralyth.software`
- A `remove detection backdoor` commit (2026-06-01) — worth reading before you adopt or reject it

What it costs you:
- A namespace rename throughout: `iiMenu.*` → `Seralyth.*`
- New plugin GUID: `org.iidk.gorillatag.iimenu` → `org.seralyth.gorillatag.seralythmenu`
- Re-applying your fork's own changes (the blue-shade UI work in `f8d79358` and anything else divergent) on top

### 6.3 Recommendation

**Take Path B if your fork's divergence from upstream is small.** Your delta appears to be primarily cosmetic (`Change background and button colors to blue shades`, typo fixes, PC binding support). Re-applying that on top of a current base is far less work than porting 551 commits and 65 reference DLLs of drift by hand.

**Take Path A only if** you have substantive proprietary changes not present upstream, or you specifically want to own the maintenance.

Either way, **do Step 5 (§6.1) regardless of path.** It is a five-line change that converts a class of silent total failures into logged, survivable ones — and given that the game updates roughly every two weeks, you will hit this again.

### 6.4 Ongoing maintenance expectation

Gorilla Tag has shipped updates about every 14 days. The maintained fork refreshed its vendored references four times in three months. Budget for a reference refresh plus a build-error sweep **after every game update**, not per release cycle.

---

## 7. Verification checklist

Work through this after applying fixes:

- [ ] `dotnet build -c Release` completes with zero `error CS`
- [ ] Plugin loads; BepInEx console shows the ASCII logo banner from `Plugin.Awake()`
- [ ] `LogManager` reports `Patched with 0 errors` (or a known, triaged count)
- [ ] Menu UI renders in-game and responds to input
- [ ] No `TypeLoadException` / `ReflectionTypeLoadException` anywhere in the BepInEx log
- [ ] Remote resource loads succeed (menu click sounds, notification audio) — or fail gracefully if intentionally vendored
- [ ] Movement mods behave correctly **inside a Gravity Zone** (Space map)
- [ ] Voice-related patches do not break normal voice chat
- [ ] Virtual Stump / `BuilderTable` patches work against current save + size-changer features
- [ ] RPC-heavy features are not silently throttled by `CallLimiter`
- [ ] `SubscriptionManager` patches behave sanely against the current VIM system

---

## 8. Reproducing this audit

No Gorilla Tag install is required for the source-level portions.

```bash
# Source-only clone (skips ~800 MB of vendored DLLs)
git clone --filter=blob:none --no-checkout https://github.com/owenobriencodes/maz3n-menu.git src
cd src && git sparse-checkout init --cone
git sparse-checkout set Menu Mods Patches Managers Utilities Classes Extensions
git checkout

# Enumerate the game types this menu patches
grep -rhoE '\[HarmonyPatch\(typeof\([A-Za-z0-9_.]+\)' . \
  | sed -E 's/.*typeof\(//; s/\)//' | sort -u

# Enumerate patched methods as Type::Method
grep -rhoE '\[HarmonyPatch\(typeof\([A-Za-z0-9_.]+\), *nameof\([A-Za-z0-9_.]+\)' . \
  | sed -E 's/.*typeof\(([A-Za-z0-9_.]+)\), *nameof\(([A-Za-z0-9_.]+)\)/\1::\2/' | sort -u

# Diff that set against the maintained fork to expose renames/removals
comm -23 m_ours.txt m_theirs.txt   # present here, gone upstream  -> removed or renamed
comm -13 m_ours.txt m_theirs.txt   # new upstream                 -> new game surface
```

```bash
# Game update history since the fork point
curl -s "https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=1533390&count=15"

# Drift in the maintained fork since this fork's merge-base
gh api "repos/Seralyth/Seralyth-Menu/compare/5ebecbb8...HEAD" \
  --jq '{commits:.total_commits, files:(.files|length)}'

# Live endpoint status
curl -s -o /dev/null -w "%{http_code}\n" https://iidk.online/serverdata
curl -s -o /dev/null -w "%{http_code}\n" \
  https://raw.githubusercontent.com/iiDk-the-actual/iis.Stupid.Menu/master/Resources/Server/Notifications.txt
```

---

## 9. Limitations — what was *not* verified

State these plainly rather than treating the findings above as exhaustive.

1. **No direct assembly diff was performed.** No current `Assembly-CSharp.dll` was available on the audit machine, and Gorilla Tag is not distributed for macOS. The drift inventory in §4.4 and §4.5 is **inferred** from the maintained fork's patch set, so it is a **lower bound**. The authoritative list comes from Step 3 of §6.1 — build against current references and read the compiler errors.
2. **No build was attempted.** Neither `dotnet` nor `mono` is installed on the audit machine, so the compile-failure claim is derived from static analysis of the type references rather than observed compiler output. The `GorillaNot` → `MonkeAgent` rename is nonetheless well-evidenced (§4.2).
3. **No runtime test was performed.** The `PatchHandler` total-failure analysis in §4.3 is a read of the control flow and .NET attribute-resolution semantics, not an observed crash.
4. **Signature-level drift is not covered.** This audit compared type and method *names*. A method that kept its name but changed its parameter list or return type would not appear in §4.4 — the compiler will catch these in Step 3.
5. **Behavioural drift is not covered.** A patch that still compiles and applies cleanly may no longer produce the intended effect if the surrounding game logic changed. Gravity Zones and the Virtual Stump rework are the most likely candidates. Only in-game testing settles this.

### Scope note

This audit answers a build-and-load compatibility question about an existing open-source codebase. It does not evaluate whether any given feature is appropriate to use in a particular context — note that this menu contains features that affect other players, and Gorilla Tag's operators ban for their use in public lobbies. Use in your own fork or in consenting private/modded lobbies is a different matter from public play, and that call is yours.

---

## Appendix A — full inventory of patched game types (81)

`AgeSlider`, `BuilderAttachGridPlane`, `BuilderPiece`, `BuilderPieceInteractor`, `BuilderPiecePrivatePlot`, `BuilderTable`, `BuilderTableNetworking`, `BundleManager`, `CosmeticsController`, `CustomMapManager`, `DeployedChild`, `DreidelHoldable`, `ForceVolume`, `FriendCard`, `FXSystem`, `GameEntityManager`, `GameMode`, `GameObject`, `GliderHoldable`, `GorillaComputer`, `GorillaGameManager`, `GorillaHandClimber`, `GorillaNetworkJoinTrigger`, `GorillaNetworkPublicTestJoin2`, `GorillaNetworkPublicTestsJoin`, **`GorillaNot`** ⚠, `GorillaPlayerScoreboardLine`, `GorillaQuitBox`, `GorillaRopeSwing`, `GorillaServer`, `GorillaSpeakerLoudness`, `GorillaTagger`, `GorillaTagManager`, `GorillaTelemetry` ⚠, `GorillaVelocityEstimator`, `GorillaVelocityTracker`, `GorillaWrappedSerializer`, `GrowingSnowballThrowable` ⚠, `GTPlayer`, `GuardianRPCs`, `HalloweenGhostChaser`, `HalloweenWatcherEyes`, `KIDManager`, `LckTelemetryClient`, `LegalAgreements`, `LightningManager`, `LuauVm`, `LurkerGhost`, `ModIOManager`, `ModIOTermsOfUse_v1`, `NetworkSystemPUN`, `NewMapsDisplay`, `PhotonNetwork`, `PhotonNetworkController`, `Player`, `PlayFabAuthenticator`, `PlayFabClientAPI`, `PlayFabClientInstanceAPI`, `PlayFabDeviceUtil`, `PlayFabHttp`, `PlayFabUnityHttp`, `PlayFabWebRequest`, `PrivateUIRoom`, `ProjectileWeapon`, `PropHuntHandFollower`, `RankedProgressionManager`, `Recorder`, `RequestableOwnershipGuard`, `RoomInfo`, `RoomSystem` ⚠, `SIGadgetChargeBlaster`, `SIGadgetPlatformDeployer`, `SIGadgetWristJet`, `Slingshot`, `SlingshotProjectile`, `SnowballThrowable`, `Speaker`, `SubscriptionManager`, `TakeMyHand_HandLink`, `VRRig`, `VRRigSerializer`

⚠ = confirmed drift, see §4.4.

## Appendix B — sources

- Steam news API, app `1533390` — game update history
- [`Seralyth/Seralyth-Menu`](https://github.com/Seralyth/Seralyth-Menu) — maintained successor fork, used as the current-API reference
- [`itsreallyhex/HexTemplate`](https://github.com/itsreallyhex/HexTemplate) — GT modding template updated 2026-07-21, used to confirm loader/runtime assumptions
- `https://iidk.online/serverdata` — upstream discontinuation notice and kill-switch state
- GitHub REST API — fork lineage, commit history, compare ranges
