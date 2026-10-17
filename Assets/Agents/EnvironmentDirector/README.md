# Hospital Environment Director

## Goal and ownership

The Environment Director is an invisible autonomous agent that manages anticipation,
scares and recovery. It belongs to the Environment Director student for IS. It observes
gameplay, predicts short event sequences and selects suitable effects without blocking
escape, taking control of enemies or extinguishing Essential illumination.

The final system is developed in a direct copy of MainHospital named
`Assets/Scenes/Sandbox/EnvironmentDirector_Test.unity`. Installation uses a
hospital-specific prefab, not replacement of the MainHospital scene.

## Agreed event scope

| Event | Effect | Dependency |
| --- | --- | --- |
| Local flicker | Brief local Director-light fluctuations, then restore | Existing Director lights |
| Local blackout | Fade local Director lights out temporarily, then restore | Essential lights remain independent |
| Phone ring | Short spatial ring at a hospital phone | Existing phone and audio clip |
| Whisper or knock | Spatial audio at a suitable nearby anchor | Audio clips and authored anchors |
| Distant apparition | Figure appears briefly in a visible corridor/doorway | One reusable character model |
| Light-pulse apparition | Same figure revealed during a controlled light pulse | Character model and local light executor |
| Hospital announcement | Distorted spatial announcement from a loudspeaker | Existing speaker and spoken audio |
| Rest | Deliberately do nothing and allow recovery | Always available |

The model and its animation are deferred until the observation, planning and other
executors are ready. Both visual events share the same model. Procedural audio can
cover rings, knocks and interference; spoken audio requires recordings or a voice service.

## AI architecture

Architecture: utility-based agent with internal memory and a predicted-state model.
Inference: zero-order Sugeno fuzzy inference calculates desired tension.
Search: bounded, depth-limited Greedy Best-First Search over predicted event sequences.
This is event planning, not physical navigation. No NavMesh or waypoint graph is
required by this Director.

Estimated tension is a gameplay proxy, not a measurement of actual human emotion.
Fuzzy inputs initially include estimated pressure, vulnerability and elapsed quiet time.
Membership functions overlap. Rule outputs combine using a weighted average, with
a defined fallback when total activation is zero.

Search nodes hold predicted tension, simulated time, cooldowns, recent categories,
locations and the sequence that led to them. Actions select an event and an anchor.
Successors advance simulated time and update only their copied prediction state.
A priority queue expands the lowest heuristic score first (`f(n) = h(n)`).

The heuristic estimates pacing undesirability using tension mismatch, repetition
and overload. Invalid actions are excluded before scoring. Default planning horizon
is three actions; an expansion budget bounds runtime work. The planner returns the
best evaluated horizon plan found, or a valid partial plan/Rest if needed. It executes
only the first action and observes the real world before replanning.

This bounded greedy search does not guarantee completeness or global optimality.
It is not A*, Minimax, a learning system, or the local-configuration beam search from
the Local Search lecture.

## PEAS

| Element | Director definition |
| --- | --- |
| Performance | Suitable pressure, variety, recovery, valid effects and bounded planning cost |
| Environment | Hospital rooms, player, existing lights/props and teammate systems |
| Actuators | Director lights, spatial audio and apparition visibility |
| Sensors | PlayerStats, player location, room zones, own history and optional gameplay signals |

## Integration boundaries

- Preserve Essential lights and the player's ability to navigate without a flashlight.
- Only one Director event runs at a time. Restore original values on completion or cancellation.
- Strong effects are suppressed during confirmed chase/critical interaction when those
  signals are supplied; missing optional signals are reported, not invented.
- Director audio is not automatically a Hunter sound event. Coordinate that separately.
- Do not modify Thief scripts, original test scenes, item-spawning logic or waypoint costs.
- Optional Thief-event subscriptions do not make the Thief a required dependency.

## Delivery

`PF_EnvironmentDirector_Hospital` packages the controller, room zones, event anchors,
audio sources and reusable apparition visual. Settings and event definitions remain
supporting assets. Scene objects such as Player and hospital lights are resolved through
validated runtime bindings rather than saved scene references in a prefab asset.

The prefab has world position/rotation zero and unit scale for this hospital. Copy
only the Director prefab into the current MainHospital; never overwrite MainHospital
with the older development scene. Shared identifiers must be explicit and validated.

## Meaningful development milestones

Each milestone is committed after its behaviour or data can be checked. The count is
approximate; do not split working changes merely to reach twenty commits.

