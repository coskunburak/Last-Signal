# Project: Last Signal

<div align="center">

![Unity 6](https://img.shields.io/badge/Unity-6000.5.0f1_(6000_LTS)-black?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-12.0_%2F_.NET_Standard_2.1-blue?logo=csharp&logoColor=white)
![Pipeline](https://img.shields.io/badge/Render_Pipeline-HDRP_17.5.0-orange?logo=unity)
![Architecture](https://img.shields.io/badge/Architecture-Hexagonal_%2F_Clean_DDD-green)
![Tests](https://img.shields.io/badge/Tests-950%2B_Passed_(EditMode_%26_PlayMode)-success)
![Target](https://img.shields.io/badge/Performance-1080p_%40_60fps_%2F_4K_%40_30fps-purple)
![License](https://img.shields.io/badge/License-Proprietary-red)

**A Systemic, High-Fidelity Post-Apocalyptic Survival Horror Experience Built with Domain-Driven Design & High-Performance Unity Engineering.**

[Overview](#-executive-summary) •
[Architecture](#-software-architecture--engineering-principles) •
[Core Subsystems](#-core-gameplay-subsystems) •
[HDRP & Graphics](#-hdrp-rendering-pipeline--visual-fidelity) •
[Testing & CI/CD](#-automated-testing--verification-suite) •
[CV / Portfolio Highlights](#-key-engineering-competencies-cv--portfolio) •
[Getting Started](#-setup--developer-guide)

</div>

---

## 📖 Executive Summary

**Project: Last Signal** is an uncompromising, systemic survival horror game set across the rural expanse of *Pine Ridge County* and the dilapidated township of *Ash Creek*. Following a catastrophic infrastructure collapse and virulent outbreak, the player assumes the role of an isolated survivor navigating persistent environmental hazards, scarce ammunition, acoustic dangers, and an adaptive infected ecosystem. The overarching objective is to restore regional emergency broadcast relays ("The Last Signal") while securing fortified safehouses and managing vital metabolic functions.

Engineered from the ground up with **Pure C# Domain-Driven Design (DDD)** and **Hexagonal (Ports & Adapters) Architecture**, the project treats `UnityEngine` strictly as an execution runtime and presentation host rather than the foundation of game rules. State mutations operate on **ACID-like transaction boundaries with escrow guarantees**, preventing item duplication, corrupted save states, or ghost ballistics.

### Core Product Pillars
1. **Systemic Survival & Biological Degradation:** Strict metabolic depletion (hydration, calories, body temperature, stamina), granular wound trauma, and blood volume tracking.
2. **Deterministic Ballistics & Frame-Accurate Combat:** Staged multi-phase firearm reload transactions, magazine retention mechanics, dual-raycast muzzle occlusion, melee swing collision deduplication, and dynamic physical dismemberment.
3. **Acoustic Simulation & Dynamic World Pressure:** Environmental sound wave propagation factoring in spatial portals and atmospheric masking, tied directly to an AI Encounter Director that calculates regional disturbance, human presence, and resource depletion.
4. **Perceptual AI & Demographic Conservation:** Sensory hearing, sight frustum, and olfactory simulation with an asynchronous scheduling pipeline and a 4-tier Simulation LOD that tracks zombie populations seamlessly across active cells and dematerialized regional token ledgers without un-teleported respawning.
5. **Modular Fortification, Power Grids & Crafting:** Socket-based base claiming, deterministic power load-shedding with fuel consumption, and multi-tier crafting engines operating via transactional escrow.
6. **Vehicular Traversal & Physics Impact:** Full drivetrain, torque curve, and suspension physics simulation with persistent vehicular trunk storage and bone-breaking kinetic collision responses against infected hordes.

---

## 🏛 Software Architecture & Engineering Principles

The defining technical strength of *Project: Last Signal* is its strict adherence to modern enterprise software engineering patterns ported into high-performance game programming. The codebase enforces a clean separation of concerns, ensuring high testability, zero-allocation runtime hot-paths, and complete engine decoupling.

```mermaid
flowchart TD
    subgraph Presentation_Adapters["Presentation & Unity Adapters (Outer Layer)"]
        UI[uGUI / Toolkit View Presenters]
        Anim[Mecanim & Rigging Presenters]
        Audio[FMOD / Unity Audio Presenters]
        InputAdapter[New Input System Synthetic Adapters]
        Phys[Physics Raycast / Sweep Portals]
    end

    subgraph Application_Layer["Application Services & Coordinators"]
        SessionCoord[Session & Scene Lifecycle Coordinator]
        CombatCoord[Combat & Weapon Fire Coordinator]
        AIOrchestrator[AI Scheduler & Population Manager]
        StreamingCoord[World Cell & Chunk Streaming Manager]
        PersistCoord[Persistence & Snapshot Coordinator]
    end

    subgraph Pure_Domain["Domain Layer (Pure C# - Zero UnityEngine Dependency)"]
        Entities[Player, Weapon, Zombie, Vehicle State]
        Transactions[Discrete Command / Event Transactions]
        CraftingEngine[Recipe & Escrow Verification]
        AcousticModel[Noise Propagation & Pressure Decay Formulas]
        SaveDTO[Immutable Save Schema DTOs]
    end

    subgraph Persistence_IO["Storage & Hardware"]
        DiskIO[Atomic Disk Multi-File Store]
        SchemaMigrator[Versioned Save Migrator]
    end

    InputAdapter --> Application_Layer
    Application_Layer --> Pure_Domain
    Pure_Domain --> Presentation_Adapters
    Application_Layer --> Presentation_Adapters
    Pure_Domain --> SaveDTO
    SaveDTO --> PersistCoord
    PersistCoord --> DiskIO
    DiskIO --> SchemaMigrator
```

### 1. Hexagonal Ports & Adapters (Clean Architecture)
* **Pure C# Domain:** All gameplay calculations—inventory management, ballistic damage resolution, crafting recipes, status effects, and save DTO structures—are compiled into pure C# logic that contains **zero references to `UnityEngine.Object` or `MonoBehaviour`**.
* **Decoupled Testing:** Over 80% of game mechanics can be verified instantaneously in headless **EditMode** unit tests without loading Unity scenes, initializing gameobjects, or spinning up renderers.
* **Assembly Definition Slices (`asmdef`):** Modular code boundaries enforce unidirectional dependencies:
  * `LastSignal.Runtime.asmdef`: Core domain contracts, simulation logic, and Unity adapters.
  * `LastSignal.Editor.asmdef`: Production authoring tools, inspectors, and CI/CD validation tools.
  * `LastSignal.Tests.asmdef`: EditMode and PlayMode integration test suites with synthetic input bindings.

### 2. Transactional Command & Event Pattern with Escrow Guarantees
To eradicate the ubiquitous item duplication bugs, ammo anomalies, and phantom inventory states that plague survival games, all state mutations are executed through transactional commands.

```mermaid
sequenceDiagram
    autonumber
    actor Player
    participant Controller as PlayerInventoryController
    participant Tx as InventoryTransactionCoordinator
    participant Escrow as TransactionEscrow
    participant Container as ContainerService
    participant EventBus as DomainEventBus

    Player->>Controller: RequestTransfer(Item, SourceSlot, TargetContainer)
    Controller->>Tx: BeginTransaction(TxId, RevisionLock)
    Tx->>Tx: ValidateCapacity & Integrity
    alt Validation Failed
        Tx-->>Controller: Reject(ResultCode.InsufficientSpace)
        Controller-->>Player: Show UI Feedback (No State Mutated)
    else Validation Succeeded
        Tx->>Escrow: LockItem(ItemInstance)
        Tx->>Container: RemoveFromSource(SourceSlot)
        Tx->>Container: CommitToTarget(TargetSlot)
        Tx->>Escrow: ReleaseLock()
        Tx->>Tx: CommitTransaction()
        Tx->>EventBus: Publish(ItemTransferredEvent)
        EventBus-->>Player: Update Inventory UI & Play Audio
    end
```

* **ACID Semantics:** If an inventory transfer, reload sequence, or crafting operation fails midway (e.g., player stunned, container destroyed, game closed), the transaction either rolls back completely or safely parks items in escrow.
* **Revision Locking:** Entities maintain optimistic concurrency tokens (`ExpectedRevision`), preventing race conditions between asynchronous worker threads, AI queries, and player input frames.

### 3. Quad-Clock Simulation & Analytical Time-Lapse Engine
Games that conflate wall-clock time with simulation time suffer from physics instability, broken pause states, and inaccurate background progression. *Project: Last Signal* separates time into four distinct domains:
1. **Real Monotonic Time (`Time.unscaledTime`):** Drives UI animations, loading timeouts, and input debounce buffers.
2. **Simulation Elapsed Time:** Governs kinematic character locomotion, weapon recoil springs, projectile ballistics, and melee swing arcs.
3. **World Elapsed Time (`WorldClock`):** Drives biological metabolism (hunger, thirst, sepsis), atmospheric weather cycles, generator fuel consumption, and celestial sun positioning.
4. **Analytical Sleep Dilation:** When the player sleeps in a fortified bed, the engine advances world state by hours in a single analytical tick. Rather than simulating millions of redundant physics steps, the `WorldSimulation` executes an analytical differential equation:
   $$\text{Hydration}_{\text{new}} = \text{Hydration}_{\text{current}} - (\text{MetabolicRate} \times \Delta t_{\text{sleep}})$$
   $$\text{Disturbance}_{\text{new}} = \text{Disturbance} \times 2^{-\Delta t_{\text{sleep}} / t_{1/2}}$$

---

## 🕹 Core Gameplay Subsystems

---

### 1. Sensory-Perceptual AI & Demographics Engine

The enemy artificial intelligence does not rely on simple distance checks or omniscient wall-hacks. Infected entities are driven by a multi-channel sensory perception pipeline combined with demographic conservation.

```mermaid
stateDiagram-v2
    [*] --> IdleWander: Spawn / Patrol Anchor
    IdleWander --> Investigate: Stimulus Heard (Hearing Event)
    Investigate --> Search: Reached Stimulus Point (Target Lost)
    Search --> IdleWander: Search Budget Expired
    
    IdleWander --> Chase: Target Confirmed (Vision Frustum)
    Investigate --> Chase: Target Confirmed (Vision Frustum)
    Search --> Chase: Visual Re-acquisition
    
    Chase --> Attack: In Attack Range & LOS
    Attack --> Chase: Attack Recovery Finished
    Chase --> Search: Line of Sight Broken (> TTL)
    
    Chase --> Stagger: Impact Exceeds Stagger Threshold
    Attack --> Stagger: Counter-Attack Parried
    Stagger --> Chase: Stagger Recovery
    
    Chase --> Dead: Health Reaches 0
    Attack --> Dead: Health Reaches 0
    Stagger --> Dead: Health Reaches 0
    Dead --> [*]: Corpse Persistence / Dismemberment
```

#### Multi-Stimulus Sensory Perception
* **Vision Cone:** Combines horizontal/vertical FOV queries, distance falloff curves, light level attenuation (flashlight, moonlight, indoor shadow), and obstacle raycasts. Raycasts are distributed across frames via an asynchronous time-sliced scheduler (`AIScheduler`), capping CPU overhead to $\le 2\text{ms}$.
* **Auditory Memory (`ZombieAuditoryMemory`):** Hearing events store `LastKnownPosition`, `StimulusIntensity`, and a decaying `Confidence` metric. When the player breaks line of sight, enemies do not automatically track coordinates; they converge on the last confirmed acoustic locus and transition into structured search sweeps.
* **Infected Archetypes (`ZombieArchetypeCatalog`):**
  * **Shambler:** Standard resilient infected with slow movement, relentless pathing, and high blunt resistance.
  * **Lurker:** Ambient ambushers hidden in indoor furniture or floor rubble, activating upon close proximity with sudden velocity bursts.
  * **Runner:** Hyper-aggressive, low-health sprinters capable of vaulting waist-high cover.
  * **Howler:** Siren archetype that emits high-decibel shrieks, generating massive `GameplayNoiseEvent` bursts that alert all infected in adjacent cells.
  * **Armored:** Riot gear / ballistic helmet infected requiring precise weakpoint hits or high-penetration caliber ammunition.

#### 4-Tier Simulation Level-of-Detail (LOD)
To support hundreds of infected across open-world terrain without bottlenecking the CPU:
1. **Tier 1 (Immediate Range, $<30\text{m}$):** Full 20 Hz decision tree, active NavMeshAgent pathfinding, full root-motion animation, and frame-accurate multi-zone hitboxes.
2. **Tier 2 (Medium Range, $30\text{m} - 80\text{m}$):** 10 Hz throttled perception ticks, simplified steering calculations, and low-priority animation rigging.
3. **Tier 3 (Distant Range, $80\text{m} - 200\text{m}$):** Coarse 2 Hz path extrapolation on NavMesh surface without local avoidance obstacle sweeps.
4. **Tier 4 (Unloaded Cells, $>200\text{m}$):** Entities dematerialize into **Demographic Group Tokens** (`WorldPopulationManager`). The system preserves exact demographic mass (species distribution, wounded state, entity ID) inside a ledger. When the cell streams back in, the exact surviving agents materialize deterministically without artificial spawning.

---

### 2. Ballistics, Melee & Visceral Combat Subsystem

The combat engine is built around physical weight, commitment, and mechanical predictability.

```mermaid
stateDiagram-v2
    [*] --> Unequipped
    Unequipped --> Equipping: Equip Command
    Equipping --> Ready: Animation Callback + Generation Match
    
    state Ready {
        [*] --> Idle
        Idle --> Firing: Fire Command (Valid Chamber)
        Firing --> Idle: Action Cycle Finished
        Idle --> Reloading: Reload Command (Valid Reserve)
        Reloading --> Idle: Stage 5 Committed (Ready)
        Idle --> ClearingJam: Clear Command (After Misfire)
        ClearingJam --> Idle: Jam Cleared
    }
    
    Ready --> Unequipping: Unequip / Switch Command
    Unequipping --> Unequipped: Holster Completed
```

#### Firearm Mechanics & Dual-Raycast Occlusion
* **Staged Reload Transactions:** Reloads are divided into 5 distinct commit checkpoints:
  $$\text{Reserve Locked} \longrightarrow \text{Magazine Detached} \longrightarrow \text{New Magazine Inserted} \longrightarrow \text{Round Chambered} \longrightarrow \text{Ready}$$
  Interrupting a reload (e.g., sprinting or taking damage) preserves the exact mechanical state. A player who ejected their magazine cannot fire until a new magazine is inserted; ammunition is never miraculously created or destroyed.
* **Dual-Raycast Solver:** Eliminates the classic first-person clipping bug where players stick weapon barrels through walls to shoot through solid barriers:
  * *Ray 1 (Camera to Aim Target):* Determines intended point-of-aim.
  * *Ray 2 (Physical Muzzle to Target):* Verifies physical muzzle clearance. If a wall, doorframe, or pillar obstructs the muzzle ray, the shot impacts the obstacle regardless of crosshair alignment.
* **Stance-Dependent Ballistic Spread:** Cone-of-fire spread dynamically modulates based on movement speed, crouch stance, stamina depletion, weapon heat, and pain trauma.

#### Melee Combat & Arc Sweep Deduplication
* **Windowed Hit Detection:** Melee weapons (combat knife, crowbar, splitting axe) do not use continuous collision detection that triggers multiple hits per frame. Instead, a deterministic attack resolver opens an **Active Hit Window** during the swing apex.
* **Spatial Deduplication:** Each attack instance generates a unique `SwingId`. During the sweep, an entity's colliders are indexed; a zombie with multiple limb colliders can only receive **one damage receipt per swing**, completely preventing frame-rate dependent damage stacking.
* **Dynamic Dismemberment & Wound System:** Incorporating mesh fracture algorithms (`OpenFracture`) and dynamic gore masks (`ZombieDismemberment`), lethal strikes to specific hit regions (head, left arm, right leg) sever limbs, trigger Ragdoll transitions, and spawn localized blood decals based on surface normal angles.

---

### 3. Acoustic Noise Engine & Dynamic World Pressure

Sound in *Last Signal* is a tangible survival mechanic. Every player action—footsteps on gravel, opening squeaky doors, melee swing impacts, gunshot muzzle blasts, and vehicle ignitions—broadcasts a spatial `GameplayNoiseEvent`.

#### Acoustic Attenuation Model
Sound propagates through world cells using physical decay equations modified by environmental conditions:
$$R_{\text{effective}} = R_{\text{base}} \times T_{\text{environment}} \times M_{\text{weather}}$$

Where:
* $R_{\text{base}}$ = Unattenuated acoustic radius (e.g., 9mm gunshot = $120\text{m}$, sprint footstep = $12\text{m}$).
* $T_{\text{environment}}$ = Structural transmission coefficient (Open Field = $1.0$, Dense Pine Forest = $0.75$, Reinforced Concrete Wall = $0.2$, Sealed Door = $0.35$).
* $M_{\text{weather}}$ = Atmospheric masking multiplier (Heavy Rainstorm = $0.6$, Dense Fog = $0.85$, Calm Night = $1.1$).

#### Three-Channel World Pressure Model
The county remembers player presence through a tripartite mathematical model:
1. **Disturbance ($D$):** Short-term acoustic agitation. Spikes upon weapon fire and engine revving; decays exponentially with a 3-hour half-life:
   $$D(t + \Delta t) = \operatorname{clamp}\left(D(t) \cdot 2^{-\Delta t / t_{1/2}} + \sum \text{NoiseEvents}, \, 0, \, 100\right)$$
2. **Presence ($P$):** Long-term cumulative survivor activity (generator usage, repeated travel). Decays over multiple in-game days.
3. **Depletion:** Percentage of permanent containers looted and resources scavenged in that chunk.

#### Dynamic AI Encounter Director
The Encounter Director analyzes the composite Danger Score ($0.7D + 0.3P$) and cycles through four operational states:
$$\text{Calm} \longrightarrow \text{Build} \longrightarrow \text{Peak} \longrightarrow \text{Recovery}$$
Instead of spawning teleporting zombies behind the player's back, high pressure causes adjacent wandering hordes to calculate pathing vectors toward the disturbance zone, creating emergent siege encounters.

---

### 4. World Streaming & Chunk Lifecycle

The game world is structured into an infinite-ready grid of **64-meter chunks** managed by the `WorldCellManager`.

```mermaid
flowchart LR
    subgraph Cell_Grid["Spatial Grid (64m x 64m)"]
        C1["Cell (X, Y-1)<br/>Unloaded (Disk DTO)"]
        C2["Cell (X-1, Y)<br/>Leased / Hydrated"]
        C3["Cell (X, Y)<br/>Active (Player Present)"]
        C4["Cell (X+1, Y)<br/>Leased / Hydrated"]
        C5["Cell (X, Y+1)<br/>Unloaded (Disk DTO)"]
    end

    C3 <-->|Portal Boundary| C2
    C3 <-->|Portal Boundary| C4
    C2 -.->|Stream In| C1
    C4 -.->|Stream Out| C5
```

* **Two-Cell Active Window Leasing:** The engine only keeps the player's active cell and immediate neighboring transition cells hydrated in memory.
* **Deterministic Delta Tracking:** When a cell unloads, it does not discard modified state. A lightweight delta snapshot records:
  * Loot containers opened / emptied.
  * Broken window panes and breached door modules.
  * Loose items dropped on terrain (`WorldItem`).
  * Zombie corpses and bloodstain persistence.
* **Seamless Streaming:** Background asynchronous serialization ensures that moving between cells introduces **0 frame drops** and zero garbage collection spikes.

---

### 5. Deterministic Persistence & Schema Migration Pipeline

Player progression is secured by a fault-tolerant, generation-based disk serialization pipeline (`SaveSession`, `SaveFileStore`).

| Section | Data Encapsulated | Validation Integrity |
|---|---|---|
| **Header** | Schema Version, World Seed, Build ID, Generation Counter, Timestamp | SHA-256 Checksum Hash |
| **Profile** | Difficulty Modifiers, Permadeath Flags, World Time Multiplier | Range & Type Validation |
| **Player** | 3D Transform, Health, Wounds, Calorie/Hydration, Inventory, Equipped Gear | Structural Schema Bounds |
| **World** | WorldClock, Weather Pattern, Active Celestial Phase, RNG Stream State | Monotonic Sequence Check |
| **Cells** | Delta Records for all visited 64m cells, Persistent Entities, Pressure Values | Topological Grid Validation |
| **Shelters**| Fortification Sockets, Installed Modules, Storage Chests, Generator Fuel | Escrow & Conservation Audit |

* **Atomic Two-Phase Commits:** Save data is never written directly over the existing save file. New generations are serialized to an isolated temporary sandbox directory, flushed to disk, and verified with SHA-256 checksums. Upon passing integrity verification, a pointer swap atomically promotes the generation.
* **Corrupted File Quarantine:** If unexpected hardware termination (e.g., sudden power loss) corrupts a write, the engine detects checksum mismatch, isolates the corrupted payload, and transparently rolls back to the previous intact generation backup.

---

### 6. Survival Systems & Metabolic Simulation

Survival in *Last Signal* is governed by biological homeostasis rather than arbitrary health bars (`PlayerSurvival`, `SurvivalState`).

```
+-----------------------------------------------------------------------------------+
|                              HUMAN METABOLIC ENGINE                               |
+-----------------------------------------------------------------------------------+
|  Hydration (0-100%)       --> Depleted by sprinting & hot weather                 |
|  Calories (0-100%)        --> Depleted by physical exertion & cold exposure       |
|  Core Temperature (35-42C)--> Modulated by ambient weather, wetness & shelter fire|
|  Blood Volume (0-5000ml)  --> Depleted by open arterial lacerations & wounds      |
|  Stamina Pool (0-100)     --> Dynamic recovery based on exhaustion & pain         |
+-----------------------------------------------------------------------------------+
```

* **Wound & Bleeding Mechanics:** Attacks inflict discrete wound types (Minor Laceration, Deep Bite, Arterial Bleed, Blunt Fracture). Unbandaged lacerations continuously drain blood volume; low blood volume causes tunnel vision, audio muffling, and eventual hemorrhagic shock.
* **Thermal Exposure:** Cold rain and river crossing induce *Wetness*, drastically increasing thermal transfer. Hypothermia impairs movement speed, induces uncontrollable weapon sway, and accelerates caloric burn.

---

### 7. Base Fortification, Electrical Grids & Crafting

Survivors can claim abandoned farmhouses and commercial cabins, converting them into fortified outposts (`ShelterSite`, `ShelterSocket`).

* **Modular Socket Fortification:** Windows and doorways feature authored sockets for barricades (wooden planks, reinforced metal sheets, steel bars). Barricades possess distinct structural health pools and acoustic absorption values.
* **Electrical Generator Grid:** Gasoline generators provide operational power to claimed safehouses:
  * **Capacity vs Demand:** Generators deliver fixed wattage output (e.g., $2500\text{W}$). Consumers (floodlights, electric stoves, communications radios, refrigerators) draw discrete loads.
  * **Deterministic Load Shedding:** If total demand exceeds generator capacity, a priority queue automatically sheds non-essential circuits to prevent generator stall.
  * **Acoustic Tradeoff:** Active generators produce continuous high-radius sound events, gradually elevating regional *Presence* and drawing wandering hordes.
* **Two-Tier Crafting Engine (`ShelterProduction`, `RecipeDefinition`):**
  * *Field Crafting (Simulation Time):* Quick, manual recipes (bandages, makeshift torches, weapon cleaning) executed in real-time with animation locks.
  * *Workbench Crafting (World Time):* Complex technical recipes (radio transceiver repair, ammunition reloading, engine component fabrication) executed across hours of elapsed world time.

---

### 8. Vehicular Simulation & Kinetic Impact Mechanics

Vehicles are not arcade props; they are heavy, simulated physical actors (`VehicleActor`, `VehicleZombieImpact`).

* **Raycast / Rigidbody Physics:** Features calculated engine torque curves, multi-gear automatic transmission, independent spring/damper suspension, and lateral tire slip friction models.
* **Kinetic Horde Collisions (`VehicleZombieImpact`):** Hitting infected at speed calculates momentum transfer ($E_k = \frac{1}{2}mv^2$):
  * Low-speed impacts ($\le 20\text{ km/h}$) stagger enemies and push them aside.
  * High-speed impacts ($> 45\text{ km/h}$) deliver lethal blunt trauma, severing limbs and triggering ragdoll trajectory launches while damaging vehicle radiator health and windshield glass.
* **Mobile Logistics:** Vehicles feature accessible trunks with persistent slot-based storage and physical fuel tanks subject to leakage if damaged in collisions.

---

### 9. Dynamic Gathering & Tree Physics Engine

Harvesting timber for fortification relies on structural physics (`GatheringWorld`, `TreePhysicsBody`).
* **Procedural Felling:** Felling a timber tree calculates wedge cut depth and directional lean vectors. When the structural integrity threshold breaks, the trunk detaches as a dynamic `Rigidbody`, reacting to gravity, terrain inclination, and obstacle collisions before splintering into haulable logs and scrap lumber.

---

## 🎨 HDRP Rendering Pipeline & Visual Fidelity

The visual presentation leverages **Unity 6 High Definition Render Pipeline (HDRP 17.5.0)** configured for photorealistic, gritty survival horror atmosphere.

```
+-------------------------------------------------------------------------------+
|                       HDRP PRODUCTION RENDERING STACK                         |
+-------------------------------------------------------------------------------+
| Physical Lighting  --> Strict Lux/Lumen intensity units with Sun Physical Sky |
| Volumetric Fog     --> Physically modeled exponential height fog + density    |
| Screen Space Decals--> PBR blood splatters, tire tracks & rain puddle wetness  |
| Custom Shaders     --> Subsurface scattering foliage, dynamic fabric rain     |
| Light Culling      --> Forward+ cluster lighting with shadow mask culling      |
+-------------------------------------------------------------------------------+
```

### Visual Production Features
* **Physical Camera & Photometric Lighting:** Directional moonlight calibrated to $0.05\text{ Lux}$, flashlight cones to $650\text{ Lumens}$, and interior kerosene lanterns to $150\text{ Lumens}$ with real-world inverse-square falloff curves.
* **Atmospheric Scattering & Volumetric Fog:** Local volumetric fog volumes simulate ground mist in low valleys and dense tree canopies, dynamically penetrated by weapon muzzle flashes and vehicle headlights.
* **High-Fidelity Foliage & Photogrammetry:** Pine Ridge features dense, wind-reactive conifer forests using GPU instancing, vertex shader wind displacement, and multi-tier billboard LODs.

### Performance Budgets (Target: 1080p @ 60 FPS / 4K @ 30 FPS)
All engineering work adheres to strict hardware frame budgets measured on baseline desktop GPUs (NVIDIA RTX 3060 / Apple Silicon M-Series):

| Subsystem | CPU Time Target | Memory Allocation / Strategy |
|---|---|---|
| **AI Perception & Scheduling** | $\le 2.0\text{ ms}$ | $0\text{ B}$ runtime allocations (pre-allocated candidate arrays) |
| **Locomotion & Physics Portals** | $\le 2.0\text{ ms}$ | NonAlloc raycast/sweep query buffers |
| **Combat & Ballistics** | $\le 1.0\text{ ms}$ | Object pooled bullet casings, impacts & sound sources |
| **World Cell Streaming** | $\le 1.5\text{ ms}$ | Asynchronous threaded chunk I/O |
| **uGUI / View Presenters** | $\le 0.8\text{ ms}$ | Event-driven UI updates (Zero per-frame polling) |
| **Total Main-Thread Frame** | $\le 16.67\text{ ms}$ | Sustained 60+ FPS lock |

---

## 🧪 Automated Testing & Verification Suite

Quality assurance is codified through automated testing, boasting **over 950 individual test cases across 159 test fixtures**.

```
+-------------------------------------------------------------------------------+
|                         AUTOMATED TEST DISTRIBUTION                           |
+-------------------------------------------------------------------------------+
| EditMode Tests (NUnit)     : 806 Test Cases (Domain logic, serialization,     |
|                              crafting transactions, ballistic math, AI memory)|
| PlayMode Tests (Unity Test): 136 Test Cases (Physics sweeps, NavMesh pathing, |
|                              scene transitions, vehicle dynamics, input tests)|
| Total Test Execution Time  : ~5.8 Minutes (Automated via Headless Shell)      |
+-------------------------------------------------------------------------------+
```

### Key Testing Methodologies
* **Synthetic Input Injection:** Player controllers are validated using `PlayerInputReader` test harnesses that inject virtual gamepad and keyboard strokes headlessly, verifying stance transitions and reload cancels without human input.
* **Fault-Injection Storage Testing:** Tests simulate sudden disk write aborts, read-only permissions, and corrupt payload hashes to verify that the `SaveSession` recovery rollback functions with 100% reliability.
* **Zero-Allocation Memory Audits:** Performance unit tests assert that critical game loop ticks (e.g., combat firing, character running, AI sensory checks) produce zero GC heap allocations ($0\text{ Bytes}$).

---

## 📂 Project Architecture & Directory Map

```
Last Signal/
├── Assets/
│   ├── LastSignal/
│   │   ├── Art/                      # High-fidelity 3D meshes, textures, materials
│   │   │   ├── Characters/           # Survivor and infected photogrammetry meshes
│   │   │   ├── Environment/          # Pine Ridge & Ash Creek modular architectural kits
│   │   │   └── Weapons/              # Firearm and melee weapon asset packages
│   │   ├── Audio/                    # Acoustic audio banks, spatial ambiances, foley
│   │   ├── Data/                     # ScriptableObject definitions (Items, Recipes, Spawns)
│   │   ├── Prefabs/                  # Production entity, vehicle, and world cell prefabs
│   │   ├── Scenes/
│   │   │   └── Production/           # Production gameplay scenes
│   │   │       ├── FirstTownCore.unity
│   │   │       ├── FirstTownMain_HDRP.unity
│   │   │       ├── IntegratedGraybox.unity
│   │   │       ├── RelayExpedition.unity
│   │   │       └── S013Cabin.unity
│   │   ├── Scripts/
│   │   │   ├── Editor/               # Custom authoring tools, inspectors, CI linters (142 scripts)
│   │   │   ├── Runtime/              # Core game systems & domain engine (349 scripts)
│   │   │   │   ├── AI/               # Sensory perception, archetypes, population managers
│   │   │   │   ├── Atmosphere/       # Celestial lighting, dynamic weather controllers
│   │   │   │   ├── Audio/            # Acoustic propagation and bus controllers
│   │   │   │   ├── Combat/           # Weapon FSM, ballistic resolvers, melee sweep hitboxes
│   │   │   │   ├── Forest/           # Dynamic vegetation, terrain heightmap interfaces
│   │   │   │   ├── Gathering/        # Tree felling physics, harvesting durability models
│   │   │   │   ├── Graphics/         # HDRP quality adapters and render pipeline settings
│   │   │   │   ├── Input/            # New Input System readers and synthetic test wrappers
│   │   │   │   ├── Interaction/      # Contextual interaction portals, door locks
│   │   │   │   ├── Inventory/        # Transactional slot inventory, equipment slots
│   │   │   │   ├── Noise/            # Acoustic event generation, spatial pressure adapter
│   │   │   │   ├── Objectives/       # Mission FSM, broadcast relay state trackers
│   │   │   │   ├── Persistence/      # Atomic disk store, schema migrators, DTO definitions
│   │   │   │   ├── Player/           # Kinematic locomotion motor, stamina, metabolic state
│   │   │   │   ├── Shelter/          # Safehouse claiming, power grid, multi-input crafting
│   │   │   │   ├── Vehicles/         # Drivetrain physics, impact solvers, mobile storage
│   │   │   │   ├── WorldCells/       # 64m spatial grid manager, chunk delta serializors
│   │   │   │   └── WorldTime/        # WorldClock, sleep time-lapse simulation engine
│   │   │   └── Tests/                # Comprehensive automated test suites (159 scripts)
│   │   │       ├── EditMode/         # Fast-running Pure C# domain verification
│   │   │       └── PlayMode/         # Scene-level integration and physics verification
│   │   ├── Settings/                 # HDRP volumes, input actions, project profiles
│   │   └── Shaders/                  # Custom surface shaders (rain wetness, gore masks)
├── Docs/                             # 600-Day Master Production GDD, Architecture & Runbooks
├── Packages/                         # Unity package manifests (HDRP, NavMesh, Input System)
├── ProjectSettings/                  # Project configuration files (Input, Tags, Graphics)
└── run_all_tests.sh                  # One-click headless test runner and XML parser script
```

---

## 💼 Key Engineering Competencies (CV / Portfolio)

This project stands as concrete proof of senior-to-lead level game engineering competence:

* **Enterprise-Grade Architecture in Games:** Demonstrated mastery in decoupling core domain simulation from engine code using **Hexagonal Ports & Adapters**, enabling test-driven development (TDD) in Unity.
* **Robust Concurrency & Data Integrity:** Implementation of **transactional escrow systems** to eliminate concurrency race conditions, duplication exploits, and data loss.
* **Custom AI Perception & Demographic Ledgers:** Engineered complex sensory perception models coupled with **Simulation LOD pipelines** capable of sustaining deep demographic continuity across massive open worlds without CPU degradation.
* **Modern Unity Systems:** In-depth technical mastery of the **Unity 6 LTS stack**, **High Definition Render Pipeline (HDRP 17.5.0)**, **New Input System**, **AI Navigation 2.0**, and **Assembly Definitions**.
* **High-Performance Optimization:** Relentless focus on **zero GC allocation loops**, cache-friendly spatial data partitioning, and strict frame-time profiling ($\le 16.67\text{ ms}$).
* **Production Discipline:** Backed by an exhaustive **60-sprint / 600-day production methodology**, automated headless CI/CD test automation, and comprehensive technical documentation.

---

## 🚀 Setup & Developer Guide

### Prerequisites
* **Unity Hub & Engine:** Unity 6 LTS (`6000.5.0f1` or later).
* **Target Platforms:** Windows 10/11 (DirectX 12 / Vulkan), macOS (Apple Silicon Metal).
* **Git LFS:** Ensure Git Large File Storage is installed prior to cloning.

### Installation & First Launch
1. **Clone the Repository:**
   ```bash
   git clone https://github.com/coskunburak/Last-Signal.git
   cd "Last-Signal"
   git lfs pull
   ```
2. **Open in Unity Editor:**
   * Launch **Unity Hub**, click **Add project from disk**, and select the repository root.
   * Ensure the editor version is set to `6000.5.0f1`.
3. **Open Production Scene:**
   * Navigate to `Assets/LastSignal/Scenes/Production/` in the Project view.
   * Open `FirstTownMain_HDRP.unity` (or `IntegratedGraybox.unity` for greybox test routing).
   * Press **Play** in the editor.

### Running Automated Test Suites Headlessly
Execute the entire test suite (EditMode and PlayMode) via command line without opening the GUI editor:
```bash
# Run complete test verification pipeline
chmod +x run_all_tests.sh
./run_all_tests.sh
```
Test results will be exported in NUnit XML format to `full-editmode-post-integrity.xml` and `full-playmode-post-integrity.xml`.

---

## 📜 Credits & Contact

* **Lead Systems Architect & Game Developer:** Burak Coşkun
* **Architecture & Engineering:** Pure C# Domain, Hexagonal Systems, AI Simulation, Save Pipeline
* **Engine & Graphics:** Unity 6000.5.0f1, High Definition Render Pipeline (HDRP 17.5.0)

---

<div align="center">
<sub>Designed and developed with engineering rigor. Project: Last Signal © 2026. All rights reserved.</sub>
</div>
