# Tylerland: Animal Kingdom — Master Script Architecture & Connection Reference

This document provides a comprehensive technical breakdown of **all 30 scripts** in the codebase (`Assets/Scripts/**/*`), detailing their core responsibilities, configurable parameters, public APIs, and interconnectivity across the project.

---

```mermaid
graph TD
    subgraph Core ["Core Architecture"]
        GFM[GameFlowManager]
        NC[NightClock]
        RM[RunManager]
        MPM[MetaProgressionManager]
        GE[GameEvents (Event Bus)]
        TB[TylerlandBootstrap]
        CDO[CoreDebugOverlay]
    end

    subgraph Config ["Configuration (ScriptableObjects)"]
        AC[AnimatronicConfig]
        NCfg[NightConfig]
        EC[EconomyConfig]
    end

    subgraph Office ["Office & Security Systems"]
        PVC[PlayerViewController]
        DSM[DoorSystemManager]
        DC[DoorController]
        CMS[CameraMonitorSystem]
        OAM[OfficeArtManager]
        HUD[OfficeHUD]
        AR[AttackResolver]
        PS[PhoneSystem]
        P2D[Procedural2DSpriteFactory]
    end

    subgraph Animatronics ["Animatronic & Threat Systems"]
        AM[AnimatronicManager]
        AU[AnimatronicUnit]
        BC[BossController]
        JM[JumpscareManager]
        SCM[SnakeCameraManipulator]
        SWS[SpecialWeaponSystem]
    end

    %% Key Interconnections
    GE --> GFM
    GE --> NC
    GE --> RM
    GE --> AM
    GE --> JM
    GE --> DC

    GFM --> NC
    GFM --> RM
    NC --> AM
    RM --> SWS
    RM --> MPM

    AM --> AU
    AU --> AR
    AR --> DC
    AR --> JM
    SWS --> AU
    AU --> OAM

    CMS --> AM
    CMS --> SCM
    HUD --> DSM
    HUD --> SWS
    HUD --> AR
    PVC --> CMS
    PVC --> DSM
```

---

## 1. Config & Data Systems (`Assets/Scripts/Config`)

### [`AnimatronicConfig.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Config/AnimatronicConfig.cs)
* **Inheritance:** `ScriptableObject`
* **How it works:** Consolidates data definitions for regular animatronics (Tyler, Orville, Sandy, Erle) and the True Final Boss. Controls identity, AI difficulty curve, camera behavior rules, special weapon weaknesses, doorway sprite frames, and jumpscare assets.
* **Parameters / Fields:**
  * `animatronicId`: Unique string ID (`"erle"`, `"tyler"`, `"orville"`, `"sandy"`, `"boss"`).
  * `displayName`, `formerRole`: Descriptive lore labels.
  * `themeColor`: Primary UI / debug color.
  * `targetDoorIndex`: Door index (0=Left, 1=Center, 2=Right, 3=Rear).
  * `activeFromNight`: Night number the character starts moving (e.g., Sandy=1, Orville=2, Erle=3, Tyler=4).
  * `passiveObserverBeforeActiveNight`: True for Tyler (watches lens on Nights 1–3 without attacking).
  * `normalStageCount` / `nightmareStageCount`: Stage length before doorway attack.
  * `cameraRouteIndices`: Camera room indices matching route.
  * `retreatStageIndex`: Stage reverted to on door block (Orville=Stage 2, Others=Stage 1).
  * `attackWindowSeconds` / `nightmareAttackWindowSeconds`: Player reaction time window at doorway.
  * `extraDurabilityLossOnBlock`: Additional door durability damage on block.
  * `energyDrainOnNormalBlock` / `energyDrainOnNightmareBlock`: Percentage door energy depleted on block.
  * `baseTickInterval`: Seconds between movement opportunity checks.
  * `lateNightAIRamp`: Bonus AI added dynamically between 12 AM and 6 AM.
  * `cameraBehavior`: Enum (`None`, `StalledWhenWatchedOnCamera`, `OrvilleProbabilisticAndReversal`, `SandyNightmareDecoys`, `TylerDestroysCameraInNightmare`, `ErleScannerAndGoodBadCop`).
  * `counterWeaponId`: Associated weapon (`"Electric Gun"`, `"Shotgun"`, `"Harpoon"`, `"Spiked Mace"`).
  * 2D Sprite Assets: `cameraStageSprites`, `nightmareCameraStageSprites`, `decoyShellSprite`, `emptyRoomSprite`, `doorwaySpritesByDamage`, `jumpscareFrames`, `nightmareJumpscareFrames`.
  * Audio Clips: `footstepSound`, `blindspotWarningSound`, `doorBlockedBangSound`, `jumpscareScreamSound`, `nightmareJumpscareScreamSound`.