1. Hospital-copy sandbox, architecture contract and testable startup/binding validation.
2. Validated final event catalogue and configurable settings.
3. Hospital room detection and authored event-anchor registration.
4. Live read-only player-state observation and optional-signal adapters.
5. Tension estimation, decay and event-memory tracking.
6. Fuzzy membership functions with inspectable inputs/outputs.
7. Sugeno rules and desired-tension inference.
8. Event/location eligibility, cooldowns and reservations.
9. Side-effect-free future-state prediction and time advancement.
10. Configurable heuristic with repetition and overload penalties.
11. Bounded Greedy Best-First sequence search and valid fallback.
12. Observe-plan-execute-replan runtime loop.
13. Complete flicker executor with original-value restoration.
14. Complete local blackout executor preserving Essential illumination.
15. Phone-ring and whisper/knock executors with spatial audio.
16. Loudspeaker announcement executor with supplied speech.
17. Shared animated apparition executor supporting distant and light-pulse variants.
18. Gameplay interruption handling for keypad entry, recent gunfire, Hunter danger, player death and victory.
19. Interruption cleanup and final prefab integration with available teammate signals.
20. Focused integrated verification, runtime measurements and viva documentation.

## Necessary checks

Check missing required bindings, all-events-on-cooldown fallback, high-pressure recovery,
valid room selection, isolated prediction branches and interrupted-effect restoration.
Use the copied hospital, avoiding a separate disposable prototype. Before handoff,
check installation in MainHospital and operation alongside the other agents.

Record planning duration/expanded nodes and one explainable decision trace. Do not
claim improved human fear based only on the internal tension score. Twenty recent
commits do not substitute for the assignment's continuous-effort expectation.

## Event catalogue and settings

DirectorSettings_Hospital references eight event definitions:
LightFlicker, LocalBlackout, PhoneRing, Whisper, Knock,
HospitalAnnouncement, DistantApparition and LightPulseApparition.

Rest is a built-in planner action and does not require an event asset.
Both apparition definitions remain unavailable for selection until
their model and executors are ready.

Event definitions contain configuration only. Runtime cooldowns,
execution state and event history belong to the running Director.

Durations and cooldowns use seconds. Predicted tension increases use
the Director's internal 0–1 tension scale and do not directly change
player health or sanity.

Event cooldowns start after completion or cancellation. The global
interval starts after the previous effect has restored its changes.
Only one Director effect may run at a time.

Validate Director Settings checks the catalogue. Validate Director
Setup also checks scene references and the assigned settings.

Door slamming is outside the current scope.

## Milestone 3: room detection and event targets

The saved EnvironmentDirector_Test scene now has RoomZones and EventAnchors under
EnvironmentDirector, with DirectorSpatialRegistry and DirectorRoomTracker on that root.
This milestone observes location and registers targets; it does not play scares yet.

The 40 floor pieces form 33 logical zones. Operating, ICU, Sterile Core,
Operating Approach and the entrance/main corridor combine their adjoining floor pieces.
Volumes extend from Y=-0.25 to Y=3.25 and use the existing floor footprints.
They do not add physics colliders or change the NavMesh.

| Anchor group | Count | Purpose |
| --- | ---: | --- |
| Lighting | 28 | Explicit references to existing Director lights only |
| Phones | 11 | References to existing wall phones |
| Speakers | 8 | References to existing loudspeakers |
| Audio | 56 | One whisper and one knock position per lit zone |
| Apparitions | 8 | Disabled positions reserved for later model integration |

Elevator, NorthServiceAlcove, OperatingServiceShaft, StairFootprint and
ElevatorServiceStrip remain detectable but have no event anchors inside them.
Apparition positions were checked against a reference body footprint, not the final
model. Check model bounds, sight lines and player distance when enabling that milestone.

### Inspect and test

1. Open EnvironmentDirector_Test, outside Play mode.
2. Select EnvironmentDirector. Check the Registry root references and the Tracker's
   Player, Registry and Settings references in the Inspector.
3. Open the Director Spatial Registry component menu and choose Validate Spatial Setup.
   The expected successful result is 33 zones and 111 anchors.
4. Expand RoomZones and select a zone. With Gizmos enabled, its volumes appear.
   Only selected zones/anchors draw gizmos to avoid covering the entire hospital.
5. Enter Play mode and walk between rooms. Current Room Id on the Tracker and
   Console messages report confirmed room changes when debug logging is enabled.
