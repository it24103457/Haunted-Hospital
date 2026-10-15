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