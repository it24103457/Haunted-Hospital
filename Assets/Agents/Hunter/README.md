# Sound Hunter

## What the agent does

The Sound Hunter is an autonomous enemy that **hunts by hearing, not by sight**. It has no general vision. Every noise the player or the world makes travels through the building's real layout: walls block it, and closed doors and intact barricades muffle it. The Hunter scores what reaches it, remembers the best few noises, walks to the strongest one with its own A* pathfinder (opening doors on the way), searches the area, and goes back to patrolling. A loud player noise, being shot, or the player being close by path starts a chase, and within reach it attacks.

The player's movement is the counter-play: crouching is nearly silent, sprinting and gunfire carry far, and closing a door while crouched hides the sound.

## How it plays

Distances are measured **along the path through the building**, not in a straight line. Defaults, no closed doors in the way.

| Player action | Noise at its source | What the Hunter does |
| --- | --- | --- |
| Walk while crouching | 1.5 | Heard within about 3 m. Never enough to investigate. |
| Walk normally | 4 | Heard within about 12 m, but only investigates when almost next to it. |
| Sprint | 10 | Heard within about 19 m. Investigates within about 8 m. |
| Fire the gun | 40 | Investigates within about 27 m. Starts a chase when heard at 12 or more (about 10 m in the open). |
| Close a door, standing | 15 | Investigates within about 16 m. |
| Close a door, crouching | 3 | Investigates only within about 3 m. |
| Open a door | none | Silent. |
| Break a barricade | 25 | Investigates within about 21 m. |
| Shoot the Hunter | n/a | Starts a chase toward the shooter and stays on alert for 4 s. |

Two further rules:

- **Proximity sense.** A player within 8 m *by path* is noticed whatever noise they make. Set `chaseProximity` to 0 to make the Hunter purely sound-driven.
- **Sound is blocked.** A closed door cuts loudness to 40 percent, an intact barricade to 25 percent, and a wall blocks it completely.

## Decision process

`HunterBrain` is a finite state machine.

| State | Entered when | Leaves when |
| --- | --- | --- |
| Patrol | It spawns, or a search or chase finishes | A noise scores 1.5 or more (Investigate), or a chase trigger fires |
| Investigate | The best remembered noise scores at least 1.5 | It arrives (Search), the walk fails (Patrol), or a clearly better noise appears (retarget) |
| Search | It reached the noise, or lost the player | The search ends after 10 s or when no points are left (Patrol), or a new noise interrupts it |
| Chase | Player noise heard at 12 or more, player within 8 m by path or in reach, or the Hunter is shot | 6 s of silence (Search at the last known spot), in reach (Attack), or the player dies (Patrol) |
| Attack | Player within 1.5 m, at a similar height, with nothing solid between | Player beyond 2.0 m or behind something (Chase), or the player dies (Patrol) |
| Dead | Health reaches 0 | It respawns after the delay |

## Hearing and memory (`HunterEars`)

1. **Propagation.** A noise travels along the waypoint graph. The loudness that reaches the Hunter is `loudness x e^(-0.12 x path metres)`, times 0.4 for each closed door and 0.25 for each intact barricade on the way. Below 1, it is not heard.
2. **Scoring.** `score = importance x heard loudness x e^(-age / 12 s)`. Importance: gunshot 1.0, barricade break 0.8, door slam 0.7, thrown object 0.6, footstep 0.4, anything else 0.3.
3. **Memory.** The best 8 are kept. A noise within 2 m of one already remembered updates it only if it is at least as strong, so a footstep cannot overwrite a gunshot. A noise is forgotten when its score fades below 0.1.
4. **Hysteresis.** The target only changes when a new noise beats the current one by 25 percent.
5. **Own sounds are ignored.** Memory is cleared on death, on respawn, and when a chase begins.

## Movement and doors (`HunterPathfinder`, `HunterNavigator`)