* **Connections:**
  * Consumed by [`AnimatronicUnit`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs) to drive runtime AI logic.
  * Referenced by [`AnimatronicManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs) to instantiate units.
  * Generated and managed by [`TylerlandSetupMenu`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Editor/TylerlandSetupMenu.cs).

---

### [`NightConfig.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Config/NightConfig.cs)
* **Inheritance:** `ScriptableObject`
* **How it works:** Configures difficulty parameters for each night (Nights 1 through 6), defining shift duration, individual AI levels, phone call clips, and boss activation flags.
* **Parameters / Fields:**
  * `nightNumber`: 1 to 6.
  * `realSecondsPerGameHour`: Real seconds per in-game hour (default 60–90s). Total night length = 6 * this value.
  * `tylerAILevel`, `sharkAILevel`, `eagleAILevel`, `snakeAILevel`: Starting base AI levels (0–20).
  * `bossActive`: Enables the Night 6 Boss encounter.
  * `phoneCallClip`: Lore audio played at 12 AM.
  * `phoneCallSubtitles`: Text lines displayed on HUD during phone calls.
  * `muteAllowed`: Whether player can skip/mute the call.
* **Connections:**
  * Injected by [`TylerlandBootstrap`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/TylerlandBootstrap.cs) into [`NightClock`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/NightClock.cs).
  * Read by [`AnimatronicManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs) to seed initial AI levels per night.
  * Read by [`PhoneSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/PhoneSystem.cs) for shift-start phone calls.

---

### [`EconomyConfig.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Config/EconomyConfig.cs)
* **Inheritance:** `ScriptableObject`
* **How it works:** Calculates salary payouts, performance bonuses, and repair cost deductions at 6:00 AM.
* **Parameters / Fields:**
  * `baseSalaryPerNight`: Array of base earnings per night.
  * `cleanNightBonusPercent`: Bonus percent if zero door damage was sustained.
  * `repairDeductionPerLevelPercent`: Percentage deducted for animatronic damage dealt.
  * `minSalaryFractionPercent`: Guaranteed minimum payout floor.
  * `soulCoinsPerDamageLevel`: Persistent Soul Coins awarded for transforming/damaging animatronics.
* **Connections:**
  * Evaluated by [`RunManager.HandleNightCompleted`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/RunManager.cs) to construct `SalaryBreakdown`.
  * Referenced by [`TylerlandSetupMenu`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Editor/TylerlandSetupMenu.cs).

---

## 2. Core Architecture & Game Flow (`Assets/Scripts/Core`)

### [`GameEvents.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameEvents.cs)
* **Inheritance:** Static class
* **How it works:** The central decoupled event bus for the entire project. All systems broadcast and listen to events here without direct hard references.
* **Events Defined:**
  * `OnGameStateChanged(GameState oldState, GameState newState)`
  * `OnNightStarted(int nightNumber)`
  * `OnNightCompleted(int nightNumber)`
  * `OnHourChanged(int displayHour, float hourBlendFactor)`
  * `OnDoorClosed(int doorIndex)` / `OnDoorOpened(int doorIndex)`
  * `OnDoorStatsChanged(int doorIndex, float energy, float durability)`
  * `OnAnimatronicStageChanged(string animatronicId, int oldStage, int newStage)`
  * `OnJumpscare(string animatronicId)`
  * `OnPlayerDied(string killerId)`
  * `OnRunReset()` / `OnRunWon()`
  * `OnMoneyChanged(int totalMoney, int delta)`
  * `OnSalaryCalculated(SalaryBreakdown breakdown)`
  * `OnPauseToggled(bool isPaused)`
* **Connections:** Listened to and fired by almost all systems (`GameFlowManager`, `NightClock`, `RunManager`, `DoorController`, `AnimatronicUnit`, etc.).

---

### [`GameState.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameState.cs)
* **Inheritance:** Enum & static helper class `GameStateRules`
* **How it works:** Enumerates all valid game loop states: `Boot`, `Menu`, `NightIntro`, `NightActive`, `NightEnd`, `Salary`, `Choice`, `Shop`, `Minigame`, `Dead`, `RunReset`, `Victory`. Provides helper queries `IsGameplayActive()`, `CanPause()`, `ShowOfficeHUD()`.
* **Connections:** Used by [`GameFlowManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameFlowManager.cs), [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs), [`AttackResolver`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/AttackResolver.cs), and [`DoorSystemManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs).

---

### [`Singleton.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/Singleton.cs)
* **Inheritance:** `MonoBehaviour` generic base `Singleton<T>`
* **How it works:** Provides thread-safe, lazily resolved singleton instances with `HasInstance` guards to prevent null errors during scene unload.
* **Connections:** Base class for `GameFlowManager`, `RunManager`, `MetaProgressionManager`, `NightClock`, `DoorSystemManager`, `AttackResolver`, `CameraMonitorSystem`, `PhoneSystem`, `JumpscareManager`, `SpecialWeaponSystem`, `AnimatronicManager`, etc.

---

### [`GameFlowManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameFlowManager.cs)
* **Inheritance:** `Singleton<GameFlowManager>`
* **How it works:** Central Finite State Machine (FSM). Validates allowed transitions (e.g. Menu → NightIntro → NightActive → NightEnd/Dead), coordinates screen fades, pauses game time, and broadcasts `OnNightStarted(night)` when entering `NightIntro`.
* **Parameters / Fields:**
  * `_currentState`: Current active `GameState`.
  * `_fadeDuration`: Fade in/out transition timing (0.4s).
  * `_nightIntroDuration`: Black screen intro card delay (2.5s).
  * `_screenFader`: Reference to `ScreenFader`.
