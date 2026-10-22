# Hospital Environment Director

## What the agent does

The Environment Director is an invisible autonomous agent that controls the hospital's horror pacing. It observes the player's condition and surroundings, decides whether to create a disturbance or allow recovery, and selects a suitable event and location. Its purpose is to maintain tension while giving the player room to explore and complete the escape objective.

The Director uses custom C# fuzzy inference, state prediction and informed search. Unity provides the lights, audio, animation and physics queries that carry out those decisions. The agent does not navigate around the hospital or control the Hunter and Thief.

### Events

| Event | Behaviour | Sanity cost per delivered event |
| --- | --- | --- |
| Local light flicker | Fluctuates the current room's Director lights for 10 seconds, with accompanying audio, then restores them. | 3 |
| Hospital blackout | Fades the hospital's Director lighting out and back in over a 30-second event. Essential lights remain independent. | 3 |
| Phone call | Plays a spatial phone ring, followed by one complete voice line. | 5 |
| Whisper | Plays one complete spatial whisper at an authored nearby anchor. | 5 |
| Hospital announcement | Plays one complete announcement from an authored loudspeaker anchor. | 5 |
| Knock | Plays the door-knocking recording at a nearby anchor; it does not move or slam a door. | 3 |
| Apparition | Darkens the target room's Director lights, reveals the woman with her voice, plays UnsettlingIdle, BodyJolt and PulsePose, then removes her and restores the lights. | 10, only after an actual visible sighting |
| Rest | Starts no effect and allows pressure and cooldowns to recover. | 0 |

Phone, whisper and announcement durations follow the selected recordings so their voice lines can finish. The apparition plays its arrival stinger at reveal, followed by one complete Monster_03 performance after the arrival cue finishes. The monster clip never loops. A separate disappearance sound continues after the model is removed. The event duration accommodates the animation sequence and the delayed monster clip. Its temporary reveal light makes the figure readable during the room's darkness.

The woman's animation clips move 13 upper-body bones through a clear pointing gesture, a pronounced backward spinal arch and sideways head tilts reaching approximately 48 degrees. Her pointing arm aims at the observed player position after animation sampling, with a short blend into and out of aiming. The hips, legs and root stay fixed. Pointing clips loop continuously, and the controller blends between poses. During a Director appearance, the executor controls the sequence so the standalone jolt transition cannot return her to idle prematurely. The current rig has no jaw bone or mouth blend shapes, so these clips do not animate the mouth.

A separate `light_pulse_apparition` definition exists in the catalogue but is currently disabled and has no supported executor. The `PulsePose` animation within the active apparition is a pose in that event, rather than a second independently running event.

## Decision process

The runtime cycle is:

1. **Observe:** capture player statistics, position, room and gameplay signals.
2. **Estimate:** calculate vulnerability and current pressure from player condition, recent damage and event memory.
3. **Set a target:** use fuzzy rules to calculate desired tension.
4. **Find candidates:** identify compatible, spatially valid event/anchor pairs.
5. **Predict and search:** simulate short sequences of events and Rest, then compare their heuristic scores.
6. **Execute and remember:** revalidate the first action, reserve its target, run the effect and record the outcome.
7. **Replan:** observe the real state again rather than blindly executing the remaining predicted sequence.

The decision component checks approximately every 0.25 seconds when planning is allowed. A room change brings the next planning opportunity forward. The player does not need to remain in a room for several seconds to accumulate permission for an event; room tracking uses a short confirmation period to avoid flickering between zones at doorways.

## Observations and spatial reasoning

`DirectorObservation` creates a timestamped value snapshot containing health, sanity, ammunition, position, room, damage recovery and the gunfire, keypad, victory and Hunter-danger signals. Health and sanity drive the pressure mathematics; not every observed field is used directly in that formula.

`DirectorLiveHunterSource` reads `IHunterStatus`. A ready Hunter chasing or attacking the player supplies an active danger signal. Missing or unready information is represented as **Unavailable**, rather than assumed safe. A manual Hunter source supports controlled simulation when needed.

`DirectorRoomTracker` resolves the player's position against authored room zones. `DirectorSpatialRegistry` holds those zones and event anchors: named locations associated with rooms, light groups, phones, speakers, sounds or apparition placements.

An apparition requires the player to be inside the same room, within the event's distance range and connected to the spawn location by an unobstructed ray. The player need not already be looking in that direction. The Director does not force the player's camera to turn. After revealing the figure, a separate camera visibility check determines whether a real sighting occurred.

## Calculating vulnerability and pressure

These values are normalized gameplay estimates between 0 and 1. They are not measurements of the player's actual emotions. The following numbers are the current hospital settings and can be tuned through the settings assets.

Let `H = health / maximum health`, `S = sanity / maximum sanity`, and `clamp01(x)` restrict a number to the range 0–1.