- **A\*.** A step costs its length, plus 3 m for a closed door. Locked doors, doors the Hunter just failed to open, and intact barricades are blocked. The estimate is the straight-line distance, which never exceeds the real cost, so the route is the cheapest.
- **Doors.** In front of a closed door the Hunter stops, calls `Door.Open`, and waits for `IsFullyOpen` before walking through. If the door does not open within 3 s it is avoided for 6 s and the route is planned again. After 3 failures the walk gives up.
- **Re-planning.** The route is planned again when any door or barricade changes state, and when the Hunter makes no progress for 6 s (twice, then it gives up).
- **Unreachable target.** It walks to the nearest reachable node and searches there.
- **Patrol and search points** come from a flood fill of the nodes the Hunter can actually reach, so a Hunter sealed in a small area still finds a valid point.

## Combat and life cycle

- **Attack.** After a 0.4 s wind-up it hits for 20 damage every 1.2 s through the player's `TakeDamage`. The player has 100 health. It faces the player while attacking.
- **Reach** is checked every 0.2 s and must be fresh (within 0.5 s). The nearest object on the line to the player must be the player, so it cannot hit through a wall.
- **Damage.** The Hunter has 100 health and each shot does 50, so two shots kill it.
- **Death.** Colliders and the NavMeshAgent switch off, the body disappears after 3 s, and it respawns after 45 s.
- **Respawn.** Only at a `HunterSpawn` marker at least 20 m from the player and outside their view. If none qualifies it retries every 3 s.

## How the implementation is organized