* **Connections:**
  * Calls `GameEvents.TriggerGameStateChanged` and `GameEvents.TriggerNightStarted`.
  * Interfaces with [`ScreenFader`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/ScreenFader.cs) for screen transitions.
  * Controlled by UI buttons in [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs).

---

### [`ScreenFader.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/ScreenFader.cs)
* **Inheritance:** `MonoBehaviour`
* **How it works:** Renders an unscaled-time full-screen UI `CanvasGroup` to smoothly transition to/from black during state changes.
* **Parameters / Fields:**
  * `_canvasGroup`: UI alpha canvas group.
* **Connections:** Used exclusively by [`GameFlowManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameFlowManager.cs).

---

### [`NightClock.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/NightClock.cs)
* **Inheritance:** `Singleton<NightClock>`
* **How it works:** Drives the 12:00 AM to 6:00 AM shift clock. Computes `HourBlendFactor` (0.0 at 12 AM to 1.0 at 6 AM) for gradual animatronic AI ramping. Triggers `OnNightCompleted` at 6:00 AM.
* **Parameters / Fields:**
  * `_realSecondsPerHour`: Duration of each in-game hour.
  * `_currentGameHour`: Current fractional hour (0.0f to 6.0f).
  * `_currentDisplayHour`: 12, 1, 2, 3, 4, 5, 6.
  * `_isClockRunning`: Active ticking state.
* **Connections:**
  * Fires `GameEvents.OnHourChanged` and `GameEvents.OnNightCompleted`.
  * Initialized by [`TylerlandBootstrap`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/TylerlandBootstrap.cs).
  * Formatted string displayed on [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs).

---

### [`RunManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/RunManager.cs)
* **Inheritance:** `Singleton<RunManager>`
* **How it works:** Manages all per-run roguelite state: current night, in-run currency, owned upgrades, single-use weapons, damage dealt to animatronics, and night stats history. Restores 1 use of all permanently unlocked weapons from `MetaProgressionManager` on run start.
* **Parameters / Fields:**
  * `_runState`: `RunState` object (night, money, upgrades, weaponsOwned, weaponsUsed, damage dict, seed).
  * `_totalNights`: Default 6 nights.
  * `_economyConfig`: Payout calculation asset.
  * Property `AllFourNightmaresActive`: True when Tyler, Orville, Sandy, and Erle are all in Nightmare state within the same run (unlocks True Final Boss).
* **Connections:**
  * Syncs permanent unlocks with [`MetaProgressionManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/MetaProgressionManager.cs).
  * Weapons consumed by [`SpecialWeaponSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SpecialWeaponSystem.cs).
  * Displayed on [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs) and [`CoreDebugOverlay`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/CoreDebugOverlay.cs).

---

### [`MetaProgressionManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/MetaProgressionManager.cs)
* **Inheritance:** `Singleton<MetaProgressionManager>`
* **How it works:** Persistent meta-progression across runs. Stores Soul Coins, unlocked soul store items, best night reached, stats, and audio/graphics accessibility settings in a local JSON file (`Application.persistentDataPath/meta_save.json`).
* **Parameters / Fields:**
  * `_metaState`: `MetaState` (soulCoins, unlockedSoulItems, bestNightReached, settings).
* **Connections:**
  * Auto-saves on `OnNightCompleted` and `OnRunReset`.
  * Polled by [`SpecialWeaponSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SpecialWeaponSystem.cs) and [`RunManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/RunManager.cs).

---

### [`TylerlandBootstrap.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/TylerlandBootstrap.cs)
* **Inheritance:** `Singleton<TylerlandBootstrap>`
* **How it works:** Scene startup orchestrator. Automatically discovers and links `NightConfig` assets (Nights 1–6) and `EconomyConfig`, subscribes to `OnNightStarted`, and initializes `NightClock`.
* **Parameters / Fields:**
  * `_nightConfigs`: Array of `NightConfig` assets for Nights 1 through 6.
  * `_economyConfig`: Primary `EconomyConfig` asset.
  * `_autoStartNight1OnPlay`: Debug flag for rapid testing.
* **Connections:** Links configs from `Assets/ScriptableObjects` to [`NightClock`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/NightClock.cs) and [`GameFlowManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameFlowManager.cs).

---

### [`CoreDebugOverlay.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/CoreDebugOverlay.cs)
* **Inheritance:** `MonoBehaviour`
* **How it works:** Playtesting overlay toggled via `H` or `O`. Shows game flow state, shift clock, run economy, live animatronic stage/AI/status table, and provides single-key cheats:
  * `[T]`: Advance all active animatronics +1 stage.
  * `[B]`: Summon animatronic directly to currently faced doorway.
  * `[G]`: Grant all 4 special weapons.
  * `[N]`: Skip to 6:00 AM victory.
  * `[J]`: Test jumpscare kill.
  * `[M]`: +$100.
  * `[C]`: +5 Soul Coins.
  * `[R]`: Force run reset.
  * `[P]`: Toggle pause.