6. Room detection samples every 0.1 seconds and requires a new room to persist for
   0.15 seconds. Standing at a shared edge keeps the current room where possible.
   Leaving all zones reports outside after the confirmation delay.
7. Stop Play mode before changing authored data. If adding or removing zones or
   anchors, choose Rebuild Spatial Registry, then Validate Spatial Setup.

The registry checks duplicate IDs, missing or cross-scene references and accidental
Essential-light targeting. GetAnchors filters by room, target type and availability;
SupportsEvent distinguishes whispers from knocks. Cooldowns, visibility, distance,
danger suppression and event execution belong to later milestones.

These bindings currently refer to objects in the test scene. Do not save this root
as a supposedly self-contained prefab and expect external scene references to survive.
Runtime binding for the final prefab is still a later integration milestone. MainHospital
has not been overwritten with the test scene.

## Milestone 4: live player observations

DirectorObservation is attached to EnvironmentDirector in EnvironmentDirector_Test.
It reads PlayerStats, the player Transform, DirectorRoomTracker and DirectorSettings_Hospital.
The interval is 0.1 seconds of game time. Current returns a value snapshot with IsValid;
consumers must reject invalid or stale snapshots before making decisions.

The snapshot exposes health and sanity (raw, maximum and 0–1 fractions), reserve ammo,
life state, exit-key possession, position, confirmed room, time in that room and damage
recovery. Having the key does not mean victory. Reserve ammo changes do not mean gunfire.
HunterDanger, RecentGunfire, KeypadInteraction and Victory remain Unavailable until their
adapters are implemented. Unavailable is distinct from a confirmed inactive signal.

Damage uses PlayerStats.Damaged and death uses PlayerStats.Died. Before the first observed
hit, HasReceivedDamage is false and SecondsSinceDamage is -1. Recovery uses the configured
DamageRecoverySeconds. Disabling observation removes its subscriptions and invalidates
Current; re-enabling starts a fresh observation session and does not invent missed damage.
The room timer resets only on a confirmed room change. It is not a scare waiting gate.
Event history and the global event pacing timer belong to later milestones.

To check this milestone:

1. Enter Play mode and select EnvironmentDirector. Expand the observation Snapshot.
2. Compare its health, sanity and reserve ammo with PlayerStats.
3. Walk between rooms and watch Room Id and Time In Room.
4. On PlayerStats, use the component menu Debug: Take 30 damage. Health should fall and
   Damage Recovery Active should become true, then expire after the configured duration.
5. Use Debug: Drain 30 sanity and Debug: Use 10 ammo and check the corresponding readings.
6. Stop Play mode. Runtime tests do not change the player's saved starting values.

The Inspector snapshot is a diagnostic display, overwritten on refresh. Editing it cannot
change PlayerStats. No new colliders, scare effects or player-control logic are introduced.

## Milestone 6: fuzzy input memberships

DirectorFuzzifier reads DirectorTensionEstimator and DirectorEventMemory, using
DirectorFuzzySettings_Hospital. It exposes three degrees per input, all between 0 and 1.
These are membership strengths, not probabilities. Desired tension and Sugeno rules
belong to milestone 7; this component does not select or execute events.

DirectorFuzzyMembership is pure calculation code. Shared X/Y/Z knots define a left
shoulder, a triangular middle and a right shoulder. This guarantees continuous coverage
without gaps for valid settings. X is where full Low ends, Y is the Medium peak, and Z
is where full High begins. The shoulders saturate beyond the outer knots.

Defaults:
- Pressure: (0.2, 0.5, 0.8), domain 0–1.
- Vulnerability: (0.2, 0.5, 0.8), domain 0–1.
- Quiet duration: (10, 30, 60), in game seconds; Low/Medium/High mean Short/Medium/Long.

The component refreshes every 0.1 game seconds. It refreshes the estimator first so
the existing observation-age checks apply. Missing/disabled sources, invalid settings,
non-finite inputs and player death invalidate live output. Consumers must check IsValid
and SampledAt. Unknown teammate signals remain a separate integration concern.

Manual verification (left to the user):
1. Select EnvironmentDirector in EnvironmentDirector_Test and find Director Fuzzifier.
2. Confirm Estimator, Event Memory and Settings are assigned.
3. Outside Play mode, set Preview Pressure=0.4, Preview Vulnerability=0.2 and
   Preview Quiet Seconds=45. Use the component menu Preview Memberships From Manual Inputs.
