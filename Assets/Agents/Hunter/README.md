# Sound Hunter

## What the agent does

The Sound Hunter is an autonomous enemy that **hunts by hearing, not by sight**. It has no general vision. Every noise the player makes travels through the building's real layout: walls stop it, and closed doors and intact barricades muffle it. The Hunter works out how loud each sound is when it arrives, scores it, remembers the best few, walks to the strongest one with its own A* pathfinder (opening doors on the way), searches the area, and goes back to patrolling. A very loud noise, being shot, or the player being close by path starts a chase, and within reach it attacks.

The player's movement is the counter-play. Crouching is nearly silent, sprinting and gunfire carry a long way, a door closed while crouching is quiet, and any closed door or barricade between the player and the Hunter hides part of the sound.

The Hunter decides, but it does not move itself. It publishes the route it wants to walk, its state and a speed multiplier. A separate mover in `AgentController/` turns those into movement, animation and audio, so the AI is independent of how it looks.

## How it plays

Distances are measured **along the path through the building**, not in a straight line, with no closed doors in the way. They follow from the defaults in the settings asset (see the maths below).

| Player action | Noise at its source | What the Hunter does |
| --- | --- | --- |
| Walk while crouching | 1.5 | Heard within about 3 m. Never strong enough to investigate. |
| Walk normally | 4 | Heard within about 11 m, but it only investigates when almost next to it. |
| Sprint | 10 | Heard within about 19 m. Investigates within about 8 m. |
| Fire the gun | 40 | Heard within about 30 m. Investigates within about 27 m. Starts a chase within about 10 m. |
| Close a door, standing | 15 | Investigates within about 16 m. Starts a chase within about 2 m. |
| Close a door, crouching | 3 | Investigates only within about 3 m. |
| Open a door | none | Silent. |
| A barricade breaks | 25 | Investigates within about 21 m. Never starts a chase, because the sound comes from the barricade, not the player. |
| Shoot the Hunter | n/a | Starts a chase toward the shooter and keeps it on alert for 4 s. |

Two further rules:

- **Proximity sense.** A player within 8 m *by path* is noticed whatever noise they make, or none. Crouching hides the sound of the player, not the player. Set `chaseProximity` to 0 to make the Hunter purely sound-driven.
- **Sound is blocked.** A closed door cuts loudness to 40 percent, an intact barricade to 25 percent, and a wall blocks it completely.

## Decision process

`HunterBrain` is a finite state machine with six states, and `HunterEars` feeds it what the Hunter heard.

```text
                  noise scores >= 1.5                       reached the noise
   Patrol ─────────────────────────────► Investigate ─────────────────────────► Search
     ▲  ▲                                    │  walk fails                         │
     │  └────────────────────────────────────┘                                     │
     │            search ends (10 s, or nowhere left to look)                      │
     └─────────────────────────────────────────────────────────────────────────────┘

   Patrol / Investigate / Search ──(very loud player noise, player within 8 m by path, or shot)──► Chase
   Chase ──(player within reach)──► Attack          Attack ──(player out of reach)──► Chase
   Chase ──(6 s with no sign of the player)──► Search at the last known spot
   Chase / Attack ──(player dies)──► Patrol          any state ──(health 0)──► Dead ──(45 s)──► Patrol
```

| State | What it does | Leaves when |
| --- | --- | --- |
| Patrol | Walks to a random reachable point at least 6 m away, waits 3.5 s there, repeats | The best noise scores 1.5 or more (Investigate), or a chase trigger fires |
| Investigate | Walks to the best remembered noise | It arrives (Search), the walk fails (Patrol), or a clearly better noise appears (it retargets) |
| Search | Visits nearby points in random order, pausing 1.5 s at each | 10 s pass or no points are left (Patrol), or a new noise interrupts it |
| Chase | Walks to where the player was last known, and keeps updating that spot | Player in reach (Attack), 6 s without a sign of the player (Search), or the player dies (Patrol) |
| Attack | Stands still, faces the player, and hits after a wind-up | Player beyond 2.5 m or behind something (Chase), or the player dies (Patrol) |
| Dead | Body disappears, then it respawns | The respawn delay ends |