* **Connections:** Reads directly from [`GameFlowManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameFlowManager.cs), [`NightClock`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/NightClock.cs), [`RunManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/RunManager.cs), and [`AnimatronicManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs).

---

## 3. Office & Security Systems (`Assets/Scripts/Office`)

### [`PlayerViewController.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/PlayerViewController.cs)
* **Inheritance:** `Singleton<PlayerViewController>`
* **How it works:** Controls 3D camera orientation in the security office (snapping or smooth turning between 4 cardinal angles: Left -90°, Center 0°, Right +90°, Back 180°). Handles `A`/`D`/`W`/`S` keys, edge-panning, and `Space` for the surveillance tablet.
* **Parameters / Fields:**
  * `_rotationSpeed`: Speed of camera turn interpolation.
  * `_currentDirection`: `OfficeViewDirection` enum (`Left`, `Center`, `Right`, `Back`).
  * `_isMonitorOpen`: Active tablet visibility state.
* **Connections:**
  * Toggles [`CameraMonitorSystem.SetMonitorRaised`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs).
  * Directs [`DoorSystemManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs) contextual `E` (door) and `F` (light) hotkeys.
  * Read by [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs) for facing compass and directional threat arrows.

---

### [`DoorController.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorController.cs)
* **Inheritance:** `MonoBehaviour`
* **How it works:** Represents a single security door (0=Left, 1=Center, 2=Right, 3=Rear). Manages steel shutter vertical translation, 2D door sprites, energy drain while closed, durability loss on block, lockout state on 0% energy, and the outside hallway blindspot light (`F`).
* **Parameters / Fields:**
  * `_doorIndex`: 0 to 3.
  * `_doorName`: Descriptive door name.
  * `_doorMeshTransform`: Shutter pivot transform.
  * `_doorSpriteRenderer`: 2D sprite renderer for shutter.
  * `_hallwayLight`: Outside blindspot point light.
  * `_currentEnergy` / `_currentDurability`: 0% to 100% gauges.
  * `_energyDrainClosed` / `_energyDrainLight`: Drain rates per second.
  * `_durabilityLossPerBlock`: Durability lost when stopping an animatronic.
  * `_rechargeDuringNight`: Strict FNAF rule: energy does **not** recharge during the night!
* **Connections:**
  * Coordinated by [`DoorSystemManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs).
  * Queried by [`AttackResolver`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/AttackResolver.cs) to check `IsDoorClosed(doorIndex)`.
  * Drained by [`AnimatronicUnit.HandleBlockedRetreat`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs).
  * Fires `GameEvents.TriggerDoorClosed`, `TriggerDoorOpened`, `TriggerDoorStatsChanged`.

---

### [`DoorSystemManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs)
* **Inheritance:** `Singleton<DoorSystemManager>`
* **How it works:** Central hub managing all 4 `DoorController` instances. Maps hotkeys `1`–`4`, context `E` (toggle faced door), and context `F` (toggle faced door hallway light).
* **Parameters / Fields:**
  * `_doors`: Array of 4 `DoorController` instances.
  * `_allowFacingInteraction`: Enables contextual `E` and `F` controls.
* **Connections:** Interfaces between player input, [`PlayerViewController`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/PlayerViewController.cs), and individual [`DoorController`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorController.cs) instances.

---

### [`AttackResolver.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/AttackResolver.cs)
* **Inheritance:** `Singleton<AttackResolver>`
* **How it works:** Universal arbiter for doorway attacks. When an animatronic reaches the doorway blindspot, it registers an `ActiveThreat` with a countdown reaction timer. If the door is closed, it executes a block, notifies the animatronic to retreat, and triggers durability/energy drain. If the timer expires and the door is open, it triggers a jumpscare and player death.
* **Parameters / Fields:**
  * `_defaultAttackWindow`: Default fallback reaction window (6.0s).
  * `_retreatDelayAfterBlock`: Delay before retreat after impact (0.8s).
  * `_simultaneousThreatCap`: Fairness guard (cap 3).
  * `_activeThreats`: Live list of active threats at doors.
  * `LastBlockStatusMessage`: Confirmation text displayed on HUD after a successful block.
* **Connections:**
  * Called by [`AnimatronicUnit.TryEnterDoorwayBlindspot`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs).
  * Evaluates [`DoorSystemManager.IsDoorClosed`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs).
  * Calls `GameEvents.TriggerJumpscare` and `GameEvents.TriggerPlayerDied` on open door kills.
  * Polled by [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs) and [`CameraMonitorSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs) for reaction time countdown bars.

---

### [`CameraMonitorSystem.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs)
* **Inheritance:** `Singleton<CameraMonitorSystem>`
* **How it works:** Renders the 4-Room 2D Surveillance Tablet GUI (OnGUI). Displays feeds for the 4 dedicated animatronic rooms (`CAM 01` Erle Left, `CAM 02` Tyler Center, `CAM 03` Orville Right, `CAM 04` Sandy Rear). Handles static bursts, Nightmare Tyler's destroyed `CAM 02` feed (audio-only tracking), Nightmare Sandy's shed decoy shells, central empty-room pointers, and the interactive 4-room radar map.
* **Parameters / Fields:**
  * `_rooms`: Array of 4 `HandDrawnCameraFeed` entries.
  * `_currentCameraIndex`: 0 to 3.
  * `_baseFeedNoiseLevel` / `_snakeNoiseBoost`: CRT static levels.
  * `_cameraAudioSource`: Ambient room loop audio.