| File | Responsibility |
| --- | --- |
| `Scripts/HunterBrain.cs` | State machine, spawn and respawn, damage, player sensing, chase and attack |
| `Scripts/HunterEars.cs` | Hearing, scoring and memory |
| `Scripts/HunterNavigator.cs` | Walks a route, handles doors and re-planning (plain C# class) |
| `Scripts/HunterPathfinder.cs` | A* and flood fill over the waypoint graph (static) |
| `Scripts/HunterSearch.cs` | Picks search points (plain C# class) |
| `Scripts/HunterSettings.cs` | Every tuning number, as one ScriptableObject |
| `Scripts/HunterState.cs` | The state enum |
| `Scripts/IHunterStatus.cs` | Read-only status for other systems |
| `Scripts/HunterDebugView.cs` | Test tool: draws memory, route and state in the Scene view |
| `Scripts/HunterTestMover.cs` | Test mover, disabled on the prefab and replaced by `HunterMover` |
| `AgentController/` | `HunterMover`, `HunterAnimationDriver`, `HunterAudioDriver` (movement, animation, audio) and `HunterSpeedFixPatcher` (editor tool) |
| `Prefabs/PF_Hunter.prefab` | The Hunter with all components |
| `Settings/HunterSettings_Hospital.asset` | The tuned settings used in the hospital |

**The brain never moves itself.** It publishes `CurrentPath`, `OnPathChanged`, `State`, `OnStateChanged`, `SpeedMultiplier` and `LookTarget`, and a mover turns those into movement.

**Other systems read the Hunter only through `IHunterStatus`**: `IsReady`, `IsActive`, `IsAlive`, `IsChasingPlayer`, `IsAttackingPlayer` and `StateChanged`. The Environment Director's `DirectorLiveHunterSource` treats chasing or attacking as Active, inactive or dead as Inactive, and a Hunter that is not ready as unavailable.

**Inputs from the rest of the game**

- Noise: `Noise.Emit` is called by `PlayerNoiseEmitter` (gunshots), `PlayerFootstepNoise` (footsteps), `Door` (a door slam when the player closes it) and `HospitalBarricade` (a break). The Hunter listens to `Noise.Emitted`.
- Doors: `Door.Open(requester)`, `IsOpen`, `IsFullyOpen`, `IsLocked`.
- Graph: the shared `WaypointGraph`. Costs, A\* and memory live in the Hunter, not in the graph.

## Setting it up in a scene

The scene needs:

- A `WaypointGraph` with its nodes (doors and barricades as their own node types).
- A baked NavMesh for the **Hospital_Blockout** agent type.
- Spawn markers whose names start with **`HunterSpawn`**.
- The player with `PlayerStats`, `PlayerNoiseEmitter` and `PlayerFootstepNoise` (all on the Player prefab).
- `PF_Hunter` placed anywhere. On Play it moves to one of the spawn markers.

`PF_Hunter` must have `HunterBrain` (with `HunterSettings_Hospital` assigned), `HunterEars`, `HunterMover` and a NavMeshAgent on the **Hospital_Blockout** type. If a reference is missing, the Hunter logs one error and reports itself as unavailable instead of failing every frame.

## Configuration

Everything is on `HunterSettings_Hospital`. The most useful values:

| Group | Setting (default) |
| --- | --- |
| Hearing | `hearingThreshold` (1), `memoryCapacity` (8), `memoryMergeRadius` (2 m), `memoryTimeConstant` (12 s), `retargetMargin` (0.25), `forgetBelowScore` (0.1), `investigateThreshold` (1.5) |
| Chase | `chaseLoudness` (12), `chaseProximity` (8 m, 0 turns it off), `chaseLoseSeconds` (6), `shotAlertSeconds` (4) |
| Attack | `attackRange` (1.5 m), `attackExitMargin` (0.5 m), `attackDamage` (20), `attackCooldownSeconds` (1.2), `attackWindupSeconds` (0.4), `freshReachSeconds` (0.5) |
| Search | `searchSeconds` (10), `searchRadius` (8 m), `searchPauseSeconds` (1.5) |
| Doors | `closedDoorExtraCost` (3), `doorOpenTimeoutSeconds` (3), `doorBanSeconds` (6), `doorRetryLimit` (3), `stuckSeconds` (6) |
| Life cycle | `maxHealth` (100), `deathLingerSeconds` (3), `respawnDelaySeconds` (45, 0 means never), `respawnMinDistanceFromPlayer` (20 m), `respawnAvoidPlayerView` (on) |
| Speed | `patrolSpeed` (0.82), `investigateSpeed` (1.14), `searchSpeed` (0.93), `chaseSpeed` (1.5), each a multiplier of the NavMeshAgent base speed (1.4) |

Edits to this asset made in Play mode are **kept** when you stop, so note what you change.

## Debugging and testing

- Tick `enableDebugLogging` on the settings asset. Filter the Console on `[Hunter]` for what it heard, which state it entered, and which door it is waiting at.
- `HunterDebugView` (on the prefab) draws in the **Scene** view while playing, with Gizmos on: a sphere and score for each remembered noise (yellow is the target), the planned route in cyan, a red circle on a door it is holding at, and its state and health above its head.
- To test a locked door, tick **Is Locked** on a door in the Inspector. Do not commit that change.

| Test | Expected |
| --- | --- |
| Fire in a room with the door closed, then open | Heard loudness is lower when closed |
| Locked door or intact barricade | The route goes around, or the Hunter stays put if the area is sealed |
| Crouch, walk and sprint near it | Sprint is noticed at about 8 m, crouch is not |
| Close a door standing, then crouched | Loudness 15, then 3 |
| Fire in two places | The stronger noise is investigated first |
| Shoot it twice | It dies, then respawns out of sight after 45 s |
| Let it kill the player | It returns to patrol |
| Break contact after a chase | After 6 s it searches the last known spot, then patrols |

## Known limitations

- **Chase speed is about 2.1 m/s** (1.4 x 1.5), slower than a walking player (4 m/s), so it only catches a crouching or cornered player. Confirm this is intended.
- **Footsteps never start a chase by loudness.** A sprint is 10 at its source and the chase threshold is 12.
- **Noise from other agents** would be heard like any other. None emit noise today.
- `thrownObjectImportance` and `doorApproachDistance` are not used yet.
- The Hunter does not drain sanity. It damages health only.
- The search does not turn to look around, a door is not checked for someone standing in the doorway, and performance with every agent running has not been measured.
- `HunterBrain` is large and depends on `HunterEars`. A planned refactor moves body switching into its own component and makes the ears independent of the brain.