`Inactive` is the state before the first spawn.

## Hearing and memory (`HunterEars`)

### 1. How loud is it by the time it arrives

A noise spreads through the shared waypoint graph, not through the air. Each step multiplies the loudness by a factor, so

```text
heard = loudness × e^(−0.12 × path metres) × 0.4 per closed door × 0.25 per intact barricade
```

Below the hearing threshold of 1, the noise is not heard at all.

`SoundPropagation` takes logarithms so the product becomes a sum:

```text
cost(path) = Σ ( 0.12 × edge length  −  ln(factor of the node entered) )      heard = loudness × e^(−cost)
```

Every cost is non-negative, so the loudest route to the Hunter is the cheapest one, and **Dijkstra's algorithm finds it exactly**. The search stops as soon as the cost passes `ln(loudness / threshold)`, so a quiet sound explores very little of the building. In these units a closed door costs the same as about 7.6 m of corridor (−ln 0.4 ÷ 0.12), and an intact barricade about 11.6 m.

The reach of a sound down an open corridor is `ln(loudness) / 0.12` metres, which is where the distances in the table above come from.

### 2. How much it matters

```text
score = importance × heard loudness × e^(−age / 12 s)
```

Importance: gunshot 1.0, barricade break 0.8, door slam 0.7, thrown object 0.6, footstep 0.4, anything else 0.3. A score halves about every 8 seconds. A noise scoring 1.5 or more makes the Hunter investigate, so the investigate distance for each noise is the distance where `heard = 1.5 ÷ importance`.

### 3. What it keeps

- **Memory.** The best 8 noises are kept. A noise within 2 m of one already remembered updates it only if the new one scores at least as high, so a footstep cannot overwrite a gunshot.
- **Forgetting.** A noise is dropped when its score fades below 0.1.
- **Hysteresis.** The target only changes when a new noise scores more than 25 percent higher than the current one, so the Hunter does not flip between two similar sounds.
- **Own sounds are ignored.** Memory is also cleared on spawn, on death and when a chase begins, because it is then hunting the player directly.

### Worked example

| Event | Heard at the Hunter | Score | Result |
| --- | --- | --- | --- |
| Walking, 5 m away | 4 × e^(−0.6) = 2.20 | 0.4 × 2.20 = 0.88 | Remembered, not investigated |
| Sprinting, 6 m away | 10 × e^(−0.72) = 4.87 | 0.4 × 4.87 = 1.95 | Investigates |
| Gunshot, 20 m away | 40 × e^(−2.4) = 3.63 | 1.0 × 3.63 = 3.63 | Investigates |
| The same gunshot with one closed door on the way | 3.63 × 0.4 = 1.45 | 1.45 | Just under 1.5, so it is remembered but not investigated |

A closed door decides whether the player is noticed, which is why shutting a door behind you matters.

## Movement and doors (`HunterPathfinder`, `HunterNavigator`)

- **A\* over the waypoint graph.** A step costs its length in metres, plus 3 m for a closed door. Locked doors, doors the Hunter has just failed to open, and intact barricades are blocked. The estimate is the straight-line distance. It never exceeds the real cost, because every step costs at least its straight-line length and the door extra is never negative, so the route found is the cheapest.
- **Doors.** The Hunter stops at the waypoint in front of a closed door, calls `Door.Open`, and waits for `IsFullyOpen` before walking through. Waiting for the fully open state, not just `IsOpen`, stops it walking into a swinging leaf. If the door has not opened within 3 s it is avoided for 6 s and the route is planned again. After 3 failures on the same door the walk gives up.
- **Re-planning.** Every 0.5 s it checks whether any door or barricade has changed state, and plans again if one has. If it makes no progress for 6 s it plans again, twice, then gives up.
- **Unreachable target.** Investigating and chasing allow a partial route: it walks to the reachable node nearest the target in a straight line, and searches there.
- **Patrol and search points** come from a flood fill of the nodes the Hunter can really reach, using the same blocking and cost rules. A Hunter sealed in by a locked door or barricade chooses points inside its own area, and when nothing is far enough away it walks to the far end of that area.
- **When a walk fails** the brain handles it: a failed investigation returns to Patrol, a failed patrol point picks another, and a failed search point moves on to the next.

