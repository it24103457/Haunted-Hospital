# Resource Thief: testing

Full results of every test run on the Resource Thief. The [Thief README](README.md) describes how the Thief works and summarises each test's outcome; this file holds the figures behind those outcomes.

There are two kinds of test:

- **Play tests** run the Thief for real in `Thief_Test`, a copy of the hospital with test tools, and record its own events during Play. The stash tests ran at 4× speed, with pass or fail checked automatically against the stash state the Thief publishes.
- **Editor checks** test the Thief's decisions directly, without playing the game. They run from **Tools > Thief Test** in the editor: each opens `Thief_Test`, calls the Thief's real decision code, logs its results and reopens the scene that was open before, without saving anything. The scripts are in `Assets/Scripts/ThiefTest/Editor/`.

## Play tests

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
| Stash chances | Added up to 100% in all 9 stash choices. |
| No repeated stash | Never chose the stash it had just used while another was reachable (8 choices), and marked that stash as skipped each time. |
| Delivers where it aimed | All 6 deliveries happened at the chosen stash; the Agent Controller stopped the Thief 0.2–0.5 m from its drop point. |
| Stash memory through vanishing | Still remembered its last stash after each of 5 vanishes and respawns. |
| Stash memory on death | Killed while it remembered a stash: the memory was cleared at once, and its next choice skipped nothing. |
| Stash variety | Used all four stashes. When a route turned out blocked mid-trip, it chose again and still avoided the stash it had just used. |
| Finds the player's weapon | At startup the Thief found the player's `PlayerWeapon`, so loaded rounds count towards the player's ammo (player on 40 reserve + 6 loaded). |

The stash tests were run twice, before and after the change to how need is judged, with the same results both times.

## Check Stash Draw

From 6 starting spots spread through the waypoint graph, the check draws a stash 5,000 times with the Thief's real stash choice and compares how often each stash won (Got) with the chance the Thief gave it (Expected). A chi-square value under 7.81 (3 degrees of freedom, 5% level) means the wins match the chances. The skip of the last stash used happens before the draw, so the play tests cover it rather than this check.

### One run

| Starting spot | S1 expected / got | S2 expected / got | S3 expected / got | S4 expected / got | Chi-square |
| --- | --- | --- | --- | --- | --- |
| Post-op recovery | 29.9% / 29.0% | 49.6% / 50.7% | 11.8% / 11.3% | 8.6% / 8.9% | 3.92 |
| Nurse office | 44.1% / 45.3% | 28.8% / 28.8% | 15.7% / 14.9% | 11.5% / 11.1% | 4.30 |
| Diagnostics and tests | 9.0% / 9.4% | 5.9% / 6.1% | 41.5% / 39.8% | 43.6% / 44.7% | 6.22 |
| Pre-op boards | 33.5% / 32.8% | 21.9% / 21.3% | 25.8% / 26.0% | 18.9% / 19.9% | 4.43 |
| Office north door | 44.1% / 43.8% | 28.8% / 28.8% | 15.7% / 16.3% | 11.5% / 11.2% | 1.57 |
| WC door | 10.3% / 10.5% | 6.7% / 6.7% | 22.6% / 22.2% | 60.4% / 60.6% | 0.44 |

Every stash's share stayed within about 1.7 percentage points of its chance. The stash numbers S1–S4 follow the order Unity lists the stashes in when the scene loads, which can differ between runs; the chances for each physical stash are the same every time.

### Repeated runs

Even with a perfectly fair draw, about 1 starting spot in 20 lands over 7.81 by chance, so single runs can show one high value. To check that this was chance and not a fault, the check was run 13 times:

| | Result |
| --- | --- |
| Starting spots checked | 78 (13 runs × 6) |
| Over 7.81 | 5 (6.4%), against 5% expected for a fair draw |
| Highest values | 15.11, 12.70, 12.31, 12.10, 9.58, spread over 4 of the 13 runs and 4 of the 6 spots |
| Average chi-square | 3.60, against 3.00 expected for a fair draw |

The draw matches the chances the Thief reports.

## Check Item Choice

Item choice has no randomness (the highest score always wins), so the check sets up fixed scenarios with known answers and confirms the Thief's real item choice picks them. The Thief stands 14 m from the player, and each item is placed at the exact distances given. Needs are given to the item choice directly, so this check covers how need is weighed; the need check covers how needs are read from the player. Weights in the scene: 0.5 far from the player, 0.3 player need, 0.2 close to the Thief.