### Player vulnerability

```text
V = [0.6 × (1 − H) + 0.4 × (1 − S)] / (0.6 + 0.4)
```

Lower health and sanity increase vulnerability. For example, 70% health and 50% sanity give:

```text
V = 0.6 × 0.30 + 0.4 × 0.50 = 0.38
```

### Estimated current pressure

```text
DamagePressure = 0.25 × [1 − clamp01(secondsSinceDamage / 10)]
P = clamp01(0.5 × V + DamagePressure + EventPressure)
```

Damage pressure is zero if no damage has been observed. Otherwise, it falls from 0.25 to zero over the 10-second recovery period.

An effect adds its configured predicted tension increase to event pressure when it is actually delivered, rather than merely selected. Event pressure is capped at 1, sustained while a delivered effect is active, and decays during recovery:

```text
EventPressureAfterRecovery = max(0, EventPressure − 0.02 × elapsedSeconds)
```

For example, with `V = 0.38`, no recent damage and event pressure of 0.20, estimated pressure is `0.5 × 0.38 + 0.20 = 0.39`.

## Fuzzy logic: choosing desired tension

The agent uses **zero-order Sugeno fuzzy inference**. It takes three inputs: current pressure, vulnerability and quiet duration. Quiet duration is time without a delivered active effect, and is reset around delivered events; it is not time spent standing in one room.

Each input has overlapping Low, Medium and High memberships. Quiet duration uses the same calculation with the interpretation Short, Medium and Long. Current knots are:

| Input | End of fully Low | Peak Medium | Start of fully High |
| --- | --- | --- | --- |
| Pressure | 0.20 | 0.50 | 0.80 |
| Vulnerability | 0.20 | 0.50 | 0.80 |
| Quiet duration | 10 seconds | 30 seconds | 60 seconds |

For input `x` and knots `(a, b, c)`, the membership functions are:

```text
Low(x)  = 1 − clamp01((x − a) / (b − a))
High(x) = clamp01((x − b) / (c − b))
Medium(x) = clamp01((x − a) / (b − a))       when x ≤ b
            1 − clamp01((x − b) / (c − b))   when x > b
```

For example, pressure 0.35 has Low membership 0.5, Medium membership 0.5 and High membership 0. This provides a gradual response as gameplay changes.

The rules asset contains all 27 combinations of the three inputs. Each rule supplies a constant desired-tension output. Examples from the hospital rules are:

- Low pressure, low vulnerability and long quiet time → target 0.70.
- Low pressure, high vulnerability and long quiet time → target 0.20.
- High pressure, high vulnerability and short quiet time → target 0.05.

Rules combine using product AND and a normalized weighted average:

```text
RuleActivation = PressureMembership × VulnerabilityMembership × QuietMembership
DesiredTension = sum(RuleActivation × RuleOutput) / sum(RuleActivation)
```

For example, two active rules with activations 0.6 and 0.4, and outputs 0.50 and 0.20, produce `(0.6 × 0.50 + 0.4 × 0.20) / 1.0 = 0.38`.

Invalid inputs or negligible total activation mark the output invalid and block normal planning. A configured fallback value is diagnostic information, not permission to execute a scare.

## Prediction and informed search

`DirectorStatePredictor` simulates actions on copied state. A node contains simulated elapsed time, vulnerability, sanity, event pressure, damage recovery, cooldowns and branch-local history. An action is **an event at an anchor**, or **Rest**.

An event advances time, adds predicted pressure, forecasts its sanity cost and starts cooldowns. Rest advances time, reduces pressure and cooldowns, and increases quiet time. The predicted sanity reduction also increases vulnerability according to sanity's weight. Predicting an apparition assumes a delivered sighting; the real sanity cost still waits for visibility.

The planner uses **bounded, depth-limited greedy best-first tree search**:

1. Place the current state in a frontier list.
2. Select the frontier node with the lowest heuristic priority.
3. Expand it using valid predicted actions.
4. Score successors and retain their parent links.
5. Stop at the depth/work limits and reconstruct the best explored full-depth plan, or a partial plan if necessary.

The hospital settings use a depth of **3 actions**, an expansion budget of **64 nodes**, and a simulated Rest duration of **5 seconds**. Additional hard caps bound generated nodes and candidate count. The frontier is a list scanned for its best node, rather than a heap-based priority queue.

This is event-sequence planning, not NavMesh pathfinding. It is heuristic-guided greedy search, not A*: there is no separate path-cost `g` plus an admissible distance estimate. It does not guarantee a globally optimal or complete solution, and it does not train or learn a model.

### Heuristic score

Lower scores represent more suitable predicted pacing. For a predicted pressure `Pnext`, target `T`, predicted vulnerability `V`, and repetition evidence `Revent` and `Ranchor`, the current local score is:

```text
h = |Pnext − T|
    + max(0, Pnext − T) × (1 + 2 × V)
    + 0.22 × Revent
    + 0.10 × Ranchor
    − ApparitionPreference
```

The terms favour matching desired tension, penalize exceeding it more strongly for vulnerable players, discourage repeating event types and locations, and gradually favour an apparition after a prolonged absence.

Repetition evidence sums the recency of previously delivered, non-debug events of the matching type or anchor, then clamps the sum to 0–1. Each record's contribution is `clamp01(1 − secondsSinceItEnded / 180)`.

The apparition preference is:

```text
0.35 × clamp01((secondsSinceActualSighting − 60) / 180)
```

Before the first sighting, elapsed time is measured from the memory component's initialization. The preference is credited at most once per predicted branch. It cannot bypass location, cooldown or safety checks, and does not guarantee a spawn.

Live search accumulates tension-tracking and repetition scores across the predicted sequence. It also permits weighted variation among similarly scored complete plans. For per-step score difference `d` from the best plan and tolerance `t = 0.16`, the sampling weight is `exp(−2d/t)` when `d ≤ t`. One best alternative per first-action label participates, preventing an event with many anchors from receiving extra sampling entries.

Only the first action is committed. Future choices are recalculated from real observations, so the predicted plan is not a fixed scare order.

## Timing, variation and execution

The minimum gap is currently **24 seconds after an event ends**, plus any event-specific cooldown and other restrictions. It is a minimum, not a promise that an event will occur exactly then. Rest may remain preferable, and the player may have no valid nearby target.

The three voice categories use shuffled clip collections without repeating a clip within their collection. Phone has 3 voices, whisper has 4 and announcements have 4. After a category completes its clips, that category becomes unavailable until all three categories complete their shared cycle. Then all three counters reset. Knock, flicker, blackout and apparition do not belong to this voice cycle. This controls repetition without imposing a fixed order between event categories.

Before execution, `DirectorEventEligibility` checks current observations, event availability, target compatibility, distance, room, cooldowns and gameplay restrictions. An executor revalidates and reserves its target before starting. Only one Director event runs at a time.

Recent gunfire blocks new non-lighting events for 6 seconds; recent damage blocks them for 10 seconds. Hunter danger and keypad interaction also restrict events according to their definitions. Flicker and blackout can start during gunfire, damage recovery or Hunter danger, but still obey spacing, target, pause, keypad and end-state checks.

Those admission restrictions do not cancel an effect already in progress. Shooting or taking damage lets the active effect finish. Pausing preserves the effect and its progress using the game's scaled time/audio pause behaviour. Disabling an executor or losing required objects can still force cleanup; cleanup restores saved light intensities, stops owned audio, removes temporary visuals and releases reservations.

Sanity is deducted once per delivered event through `PlayerStats.DrainSanity`. Apparition cost waits for an unobstructed on-screen sighting and is applied at most once. Event history records the event, anchor, room, timing, delivery and outcome. Cooldowns and voice-cycle counters are stored separately from the bounded history, so removing old diagnostic records does not erase those restrictions.

## How the implementation is organized

| Responsibility | Main scripts |
| --- | --- |
| Scene references and authored targets | `DirectorSceneBinding`, `DirectorSpatialRegistry`, `DirectorRoomZone`, `DirectorEventAnchor` |
| Player and Hunter observations | `DirectorObservation`, `DirectorObservationSnapshot`, `DirectorPlayerSignals`, `DirectorLiveHunterSource` |
| Room tracking and pressure estimate | `DirectorRoomTracker`, `DirectorTensionEstimator`, `DirectorEventMemory` |
| Fuzzy target calculation | `DirectorFuzzyMembership`, `DirectorFuzzifier`, `DirectorSugenoInference`, `DirectorDesiredTension` |
| Prediction, scoring and search | `DirectorPlanningState`, `DirectorStatePredictor`, `DirectorHeuristic`, `DirectorGreedyPlanner` |
| Runtime planning and admission | `DirectorPlannerPreview` (also supplies live plans), `DirectorDecisionLoop`, `DirectorEventEligibility`, `DirectorTargetReservations` |
| Visible and audible effects | Flicker, Blackout, Phone, Whisper, Announcement, Knock and Apparition executors; `DirectorApparitionVoice` |

ScriptableObject assets hold the event catalogue, fuzzy knots, rule outputs, tension parameters and scoring weights. The reusable `Prefabs/PF_EnvironmentDirector.prefab` packages the components, zones and anchors. Its scene binder reconnects it to player, objective, lighting and source objects before the other components initialize. Its authored zones and source paths describe this hospital layout; a different layout needs corresponding zones, anchors and bindings.

The result is an agent with explicit observations, internal memory, a predictive model, fuzzy goals and bounded informed search. Its decisions adapt to changing gameplay while its executors handle the concrete Unity effects.
