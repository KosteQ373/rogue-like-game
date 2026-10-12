# PROJECT CONTEXT & ARCHITECTURE BLUEPRINT

## 1. Project Overview & Vision
* **Project Name**: Overdose (working title / `rogue_like`)
* **Framework**: MonoGame (DesktopGL) running on **.NET 8.0**
* **Target Platform**: Desktop (Linux / Pop!_OS, Windows, macOS)
* **Genre**: Top-Down Arena Roguelike / Horde Survival (inspired by *Vampire Survivors*, *Brotato*, *20 Minutes Till Dawn*)
* **Core Loop**:
  1. Movement, dodging, and continuous twin-stick/auto-aim combat against scaling swarms.
  2. Dynamic XP gem collection and real-time level-up selection with mutually exclusive build paths.
  3. Survival pacing interspersed with deterministic boss encounters every wave cycle.
  4. End-of-run progression and score accumulation.

---

## 2. Development Environment & Build Pipeline
* **Host OS**: Linux (Pop!_OS).
* **IDE**: JetBrains Rider (often run in containerized / Flatpak environments).
* **MGCB Build Process**:
  * Because containerized IDEs (like Flatpak Rider) do not natively invoke local tool bootstrappers without IPC access, compilation of raw assets (fonts, audio, textures) is managed via the **.NET Local Tools CLI** or MSBuild build tasks.
  * Manifest file: `.config/dotnet-tools.json` tracking `dotnet-mgcb` / `dotnet-mgcb-editor-linux`.
  * Asset source: `Content/Content.mgcb`.
  * Font rendering: Crisp pixel-art output rendered via `SpriteFont` compiled with `SamplerState.PointClamp` (anti-aliasing completely disabled for authentic retro sharpness).

---

## 3. Core Architectural Pillars & Performance Standards

### 3.1. Zero Runtime Allocations (GC Pressure Minimization)
* In a horde shooter with thousands of concurrent entities, the .NET Garbage Collector is the primary cause of frame drops and stuttering.
* **Rule**: Absolutely **no `new` allocations** inside the per-frame update or render loops (`Update()`, `Draw()`).
* **Object Pooling**:
  * All short-lived game objects (`Enemy`, `Bullet`, `XpGem`, `DamageNumber`, `DeathParticle`, `ExplosionParticle`) implement `IPoolable` and are managed by pre-allocated, expandable `ObjectPool<T>` instances.
  * Spawning fetches from the inactive pool; death or expiration flags the entity inactive and returns it without heap overhead.
* **Preallocated Collections**:
  * Temporary scratch buffers, queries, and spatial neighbor lists are reused across frames to prevent list reallocations.

### 3.2. Spatial Partitioning & Horde Intelligence
* **Spatial Grid (`SpatialGrid.cs`)**:
  * Prevents costly $O(N \cdot M)$ or $O(N^2)$ brute-force comparisons for collisions, proximity checks, homing logic, and boids separation.
  * Entities register and update their cell indices dynamically.
* **Boids Horde Movement**:
  * Enemies navigate towards the player while querying local neighbors within their spatial grid cell to resolve crowd repulsion and steering forces smoothly.

### 3.3. Deterministic Bitwise Tags & Exclusivity
* Replaced string-based identification with `[Flags]` Enums (e.g., `UpgradeTags`, `BulletModifiers`).
* Tag operations utilize bitwise logic (`&`, `|`, `~`) for $O(1)$ constant-time exclusivity validation, eliminating string parsing and string allocation overhead in the level-up system.

### 3.4. Dirty-Flag Stat System (`CharacterStat`)
* Player and weapon modifiers rely on a decoupled stat system.
* Base values and additive/multiplicative modifiers are aggregated lazily using a `_isDirty` boolean flag, avoiding recalculations until a stat is explicitly queried.

---

## 4. Codebase Structure & File Responsibilities

| File / Component | Responsibility |
| :--- | :--- |
| `Game1.cs` | Engine lifecycle, initialization, core fields, resource loading (`LoadContent`). |
| `Game1.Update.cs` | High-level update loop, state machine routing, boss mechanics, input handling. |
| `Game1.Draw.cs` | Rendering pipeline, world matrix cameras, pixel-aligned HUD, level-up and game over modals. |
| `SpatialGrid.cs` | Grid-based 2D spatial partitioning structure for rapid radius queries. |
| `CollisionSystem.cs` | Pure static resolution system handling projectile-enemy and player-enemy interactions. |
| `ObjectPool.cs` / `IPoolable.cs` | Reusable generic pooling architecture. |
| `CharacterStat.cs` | Modifier-aware numerical stat container with cached valuation. |
| `UpgradeCard.cs` / `UpgradeDatabase.cs` | Upgrade catalog, bitwise compatibility rules, and application actions. |
| `Bullet.cs` / `BulletModifiers.cs` | Multi-behavior projectile entity (bounce, pierce, homing, explosive, elemental). |
| `Enemy.cs` / `Boss.cs` | AI agents, health management, status effects (Burning, Freezing, Knockback). |

---

## 5. Coding Standards & Conventions
1. **Type Safety & C# Modern Features**:
   * Use pattern matching, expression-bodied members, and strongly-typed enums over loose primitives.
2. **Separation of Concerns (SRP)**:
   * Keep rendering logic isolated to `Game1.Draw.cs` or dedicated render passes. Entity classes should not draw directly to the GPU.
3. **Data-Oriented Mindset**:
   * Favor contiguous array/list structures (`List<T>` with indexed `for` loops) over linked nodes or LINQ queries inside hot loops.
4. **Coordinate Space**:
   * World Space: Float-based coordinates transformed by `Camera.GetViewMatrix()`.
   * Screen Space: UI elements scaled via `uiScale` dynamic clamp, rendered in a dedicated `SamplerState.PointClamp` batch without camera offsets.

---

## 6. Technical Roadmap & Immediate Milestones

### Phase 1: Architecture Polish & Asset Decoupling (Current)
- [x] Integrate high-performance `SpriteFont` rendering via `Content.mgcb`.
- [x] Spatial Grid partitioning for enemies and projectiles.
- [x] Bitwise exclusivity system for upgrade generation.
- [x] Floor tile textures (5 variants) registered in `Content.mgcb` and rendered in `DrawArenaBackground()`.
- [ ] Migrate `Game1` partial classes into a formal **Scene/State Management Pattern** (`IScene` or `IGameScreen`) to prevent file bloat.

### Phase 2: Game Feel & Polish ("Juice")
- [ ] **Screen Shake System**: Traumatic directional and rotational shake with quadratic decay.
- [ ] **Hitstop / Frame Freeze**: Micro-pauses (30-60 ms) on heavy impacts and critical hits.
- [ ] **Damage Flash Shaders**: GPU-driven white silhouette flashes on hurt entities instead of CPU-side color tinting.

### Phase 3: Modular Combat & Weapons System
- [ ] Abstract weapons from hardcoded player variables into modular weapon controllers (`IWeapon`).
- [ ] Introduce active weapon slots with independent cooldowns, spread angles, and upgrade trees.

### Phase 4: Audio Engine
- [ ] Integrate a non-blocking audio manager supporting pitch randomization per hit to prevent audio fatigue.

### Phase 5: Persistence & Meta-Progression
- [ ] JSON-based save system storing unlocked characters, currency, and permanent base stat upgrades.