* **Connections:**
  * Toggled by [`PlayerViewController`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/PlayerViewController.cs).
  * Queries [`AnimatronicManager.GetAnimatronicsInCameraRoom`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs).
  * Distorted by [`SnakeCameraManipulator`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SnakeCameraManipulator.cs).

---

### [`OfficeArtManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeArtManager.cs)
* **Inheritance:** `MonoBehaviour` (`[ExecuteAlways]`)
* **How it works:** Enforces the 100% 2D Sprite aesthetic in the 3D office. Manages 2D wall cutouts, corridor backdrops, desk, floor, ceiling, and blindspot animatronics. Automatically ensures renderers exist at runtime, hides all legacy 3D meshes, and dynamically syncs blindspot animatronic sprites with subtle silhouette lighting in the dark and full brightness when the `F` light is on.
* **Parameters / Fields:**
  * `_wallSlots`: 4 `DirectionalWallArtSlot` definitions (Left, Center, Right, Rear).
  * `_corridorDarkTint` / `_animatronicSilhouetteTint` / `_corridorLitTint`: Dynamic 2D lighting colors.
  * Sprite references for walls, corridors, desk, floor, and ceiling.
* **Connections:**
  * Polled by [`DoorSystemManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs) for light state.
  * Syncs doorway sprites with [`AnimatronicManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs).
  * Generates default textures via [`Procedural2DSpriteFactory`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/Procedural2DSpriteFactory.cs).

---

### [`Procedural2DSpriteFactory.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/Procedural2DSpriteFactory.cs)
* **Inheritance:** Static class
* **How it works:** Runtime 2D procedural pixel-art sprite generator. Ensures the project has 100% 2D sprites for Walls (with doorway cutout), Steel Door Shutters, Corridor Tunnels, Security Desk, Floor, Ceiling, and Animatronics (Normal and Damaged/Nightmare) with zero 3D primitive meshes required.
* **Connections:** Called as asset fallbacks by [`OfficeArtManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeArtManager.cs), [`DoorController`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorController.cs), and [`CameraMonitorSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs).

---

### [`HandDrawnBillboard.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/HandDrawnBillboard.cs)
* **Inheritance:** `MonoBehaviour` (`[ExecuteAlways]`)
* **How it works:** Rotates 2D sprites to face the 3D main camera (with locked Y axis), allowing 2D hand-drawn artwork to exist naturally inside a 3D acoustic room.
* **Parameters / Fields:**
  * `_lockYOnly`: Restricts rotation to horizontal yaw.
  * `_flipHorizontal`: Reverses sprite orientation if drawn facing backwards.
* **Connections:** Attached to arbitrary 2D props or environment billboards facing the player.

---

### [`OfficeHUD.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs)
* **Inheritance:** `MonoBehaviour`
* **How it works:** Diegetic on-screen shift interface:
  * Top-Right: Shift Clock, Night, and True Ending badge.
  * Bottom-Left: 4 Door energy/durability gauges and live animatronic location tags.
  * Bottom-Right: Special weapon status (`Electric Gun`, `Shotgun`, `Harpoon`, `Spiked Mace`).
  * Bottom-Center: View compass & control guide.
  * Edge-of-Screen Directional Arrow Pointers: Flashes Left (`◄◄`), Top (`▲▲`), Right (`►►`), or Bottom (`▼▼`) when an animatronic leaves its room and attacks a door you aren't looking at.
  * In-Doorway Target Pointer & Countdown Bar: Displayed when facing an attacking threat.
  * Auto-Play Night 4 Demo Bot: Toggleable via `V` key or Main Menu button.
* **Connections:** Reads [`NightClock`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/NightClock.cs), [`DoorSystemManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/DoorSystemManager.cs), [`AnimatronicManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs), [`AttackResolver`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/AttackResolver.cs), and [`SpecialWeaponSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SpecialWeaponSystem.cs).

---

### [`PhoneSystem.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/PhoneSystem.cs)
* **Inheritance:** `Singleton<PhoneSystem>`
* **How it works:** Plays telephone calls at 12:00 AM on each night, ducks ambient/music audio channels, and provides live subtitles on the top center of the HUD.
* **Parameters / Fields:**
  * `_audioSource`: Audio player for voice clips.
  * `_currentSubtitle`: Live subtitle string.
* **Connections:**
  * Triggers on `OnNightStarted`.
  * Read by [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs) for subtitle rendering.

---

## 4. Animatronic AI & Threat Systems (`Assets/Scripts/Animatronics`)