## Chase and attack

- **What starts a chase** (from Patrol, Investigate or Search): a noise made by the player that reaches the Hunter at loudness 12 or more, the player within 8 m by path or within reach, or being shot by the player.
- **During a chase** the last known position of the player is refreshed by any noise the player makes, by coming within 8 m by path, and by shooting the Hunter. After 6 s with none of these (unless it was shot in the last 4 s) it searches the last known spot.
- **Reach** is checked every 0.2 s and has to be fresh (within 0.5 s). The player must be within 1.5 m, at about the same height, and the nearest thing on the line to them must be the player, so it cannot hit through a wall or a vent grille. Once attacking, the player has to get beyond 2.5 m to break it off, so it does not flick between Attack and Chase.
- **Attack.** After a 0.55 s wind-up it hits for 20 damage every 1.6 s through the player's `TakeDamage`. The player has 100 health, so five hits kill them, about 7 seconds after the first contact.
- **Taking damage.** Each shot does 25 and the Hunter has 100 health, so four shots kill it. A hit also makes it flinch for 0.35 s.

## Life cycle

- **Death.** Colliders and the NavMeshAgent switch off, the body disappears after 3 s, and memory is cleared.
- **Respawn.** After 45 s, at a marker named `HunterSpawn…` that is at least 20 m from the player and outside their view. If none qualifies it tries again every 3 s. Setting the delay to 0 means it never respawns.
- **First spawn.** It prefers an allowed marker, and uses any marker if none qualifies yet.

## Edge cases handled

| Situation | What the Hunter does |
| --- | --- |
| A door is locked or an intact barricade blocks the way | Plans around it, or stays inside its sealed area and patrols there |
| A door does not open in time | Avoids it for 6 s, re-plans, and stops trying that route after 3 failures |
| A door or barricade changes while it is walking | Notices within 0.5 s and plans again |
| The noise is somewhere it cannot reach | Walks as near as it can, then searches |
| The player is close on the other side of a wall | Not near, because the check uses path length |
| Two noises in the same place | Keeps the stronger one |
| A new noise is only slightly better than the current target | Ignores it (25 percent margin) |
| It gets stuck on the way | Plans again twice, then gives up and picks another goal |
| The player dies mid-chase or mid-attack | Returns to Patrol |
| It is shot while patrolling, searching or investigating | Chases toward the shooter and stays on alert for 4 s |
| A required reference is missing | Logs one error and reports itself as unavailable instead of failing every frame |
| It respawns with no safe marker available | Waits and retries, so it never appears in front of the player |

## Design choices

| Choice | Alternative | Why |
| --- | --- | --- |
| Hear through the waypoint graph with Dijkstra | Straight-line distance, or a raycast per noise | A straight line passes through walls. The graph has no edge through a wall, and doors and barricades are nodes, so muffling is part of the maths. |
| Score = importance × loudness × decay, with 8 memories | React to the latest noise | The Hunter is not steered by every footstep, a gunshot outranks a footstep in the same place, and old noises fade. |
| 25 percent retarget margin | Always take the best noise | Stops it flipping between two similar noises. |
| Own A\* on the waypoint graph | `NavMeshAgent` pathing alone | Closed doors carve the NavMesh, so the NavMesh treats them as walls and cannot plan through them. On the graph a closed door is a node the Hunter can price and open. The `NavMeshAgent` is still used for local movement. |
| Plain list scan for the cheapest A\* node | A heap | The graph has about 127 nodes, so the scan is fast enough and simpler to read. |
| Finite state machine | Behaviour tree or GOAP | Six states with clear triggers, so it is easy to debug and to explain. |
| Proximity sense of 8 m by path | Purely sound-driven | A crouching player right next to it should not be invisible. It is a setting, and 0 removes it. |
| All numbers in one `HunterSettings` asset | Constants in code | Tuning never means editing code, and each value has a tooltip. |
| The brain publishes a path and a state | The brain moves the Hunter | Movement, animation and audio can change without touching the decisions. Other systems read only `IHunterStatus`. |

