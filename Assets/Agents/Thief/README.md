# Resource Thief

## What the agent does

The Resource Thief is an autonomous agent that competes with the player for supplies. It looks for the Medkits, Sanity Bottles and Ammo Boxes the player needs, steals one, carries it back to a stash and disappears with it. Once an item reaches a stash it is gone for good, and the Item Spawn Director replaces it more slowly than an item the player picked up.

The Thief turns supply management into a contest. Supplies left lying around are at risk, so the player is pushed to collect what they find. Supplies far from the player are the ones most likely to vanish. Chasing the Thief to win an item back is risky, because its routes favour the Hunter's haunts. Simply catching sight of it costs the player sanity, so it is a threat even when it is not stealing.

The Thief makes its own decisions: what to steal, which stash to use, which route to take and how to deal with closed doors and barricades it meets on the way. It does not move itself. It publishes the route it wants to walk and its current state, and the Agent Controller's code walks and animates it from that. It never steals the exit key or code notes, so it cannot block the escape.

There is only ever one Thief. It reuses the same object every time it comes back.

### What the player sees

| Behaviour | Detail |
| --- | --- |
| Appears | Arrives at one of three Thief spawn points 20–30 seconds into the game, and again after each disappearance or death. |
| Steals | Picks the most valuable supply it can reach, walks to it and takes it. |
| Carries to a stash | Takes the item to the nearest of four stashes, hands it over and pauses for 3 seconds. |
| Vanishes | Disappears with the item, then returns 24–45 seconds later to steal again. |
| Opens doors | Opens closed doors itself and waits for them to swing fully open before walking through. |
| Avoids barricades | Discovers intact barricades in its way, remembers them and goes round. |
| Drops loot when killed | Shot dead while carrying, it drops the item on the floor where it fell, ready for the player to pick up. It returns 30–45 seconds later. |
| Drains sanity | Each time the player catches sight of it, the player loses 5 sanity. |

## Behaviour loop

The Thief is a state machine. Each state has one job, and every change of plan goes back through `Deciding`.

```text
Inactive ──(20–30 s)──► Deciding ─► GoToItem ─► Stealing ─► GoToStash ─► Delivering ─► Vanished
                           ▲            │                       │                          │
                           │         WaitAtDoor              WaitAtDoor                    │
                           │                                                               │
                           └──────────────(reappears after 24–45 s)────────────────────────┘

Any state while in play ──(health reaches 0)──► Dead ──(30–45 s)──► Deciding
```

| State | Meaning |
| --- | --- |
| `Inactive` | Not in the level yet, at the start of a run. |
| `Deciding` | Choosing an item, or a stash when already carrying one. |
| `GoToItem` / `GoToStash` | Walking a planned route. |
| `WaitAtDoor` | Has opened a door and is waiting for it to finish opening. |
| `Stealing` | At the item, taking it. |
| `Delivering` | At the stash, handing the item over. |
| `Vanished` | Gone after a delivery, keeping what it has learned about the building. |
| `Dead` | Killed, until it respawns. |

## Decision process

Each decision follows the same steps:

1. **Observe:** take one snapshot of the player: position, health, sanity and reserve ammo.
2. **List targets:** find every supply in the level that is free to steal.
3. **Score:** rate every item with the utility function below.
4. **Plan:** try to plan a route to the best item. If it cannot be reached, drop it from this decision and try the next best.
5. **Act:** walk the route, open doors, steal, then choose a stash and plan the route there.
6. **Replan:** decide again whenever the plan stops making sense: the target is taken by someone else, a door turns out to be locked, a barricade turns out to be intact, or the Thief reappears.

If nothing can be reached, it looks again one second later.

## Observing the player

The Thief reads the player **once per decision**: before choosing an item. It does not track the player as it moves.

From `PlayerStats` it works out how much the player needs each supply, from 0 (stat full) to 1 (stat empty):

```text
HealthNeed = 1 − health / maximum health
SanityNeed = 1 − sanity / maximum sanity
AmmoNeed   = 1 − reserve ammo / maximum reserve ammo
```

For example, a player on 20 of 100 health has a health need of 0.8. With 40 of 120 reserve rounds, their ammo need is 0.67.

