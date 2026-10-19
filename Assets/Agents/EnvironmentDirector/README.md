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

## Milestone 8 - event eligibility, cooldowns and reservations

DirectorEventEligibility produces event/anchor candidates with a readable rejection
reason. Rest is always included as a no-effect fallback. It does not execute scares,
load audio, choose the best event, or replace the later search planner.

New files: DirectorEligibilityResult.cs, DirectorEventEligibility.cs,
DirectorTargetReservations.cs and Editor/DirectorEligibilitySetup.cs.
DirectorEventMemory now keeps per-event-ID cooldown expiry independently of bounded
history. Completed, cancelled and failed real event attempts start their configured
cooldown AFTER FinishEvent (which must follow effect cleanup). Debug pressure records
do not start catalogue cooldowns. Cooldowns and global spacing use game time and do
not reset on room changes. They restart with a fresh Play session.

Installation (outside Play mode):
1. Open Assets/Scenes/Sandbox/EnvironmentDirector_Test.unity.
2. Wait for Unity to import and compile the new scripts.
3. Choose Tools > Environment Director > Set Up Commit 8 In Test Scene.
4. On EnvironmentDirector, inspect Director Event Eligibility and Director Target
   Reservations. The setup reuses existing components and does not duplicate them.
5. Ensure Player Camera is the actual gameplay camera under Player. If more than one
   enabled camera exists, assign the correct one manually.
6. Save the test scene with Ctrl+S. The setup intentionally refuses MainHospital.

Policy:
- Requires fresh, living player observation, recognized room, enabled valid catalogue
  event, available compatible registered anchor, configured distance and global spacing.
- Lighting targets only the current room. Audio and apparition anchors can be in other
  rooms within distance limits; no stationary-room waiting period is required.
- Rejects Essential lights and light references outside the Director light root.
- Damage recovery blocks effects. Victory and recent gunfire must explicitly be Inactive;
  applicable keypad and hunter checks must also explicitly be Inactive.
- IMPORTANT: Current observation still supplies Unavailable for teammate safety signals.
  Consequently real effects remain blocked until those adapters are integrated. This is
  intentional. Do not change Unavailable to Inactive simply to make events eligible.
- Visibility-required events need the assigned player camera, a point within its view,
  and an unobstructed physics ray (ignoring triggers, player colliders and source colliders).
  Apparition visibility targets one metre above its anchor. Geometry needs colliders for
  ray occlusion. Model bounds visibility and executor asset readiness are later checks.
- Candidate eligibility does not mean an executor, audio clip or apparition model exists.

Future execution contract:
1. CollectCandidates returns Rest plus eligible event/anchor pairs (or request rejected
   pairs too for diagnostics).
2. After selection, TryReserveCandidate rechecks the candidate and grants a two-second
   pending lease. Store its returned token. Only one reservation is allowed at a time.
3. Immediately before starting, Evaluate(definition, anchor, token) again. If rejected,
   release the token and replan. A stale preview is never permission to execute.
4. Call EventMemory.BeginEvent before any effect. If it fails, release the token.
5. Renew the reservation for the effect duration plus cleanup margin; maintain/renew it
   if needed. EventMemory.EventActive independently prevents concurrent real effects.
6. MarkEffectDelivered only once the effect is actually delivered.
7. On completion, cancellation or failure, restore lights/audio/visuals first, then
   FinishEvent and release the token. Release on every failure path. Old tokens cannot
   release a newer reservation. Expired leases and disabled reservations are cleared.
8. During execution use future cancellation monitoring; Evaluate is a pre-start check
   and correctly rejects while EventMemory reports an active event.

Manual checks for the user - not run during implementation:
1. Enter Play mode and wait one second. On Director Event Eligibility open the component
   menu > Preview Eligibility (Play mode, no effects). Expand Preview in the Inspector.
   First entry is Rest with Eligible checked. Other rows show event, anchor, distance and
   their first blocking reason. Eligible Event Count excludes Rest.
2. Missing-signal messages are expected for otherwise suitable candidates. No sound,
   flicker or blackout should occur. Re-run Preview after each change; it is not live UI.
3. Walk into another room and preview again. Lighting candidates from other rooms should
   say they must target the current room; nearby current-room candidates progress to
   safety checks. Distance-rejected candidates should change as you move.
4. Disable Director Observation during Play, preview again: valid configured candidates
   should report observation unavailable/stale. Re-enable it and preview after one second.
5. Use PlayerStats' existing damage debug action, then preview a nearby compatible event:
   Damage recovery should block it for the configured recovery time (provided earlier
   distance/pacing checks pass). Its next reason may then be an unavailable safety signal.
6. With all required live adapters integrated later, test visibility by facing toward and
   away from an enabled visibility-required target and putting a wall between it and the
   camera. Disabled apparition definitions remain disabled until their implementation.
7. With the executor or a developer harness later: begin and finish a real catalogue
   event, then read CooldownRemaining(eventId). It should count down from the definition's
   Cooldown even across room changes and history eviction. Global spacing should also
   block new attempts until its interval ends. The earlier debug pressure menu tests only
   global spacing, deliberately NOT per-event cooldowns.
8. Reservation API checks for a developer harness: reserve an available anchor, attempt
   a second reservation (must fail), release with a wrong token (must fail), then release
   with the right token (must succeed). Let a short lease expire and reserve again. These
   calls must run in Play mode. No fake live safety overrides have been added.
9. Stop Play mode. Do not save temporary Play-mode configuration changes.

Suggested commit message: feat(director): filter event candidates with safety checks,
cooldowns and target reservations

## Milestone 9 - future-state prediction