| Check | Chosen | Expected | Result |
| --- | --- | --- | --- |
| Worked example: 20/100 health, full sanity, starting ammo (scores: Medkit 0.67, Sanity Bottle 0.56, Ammo Box 0.50) | Medkit | Medkit | Pass |
| Fallback order, rescored after each removal | Medkit, Sanity Bottle, Ammo Box | Medkit, Sanity Bottle, Ammo Box | Pass |
| Same items, player 1.3 m from the Medkit (Ammo Box 0.50 against 0.47) | Ammo Box | Ammo Box | Pass |
| Sanity Bottle and Ammo Box at equal distances, low sanity | Sanity Bottle | Sanity Bottle | Pass |
| The same two items, low ammo | Ammo Box | Ammo Box | Pass |
| Health at 50%, Medkit 8 m from the player against an unneeded Sanity Bottle 12 m away (0.63 against 0.50) | Medkit | Medkit | Pass |
| Two Medkits equally far from the Thief | The one further from the player | The one further from the player | Pass |
| Two identical Medkits, A listed first | Medkit A | Medkit A | Pass |
| Two identical Medkits, B listed first | Medkit B | Medkit B | Pass |
| No need at all (distance alone) | Sanity Bottle | Sanity Bottle | Pass |

## Check Need

### Need values

The player's health, sanity, reserve ammo and loaded rounds are set to known amounts and the Thief's real player sampling reads them. Health and sanity count any shortfall against their maximum of 100; ammo (reserve plus loaded) only counts below the comfortable amount of 46.

| Player | Health need | Sanity need | Ammo need | Result |
| --- | --- | --- | --- | --- |
| Fresh start: everything full, 40 reserve + 6 loaded | 0.00 | 0.00 | 0.00 | Pass |
| More ammo than the comfortable amount (80 + 6) | 0.00 | 0.00 | 0.00 | Pass |
| Ammo running low (10 + 2) | 0.00 | 0.00 | 0.74 | Pass |
| Ammo empty (0 + 0) | 0.00 | 0.00 | 1.00 | Pass |
| 40 reserve and an empty gun (loaded rounds count) | 0.00 | 0.00 | 0.13 | Pass |
| Health 20% | 0.80 | 0.00 | 0.00 | Pass |
| Sanity 50% | 0.00 | 0.50 | 0.00 | Pass |
| Health 15%, sanity 20%, ammo 20 + 6 | 0.85 | 0.80 | 0.43 | Pass |

### Pick rates

For each situation, 5,000 random layouts (player, Thief and items anywhere in a 60 by 40 m area) go through the Thief's real item choice, with the needs read from the player as above. Item counts follow the Item Spawn Director: 2 of each supply, but only 1 of a supply whose stat is below 30% of its maximum.

- **One supply needed:** passes when that supply wins at least 70% of the time. The rest go to items far safer from the player, which the distance weights are meant to allow.
- **More than one needed:** the Thief is meant to split between them, so it passes when the most-needed supply wins most often and the needed supplies together win at least 90%.
- **Nothing needed:** passes when every supply gets 33% ± 5%.

| Situation | Items | Medkit | Sanity Bottle | Ammo Box | Result |
| --- | --- | --- | --- | --- | --- |
| Health 20%, starting ammo | 1 Medkit, 2 Sanity, 2 Ammo | **81.2%** | 9.6% | 9.2% | Pass |
| Health 50%, starting ammo | 2 of each | **93.9%** | 3.2% | 2.8% | Pass |
| Sanity 25%, starting ammo | 2 Medkit, 1 Sanity, 2 Ammo | 9.5% | **80.4%** | 10.1% | Pass |
| Health 20% and sanity 60%, starting ammo | 1 Medkit, 2 Sanity, 2 Ammo | **62.5%** | 32.7% | 4.9% | Pass |
| Ammo nearly out (4 + 2), the rest full | 2 Medkit, 2 Sanity, 1 Ammo | 9.8% | 10.3% | **79.9%** | Pass |
| Fresh start, nothing needed | 2 of each | 35.1% | 31.9% | 33.0% | Pass |

In the case with two needs, health leads and the needed supplies together take 95%. The Medkit's share is held down mainly by being outnumbered: with only 1 Medkit against 2 Sanity Bottles, there are twice as many chances for a Sanity Bottle to sit somewhere safe. With 2 of each, the same needs give the Medkit about 82%.

### Before and after the need rules changed

The Thief used to measure ammo as reserve rounds against the 120 maximum, so the starting 40 rounds already counted as a need of 0.67, and it used raw needs rather than needs relative to the highest. Estimated with the same scoring in a simulation of the same kind of random layouts, the share of picks going to the most-needed supply changed as follows:

| Situation | Before | After |
| --- | --- | --- |
| Health 20%, starting ammo, one of each | 54% | 84% |
| Health 20%, starting ammo, 1 Medkit against 2 + 2 | 41% | 80% |
| Health 50%, starting ammo, one of each | 37% | 84% |
| Fresh start, nothing needed (share going to ammo) | 70% | 33% |

The last row shows the old rule's bias: with nothing needed, the Thief used to go for ammo most of the time, because the starting ammo looked needed. Now nothing is preferred.