### [`AnimatronicManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs)
* **Inheritance:** `Singleton<AnimatronicManager>`
* **How it works:** Oversees the entire animatronic cast according to GDD Section 4:
  * Door 0 (Left / CAM 01): Erle the Eagle (Active Night 3+)
  * Door 1 (Center / CAM 02): Tyler the Lion (Passive Nights 1–3, Attacks Night 4+)
  * Door 2 (Right / CAM 03): Orville the Orca (Active Night 2+)
  * Door 3 (Rear / CAM 04): Sandy the Snake (Active Night 1+)
  * True Final Boss (Active when all 4 are in Nightmare form)
  * Spawns unit GameObjects, seeds AI levels on `OnNightStarted`, and handles testing hotkeys (`T` step, `B` summon).
* **Parameters / Fields:**
  * `_tylerConfig`, `_sharkConfig`, `_eagleConfig`, `_snakeConfig`, `_bossConfig`: Serialized config assets.
  * `_units`: List of runtime `AnimatronicUnit` instances.
* **Connections:**
  * Spawns and initializes [`AnimatronicUnit`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs) instances.
  * Attached to [`BossController`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/BossController.cs).
  * Queried by [`CameraMonitorSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs), [`OfficeArtManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeArtManager.cs), and [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs).

---

### [`AnimatronicUnit.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs)
* **Inheritance:** `MonoBehaviour`
* **How it works:** Individual animatronic state machine executing all GDD Section 4 character rules:
  * **Movement Ticks:** Rolls against `EffectiveAILevel` / 20 periodically.
  * **Tyler:** Passive observer on Nights 1–3. Firing Shotgun breaks head casing, halts attacks for that night, and triggers Nightmare Tyler who destroys `CAM 02`.
  * **Orville:** Normal camera view rolls 80% retreat 1 stage / 20% advance 2 stages. Door block drains Durability and Energy and retreats to Stage 2. Harpoon triggers Nightmare Orville (watching advances him 1 stage, 30s unattended retreats, 60s unattended triggers auto-attack).
  * **Sandy:** Normal 4 stages, no fake feeds. Spiked Mace triggers Nightmare Sandy (5 stages, 8s reaction window, heavy energy drain, sheds decoy shells on `CAM 04`).
  * **Erle:** Identity scanner check (25% Authorized Staff cooldown; otherwise Intruder). Electric Gun triggers Nightmare Erle (Good Cop slow / Bad Cop fast). Closing Left Door unnecessarily triggers Broken Authority (switches him to Bad Cop).
  * **Doorway Arrival:** Invokes `AttackResolver.RegisterAttackArrival` upon reaching final stage.
* **Parameters / Fields:**
  * `_config`: Assigned `AnimatronicConfig`.
  * `_currentStage`: Current stage index.
  * `_isAtDoorBlindspot`: True when attacking at the doorway.
  * `_isNightmare`: True if damaged.
  * `_isPassiveObserver`: True for Tyler before Night 4.
  * `_isCameraDestroyed`: True for Nightmare Tyler.
  * `_hasActiveDecoyShell`: True for Nightmare Sandy decoy.
  * `_isBadCopMode`: True for Nightmare Erle Bad Cop.
* **Connections:**
  * Coordinates with [`AttackResolver`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/AttackResolver.cs) for doorway resolution.
  * Interacts with [`CameraMonitorSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs).
  * Transformed by [`SpecialWeaponSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SpecialWeaponSystem.cs).

---

### [`BossController.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/BossController.cs)
* **Inheritance:** `MonoBehaviour`
* **How it works:** Controls the True Final Boss ("The Amalgam"). Escalates across 3 phases based on `NightClock.HourBlendFactor`. Dynamically shifts its attack vector across all 4 office doors (0 to 3) rather than staying on one door.
* **Parameters / Fields:**
  * `_isBossActive`: True only when all 4 animatronics are in Nightmare state.
  * `_currentPhase`: Phase 1, 2, or 3.
  * `_bossUnit`: Reference to Boss `AnimatronicUnit`.
* **Connections:**
  * Managed by [`AnimatronicManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicManager.cs).
  * Overrides door target on [`AnimatronicUnit.SetDynamicDoorOverride`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs).

---

### [`JumpscareManager.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/JumpscareManager.cs)
* **Inheritance:** `Singleton<JumpscareManager>`
* **How it works:** Full-screen jumpscare and death sequencer:
  1. Closes surveillance tablet immediately.
  2. Snaps camera rotation to face the killer's doorway.
  3. Executes procedural 3D camera shake.
  4. Renders full-screen 2D hand-drawn jumpscare animation frames (Normal vs. Nightmare frames).
  5. Displays GDD character-specific kill flavor text and the Game Over death screen.
* **Parameters / Fields:**
  * `_jumpscareDuration`: Active jumpscare timing (1.6s).
  * `_cameraShakeMagnitude`: Shake intensity (6.5f).
  * `_killerId`, `_killerDisplayName`, `_wasKillerNightmare`, `_killFlavorDescription`.
* **Connections:**
  * Triggered via `GameEvents.OnJumpscare`.
  * Snaps camera using [`PlayerViewController`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/PlayerViewController.cs).
  * Initiates run reset through [`RunManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/RunManager.cs) and [`GameFlowManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/GameFlowManager.cs).

---