Added DirectorPlanningState.cs (detached state and copied cooldowns),
DirectorStatePredictor.cs (pure event/Rest transitions), and DirectorPredictionPreview.cs
(Inspector preview and live snapshot capture). Event Memory exposes PlanningEventPressure,
a read-only projection that does not advance or change its stored pressure. No Editor
helper, new asset, model, audio clip, executor or automatic preview loop is required.

Prediction model:
- Vulnerability remains fixed within a short planning branch. No future damage, healing,
  movement or resource pickup is invented.
- An event is assumed delivered at its start and cleaned up at the end of Duration.
- E_next = max(0, min(MaximumEventPressure, E + eventIncrease) - decayRate * duration).
  Rest has zero increase and uses RestDuration.
- DamageRemaining_next = max(0, DamageRemaining - duration).
- Pressure_next = clamp01(vulnerability * VulnerabilityContribution +
  MaximumDamageContribution * DamageRemaining_next / DamageRecoverySeconds + E_next).
  The damage term is zero when recovery duration is zero.
- Quiet time increases by RestDuration during Rest. For an event it resets on assumed
  delivery, so its value at the end is that event's Duration.
- Existing cooldowns decrease as simulated time advances. On predicted event completion,
  that event receives its full Cooldown and global spacing receives MinimumTimeBetweenEvents.
- An event cannot be predicted while its cooldown, global spacing or damage recovery is
  positive; predict Rest transitions first. Disabled or invalid events are rejected.
- State transitions create new states, including independent cooldown arrays. Branches
  cannot modify their parent or siblings. Live timers, reservations and history are untouched.

Limits: predicted success is a modelling assumption, not authorization to run a scare.
The preview intentionally operates on catalogue events without requiring spatial eligibility
or teammate signals; it may therefore predict a mathematical outcome for an event that
commit 8 currently blocks. Future search must separately apply eligibility, recheck the
player/anchor at execution, and handle cancellation. Current room, camera visibility,
hunter movement and future safety signals are not predicted here. Desired tension is
not recalculated by this component; scoring and search are the next milestones.

Setup if the component is not already attached:
1. Open EnvironmentDirector_Test outside Play mode.
2. Select EnvironmentDirector; Add Component > Director Prediction Preview.
3. Assign DirectorSettings_Hospital to Director Settings and
   DirectorTensionSettings_Hospital to Tension Settings from the Settings folder.
4. Drag EnvironmentDirector into Observation and Event Memory to use its existing components.
5. Save the test scene. No Editor setup menu is needed for this milestone.

Manual checks (left to the user; not executed during implementation):
1. Select EnvironmentDirector > Director Prediction Preview. Open its component menu and
   choose Preview Predictions From Manual Inputs. This can run outside Play mode.
2. With default settings and inputs vulnerability .2, event pressure .4, damage recovery 0,
   quiet 20, global cooldown 0 and an empty manual cooldown list, Starting State pressure
   should be .50. Rest (5 seconds, .02 decay/second) should predict event pressure .30,
   total pressure .40, quiet 25 and elapsed 5.
3. Inspect any enabled event definition's Duration and Predicted Tension Increase. Using
   those exact values, calculate max(0, min(1, .4 + increase) - .02 * duration), then add
   .10 vulnerability pressure and clamp to 0..1. Compare its prediction row. Quiet should
   equal Duration, global cooldown should be 12 and that event's cooldown should equal
   the definition's full Cooldown, assuming the unchanged default settings.
4. Set Manual Damage Recovery Remaining to 5 and preview. Starting pressure should be
   .625; Rest should end at .40 as the 5 seconds of damage recovery expire. Event rows
   should be blocked until recovery is zero. Restore the input to 0.
5. Set Manual Global Cooldown to 8: events must be blocked and Rest should predict 3
   seconds remaining. Restore to 0. Add one Manual Cooldowns entry with an actual Event ID
   and remaining 20: its event must be blocked, and Rest should reduce it to 15. Clear the
   manual list afterward. These inputs do not alter real cooldowns.
6. Use event pressure 1 and a long Rest/large decay in a DUPLICATE settings asset if you
   want boundary checks; pressure must remain bounded and vulnerability must not decay.
   Restore the original settings reference afterward.
7. In Play mode, use Preview Predictions From Live State after observation initializes.
   Starting State should reflect current vulnerability, pressure and timers. Missing
   victory/keypad/hunter signals do not block this mathematical preview. They continue
   to block actual eligibility. No light, audio or real event should start.
8. Repeated previews must not add history entries or event pressure, reset quiet time,
   start cooldowns or reserve targets. Natural live time decay still proceeds in Play mode.
9. Disable Observation and refresh the live preview: it should reject missing/stale input.
   Stop Play mode after testing. Preview output refreshes only when you invoke its menu.

Suggested commit: feat(director): predict event outcomes and rest transitions for planning

## Milestone 10 - heuristic scoring

New runtime files: DirectorScoringSettings.cs, DirectorHeuristic.cs and
DirectorScoringPreview.cs. The settings asset is DirectorScoringSettings_Hospital.
The preview component attaches to EnvironmentDirector in EnvironmentDirector_Test.
No Editor helper is added. No preview or gameplay tests are run during installation.

Lower total scores are preferable. With defaults:
  target difference = abs(predicted pressure - desired tension)
  overshoot penalty = max(0, predicted pressure - desired tension) * (1 + 2 * vulnerability)
  event repetition penalty = event recency * .15
  anchor repetition penalty = anchor recency * .10
  total = sum of these four terms

Weights can be adjusted in the settings asset. Scores need not lie within 0..1.
Invalid inputs/settings or overflow produce Valid=false and positive infinity; consumers
must check Valid before comparing scores. This heuristic is for bounded greedy best-first
search, not a claim of an admissible A* heuristic or an optimal plan.