The single snapshot is deliberate. The Hunter is the agent that hunts the player in real time. The Thief is an opportunist: it notes where the player was and what they were short of, commits to a plan and carries it out. That keeps the two agents distinct, makes the Thief's choices readable to the player, and avoids the Thief constantly retargeting as the player walks around.

## Choosing an item: utility scoring

Every free supply gets a score from three factors, each scaled from 0 to 1:

| Factor | Meaning | Calculation |
| --- | --- | --- |
| **P**: far from the player | Items the player is far from are safer to steal. | item's distance from the player ÷ the largest such distance among all items |
| **N**: player need | Items the player is short of hurt the most to lose. | the player's need for that supply type |
| **T**: close to the Thief | Nearby items mean short, less exposed trips. | 1 − item's distance from the Thief ÷ the largest such distance among all items |

```text
score = 0.5 × P + 0.3 × N + 0.2 × T
```

The highest score wins. Distances are measured flat, ignoring height, because items sit on beds, shelves and trolleys at different heights. Each factor is divided by the largest value among the current items, so the three factors stay on the same scale wherever the items are.

### Why these weights

- **Far from the player (0.5)** is strongest. The Thief steals where the player isn't. This keeps it out of the player's way most of the time and makes losses feel like the hospital working against the player, rather than the Thief walking into their line of fire.
- **Player need (0.3)** is the threat. When the player is low on something, the Thief visibly goes for that supply, which is what makes it feel like it is targeting the player. It is not the largest weight, because a Thief that always heads straight for the item the player wants most would walk into the player constantly.
- **Close to the Thief (0.2)** is the tie-breaker. It stops long trips across the map for a slightly better item and keeps each theft short.

Each weight is a 0–1 slider in the Inspector, and setting one to 0 turns that factor off. This made it quick to see the effect of each factor while tuning.

### Worked example

The player has 20/100 health, full sanity and 40/120 reserve ammo. Three items are in the level:

| Item | From player | From Thief | P | N | T | Score |
| --- | --- | --- | --- | --- | --- | --- |
| Medkit | 10 m | 6 m | 0.50 | 0.80 | 0.60 | 0.25 + 0.24 + 0.12 = **0.61** |
| Ammo Box | 20 m | 15 m | 1.00 | 0.67 | 0.00 | 0.50 + 0.20 + 0.00 = **0.70** |
| Sanity Bottle | 16 m | 3 m | 0.80 | 0.00 | 0.80 | 0.40 + 0.00 + 0.16 = **0.56** |

The Ammo Box wins. The Medkit is what the player needs most, but it is close to them. The Ammo Box is still needed and is as far from the player as any item can be. If the player moved away from the Medkit, it would become the target.

### Why utility scoring

A simple rule such as "steal the nearest item" or "steal what the player needs most" would be easy to predict and easy to exploit. A player could guard one item type, or stand near the supplies they want. Scoring several factors at once lets the Thief balance safety, damage to the player and effort, and its choice changes naturally as the player's position and stats change, without a long chain of if/else rules.

## Choosing a stash

Once it is carrying an item, the Thief heads for the stash whose drop point is nearest in a straight line. If no route to it exists, it tries the others in order of distance. The player's position plays no part here: the item is already stolen, and the Thief's goal is to get it home quickly.

## Pathfinding: A* on the shared waypoint graph

The Thief plans every route with **A\*** over a waypoint graph that it shares with the Hunter. The graph has 127 nodes and 207 edges covering every room and corridor of the hospital. Each of the 12 doors and 8 barricades has three nodes in a line through the opening: one on each side and one in the doorway.

The graph holds only facts (positions, connections, what sits at a node) and no costs. Each agent prices the edges in its own way, so the Hunter and the Thief can plan very differently on the same map.

### The Thief's edge cost

```text
cost(step) = infinity                         if the next node is known to be blocked
           = length of the step in metres
             × 0.5   if either end is a Hunter spawn point
             + 2     if the step enters a doorway
```

- **Hunter spawn discount (× 0.5).** Edges touching one of the Hunter's three spawn points look half as long. The Thief therefore prefers routes past the Hunter's haunts. A player who chases the Thief to get an item back is led towards the Hunter. This is how the two enemies co-operate without ever talking to each other.
- **Door penalty (+2).** Each door crossed adds the equivalent of 2 m, standing in for the time spent opening it. Between two otherwise similar routes, the one through fewer doors wins.
- **Blocked nodes (infinity).** A door or barricade the Thief has found impassable is never planned through (see the next section).

