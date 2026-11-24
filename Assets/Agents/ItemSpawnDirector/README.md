# Item Spawn Director

The hospital uses two spawn systems to keep a run supplied and to build its escape sequence.

| System | What it places | Marker names |
| --- | --- | --- |
| Supply director | Medkits, Ammo Boxes and Sanity Bottles | `SP_S` |
| Objective Spawner | Four code papers and the exit key | `SP_C`, `SP_K` |

Both systems use spawn markers placed around the hospital.

## Key terms

| Term | Meaning |
| --- | --- |
| Spawn marker | A point placed in the hospital where an item can appear. |
| Free marker | A spawn marker that currently has no item on it. |
| Supply | A Medkit, Ammo Box or Sanity Bottle that the player can collect. |
| Objective item | A code paper or the exit key. |
| Barricaded marker | A marker behind a barricade that becomes reachable when the barricade is cleared. |
| Reachable marker | A marker that is not blocked by an intact barricade. |
| Replacement cap | The maximum number of a supply type that replacements maintain. |

## Supply spawning

The current hospital layout starts with nine supplies:

| Marker group | Initial supplies |
| --- | --- |
| Barricaded markers | 1 Medkit, 1 Ammo Box, 1 Sanity Bottle |
| Ordinary markers | 2 Medkits, 2 Ammo Boxes, 2 Sanity Bottles |

At startup, the supply director finds every spawn marker whose name starts with `SP_S`. Markers listed as barricaded form one group; all remaining markers form the ordinary group.

Each successful pickup or theft starts its own replacement timer.

| Event | Timer | What happens |
| --- | --- | --- |
| Player collects a supply | 10 s | A replacement is queued. |
| A stolen supply is stashed by the Thief | 20 s | A replacement is queued. |
| A stolen supply is dropped by the Thief | — | The item returns to the world, keeps its reserved marker and still counts toward its type's cap. |

When a timer finishes, the supply director chooses at random among the supply types that are below their caps and places a new item on a free marker.

### Initial layout

The initial plan is placed by a solver. Every item must occupy a unique marker, and the solver applies the 8 m player-distance rule. The solver can use additional markers in stages to complete the layout.

| Mode | Behaviour |
| --- | --- |
| Hard Split | Places the nine supplies using the barricaded and ordinary counts. |
| Random Fill | Places up to the configured Target Count, keeping each type within its normal cap. |
| Spawn On All Markers | Fills every `SP_S` marker in rotation for reachability and visibility testing. |

### Replacement caps

Each supply type has a two-level replacement cap based on the player's matching stat. Health governs Medkits, reserve ammo governs Ammo Boxes, and sanity governs Sanity Bottles.

| Player's stat | Cap for that type |
| --- | --- |
| Below 30% of maximum | 1 |
| 30% or above | 2 |

Exactly 30% uses the normal cap of 2. The cap is checked when the replacement timer finishes.

The initial layout uses the fixed counts above; the caps apply to replacements. Existing supplies stay in the world when a cap changes.

### Choosing a replacement marker

The supply director uses three rules when it places a replacement:

1. **Reachability:** replacements keep at least one supply of the type on a reachable marker. When a type is capped at 1, its replacement is placed on a reachable marker.
2. **Distance:** markers at least 8 m from the player are preferred.
3. **Spread:** markers are ranked by how far they sit from existing supplies, and the director chooses randomly among the three most isolated candidates.

Barricade state is checked live. Clearing a barricade immediately opens its markers for placement.

## Objective spawning

The Objective Spawner creates a four-digit elevator code and reveals it one digit at a time. The full sequence is:

`Start → Paper 1 → Paper 2 → Paper 3 → Paper 4 → Keypad → Exit Key → Elevator switch`

| Step | What happens |
| --- | --- |
| Start of run | A four-digit code is generated and stored for the run. Paper 1 appears on an `SP_C` marker, and the on-screen objective asks the player to find it. |
| Papers 1–3 are read | The next paper appears on an unused `SP_C` marker, and the on-screen objective shows the discovered digit. |
| Paper 4 is read | The on-screen objective directs the player to enter the completed code at the elevator keypad. |
| Correct code entered | The exit key appears on an `SP_K` marker, and the on-screen objective announces it. |
| Key collected | The on-screen objective directs the player to the key switch inside the elevator. |