Repetition uses the most recent delivered, non-debug history record matching the event
TYPE or anchor ID. Recency = max(0, 1 - secondsSinceRecordEnded / 90), with 90 configurable.
The two recencies are independent, so a different event on the same anchor can still
receive an anchor penalty. Delivered effects count even if subsequently cancelled or
failed; undelivered attempts and debug records do not. The evidence is limited to retained
Event Memory history (default 20 entries), which is a deliberate bounded-memory policy.
Rest has no repetition penalty. Future search must pass branch history including simulated
deliveries so its own repeated actions are penalized. Evaluate recency at selection time,
not after the proposed effect, so long effects do not dilute their repetition penalty.

The live preview reads a fixed desired-tension sample, captures a planning state and copies
history. It scores only eligible, successfully predicted candidates. Blocked candidates
retain their reason and have invalid scores. Best Candidate Index is informational; it
neither reserves nor plays an event. Exact ties retain candidate order (Rest comes first).
Future search, event execution and automatic decision making are separate milestones.

Manual test steps (for the user):
1. Open EnvironmentDirector_Test and select EnvironmentDirector.
2. Find Director Scoring Preview. Confirm all seven references are assigned: Scoring
   Settings, Director Settings, Tension Settings, Prediction, Desired Tension, Eligibility,
   and Event Memory. The last four are existing components on EnvironmentDirector.
3. Outside Play mode, use the component's three-dot menu > Preview Score From Manual Inputs.
4. With predicted pressure .58, desired .55, vulnerability .2, Is Rest false, and both
   recencies 0, expand Manual Score: Valid=true, Target Difference=.03, Overshoot Penalty=.042,
   repetition penalties=0 and Total approximately .072. Floating-point rounding is normal.
5. Change predicted pressure to .48 and refresh: difference=.07, overshoot=0, total=.07.
   This slightly beats .58 because the default policy penalizes overshooting more strongly.
6. Set predicted pressure=.55: total=0. Set vulnerability=.8 while predicted pressure=.58:
   overshoot=.078 and total=.108. Restore vulnerability=.2.
7. With pressure=.58, target=.55, vulnerability=.2, Event Recency=1 and Anchor Recency=1:
   penalties should be .15 and .10; total=.322. Set Is Rest=true: repetition penalties
   should become zero, giving .072. Restore Is Rest=false and recencies=0 afterward.
8. Recency .5 means a match 45 seconds ago with the default 90-second window; the event
   penalty is .075 and anchor penalty .05. Manual recencies are artificial test inputs;
   they do not write to real history or mean an event actually occurred.
9. Enter Play mode, wait for observations and inference, then choose Preview Live Candidate
   Scores (Play mode, no effects). Expand Candidates. Rest should have a valid score.
   Missing victory/keypad/hunter connections still block actual event candidates, and
   those rows must remain unscored. They must not win because an invalid total is zero.
10. After teammate signals are integrated, refresh in a lit room: eligible candidates
    should have predicted pressure, repetition evidence and a full score breakdown.
11. Disable Director Desired Tension and refresh: status should report unavailable/stale
    desired tension and the candidate list should clear. Re-enable it after checking.
12. Repeated previews must not change event history, start a scare, create a reservation,
    reset quiet time or start cooldowns. Natural gameplay time still advances in Play mode.

No automatic scoring loop or 20-minute pacing claim is introduced here. The next milestone
uses the heuristic to order the bounded search frontier and compare action sequences.

Suggested commit: feat(director): score predicted outcomes with tension and repetition penalties

Manual setup fallback if Editor installation has not completed:
1. In the Director Settings folder, right-click > Create > Haunted Hospital > Environment
   Director > Scoring Settings; name the asset DirectorScoringSettings_Hospital.
2. On EnvironmentDirector in EnvironmentDirector_Test, Add Component > Director Scoring Preview.
3. Assign the new asset to Scoring Settings, and the existing DirectorSettings_Hospital and
   DirectorTensionSettings_Hospital assets to Director Settings and Tension Settings.
4. Drag the EnvironmentDirector GameObject into Prediction, Desired Tension, Eligibility
   and Event Memory so Unity binds its corresponding components. Save the test scene.

## Milestone 11 - bounded Greedy Best-First Search

Added DirectorSearchPlan.cs (actions, steps, results and trace), DirectorGreedyPlanner.cs
(pure bounded search), and DirectorPlannerPreview.cs (manual/live search previews and
BuildLivePlan entry point). No Editor helper, extra settings asset or event executor is
added. DirectorSettings_Hospital supplies Planning Depth (3) and Expansion Budget (64).

Search contract:
- Captures desired tension once per search. Location, visibility and available targets are
  frozen at capture; it does not predict where the player or Hunter will move.
- Rest is always a candidate. Live event targets must pass current safety/spatial checks.
- CollectPlanningCandidates defers ONLY event/global cooldown and damage recovery checks
  to copied-state prediction. It does not bypass missing safety signals, reservations,
  active effects, disabled anchors, visibility, distance or player life-state checks.
  Ordinary Evaluate/CollectCandidates retain their original immediate-execution policy.
- A branch contains its own state and history. Each hypothetical delivered event adds a
  record only to that branch. Sibling branches and real Event Memory are untouched.
- Frontier nodes are expanded in ascending heuristic priority (ties by generation order).
  Priority = endpoint target difference + endpoint overshoot + accumulated repetition
  penalties from the branch. Rest does not erase earlier repetition penalties.