4. Expand the Preview outputs: pressure approximately (0.3333,0.6667,0), vulnerability
   (1,0,0), quiet (0,0.5,0.5). Preview is separate from live decision inputs.
5. Check pressure preview at 0, 0.2, 0.5, 0.8 and 1: respectively (1,0,0), (1,0,0),
   (0,1,0), (0,0,1), (0,0,1). Quiet at 0/10 is Short=1, 30 Medium=1, 60/120 Long=1.
6. Enter Play mode. Output Valid should become true for a living player with valid sources.
   Damage/sanity debug actions should change the live inputs. Small changes inside a
   shoulder plateau can legitimately leave the memberships unchanged.
7. Walk between rooms: global quiet time must keep increasing. Simulate a delivered event
   using Event Memory's debug menu: quiet time resets and event pressure increases.
8. Disable Director Observation: live output should become invalid on refresh. Re-enable
   it: output should recover. Stop Play mode when finished.

This milestone was installed without running Play-mode or manual-preview tests, at the
user's request. The preview fields are diagnostic values and never feed the live output.

## Milestone 7: zero-order Sugeno desired tension

DirectorDesiredTension on EnvironmentDirector reads the existing Fuzzifier. The
DirectorSugenoRules_Hospital asset holds exactly 27 combinations of pressure,
vulnerability and quiet-duration categories. Low/Medium/High for quiet duration mean
Short/Medium/Long. Current estimated pressure is distinct from this desired target.

DirectorSugenoInference is pure calculation code for reuse in future prediction.
Rule activation is the product of the three antecedent memberships. Each consequent
is a constant. Desired tension = sum(activation * consequent) / sum(activation).
Only contributing rules appear in the explanation list; each exposes its categories,
activation, constant output and weighted contribution. The default product/partition
configuration gives a total activation of approximately 1 for valid inputs.

The default targets are initial pacing choices, not measured human fear:

| Pressure | Vulnerability | Short | Medium quiet | Long |
| --- | --- | ---: | ---: | ---: |
| Low | Low | .25 | .50 | .70 |
| Low | Medium | .20 | .35 | .45 |
| Low | High | .10 | .15 | .20 |
| Medium | Low | .20 | .35 | .45 |
| Medium | Medium | .15 | .25 | .30 |
| Medium | High | .08 | .10 | .12 |
| High | Low | .10 | .15 | .20 |
| High | Medium | .08 | .10 | .12 |
| High | High | .05 | .06 | .08 |

Validation requires 27 unique combinations and finite outputs within 0–1. Missing
input blocks normal output. Invalid rules or zero meaningful activation return an
explicit invalid result with a diagnostic fallback (default .15). The planner must
check IsValid and SampledAt; a fallback is not permission to execute a scare. Life-state
and stale-observation checks flow through the refreshed Fuzzifier. Teammate safety
signals remain future eligibility dependencies. No events are executed by inference.

Manual tests for the user (not run during installation):
1. Select EnvironmentDirector > Director Desired Tension. Confirm Fuzzifier, Rules and
   Preview Fuzzy Settings references are assigned.
2. Outside Play mode, set manual preview inputs and choose the component menu
   Preview Desired Tension From Manual Inputs. This affects only preview output.
3. With default assets, expected outputs are:
   - Pressure .1, vulnerability .1, quiet 0 seconds: .25.
   - Pressure .1, vulnerability .1, quiet 30 seconds: .50.
   - Pressure .1, vulnerability .1, quiet 60 seconds: .70.
   - Pressure .8, vulnerability .8, quiet 60 seconds: .08.
   - Pressure .4, vulnerability .2, quiet 45 seconds: approximately .466667,
     with four contributing rules and total activation approximately 1.
4. Select the rule asset and use Validate Sugeno Rules to check coverage. To exercise
   invalid coverage, duplicate the asset, remove a rule from the duplicate and temporarily
   assign it to a preview: Preview Valid must be false. Reassign the original afterward.
5. In Play mode, Output Valid should become true with a living observed player. Wait,
   use existing damage/sanity debug actions and inspect desired tension and contributions.
6. Disable Director Observation: live inference should become invalid. Re-enable it to
   recover. Room changes must not reset quiet time. Stop Play mode after testing.

Manual preview uses the assigned Preview Fuzzy Settings asset; keep it the same as the
Fuzzifier's asset when comparing live and preview calculations. No test result is claimed
by this setup; runtime and preview verification are left to the user.