## Working with the rest of the game

| System | How the Hunter works with it |
| --- | --- |
| Noise board (`Noise.Emit`, `Noise.Emitted`) | The Hunter listens to every noise. The emitters are `PlayerNoiseEmitter` (gunshots), `PlayerFootstepNoise` (footsteps), `Door` (a slam when the player closes it) and `HospitalBarricade` (a break). |
| `Door` | `Open(requester)`, `IsOpen`, `IsFullyOpen`, `IsLocked`. Doors opened by the Hunter make no noise. |
| `WaypointGraph` | Shared with the Thief. The graph holds only facts. The Hunter and the Thief each price the edges their own way. |
| Environment Director | `DirectorLiveHunterSource` reads `IHunterStatus`. A ready Hunter that is chasing or attacking counts as danger, and a Hunter that is not ready is treated as unavailable. |
| Player | `PlayerStats.TakeDamage` for attacks. It never touches sanity directly. |

`IHunterStatus` exposes `IsReady`, `IsActive`, `IsAlive`, `IsChasingPlayer`, `IsAttackingPlayer` and `StateChanged`. Nothing else in the game needs to know how the Hunter works.

## How the implementation is organized

| File | Responsibility |
| --- | --- |
| `Scripts/HunterBrain.cs` | State machine, spawn and respawn, damage, player sensing, chase and attack |
| `Scripts/HunterEars.cs` | Hearing, scoring and memory |
| `Scripts/HunterNavigator.cs` | Walks a route, handles doors and re-planning (plain C# class) |
| `Scripts/HunterPathfinder.cs` | A\* and flood fill over the waypoint graph (static) |
| `Scripts/HunterSearch.cs` | Picks search points (plain C# class) |
| `Scripts/HunterSettings.cs` | Every tuning number, as one ScriptableObject |
| `Scripts/HunterState.cs`, `Scripts/IHunterStatus.cs` | The state enum, and the read-only status for other systems |
| `Scripts/HunterDebugView.cs` | Test tool: draws memory, route and state in the Scene view |
| `Scripts/HunterTestMover.cs` | Test mover. Disabled on the prefab, which uses `HunterMover` |
| `AgentController/HunterMover.cs` | Walks `CurrentPath` with the `NavMeshAgent`, holds still at doors, pauses 0.35 s on a hit |
| `AgentController/HunterAnimationDriver.cs`, `HunterAudioDriver.cs` | Animation and audio, driven by the brain's state and events |
| `AgentController/HunterSpeedFixPatcher.cs` | Editor tool that sets the animation and tuning values (Haunted Hospital menu) |
| `Prefabs/PF_Hunter.prefab`, `Settings/HunterSettings_Hospital.asset` | The Hunter with all components, and the tuned settings for the hospital |

## Setting it up in a scene

The scene needs:

- A `WaypointGraph` with its nodes (doors and barricades as their own node types).
- A baked NavMesh for the **Hospital_Blockout** agent type.
- Spawn markers whose names start with **`HunterSpawn`**.
- The player, with `PlayerStats`, `PlayerNoiseEmitter` and `PlayerFootstepNoise` (all on the Player prefab).
- `PF_Hunter` placed anywhere. On Play it moves to one of the spawn markers.

`PF_Hunter` carries `HunterBrain` (with `HunterSettings_Hospital` assigned), `HunterEars`, `HunterMover` and a `NavMeshAgent` on the **Hospital_Blockout** type.

## Configuration

Everything is on `HunterSettings_Hospital`. The values currently set are:

| Group | Setting (value) |
| --- | --- |
| Hearing | `hearingThreshold` (1), attenuation (0.12 per m), closed door (×0.4), barricade (×0.25), `memoryCapacity` (8), `memoryMergeRadius` (2 m), `memoryTimeConstant` (12 s), `retargetMargin` (0.25), `forgetBelowScore` (0.1), `investigateThreshold` (1.5) |
| Patrol and search | `patrolWaitSeconds` (3.5), `patrolMinLegDistance` (6 m), `searchSeconds` (10), `searchRadius` (8 m), `searchPauseSeconds` (1.5) |
| Chase | `chaseLoudness` (12), `chaseProximity` (8 m, 0 turns it off), `chaseLoseSeconds` (6), `shotAlertSeconds` (4) |
| Attack | `attackRange` (1.5 m), `attackExitMargin` (1 m), `attackDamage` (20), `attackCooldownSeconds` (1.6), `attackWindupSeconds` (0.55), `freshReachSeconds` (0.5) |
| Doors and routes | `closedDoorExtraCost` (3 m), `doorOpenTimeoutSeconds` (3), `doorBanSeconds` (6), `doorRetryLimit` (3), `replanIntervalSeconds` (0.5), `stuckSeconds` (6) |
| Life cycle | `maxHealth` (100), `deathLingerSeconds` (3), `respawnDelaySeconds` (45), `respawnMinDistanceFromPlayer` (20 m), `respawnAvoidPlayerView` (on) |
| Speed | The agent's base speed is 1.4 m/s, multiplied by `patrolSpeed` 0.7 (0.98 m/s), `searchSpeed` 0.8 (1.12 m/s), `investigateSpeed` 1.0 (1.4 m/s) and `chaseSpeed` 1.29 (1.8 m/s) |

Edits to this asset made in Play mode are **kept** when you stop, so note what you change.

## Debugging and testing

- Tick `enableDebugLogging` on the settings asset, then filter the Console on `[Hunter]` to see what it heard, which state it entered, and which door it is waiting at.
- `HunterDebugView` (on the prefab) draws in the **Scene** view while playing, with Gizmos on: a sphere and score for each remembered noise (yellow is the target, with a line to it), the planned route in cyan, a red circle at the door it is holding at, and its state, health and walk status above its head. Open `NoiseDebugView` as well to see how far each sound spreads.
- To test a locked door, tick **Is Locked** on a door in the Inspector, and do not commit that change.

The Hunter has no automated tests. These checks are run by hand in the hospital or in `Scenes/Sandbox/HunterTest.unity`:

| Test | Expected |
| --- | --- |
| Fire in a room with the door closed, then open | Heard loudness is lower when closed |
| Fire 20 m away with a closed door in between, then with it open | Not investigated when closed (score 1.45), investigated when open (3.63) |
| Crouch, walk and sprint near it | Sprint draws it from about 8 m, crouch does not |
| Close a door standing, then crouched | Loudness 15, then 3 |
| Fire in two places | The stronger noise is investigated first |
| Lock a door or leave a barricade intact | The route goes round, or it stays in its area if sealed |
| Shoot it four times | It dies, then respawns out of sight after 45 s |
| Stand close behind a wall | It does not notice you, because path distance is used |
| Break contact after a chase | After 6 s it searches the last known spot, then patrols |
| Let it kill the player | It returns to patrol |

## Known limitations

- **It is slower than the player at every speed.** Chase speed is about 1.8 m/s, below crouching (2 m/s), walking (4 m/s) and sprinting (6.5 m/s). It can only catch a player who stops, is cornered, or is delayed by a door. Confirm this is intended.
- **Chase by loudness is rare.** Only a gunshot (within about 10 m) or a standing door slam (within about 2 m) reaches the threshold of 12. Footsteps never do, and a barricade break is emitted by the barricade, so it draws an investigation but not a chase.
- **Two settings are not used yet.** `thrownObjectImportance`, because nothing emits a thrown-object noise, and `doorApproachDistance`, because the Hunter stops at the waypoint in front of the door.
- **No other agent makes noise.** The Thief does not emit any, so the Hunter ignores it.
- **The Hunter damages health only.** Sanity loss comes from the Environment Director and the Thief.
- **Performance with all four agents running has not been measured.** Each A\* call creates new lists and dictionaries and scans a list for the cheapest node. This is fine for one Hunter on a graph of about 127 nodes, but it is not free.
- **`HunterBrain` is large** (about 900 lines), because it holds the state machine, spawning, sensing and combat.