- This is greedy best-first h-ordering, not A* g+h and not an optimality guarantee.
- Full-depth nodes are terminal candidates. Return the lowest-scoring discovered full-depth
  plan; if the budget stops search before any full-depth plan exists, return the best
  discovered partial plan and mark Reached Horizon false. Terminal nodes are not expanded.
- Expanded Nodes counts root/nonterminal successor generation. Generated Nodes includes
  the root and successful scored successors. Maximum Depth Reached never exceeds depth.
- Safety caps: 8192 generated nodes (including root), 256 candidate actions including Rest,
  depth 1..5 and expansion budget at most 4096. Oversized inputs are rejected. The expansion
  trace keeps the first 256 expansions. Defaults are far below these caps.
- The chosen sequence is a recommendation from a snapshot, never execution permission.
  Only its FIRST action should later be considered. Revalidate before execution and replan
  after it finishes; do not blindly run the remaining steps. Executor readiness remains
  a later requirement. A manualOnly plan must never be handed to the runtime executor.

Two preview modes:
- Manual Search: synthetic anchor IDs manual_<eventId>, manual room and explicit numeric
  state. It does not read safety signals, fake observations or enable anything in gameplay.
  Empty Manual Events means Rest only. Disabled/invalid catalogue events stay unusable.
- Live Search: fresh living player snapshot, valid desired tension, current anchors and real
  history. Missing safety signals lead to a Rest-only candidate pool. Live Candidates shows
  the reason for each rejected pair. Manual fields have no effect on live planning.

Setup if not already attached:
1. Open EnvironmentDirector_Test outside Play mode and select EnvironmentDirector.
2. Add Component > Director Planner Preview.
3. Assign DirectorSettings_Hospital, DirectorTensionSettings_Hospital and
   DirectorScoringSettings_Hospital to their corresponding settings fields.
4. Assign existing Director Prediction Preview, Director Desired Tension, Director Event
   Eligibility and Director Event Memory components from the same EnvironmentDirector.
5. In Manual Events, add enabled catalogue event assets to explore (for example light_flicker).
6. Save the test scene. No new asset needs to be downloaded or authored.

Detailed manual tests - left to the user, not run during implementation:

A. Deterministic Rest-only search, outside Play mode
1. Select EnvironmentDirector > Director Planner Preview. Use vulnerability .2, event
   pressure .4, damage recovery 0, quiet 20, global cooldown 0, desired tension .55 and
   empty Manual Cooldowns. Set Manual Events size to 0.
2. With unchanged default settings (depth 3, budget 64, Rest 5 sec, decay .02/sec), choose
   the component menu > Preview Manual Search (synthetic targets, no effects).
3. Expand Plan: Valid and Manual Only should be checked; Reached Horizon checked;
   Recommended First Action Rest; Candidate Count 1; Expanded Nodes 3; Generated Nodes 4;
   Maximum Depth Reached 3; stop reason Frontier exhausted within depth limit.
4. Expand Steps (3 elements). Their estimated pressures should be .40, .30 and .20;
   elapsed times 5, 10, 15; quiet times 25, 30, 35. Vulnerability stays .2. Event pressure
   should be .30, .20, .10. Final Score should be approximately .35 with desired .55.
5. Expand Expansion Trace: Root depth0, Rest depth1, Rest depth2. These rows show the nodes
   that generated successors; the terminal Rest depth3 is in Steps, not the trace.

B. Event branches and predictable future cooldowns
1. Add one enabled event asset from the existing catalogue to Manual Events (e.g. the
   light_flicker definition). Use desired tension 1, event pressure 0, vulnerability 0,
   damage 0 and Manual Global Cooldown 8. Keep depth3 and RestDuration5.
2. Run manual search. Any event in the plan must come AFTER two Rest steps (8 -> 3 -> 0).
   Depending on the event's increase/duration, the chosen plan may remain Rest if it
   cannot improve pressure. To get a useful example, select an enabled event whose
   PredictedTensionIncrease exceeds decayRate * Duration.
3. If selected, the third step is that event. Its final global cooldown must be the full
   MinimumTimeBetweenEvents (12 by default); its own cooldown is the full event Cooldown.
4. Add Manual Cooldowns entry with that exact Event ID and Remaining=100. Refresh: with
   depth3 it cannot occur because the horizon is too short; all three steps should be Rest.
5. Clear the manual cooldown entry. Set global=0 and damage recovery=8: an event must again
   wait through two Rest steps. Restore damage=0, global=0 after checking.
6. Add several enabled event assets. Refresh: Candidate Count increases; each plan step
   displays its action, predicted state, local score and full branch score. Manual anchors
   are deliberately synthetic and have no scene-object reference.

C. Depth, budget and deterministic behaviour
1. DUPLICATE DirectorSettings_Hospital for experiments, temporarily assign the duplicate
   to Director Planner Preview only. Keep its event catalogue intact.
2. Set depth1 and budget64. Refresh: Maximum Depth Reached=1 and chosen Steps has one entry.
3. Set depth3 and budget3 (budget cannot be smaller than depth in current settings). With
   several manual events, Expanded Nodes must be <=3. If no terminal node was reached,
   Reached Horizon is unchecked and the reason identifies a partial plan. This is valid
   bounded behaviour, not a test failure. Generated Nodes can exceed Expanded Nodes.
4. With unchanged manual inputs, refresh twice: the plan and counters should match exactly.
5. Restore the ORIGINAL DirectorSettings_Hospital reference when done. Do not edit shared
   gameplay settings just to manufacture a test result.

D. Repetition within a branch (optional focused manual test)
1. Duplicate Director Settings and ONE enabled event definition. In the duplicate catalogue
   replace that event with its duplicate; use that same duplicate in Manual Events.