### The heuristic

```text
h(node) = straight-line distance to the goal × 0.5
```

A* needs a heuristic that never overestimates the remaining cost, or it can return a worse route. Because Hunter-spawn edges are discounted, a metre can cost as little as 0.5. Multiplying the straight-line distance by that lowest cost per metre keeps the estimate below the real cost, so A* always finds the cheapest route under the Thief's rules, and still searches far less of the graph than an uninformed search.

### Why A*

The Thief needs the cheapest route under its own preferences (Hunter spawns, doors), not just the shortest walk, and it needs to replan quickly whenever it learns something new about the building. A* gives the optimal route for any cost function with an admissible heuristic, and the graph is small enough for each search to finish within a frame. The search itself (`ThiefPathfinder`) knows nothing about the Thief: it takes the cost function as an argument. The policy (what is cheap, what is blocked) lives in `ThiefAgent`, and the algorithm lives in the pathfinder.

### Reaching items off the graph

Items sit on furniture, away from the graph's nodes. For each target the Thief finds a free floor spot beside the item. It searches in rings up to 1.5 m out, keeping 0.4 m from walls and furniture, and checks that the spot is genuinely next to the item and not through a wall. It then adds a short NavMesh path from the last graph node to that spot. If there is no such path, the item counts as unreachable and the Thief moves on to the next best item.

## Doors and barricades: discovery and memory

The Thief does not know in advance which doors are locked or which barricades are still up. It plans **optimistically**, as if every opening is passable, and finds out when it gets there.

To make that possible, a route is handed out one segment at a time. Each segment stops just in front of the next door or barricade. On arrival the Thief checks the opening:

| What it finds | What it does |
| --- | --- |
| Door already fully open | Walks straight through without stopping. |
| Door closed | Opens it, waits for it to swing fully open (1 second), then walks through. If the player shuts the door during the wait, it opens it again and restarts the wait. |
| Door locked | Remembers it as blocked and replans around it. |
| Barricade broken | Walks through, and forgets it if it had been remembered as blocked. |
| Barricade intact | Remembers it as blocked and replans around it. |

**Memory.** Blocked openings go into the Thief's memory, and A* treats them as impassable from then on. Every time the Thief stops at the end of a segment, it re-checks any remembered opening within 3 m. One that has since been cleared, such as a barricade the player shot down, is forgotten, and routes through it open up again. Openings it never passes stay remembered, like a real memory rather than a perfect map.

**Learning and losing it.** The memory survives vanishing, so the longer the Thief survives, the better it knows the building. Killing it wipes its memory, so the player is rewarded for shooting it, and it has to learn the barricades again.

This is more interesting than giving the Thief a perfect map. It visibly tries routes, hits a barricade and turns back, and the player's own actions (clearing barricades, shutting doors) change how it moves.

## Health, vanishing and respawning

- The Thief has 100 health and takes damage from the player's gun through the shared damage interface, the same one the Hunter and barricades use.
- **Killed while carrying:** it drops the item on the floor where it fell. The item can be picked up again and still counts towards the Item Spawn Director's limits, so nothing is duplicated.
- **Killed:** it disappears, forgets the barricades and locked doors it learned, and returns after 30–45 seconds at a random Thief spawn point with full health.
- **Vanished** after a delivery: it keeps its memory and returns after 24–45 seconds at a random spawn point, with its health restored.
- While it is not in the level (`Inactive`, `Vanished`, `Dead`) it cannot be hit or seen.

## Sanity drain on sighting

While the Thief is in the level, it checks every frame whether the player can see it. The player must be alive, the Thief within 20 m of the player's camera and inside the camera's view, and a line from the camera must reach the Thief with nothing solid in between.

When a new sighting starts, the player loses **5 sanity** once. Staying in view drains nothing more. Once the Thief has been out of view for 3 seconds, the next sighting counts again. This makes the Thief frightening even when it is not stealing from the player, without draining sanity every frame.

## Edge cases handled