### [`SnakeCameraManipulator.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SnakeCameraManipulator.cs)
* **Inheritance:** `Singleton<SnakeCameraManipulator>`
* **How it works:** Nightmare Sandy camera spoofing. When Sandy is in Nightmare state, he can spoof camera feeds (showing static noise or freezing a false stage) while secretly advancing towards the Rear Door.
* **Parameters / Fields:**
  * `_isFeedCurrentlySpoofed`: Active spoof state.
  * `_spoofedCameraIndex`: Targeted camera.
  * `_staticNoiseBoost`: Extra static sent to tablet.
* **Connections:**
  * Injects noise and feed overrides into [`CameraMonitorSystem`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/CameraMonitorSystem.cs).
  * Controlled by [`AnimatronicUnit`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs) (Sandy).

---

### [`SpecialWeaponSystem.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/SpecialWeaponSystem.cs)
* **Inheritance:** `Singleton<SpecialWeaponSystem>`
* **How it works:** Handles deployment of the 4 special counter-weapons (`Q` key):
  * Left Door (Door 0): `Electric Gun` vs. Erle the Eagle
  * Center Door (Door 1): `Shotgun` vs. Tyler the Lion
  * Right Door (Door 2): `Harpoon` vs. Orville the Orca
  * Rear Door (Door 3): `Spiked Mace` vs. Sandy the Snake
  * Firing a weapon permanently unlocks it across deaths in `MetaProgressionManager` (restoring 1 use every run), interrupts the animatronic attack, damages them, and triggers their Nightmare transformation.
* **Parameters / Fields:**
  * `LastStatusMessage`: Confirmation feedback shown on HUD.
  * `GrantAllWeapons()`: Cheat/testing helper.