2. In these temporary assets only, set event cooldown=0 and minimum global spacing=0 so
   repeated actions are reachable within depth3. Choose a target/initial pressure that
   gives positive predicted benefit from repeated events.
3. If the chosen plan repeats the event, later steps should have Event Repetition Penalty
   .15 and Anchor Repetition Penalty .10 at an immediate repeat with default scoring.
   After a Rest the evidence decays with time. Branch Score retains earlier repetition
   penalties even when the last action is Rest. The search may avoid repeating because
   the penalty makes another sequence better; that is expected.
4. Restore the original settings and event references. Never disable real cooldowns just
   to make the final game trigger more events.

E. Live integration preview, without playing effects
1. Enter Play mode and wait one second for observation/inference. Select the component menu
   > Preview Live Search (Play mode, no effects).
2. Plan.Manual Only must be unchecked. With current unavailable safety signals, first action
   should be Rest, and Live Candidates should explain the blocked event/anchor pairs.
3. Changing the manual inputs must not change a live plan for otherwise identical live
   inputs. Time naturally advances, so compare behaviour rather than expecting live
   numerical values to stay identical across clicks.
4. Disable Director Desired Tension and refresh: the plan becomes invalid with a missing or
   stale desired-tension reason, and recommended action is None. Re-enable afterward.
5. Move rooms and refresh. Once safety integration exists, targets are rebuilt for the
   new location; an old recommended anchor is never authorization to execute.
6. Previewing must not play audio, alter lights, add real history, reset quiet time, start
   a real cooldown, or reserve a target. Stop Play mode after testing.

Suggested commit: feat(director): plan event sequences with bounded greedy best-first search

## Commit 12 — recurring decisions and replaceable Hunter input

DirectorDecisionLoop calls the existing live planner once per second. It exposes a current recommendation and full plan in the Inspector. It does not execute effects, reserve anchors, or write event memory. Strict eligibility is checked again for the first action, because planning can simulate cooldowns expiring in future steps. A recommendation is not permission to execute later without revalidation.

DirectorHunterSignalSource is the contract for a future real Hunter adapter: TryRead returns Active/Inactive/Unavailable with a sample timestamp. DirectorObservation accepts only an enabled same-scene source with a finite, non-future timestamp no older than Maximum Hunter Signal Age (default 0.5 seconds). Missing, disabled and stale sources remain Unavailable.

DirectorManualHunterSource is an explicit simulation source. Enable Simulation must be checked; Signal Available and Danger Active control its current state. Freeze Timestamp deliberately lets a previously sampled signal become stale. Replace the source reference with the real adapter later; no planner changes are required.

The current branch still leaves recent gunfire, keypad interaction and victory unavailable. The Hunter mock does not override them. Rest-only decisions are therefore expected until those adapters are connected. Do not interpret Rest as confirmation that all safety signals are available.

### Manual checks for the user (not run during implementation)

1. Open EnvironmentDirector_Test outside Play mode. Select EnvironmentDirector.
2. Confirm Director Decision Loop references the local Planner Preview, Observation, Event Eligibility and Event Memory. Interval is 1 second.
3. Confirm Observation's Hunter Source points to Director Manual Hunter Source.
4. Enter Play mode and stand inside a registered room. Watch Planning Attempts increase approximately once per second and Latest Plan populate. No effect should be executed.
5. On Manual Hunter Source, check Enable Simulation and Signal Available, and leave Danger Active unchecked. Observation's Hunter Danger should become Inactive.
6. Check Danger Active. Hunter Danger should become Active after the next observation refresh. Existing immediate-danger restrictions should appear in eligibility; other unavailable signals may be the first reported rejection.
7. Uncheck Signal Available, or disable the component. Hunter Danger must become Unavailable.
8. Restore availability and unfreeze the timestamp for at least one sample. Check Freeze Timestamp, then wait longer than 0.5 seconds. Hunter Danger must become Unavailable. Unfreeze it to resume fresh samples.
9. Walk through rooms. Decisions should use the current room; event-memory quiet time and cooldowns must not reset on room entry.
10. Disable the Decision Loop. Its recommendation and latest plan clear and planning attempts stop increasing. Re-enable it to resume.
11. Disable Observation or Desired Tension. The loop should report unavailable observations or an invalid/stale plan rather than retaining an executable recommendation.
12. Leave Play mode. Runtime changes to simulation toggles are not saved; deliberately configure the test scene outside Play mode if you want different starting values.

No test was run automatically. The manual source belongs in the sandbox until replaced with the real Hunter adapter. Full automatic scare execution is a later milestone.

## Commit 13 — player signals and local flicker execution

New DirectorPlayerSignals subscribes to PlayerWeapon.Fired, reads ObjectiveState readiness/keypad/escape/win/pause, and supplies the observer. The first six seconds after binding conservatively count as gunfire recovery (the configured GunfireRecoverySeconds), because earlier shots cannot be inferred. Missing/disabled weapon or objectives remain Unavailable. Escape-in-progress and pause suppress scares through the ending signal.

DirectorFlickerExecutor is the first supported executor. DecisionLoop filters automatic planning to supported effects; manual previews can still inspect the full catalogue. A live plan with an executor now scores target error/overshoot across all predicted steps, retaining repetition penalties, so repeated planning does not indefinitely defer a beneficial event to its last step. Manual commit-11 calculations retain their previous endpoint scoring.

The executor reserves and revalidates the target, captures enabled Director light intensities, starts a 3D electrical clip, flickers for the event's duration, and restores intensities before recording completion and releasing the target. Essential lights are excluded. No gameplay noise event is emitted by the sound. Turning off the executor, losing required signals, entering keypad interaction, shooting, damage, death, an ending, pause or leaving the room cancels the effect and restores lights. The clip uses the first affected light's position. Fixture materials are not animated in this milestone.