Each paper carries its code position and digit. Papers remain in the world and can be read again; progress is recorded once per code position. If the spawner is re-enabled during a run, it continues from the player's current progress. Spawning stops once the escape is underway or complete.

### Objective placement

- Papers use `SP_C` markers; the exit key uses `SP_K`.
- Each paper marker is used once per run.
- Markers already occupied by a code paper or pickup are skipped.
- Markers behind an intact barricade are skipped and become available when the barricade is cleared.
- Placement prefers markers at least 8 m from the player.
- Candidates are ranked by how far they sit from the player and existing code papers and pickups; the spawner chooses among the three most isolated.
- If no suitable marker is free, the spawner retries after one second.
- The exit key is placed once, after the code is accepted.

## Configuration

The main settings are exposed on the two components and can be tuned in the Inspector. These are the current `MainHospital` values.

### Supply director

| Setting | Value |
| --- | --- |
| Initial layout | Hard Split |
| Initial split | 1 of each type on barricaded markers; 2 of each type on ordinary markers |
| Random Fill target count | 9 |
| Item prefabs | Medkit, Ammo Box and Sanity Bottle prefabs |
| Marker prefix | `SP_S` |
| Barricade search radius | 7.25 m |
| Minimum spawn distance | 8 m |
| Spread top count | 3 |
| Player pickup delay | 10 s |
| Stolen item delay | 20 s |
| Dynamic caps | Enabled; 2 normal, 1 below 30% |

### Objective Spawner

| Setting | Value |
| --- | --- |
| Paper prefab | `PF_DigitPaper` |
| Key prefab | `PF_ExitKey` |
| Paper marker prefix | `SP_C` |
| Key marker prefix | `SP_K` |
| Barricade search radius | 7.5 m |
| Minimum spawn distance | 8 m |
| Spread top count | 3 |

Both components provide detailed logs and a `Debug: Dump State` action in the component context menu.

## Supply test harness

`ItemSpawnDirectorTestHarness` is a play-mode suite in `Assets/Agents/ItemSpawnDirector/Tests`. The `ItemSpawner_Hospital_Test` scene includes the harness. Run individual checks from its context menu or use `RunAllTests`; results appear as `TEST PASS` or `TEST FAIL`.

The 13 checks cover:

| Area | Tests |
| --- | --- |
| Solver and spread | Solver Unit, Spread Ranking |
| Initial layout and caps | Initial Placement, Dynamic Caps, Cap Enforcement |
| Replacement lifecycle | Player Collect Respawn, Stolen Respawn, Dropped Item Keeps Spot, Distance Window |
| Reachability and spread | Desperation Floor, Cap One Forces Reachable, Cleared Room Becomes Reachable, Respawn Spreads Out |

The suite uses the same public item calls as the player pickup and Thief theft paths, so it runs without a Thief in the scene. The test scene uses 10 s and 20 s replacement delays, an 8 m distance check, a 5 m spread check and 9 initial supplies.

## Implementation

| Responsibility | Where |
| --- | --- |
| Supply placement, replacements, caps and marker choice | `Scripts/ItemSpawnDirector.cs` |
| Initial-layout constraint solver using MRV, forward checking and backtracking | `Scripts/SpawnCspSolver.cs` |
| Code papers, exit key and on-screen objective messages | `Scripts/ObjectiveSpawner.cs` |
| Supply play-mode tests | `Tests/ItemSpawnDirectorTestHarness.cs` |

## Related systems

The spawn systems connect to these existing gameplay systems:

- **Pickup and theft:** Supplies are collected through the project's pickup interaction system. Player pickups and Thief theft events start replacement timers.
- **Objective tracking:** The Objective Spawner reads and updates the project's objective state so code progress is retained when the component is re-enabled.
- **Objective interaction and messages:** Code papers, the elevator keypad, the elevator key switch and the on-screen objective messages guide the player through the escape sequence.

Together, the two systems keep the hospital supplied and carry the player through the code-paper, keypad, exit-key and elevator sequence.