* **Connections:**
  * Interacts with [`RunManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/RunManager.cs) (consumes weapon) and [`MetaProgressionManager`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Core/MetaProgressionManager.cs) (persists unlock).
  * Invokes [`AnimatronicUnit.OnHitBySpecialWeapon`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Animatronics/AnimatronicUnit.cs).
  * Triggered via hotkey or [`OfficeHUD`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Office/OfficeHUD.cs).

---

## 5. Editor & Automation (`Assets/Scripts/Editor`)

### [`TylerlandSetupMenu.cs`](file:///C:/Users/abpan/Documents/GitHub/GameDevelopmentProject/GameDevelopmentFinal/Assets/Scripts/Editor/TylerlandSetupMenu.cs)
* **Inheritance:** Static class (`UnityEditor`)
* **How it works:** Top Unity menu bar automation tool (`Tylerland → ...`):
  1. `1. Generate Default Config Assets`: Automatically generates and configures `Tyler.asset`, `Orville.asset`, `Erle.asset`, `Sandy.asset`, `Boss.asset`, `Night_1.asset` through `Night_6.asset`, and `EconomyConfig.asset` according to GDD Section 4.
  2. `2. Setup Core Systems in Current Scene`: Instantiates `[GAME_CORE]` with all Singletons and event connections.
  3. `3. Connect Configs to [GAME_CORE]`: Injects ScriptableObjects into scene references.
  4. `4. Build Complete 2D Sprite Office Environment in Scene`: Builds the complete 100% 2D Sprite office hierarchy (4 Walls with doorway cutouts, 4 Corridors, 4 Steel Door Shutters, 4 Doorway Blindspot Animatronic layers, Desk, Floor, Ceiling, Main Camera, and all Manager components) with one click.
* **Connections:** Editor builder referencing almost all runtime scripts to create asset files and build scenes automatically.

---

## Complete 30-Script System Matrix

| # | Script Name | Folder | Base Type | Primary Role & Responsibility | Key Connected Scripts |
|---|---|---|---|---|---|
| **1** | `AnimatronicConfig.cs` | `Config/` | `ScriptableObject` | Data container for animatronics, AI parameters, GDD behaviors & sprites | `AnimatronicUnit`, `AnimatronicManager`, `TylerlandSetupMenu` |
| **2** | `EconomyConfig.cs` | `Config/` | `ScriptableObject` | Payout math, salary deductions, and Soul Coin awards | `RunManager`, `TylerlandBootstrap` |
| **3** | `NightConfig.cs` | `Config/` | `ScriptableObject` | Per-night configuration (seconds/hour, base AI table, boss flag) | `NightClock`, `TylerlandBootstrap`, `AnimatronicManager` |
| **4** | `GameEvents.cs` | `Core/` | `static class` | Decoupled global event bus for the entire project | All systems |
| **5** | `GameState.cs` | `Core/` | `enum / static class` | Finite state machine enum and gameplay rule queries | `GameFlowManager`, `OfficeHUD`, `DoorSystemManager` |
| **6** | `Singleton.cs` | `Core/` | `MonoBehaviour` | Thread-safe generic singleton base with `HasInstance` guard | All manager Singletons |
| **7** | `GameFlowManager.cs` | `Core/` | `Singleton` | Core loop state machine, scene fades, and night start broadcaster | `ScreenFader`, `GameEvents`, `RunManager`, `OfficeHUD` |
| **8** | `ScreenFader.cs` | `Core/` | `MonoBehaviour` | Unscaled UI canvas fade to/from black during transitions | `GameFlowManager` |
| **9** | `NightClock.cs` | `Core/` | `Singleton` | 12 AM – 6 AM shift clock, hour blend calculation, night completion | `TylerlandBootstrap`, `GameEvents`, `OfficeHUD` |
| **10** | `RunManager.cs` | `Core/` | `Singleton` | Current run state, weapons, upgrades, damage tracking, victory check | `MetaProgressionManager`, `SpecialWeaponSystem`, `OfficeHUD` |
| **11** | `MetaProgressionManager.cs` | `Core/` | `Singleton` | Persistent JSON save data (Soul Coins, permanent weapons, settings) | `RunManager`, `SpecialWeaponSystem`, `GameEvents` |
| **12** | `TylerlandBootstrap.cs` | `Core/` | `Singleton` | Injects ScriptableObject configs into scene singletons on boot | `NightClock`, `GameFlowManager`, `AnimatronicManager` |
| **13** | `CoreDebugOverlay.cs` | `Core/` | `MonoBehaviour` | Runtime diagnostic HUD and single-key playtesting cheat panel | `GameFlowManager`, `NightClock`, `AnimatronicManager` |
| **14** | `PlayerViewController.cs` | `Office/` | `Singleton` | 3D camera look orientation, 4 angles (A/D/W/S), tablet toggle | `CameraMonitorSystem`, `DoorSystemManager`, `OfficeHUD` |
| **15** | `DoorController.cs` | `Office/` | `MonoBehaviour` | Steel shutter movement, energy drain, durability wear, lockout, light | `DoorSystemManager`, `AttackResolver`, `AnimatronicUnit` |
| **16** | `DoorSystemManager.cs` | `Office/` | `Singleton` | Manages 4 doors, direct hotkeys (1-4), and contextual E/F hotkeys | `DoorController`, `PlayerViewController`, `AttackResolver` |
| **17** | `AttackResolver.cs` | `Office/` | `Singleton` | Doorway attack arbiter, reaction timers, door block resolution | `DoorSystemManager`, `AnimatronicUnit`, `JumpscareManager` |
| **18** | `CameraMonitorSystem.cs` | `Office/` | `Singleton` | 4-Room 2D security tablet, radar map, Tyler destroyed feed | `PlayerViewController`, `AnimatronicManager`, `SnakeCameraManipulator` |
| **19** | `OfficeArtManager.cs` | `Office/` | `MonoBehaviour` | 100% 2D sprite layers (walls, corridors, desk, floor), dynamic light | `DoorSystemManager`, `AnimatronicManager`, `Procedural2DSpriteFactory` |
| **20** | `Procedural2DSpriteFactory.cs` | `Office/` | `static class` | Runtime pixel-art generator for 2D walls, shutters, desks, characters | `OfficeArtManager`, `DoorController`, `CameraMonitorSystem` |
| **21** | `HandDrawnBillboard.cs` | `Office/` | `MonoBehaviour` | Billboards 2D sprites towards camera in the 3D office space | `Camera.main` |
| **22** | `OfficeHUD.cs` | `Office/` | `MonoBehaviour` | Diegetic HUD, directional threat arrows, countdowns, Auto-Play Bot | `NightClock`, `DoorSystemManager`, `AttackResolver`, `SpecialWeaponSystem` |
| **23** | `PhoneSystem.cs` | `Office/` | `Singleton` | 12 AM phone calls, audio ducking, subtitle presentation | `NightConfig`, `GameEvents`, `OfficeHUD` |
| **24** | `AnimatronicManager.cs` | `Animatronics/` | `Singleton` | Spawns and coordinates Tyler, Orville, Sandy, Erle, and True Boss | `AnimatronicUnit`, `BossController`, `CameraMonitorSystem` |
| **25** | `AnimatronicUnit.cs` | `Animatronics/` | `MonoBehaviour` | Character AI state machine, GDD mechanics, blindspot attacks | `AnimatronicConfig`, `AttackResolver`, `CameraMonitorSystem`, `SpecialWeaponSystem` |
| **26** | `BossController.cs` | `Animatronics/` | `MonoBehaviour` | Night 6 Boss 3-phase progression and dynamic door switching | `AnimatronicManager`, `AnimatronicUnit`, `NightClock` |
| **27** | `JumpscareManager.cs` | `Animatronics/` | `Singleton` | Jumpscare presentation, 2D animation, camera shake, death menu | `PlayerViewController`, `GameEvents`, `GameFlowManager` |
| **28** | `SnakeCameraManipulator.cs` | `Animatronics/` | `Singleton` | Nightmare Sandy feed spoofing and CRT static manipulation | `CameraMonitorSystem`, `AnimatronicUnit` |
| **29** | `SpecialWeaponSystem.cs` | `Animatronics/` | `Singleton` | Q-key weapon dispatch, permanent unlock handling, Nightmare triggers | `RunManager`, `MetaProgressionManager`, `AnimatronicUnit`, `OfficeHUD` |
| **30** | `TylerlandSetupMenu.cs` | `Editor/` | `static class` | 1-Click Unity Editor automation for configs and 2D sprite scenes | All Configs, `TylerlandBootstrap`, `OfficeArtManager` |