Sound: Assets/Audio/World/Backgorund_Sounds/18_Fluorescent_Starter_Failure.wav, volume 0.35, 3D, 2m minimum / 18m maximum, no loop. Existing event defaults: duration 3 seconds, type cooldown 45 seconds after completion, global gap 12 seconds. There is no guaranteed event count: safety, pressure and availability still govern decisions.

### User checks (not run by the assistant)
1. Open EnvironmentDirector_Test. Confirm one Objectives object has ObjectiveState; Player Signals references PlayerWeapon and that ObjectiveState.
2. Select EnvironmentDirector. Verify the Decision Loop has its Flicker Executor reference, and the executor has both light roots, memory, reservations, observation, eligibility, Audio Source and sound clip.
3. Enter Play mode. Hunter mock should be explicitly enabled, available, danger off, timestamp unfrozen.
4. With a living healthy player, walk into a room with an enabled Director light group. Stop shooting and close the keypad. After the initial recovery/global interval, watch Decision Loop and Flicker Executor status. A selected flicker should last about 3 seconds with spatial electrical sound, then return to the original intensity.
5. Check Event Memory records one delivered/completed event, and Target Reservations clears. Expect at least the 45-second flicker cooldown before another flicker; other eligibility conditions can delay it further.
6. Fire a real shot, open/close the keypad, and toggle mock Hunter danger. Observation should reflect the real player signals and simulated Hunter signal. Flickers should be suppressed or cancelled as appropriate.
7. During flickering, leave the room or disable Flicker Executor: lights must restore, audio stop and reservation clear. Re-enable afterward.
8. Verify Essential lights keep their original brightness throughout.
9. Inspect the Objectives component's debug menu to test Begin Escape/Win if a complete objective run is inconvenient. These states must suppress new scares.
10. If nothing happens, use Event Eligibility's existing preview menu and inspect rejected reasons. Check current room, observation age, player signal readiness, light availability and cooldowns. Do not force missing signals to safe.
11. No Play-mode tests or effect previews were run during implementation. MainHospital is not updated by this setup.

## Commit 14 — local blackout execution

DirectorBlackoutExecutor adds local blackouts to the live decision loop alongside flicker and Rest. The planner considers only effects with configured, enabled executors. Both light effects use the same event memory and target reservation: only one Director event can run at a time. Manual search previews still execute nothing.

The existing ED_LocalBlackout definition remains at duration 4 seconds, cooldown 100 seconds and predicted pressure increase 0.22. Duration includes a 0.35-second smooth fade out, 2.9 seconds fully off and a 0.75-second smooth recovery. Only the selected anchor's enabled Director lights are affected. Original intensities are captured each time and restored on completion or cancellation. Essential lights and fixture materials are unchanged. Baked light contributions cannot be removed by changing realtime light intensity.

BlackoutAudio is a separate 3D AudioSource under EnvironmentDirector using 19_Electrical_Relay_Clicks.wav, volume 0.35, min distance 2m, max distance 18m, linear falloff, no loop or play on awake. Playback begins at the first affected light and stops when the effect ends. Audio does not emit a gameplay gunfire/noise event.

The blackout cancels and restores lights immediately when the player changes room, shoots, takes damage, dies, opens the keypad, starts escaping, pauses, or receives Hunter danger/unavailable signals. Missing or stale observations, invalid targets, disabled audio and lost reservations also cancel. The temporary Hunter source remains replaceable by a real adapter.

### Manual testing — to be performed by the user

1. Open EnvironmentDirector_Test outside Play mode and select EnvironmentDirector.
2. Confirm Director Decision Loop > Blackout Executor references the local Director Blackout Executor. Confirm the blackout component has all six shared references plus Sound and Blackout Sound. Expand the root to find BlackoutAudio.
3. Enter Play mode. Keep Manual Hunter Source enabled/available, Danger Active off and Freeze Timestamp off. Keep the player alive, close the keypad and stop shooting.
4. Enter a room with an enabled Director light group and wait past the initial 12-second global interval. Inspect the decision loop and blackout Status. Rest or flicker may be selected first; a blackout is not guaranteed on a fixed timer.
5. When selected, Status should progress through Fading out, Blackout and Restoring. The whole effect lasts about four seconds. Essential lights must remain constant; Director lights must return to their individual original intensities. Expect relay sound from the room.
6. Check Event Memory records local_blackout as delivered/completed and Target Reservations clears afterward. Blackout cannot repeat until its 100-second cooldown expires; global spacing and other restrictions can delay it further.
7. To focus on blackout during this Play session only, disable the Director Flicker Executor component. Do not disable its GameObject, which would disable the whole Director. The planner will choose between blackout and Rest; it can still rest. Re-enable flicker afterward. Avoid changing shared event assets to force a result.
8. During a blackout, leave the room or shoot. Lights should restore immediately, audio stop, reservation clear and memory record cancellation. You can repeat using keypad entry, mock Hunter Danger Active or disabling only Director Blackout Executor.
9. After restoring safe signals, both effects should become eligible again subject to cooldowns. They must never run concurrently.
10. If the room remains bright with Director intensities at zero, inspect Essential illumination and baked lighting separately. Do not remove the Essential layer to force darkness.
11. If no event starts, inspect Event Eligibility rejection reasons, observation signal readiness, current room, cooldowns and planner status. This implementation does not guarantee a particular scare frequency.
12. Stop Play mode to discard temporary component toggles. No gameplay testing was run during implementation.

Suggested commit: feat(director): add local blackouts with relay audio and safe light restoration