| Situation | What the Thief does |
| --- | --- |
| The target is taken (by the player) while the Thief is on its way | Notices it is no longer stealable and decides again. |
| The best item cannot be reached | Removes it from this decision and tries the next best item. |
| No item can be reached | Waits one second and looks again. |
| The nearest stash cannot be reached | Tries the other stashes in order of distance. |
| A door is found locked, or a barricade intact | Remembers it, replans and goes round. |
| A remembered barricade is later cleared | Forgets it when passing nearby, and can use the route again. |
| A door is already open | Walks through without stopping. |
| The player shuts a door during the wait | Opens it again and waits for it to finish opening, so it never walks into a moving door. |
| Killed while carrying | Drops the item on the floor, ready to collect; it is neither lost nor duplicated. |
| Shot while vanished or not yet spawned | The damage is ignored. |
| Seen continuously | Only one sanity drain per sighting. |
| Supplies spawned at any time, by any system | Lists the items itself at every decision, so it works with whatever spawns them. |

## Design choices

**Need instead of scarcity.** The original plan scored items by how scarce each supply type was in the level. The final version scores by how much the player needs it, worked out from their stats. The Item Spawn Director already controls scarcity, with fewer replacements while the player is very low. Measuring need directly makes the Thief react to the player's situation, which is what the player notices.

**Distance from the player instead of risk-aware routes.** Rather than recalculating routes around the player's live position, the Thief chooses items the player is far from, and routes past the Hunter's spawn points. Keeping the player out of route planning keeps the Thief distinct from the Hunter, which tracks the player, and the snapshot rule above keeps its choices stable.

**Committing to a target.** Once it has chosen an item, the Thief keeps it until it steals it or the item is gone. It does not switch whenever another item scores slightly higher. Because it only scores items when it decides, it cannot flip between targets mid-trip, so no switching threshold is needed.

**How it differs from the Hunter.** The Hunter hears noises and hunts the player, tracking them as they move. The Thief ignores noise and hunts the player's supplies. It reads the player only when it decides, prices routes towards danger rather than away from it, and learns the building's barricades as it goes.

## Working with the other agents

| System | How the Thief works with it |
| --- | --- |
| Item Spawn Director | Marks each item it takes as stolen. The Director sees that flag when the item is delivered and waits 20 seconds before replacing it, instead of the usual 10. A dropped item keeps its spot, so nothing is duplicated. |
| Sound Hunter | Shares the same waypoint graph, each with its own costs, and favours routes past the Hunter's spawn points. The two never call each other: the Thief only knows where the Hunter's spawn points are. |
| Player stats | Reads health, sanity and ammo for its decisions, and drains sanity when seen. |
| Agent Controller and HUD | Read the Thief's published route, state and events (stolen, delivered, dropped, appeared, vanished, died). Nothing ever calls back into the Thief, so it stays independent of how it is shown. |

## Testing

The Thief was tested in `Thief_Test`, a copy of the hospital with test tools, by recording its own events during Play:

| Test | Result |
| --- | --- |
| Full steal loop | 6 complete cycles in a row with different item types and spawn points, and no stuck states. |
| Timings | First appearance about 23 s; vanish times 26–45 s; 3 s stash pause, all within their settings. |
| Sanity drain | 100 → 70 over 6 sightings, 5 each, counted again only after being out of view. |
| Closed and open doors | 1 second wait at closed doors; walked straight through doors already open. |
| Door shut during the wait | Did not walk into the closing door, opened it again and continued once it was fully open. |
| Killed while carrying | Dropped the item on the floor, collectable; reappeared 44.7 s later at full health. |
| Locked door | Found it locked, replanned through two other doors, and avoided it on the next route as well. |
| Hit while vanished | Damage ignored. |

## How the implementation is organized

| Responsibility | Where |
| --- | --- |
| Decisions, utility scoring, state machine, door and barricade memory, sightings | `Scripts/ThiefAgent.cs` |
| A* search, independent of any agent's rules | `Scripts/ThiefPathfinder.cs` |
| Shared waypoint graph | `Assets/Scripts/Navigation/WaypointGraph.cs`, `WaypointNode.cs`, `PF_WaypointGraph` |
| Theft support on pickups (stolen flag, carrying, dropping) | `PickupInteractable` (`MarkStolen`, `Release`, `WasStolen`) |
| Test tools | `Assets/Scripts/ThiefTest/` (used only in `Thief_Test`) |

Every number above is an Inspector setting on `ThiefAgent`, so it can be tuned without changing code. The result is an agent with its own observations, a utility-based decision, cost-aware A* planning and a memory of the building that it builds up and loses. It reacts to what the player does, and pulls the player towards the Hunter.