## Commit 15 — phone ringing followed by rotating voice lines

DirectorPhoneExecutor is now a supported automatic effect alongside flicker and blackout. It reserves an eligible phone anchor, plays the full Phone ringing sound, waits 0.2 seconds, and plays one complete voice line through a second spatial AudioSource at the same phone. Two scheduled sources prevent ring/voice overlap. Pitch is 1 and Doppler is disabled. The event stays active until the selected clip ends; other Director events cannot overlap it.

The three voices use a shuffled cycle: every line completes once before the next cycle begins, with no immediate repeat at a cycle boundary. The cycle is shared across all phones and resets when a new Play session begins. An interrupted line is retained for the next safe phone event. Current Voice, Current Duration, Completed In Cycle, Completed Cycles and Status appear on the executor.

Imported audio from the external Projects/Sounds folder into Assets/Audio/World:
- Backgorund_Sounds/Phone ringing sound.mp3: 20.193 seconds.
- Voices/Phone_voice_01.mp3: 10.92 seconds; total event about 31.46 seconds.
- Voices/Phone_voice_02.wav: 5 seconds; total event about 25.54 seconds.
- Voices/Phone_voice_03.mp3: 7.68 seconds; total event about 28.22 seconds.
- Backgorund_Sounds/Blackout sound.mp3: 3.48 seconds, replacing the previous blackout relay clip without changing four-second blackout timing.

Totals include a 0.15-second audio scheduling lead and 0.2-second ring-to-voice gap. ED_PhoneRing's fallback duration covers the longest sequence. Live planning uses the selected line's actual duration via DirectorPlanningAction.durationOverride; future hypothetical phone actions use that same snapshot duration, and the planner captures a fresh duration on every real decision. Phone cooldown remains 60 seconds after the event finishes.

PhoneRingAudio and PhoneVoiceAudio are children of EnvironmentDirector in EnvironmentDirector_Test. Both use spatial audio (2m minimum, 18m maximum, linear falloff), no looping and no Play On Awake. The executor sets volumes to 0.4 for ringing and 0.7 for voices. Walking across a room boundary does not cut playback, but walking away makes it quieter. Required safety interruptions (shooting, hunter danger, damage, keypad use, death, victory, pause, missing/stale signals, disabled target or executor) still cancel playback. This audio does not publish gameplay noise to the Hunter. No telephone pickup interaction or handset animation is added.

### Manual checks for the user (not run by the assistant)
1. Open EnvironmentDirector_Test, let Unity finish compilation, and select EnvironmentDirector.
2. In Director Phone Executor, check Ring Clip and the three Voice Clips are assigned. Confirm its Ring Source and Voice Source reference the two separate child objects, and Decision Loop's Phone Executor reference is filled.
3. Enter Play mode. Keep the Hunter mock available with danger inactive, the player alive, and avoid shooting or opening the keypad.
4. For a focused phone check, disable ONLY the Director Flicker Executor and Director Blackout Executor components during Play mode. Leave Decision Loop, observation, eligibility, memory and Phone Executor enabled. Stop Play afterward to discard these temporary toggles.
5. Stand in a room containing a registered phone, between 2 and 12 metres from it. Phone eligibility still depends on cooldowns, safety signals and the planner; there is no fixed guarantee of immediate playback. Use the existing Event Eligibility preview if needed to see rejection reasons.
6. When phone_ring starts, watch Phone Executor Status. Expect about 20.2 seconds of ringing, a short gap, then the selected complete voice line. Current Duration should match approximately 25.54, 28.22 or 31.46 seconds. No other Director event should start during this sequence.
7. Watch Current Voice and let three phone events complete. All three lines should be different. Completed In Cycle goes 1, 2, then 0, and Completed Cycles becomes 1. The fourth begins a new shuffled cycle. Allow at least the 60-second phone cooldown between completions and later starts; other rules can delay it further.
8. Cross the room boundary during a voice: it should continue at the original phone, fading with distance. Stay near the phone when checking the entire sentence is audible.
9. During a separate phone event, shoot or enable mock Hunter danger. Both sounds should stop, Event Memory should record cancellation, and the reservation should clear. After safety and cooldowns allow another phone event, the unfinished voice should be retried.
10. Restore the light executor components, then check a blackout uses Blackout sound rather than the old relay clip, keeps Essential lights unchanged and restores Director lights afterward.
11. Stop Play mode. MainHospital has not been updated by this setup. No automated tests or Play-mode tests were run.

Suggested commit: feat(director): add sequenced phone voices and update blackout audio

## Pacing revision after commit 15

Supersedes the earlier local-blackout and three-second flicker descriptions:
- Flicker lasts 10 seconds in its selected room. Electrical audio loops at original pitch and fades during the final 0.4 seconds, then stops. Walking out does not cancel it; safety interruptions still do.
- Blackout lasts 30 seconds across all currently illuminated Director lights beneath the Director light root. Essential lights are excluded. It captures and restores each light's own intensity. Room changes no longer cancel it. Existing local_blackout ID/type are retained for serialized compatibility; the initiating local anchor still supplies eligibility/reservation context.
- Blackout audio uses a serialized start offset measured from the first meaningful signal in the clip, with a short pre-roll. The original sound file remains intact; audio is preloaded and execution waits for loaded audio. The sound plays once at the start, not on a 30-second loop.
- Live pressure is held while a delivered event is active, then decays afterward. Quiet time stays zero during a delivered effect. Prediction follows the same sustained-pressure rule. Phone predicted pressure increases to 0.18; its full voice-cycle timing is unchanged.
- Repetition recency accumulates across delivered records (capped at 1). Window 180 seconds, event penalty weight 0.22, anchor weight 0.10. Repetition is penalized, not forbidden.
- Live best-first search groups complete plans by their first event type and retains the best plan per type. It randomly selects among plans within 0.16 average score per search step of the best, weighted toward lower scores. Rest remains a candidate; missing targets, cooldowns and safety still apply. This prevents extra light anchors becoming extra lottery tickets. Manual previews remain deterministic. This is not an event rotation, and no fixed event frequency is guaranteed.
- Global spacing remains 12 seconds after completion, with cooldowns of 45 seconds for flicker, 100 for blackout and 60 for phone. Strong repetition scoring can still lead to Rest. Automatic planner Live Candidates now refreshes with planning eligibility/rejection reasons; a planning-eligible event can still have a live cooldown.

### User checks for this revision (not run by the assistant)
1. In EnvironmentDirector_Test, enter Play mode with valid player signals and the Hunter mock safe. Keep all three executors enabled.
2. When a flicker starts, time approximately 10 seconds; confirm audio covers the effect and stops with restoration. Walking away should not terminate the original room's effect.
3. When blackout starts, walk between rooms: Director lighting should stay off throughout the hospital for about 30 seconds including fades, while Essential lighting remains. Confirm each room returns to its previous brightness.
4. Check blackout audio starts with the fade-out rather than seconds later. The measured Blackout Sound Start Seconds field is adjustable if the desired dramatic transient is later than the detected sound onset.
5. Stay within 2–12 metres of a registered phone for some decisions. Confirm complete ring then voice playback. Check several sessions for variety; identical order remains possible by chance and short sessions cannot prove a distribution.
6. Inspect Planner Preview's Live Candidates if phone is absent. Distinguish unavailable targets/safety from cooldowns and score selection. No eligible phone means it cannot be selected.
7. Repeat an event and inspect the search score's repetition penalty. It should remember repeats beyond the 100-second blackout cooldown. Consecutive repeats are possible but disadvantaged.
8. During a light effect, shoot or activate mock Hunter danger: audio stops, lights restore, reservation clears. Death, victory, missing signals, pause or disabling the executor also stop effects.
9. No gameplay or automated tests were run. Shared event/scoring settings affect other scenes that reference these assets; scene wiring changes are confined to EnvironmentDirector_Test.

## Commit 16 — spatial whispers with four-voice rotation

DirectorWhisperExecutor adds the whisper action to automatic live planning. It plays the selected clip at an existing eligible whisper anchor with spatial falloff, original pitch, and no looping. It uses Wisper_01.mp3, Wisper_02.mp3, wisper_03.mp3 and wisper_04.wav from Assets/Audio/World/Voices.

Each completed whisper consumes one entry from a shuffled four-clip cycle shared across all anchors. All four finish once before a new shuffled cycle begins; the first clip of a new cycle cannot immediately repeat the last clip of the previous cycle. A safety interruption retains the unfinished clip for the next eligible whisper. The cycle resets on a new Play session.

Live prediction and the reservation use the selected clip's complete length plus a 0.15-second scheduling lead. The event asset stores the longest duration as a fallback. Existing defaults remain: 55-second whisper cooldown, 2–8m eligibility range, predicted pressure increase 0.14, plus global spacing and safety restrictions. The planner still chooses between supported events and Rest using tension, repetition and weighted near-best selection; whispers are not forced into an event order.

WhisperAudio is a child of EnvironmentDirector in EnvironmentDirector_Test. It uses 3D audio, 2m minimum / 14m maximum linear falloff and volume 0.7. Crossing rooms does not truncate the clip; sound stays at its anchor and becomes quieter with distance. Shooting, danger, damage recovery, keypad interaction, death, victory, pause, missing signals or disabling the executor cancels it and clears the reservation. No Hunter gameplay-noise event is emitted.

### User testing steps (not run by the assistant)
1. Open EnvironmentDirector_Test outside Play mode and allow compilation to finish. Select EnvironmentDirector. Confirm Director Whisper Executor has all four Voice Clips, the WhisperAudio source and its four system references. Confirm Decision Loop references Whisper Executor.
2. Enter Play mode with a living player and valid player signals; keep the Hunter mock available and danger off. Avoid shooting or keypad use.
3. Walk near an existing whisper anchor (2–8m). Watch the Decision Loop and Whisper Executor Status. The planner may select another event or Rest; selection is not immediate or guaranteed.
4. To focus on whispers for this session, disable only Flicker Executor, Blackout Executor and Phone Executor components during Play mode. Keep Whisper Executor, the Decision Loop and other Director components enabled.
5. When whisper starts, Current Voice shows the chosen filename and Current Duration shows its full length plus 0.15 seconds. Listen for complete playback and check Event Memory records whisper completion and clears its active event/reservation.
6. Allow four events to finish, noting Current Voice. Every clip should occur once. Completed In Cycle runs 1,2,3,0 and Completed Cycles reaches 1. A fifth begins a fresh shuffled cycle. Allow at least the 55-second whisper cooldown after each event, plus any planner delay.
7. Walk across a room boundary while staying close enough to hear: playback continues at its original anchor. Shoot during a separate event: playback should cancel, and the unfinished clip should be retained for a later safe whisper.
8. If no whisper occurs, inspect Planner Preview Live Candidates or the Event Eligibility preview. Check distance, compatible/active anchors, observation freshness, safety signals and cooldowns.
9. Stop Play mode to discard temporary executor toggles. No Play-mode or automated tests were run. MainHospital scene wiring is unchanged; shared whisper event settings also apply wherever that asset is referenced.

Suggested commit: feat(director): add spatial whispers with shuffled four-voice cycles
