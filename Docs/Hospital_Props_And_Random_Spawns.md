# Hospital props, furniture and random spawn placement
For Unity 6000.6.2f1. Prepared 3 October 2026.

This is a manual placement guide. No scene, prefab, package or gameplay code was changed. The saved `Assets/Scenes/Main/MainHospital.unity` was read to check the layout. Its 40 floor transforms match the coordinate schedule. Unsaved Editor changes are outside this check.

**Use the existing traced layout, not the metre dimensions printed inside the blueprint.** This guide keeps your floors, walls, corridors, doors and four hiding pockets.

## Your rules — this revision supersedes the earlier animation instructions

- **Your role:** import, size and place world assets; apply materials/colliders; add empty spawn markers; preview/apply animation clips already supplied with the asset.
- **Not your role:** interaction triggers, runtime animation control, random spawning, keypad/code/key behavior or gameplay scripts.
- **No Blender, no conversion/export workflow, no animation creation from scratch.** A rig or separate lid is not enough: the download must contain usable opening motion.
- Prefer Unity packages/prefabs, then animated FBX. A Sketchfab animation badge alone does not prove the original download is directly usable in Unity.
- The **Ulf cabinet, Poly Haven tool chest and RayznGames case are removed** from the animated-prop selections.
- Replacement status: the Unity chest is publisher-confirmed to include opening motion. The drawer cabinet is a candidate pending its downloadable format/Unity preview. A matching free preanimated wall first-aid box has **not been verified**, so K01–K04 are reserved locations, not a completed asset selection.

The static furniture and all S/C/K location IDs remain usable. The new animated assets have not been imported or tested in your Unity project. Do not interpret the coordinate check as verification of their animation files.

## 1. What you are building

The furniture stays in fixed places. The game randomly chooses which furniture contains an item at the start of a run.

| Group | Furniture/marker locations | Active contents |
|---|---:|---|
| Health, ammunition, sanity | 15 | 3 health + 3 ammo + 3 sanity = 9 initially; never more than 3 of each |
| Digit clues | 10 | 4 papers, one for each position in the elevator code |
| First-aid key boxes | 4 | Empty initially; exactly 1 key after the correct code |
| Additional room furniture | 17 | Decoration only |

There are **46 planned furniture slots** (including unresolved animated slots) in the placement schedule. Add the elevator door assembly and keypad separately. The three chest stations each contain a reused desk and a small archive chest. This gives you a sparse playable pass rather than an attempt to fill every square metre.

The supply locations use exactly **four types**: desk, bedside table, shelf and hospital bed. The digit locations use exactly **three types**: opening drawer cabinet, opening archive chest on a desk, and a static desk. Ordinary bedside tables do not need to open.

**Barricades remain part of the difficulty.** Five supply candidates, four digit candidates and two key boxes are behind planned barricades. The spawn system must allow these locations before the boards are broken.

### Important limit on “exact sizes”

The positions and target sizes below are a checked placement design. They are **not claims about the original sizes, pivots or shelf heights of downloaded models**. The assets have not been imported or tested in your Unity project.

Prepare each reusable prefab once: face it correctly, fit it to the target size and put its origin at the specified point. Then every instance uses the listed Position/Rotation and Scale `1,1,1`. Spawn heights on mattresses, shelves and inside drawers need one calibration against the actual mesh. These are identified explicitly below; guessing an imported model's Scale would be misleading.

![Placement map](Hospital_Prop_Placement_Map.png)

Blue S = supply furniture; brown C = clue furniture; pink K = key boxes; grey P = decoration. An asterisk means a planned barricade blocks access. The map is a placement reference, not a NavMesh result.

## 2. Free asset shortlist

Download only the chosen models, not whole demonstration scenes. Free availability and the stated features were checked against the creator/store pages. A downloadable model does not automatically include a Unity interaction script.

| Your prefab | Free source | What to expect |
|---|---|---|
| PF_Bed | [Hospital Bed — CG Cobble Games](https://marketplace.unity.com/packages/3d/props/interior/hospital-bed-free-3d-asset-190310) | Listed FREE under the Unity Asset Store EULA. A reusable medical bed; its styling is more modern than a rusty asylum bed. Use subdued materials. Unity 6 compatibility has not been play-tested. |
| PF_Desk | [Metal Office Desk — Poly Haven](https://polyhaven.com/a/metal_office_desk) | Use as a static desk/work surface in offices and utility/clinical areas. Prefer this metal furniture over a rustic dining table. |
| PF_Nightstand | [Painted Wooden Nightstand — Poly Haven](https://polyhaven.com/a/painted_wooden_nightstand) | Small worn bedside table. Use the top only; no opening animation is assumed. |
| PF_Shelf | [Steel Frame Shelves 01 — Poly Haven](https://polyhaven.com/a/steel_frame_shelves_01) | Open industrial shelving; supports remain visible. Use one reachable shelf for supplies. |
| PF_DrawerCabinet | [500 Lower Cabinet 3 Drawers — Symeon3D](https://sketchfab.com/3d-models/500-lower-cabinet-3-drawers-395760c7278b47fb972ea3e4cbec2451) | Free download, CC Attribution. Creator explicitly supplies a drawer animation. **Candidate only:** the public listing does not establish the original download format. Accept only an animated FBX that previews in Unity; if it requires Blender/conversion, reject it. No new animation work. |
| PF_ChestStation | [Animated Old Chest — NOT_Lonely](https://assetstore.unity.com/packages/3d/props/animated-old-chest-20179) plus PF_Desk | FREE Unity asset; an included opening animation is confirmed by the [creator](https://not-lonely.com/assets/animated-old-chest-free/). Use as an old archive/storage chest on the desk. More rustic than a medical cabinet; retain subdued materials. Teammate handles closing playback from the supplied motion. |
| PF_Toilet | [Toilet — HippoStance](https://sketchfab.com/3d-models/toilet-132a8ee2af3a40d39d270fbed3d3666c) | Free downloadable model, CC Attribution. Use as static bathroom furniture. |
| PF_Sink | [Simple Sink — Andrew.Mischenko](https://sketchfab.com/3d-models/simple-sink-73bda35b6e7a499a80427fc1b049b192) | Free downloadable model, CC Attribution. Use the basin as a wall-mounted sink. |
| PF_Stool | [Folding Wooden Stool — Poly Haven](https://polyhaven.com/a/folding_wooden_stool) | Two static seats; no folding gameplay needed. |
| PF_Bin | [Metal Trash Can — Poly Haven](https://polyhaven.com/a/metal_trash_can) | Repeat two bins in Waste Disposal. Use one can/body and its lid, not an entire multi-object preview arrangement. |
| PF_KeyBox | **No approved replacement yet** | The previous case only offered animatable parts. A free wall-mounted first-aid box with verified included opening motion and direct Unity import has not been found. Preserve K01–K04 as empty location reservations. Do not download a rig-only substitute or create its animation. |
| PF_ElevatorDoors | [Elevator — Low Poly Animated, JeffK](https://sketchfab.com/3d-models/elevator-low-poly-animated-3a9cc99aeb284a4080c03374277231ae) | Free, CC Attribution; creator lists `01_idle` and `02_open`. Includes an elevator box, not just guaranteed separate door leaves. Low-poly, baked-lighting appearance is a visual compromise. Inspect the hierarchy and doorway before using it. |
| PF_Keypad | [Basic Door KeyPad/CodeLock — AMMediaGames](https://sketchfab.com/3d-models/basic-door-keypadcodelock-free-3ea0ea6305ad47cbb744de5a6eab2b5e) | Free, CC Attribution. A visual keypad, not a working code-entry system. |
| PF_ExitKey | [Key 9 — plaggy on Fab](https://www.fab.com/listings/f381f02e-1ea9-42b8-bcf8-25ef69a5fa58) | Free; creator describes CC0 and supplies FBX/PBR formats. Use a single key. |
| PF_DigitPaper | The small static Unity paper in section 10 | No dependency on the removed cabinet. You prepare the paper and sample text; a teammate assigns random digits at runtime. |

Poly Haven assets are offered under [CC0](https://polyhaven.com/license). Keep creator names and source URLs in your project's credits anyway. For CC Attribution models, include the author, model link, applicable license link and a note of modifications such as resizing/material changes. Keep Unity Asset Store assets within the permitted team/project use; do not publish their source files as a free asset pack.

Optional alternative for a more consistent hospital art set: [Madduck's Free Modular 3D Hospital Environment](https://madduck.itch.io/modular-3d-hospital-environment). It contains 25 assets with a separate FBX download and texture archive. The author permits commercial/noncommercial game use and private team sharing. Individual prop contents and opening parts were not verified here, so it is **an alternative to inspect**, not the source of the placement measurements below. Do not import its building layout over yours.

No health/ammo/sanity briefcase assets are included in this shopping list, as requested.

## 3. Download and prepare one copy of each type

1. Stop Play mode and save your current scene.
2. In the Project window, make these folders if they do not already exist:
   - `Assets/Art/Models/HospitalProps`
   - `Assets/Art/Materials/HospitalProps`
   - `Assets/Prefabs/Hospital/Props`
   - `Assets/Prefabs/Hospital/Gameplay`
   - `Assets/Animations/HospitalProps`
3. For the static Poly Haven props, choose **FBX** and **1K or 2K textures**, not 8K. Extract the downloaded ZIP before dragging the files into Unity.
4. For the Unity bed, add it to your Unity account and import it from Package Manager's My Assets view. Import model/material dependencies; demo scenes are not required.
5. For NOT_Lonely’s chest, add the free asset to My Assets and import its prefab, model, materials and existing animation dependencies. Preview its supplied opening clip.
6. For Sketchfab, sign in if required. Use an original animated FBX when supplied. If only Blender/OBJ is available, reject it for an opening prop. A GLB-only file is outside this simple-import workflow unless the project already imports it with animation correctly; do not add a conversion task for yourself.
7. Put each asset in its own source subfolder. Keep the supplied attribution/license information.
8. Make URP/Lit material copies if imported materials appear pink. Assign Albedo/Base Color to Base Map, and import normal textures as **Normal Map** before assigning them. Do not place a roughness map directly into a smoothness slot or use an ORM map as a ready-made Unity metallic map.
9. Preserve the existing worn texture detail. Use muted off-white/grey metal, faded blue-grey upholstery and dull wood. Start with Base Color white so it does not multiply the supplied texture into darkness. Reduce excessive shine; painted surfaces usually need low metallic response. Avoid bright colour changes per room.
10. Prepare the first copy outside the playable hospital, then make a prefab. Do not repeat sizing work 46 times.

### Accept only existing animation clips

1. Import the chosen Unity package or animated FBX.
2. Select the imported model in the Project window. In its Animation settings, enable **Import Animation** if applicable.
3. Inspect the existing clip list and use the Preview play button. Do not press Create to author a new clip.
4. Confirm that the relevant drawer/lid moves, not just a turntable camera or the entire object.
5. Keep the model hierarchy and any supplied Animator/controller intact. Place the imported prefab beneath your placement wrapper.
6. If a controller is not supplied, hand the imported clips/model to your teammate for runtime playback wiring. You can still inspect the clips in Preview.
7. Record which clip opens which part, then continue with world placement. Do not invent keyframes, bake an animation or export through Blender.

**Drawer candidate:** the creator documents a drawer animation, but direct animated-FBX availability still needs confirmation from the download options. It is not being represented as a tested drag-and-drop Unity asset. Do not commit to four copies until the first imported copy passes this preview.

Another cabinet to inspect is [Laboratory Cabinet Storage — Naked Singularity Studio](https://www.fab.com/listings/d6284011-bf69-4af2-b2ba-21c93d037910?lang=en). Its listing confirms a free FBX download and included animations, but calls those animations mainly for preview. That makes it a **conditional alternative**, not a verified gameplay-opening replacement. Accept it only if its supplied clip opens the required compartment; do not create or repair motion. Its different shape would also need a new size/clearance check before using the drawer-cabinet placement slots.

## 4. Standard sizes and prefab origins

**Width = local X; height = local Y; depth = local Z.** Every floor-standing prefab has its origin at the bottom centre, with its front toward **local +Z**.

A rotation of Y = 0 faces north/+Z; 90 faces east/+X; 180 faces south/-Z; 270 faces west/-X.

| Prefab/type | Target width X | Target height Y | Target depth Z | Front/origin |
|---|---:|---:|---:|---|
| PF_Bed | 1.05 | 1.05 | 2.2 | Foot of bed; bottom-centre origin |
| PF_Desk | 1.4 | 0.76 | 0.7 | Seated/user side; bottom-centre origin |
| PF_Nightstand | 0.55 | 0.65 | 0.5 | Drawer side (kept static); bottom-centre origin |
| PF_Shelf | 1.2 | 1.8 | 0.45 | Open shelf side; bottom-centre origin |
| PF_DrawerCabinet | 0.5 | 0.9 | 0.6 | Animated drawer side; bottom-centre origin; candidate envelope |
| PF_ChestStation | 1.4 | 1.16 | 0.7 | Archive chest latch side; chest on desk; bottom-centre origin |
| PF_Toilet | 0.45 | 0.8 | 0.75 | Bowl/front; bottom-centre origin |
| PF_Sink | 0.6 | 0.4 | 0.5 | Basin user side; bottom-centre origin |
| PF_Stool | 0.45 | 0.45 | 0.45 | Arbitrary; bottom-centre origin |
| PF_Bin | 0.55 | 0.8 | 0.55 | Arbitrary; bottom-centre origin |
| PF_KeyBox | 0.55 | 0.45 | 0.18 | Opening face; CENTER origin; centre origin |

The chest station is a 1.40 × 0.76 × 0.70 desk with a **0.65 × 0.40 × 0.40** closed archive chest on it. Put the chest's bottom-centre root at local `(0,0.76,0)`. Its combined height is 1.16 m. Reuse PF_Desk as a child; do not download another stand.

Sink roots sit at Y = 0.65, so the fitted 0.40 m basin assembly ends at Y = 1.05. The sink origin is the bottom centre of the basin assembly, not a floor-level pedestal.

Key boxes use a **centre pivot**, because they mount on walls. Their centre height is 1.45 m, with bottom 1.225 m and top 1.675 m.

### Size the actual model, then save the prefab

1. In a temporary preparation scene, create an Empty named `PF_Bed` (or the appropriate type).
2. Reset its Transform: Position 0/0/0, Rotation 0/0/0, Scale 1/1/1.
3. Create an Empty child called `Visual`; reset it.
4. Drag the model under Visual. Correct the **model child's** rotation until it is upright and its front points +Z. For beds, +Z means the foot of the bed.
5. Fit the visible model to the target dimensions. Do not put these dimensions straight into the imported model's Scale fields: those are multipliers.
6. The exact relationship is `new visual scale = current visual scale × target dimension ÷ measured dimension`, separately for X/Y/Z after orientation.
7. Move Visual so the visible object's bottom is Y = 0 and its footprint centre is X/Z = 0. For KeyBox use the visible closed body's centre at 0/0/0.
8. The sizing helper in `HospitalPropSizer.cs.txt` is optional tooling for a teammate to install, not a coding task assigned to you. You can size visually against a temporary cube with the target dimensions instead. Remove the measuring cube afterward.
9. Check that the dimensions do not make the model look distorted. These target envelopes reserve the layout space; a smaller, proportionally scaled model is also safe, but then recalibrate its item marker height. Do not exceed the planned footprint without rechecking clearance.
10. Add simple colliders for the body/legs. For an approved opening cabinet, leave the interior hollow and give the moving part its own collider. A single solid box over the whole cabinet can stop interaction rays reaching the paper.
11. Add and calibrate the SpawnPoint only on prefabs that need one. See section 9.
12. Drag the completed root into `Assets/Prefabs/Hospital/Props`.

Do not mark moving drawers, lids, keys or papers as static. Static furniture bodies can remain fixed. Keep animations on children so they do not move the entire furniture root or reset its scene position.

## 5. Create the furniture parents

Under `MainHospital/Props`, create these Empty GameObjects:

```text
MainHospital
└── Props
    ├── SupplyFurniture
    ├── ClueFurniture
    ├── KeyBoxes
    └── RoomFurniture
```

Reset all four parents to Position 0/0/0, Rotation 0/0/0, Scale 1/1/1. MainHospital and Props already use identity transforms in the saved scene.

All placement positions below are then both world coordinates and local coordinates relative to these parents. Do not enter a room floor cube as the parent: floor cubes have non-unit scale.

For each row:
1. Drag the matching prepared prefab under the stated parent.
2. Rename it exactly `ID_Type`, for example `S01_Desk`.
3. Enter Position X/Y/Z.
4. Enter Rotation X = 0, Y = the table value, Z = 0.
5. Leave Scale X/Y/Z = **1/1/1**.
6. Press F to frame it and compare it with the placement map.
7. Save after completing each group.

## 6. Place all 15 supply supports

Parent: `MainHospital/Props/SupplyFurniture`.

These are ordinary furniture instances. Do not put permanent health/ammo/sanity briefcases on them. A candidate is allowed to hold **any one** of the three item types, and six candidates will be empty at the start of each run.

| Object name | Room | Position X, Y, Z | Rotation Y | Access |
|---|---|---|---:|---|
| S01_Desk | Nurse Office | -2, 0, 13 | 270 | Ordinary door route |
| S02_Desk | Staff room | 13, 0, 11.5 | 180 | Ordinary door route |
| S03_Desk | Diagnostics | 14.5, 0, 36 | 270 | Ordinary door route |
| S04_Desk | Staff utility | 2.6, 0, 40.8 | 180 | Boards_StaffUtility |
| S05_Nightstand | Post-op | -10, 0, 1.25 | 0 | Ordinary door route |
| S06_Nightstand | General Ward | -14, 0, 12.7 | 90 | Ordinary door route |
| S07_Nightstand | Recovery | 7.8, 0, 1.2 | 0 | Ordinary door route |
| S08_Nightstand | Pre-op | 13.8, 0, 19 | 0 | Boards_PreOp |
| S09_Shelf | Storage | 14.5, 0, 16.8 | 180 | Boards_Storage |
| S10_Shelf | Waste | 14.8, 0, 4.8 | 270 | Boards_Waste |
| S11_Shelf | Sterile Core | 14.8, 0, 29 | 270 | Ordinary door route |
| S12_Shelf | Washroom alcove | -2.3, 0, 22 | 270 | Boards_Washroom |
| S13_Bed | Post-op | -11.5, 0, 2.4 | 0 | Ordinary door route |
| S14_Bed | General Ward | -13.8, 0, 10 | 0 | Ordinary door route |
| S15_Bed | ICU | 7.4, 0, 39.9 | 180 | Ordinary door route |

The beds S13–S15 already count as room furnishings. Do not add duplicate decorative beds at those positions.

The five gated candidates are S04, S08, S09, S10 and S12. A closed barricade must **not** make them ineligible for initial random selection. The furniture and selected item exist behind the boards from the start.

## 7. Place all 10 digit-clue supports

Parent: `MainHospital/Props/ClueFurniture`.

| Object name | Room | Position X, Y, Z | Rotation Y | Reveal method | Access |
|---|---|---|---:|---|---|
| C01_DrawerCabinet | Nurse Office | -2, 0, 15.35 | 180 | Open animated drawer | Ordinary door route |
| C02_DrawerCabinet | Diagnostics | 10.2, 0, 37.5 | 180 | Open animated drawer | Ordinary door route |
| C03_DrawerCabinet | Storage | 10, 0, 13.5 | 0 | Open animated drawer | Boards_Storage |
| C04_DrawerCabinet | Staff utility | 0.8, 0, 40.8 | 180 | Open animated drawer | Boards_StaffUtility |
| C05_ChestStation | Operating Room | -13.8, 0, 34 | 180 | Open chest lid | Ordinary door route |
| C06_ChestStation | Anaesthesia | -14.5, 0, 20.4 | 0 | Open chest lid | Ordinary door route |
| C07_ChestStation | Pre-op | 14.5, 0, 22.8 | 180 | Open chest lid | Boards_PreOp |
| C08_Desk | Sterile Core | 11.2, 0, 30.6 | 180 | Paper on desk top | Ordinary door route |
| C09_Desk | Recovery | 8.8, 0, 5.7 | 180 | Paper on desk top | Ordinary door route |
| C10_Desk | Waste | 12, 0, 1.3 | 0 | Paper on desk top | Boards_Waste |

This gives **4 drawer cabinets + 3 chest stations + 3 static desks**. Build three reusable clue-support prefabs, not ten.

C03, C04, C07 and C10 are behind barricades. The recommended hard-mode selection is **two papers behind barricades and two papers in ordinary-access rooms**, chosen randomly within those groups. Shuffle their code-order assignments too. There is no permanently designated “first digit room.”

### Apply the supplied motion only

C01–C04 use the drawer that actually moves in the chosen cabinet's included clip. Put the paper marker on that moving drawer, preserving its position. Do not assume a specific drawer or 0.35 m travel before previewing the asset.

C05–C07 use NOT_Lonely's supplied chest opening clip. Put the paper marker inside the fixed chest body, not on its lid. Keep the original model's moving parts and animation bindings intact.

You do not create Open/Close clips, keyframe transforms or repair a rig. Clip playback, reverse playback for closing, end-pose holding and interaction triggers belong to your teammate. Preview the imported motion through its complete range and leave at least 1.0 m of usable approach space; if the animation sweeps outside the reservation, the placement needs review before duplication.

## 8. Place the four first-aid key boxes

Parent: `MainHospital/Props/KeyBoxes`.

**Asset selection unresolved:** reserve these four Empty roots and their mounting envelopes. Do not build/animate a replacement case yourself. The eventual approved asset must include opening motion. The planned behavior remains four visible first-aid boxes with one randomized exit key; implementing that behavior is your teammate’s work.

| Object name | Room/wall | Centre Position X, Y, Z | Rotation Y | Access |
|---|---|---|---:|---|
| K01_KeyBox | General Ward / east wall | -6.96, 1.45, 13.7 | 270 | Ordinary door route |
| K02_KeyBox | ICU / west wall | 1.96, 1.45, 34.5 | 90 | Ordinary door route |
| K03_KeyBox | Storage / east wall | 15.79, 1.45, 13.8 | 270 | Boards_Storage |
| K04_KeyBox | Pre-op / north wall | 10.5, 1.45, 23.79 | 180 | Boards_PreOp |

The fitted box back is 0.02 m in front of the wall surface. K03 sits south of Storage's east window. K04 uses Pre-op's **north wall**, avoiding its east window. These mounting positions are intentional.

The previous RayznGames recommendation is withdrawn for this workflow because its description establishes animatable mechanical parts, not included opening clips. A generic preanimated cabinet relabelled FIRST AID would be a possible design compromise, but it has not been selected as a verified wall-box replacement.

Keep K01–K04 reserved while that asset is unresolved. Do not calibrate their final interior marker against an imaginary shelf or substitute a static box that cannot open.

PF_ExitKey remains a static model. Its target dimensions are 0.12 m X × 0.008 m Y × 0.04 m Z, bottom-centre origin; the teammate controls its spawn/pickup. Final placement inside the box waits for the actual approved interior.

## 9. Exact marker schedule and the one-time surface check

A marker is an **Empty GameObject**. It is an address where your programmer instantiates an item. It is not the item itself, and it does not render in the game.

Create a child called `SpawnPoint` on each supply/clue support, and `KeySpawn` on each key box. Rename scene-instance markers to `SP_S01`, `SP_C01`, `SP_K01`, etc., so all 29 IDs are unique.

Use these **local design offsets** on the normalized prefab:

| Type | Marker local X, Y, Z | Support/calibration |
|---|---|---|
| Desk | 0, 0.78, 0 | 0.02 m above its 0.76 m top |
| Nightstand | 0, 0.67, 0 | 0.02 m above its 0.65 m top |
| Shelf | 0, 1.02, 0.12 | Reachable shelf at approximately 1.00 m; check actual shelf elevation |
| Bed | 0, 0.68, 0 | Mattress surface assumed 0.66 m; check actual mattress, not the top of the rails |
| DrawerCabinet | 0, 0.65, 0.12 | Candidate animated-drawer interior target; calibrate to its actual bottom, then parent to the moving drawer preserving world pose |
| ChestStation | 0, 0.81, 0 | Interior base of the chest above the 0.76 m desk; check its interior |
| KeyBox | 0, -0.12, 0.01 | Inside fixed body, relative to centre pivot; check open-case interior |

The offsets for shelves, beds and opening containers are **initial placement targets, not measured asset surfaces**. Set each once in Prefab Mode: temporarily place a small test cube with a bottom-centre pivot at the marker, inspect from the side, and adjust marker Y until the cube rests just above the real surface. Delete the test cube. This adjusts all repetitions without changing their room positions or floors.

For supply-case planning, allow up to **0.32 m wide × 0.14 m high × 0.22 m deep**, with a bottom-centre pivot. The actual resource briefcases remain your team's work.

The table below is the world position resulting from the nominal offsets. If you recalibrate a surface, the marker's final Y changes accordingly; moving drawer markers also move when opened. **The local child is the authority at runtime, not this world-coordinate table.**

| Marker | Parent object | Nominal world X, Y, Z | Local marker rotation |
|---|---|---|---|
| SP_S01 | S01_Desk | -2, 0.78, 13 | 0, 0, 0 |
| SP_S02 | S02_Desk | 13, 0.78, 11.5 | 0, 0, 0 |
| SP_S03 | S03_Desk | 14.5, 0.78, 36 | 0, 0, 0 |
| SP_S04 | S04_Desk | 2.6, 0.78, 40.8 | 0, 0, 0 |
| SP_S05 | S05_Nightstand | -10, 0.67, 1.25 | 0, 0, 0 |
| SP_S06 | S06_Nightstand | -14, 0.67, 12.7 | 0, 0, 0 |
| SP_S07 | S07_Nightstand | 7.8, 0.67, 1.2 | 0, 0, 0 |
| SP_S08 | S08_Nightstand | 13.8, 0.67, 19 | 0, 0, 0 |
| SP_S09 | S09_Shelf | 14.5, 1.02, 16.68 | 0, 0, 0 |
| SP_S10 | S10_Shelf | 14.68, 1.02, 4.8 | 0, 0, 0 |
| SP_S11 | S11_Shelf | 14.68, 1.02, 29 | 0, 0, 0 |
| SP_S12 | S12_Shelf | -2.42, 1.02, 22 | 0, 0, 0 |
| SP_S13 | S13_Bed | -11.5, 0.68, 2.4 | 0, 0, 0 |
| SP_S14 | S14_Bed | -13.8, 0.68, 10 | 0, 0, 0 |
| SP_S15 | S15_Bed | 7.4, 0.68, 39.9 | 0, 0, 0 |
| SP_C01 | C01_DrawerCabinet | -2, 0.65, 15.23 | 0, 0, 0 |
| SP_C02 | C02_DrawerCabinet | 10.2, 0.65, 37.38 | 0, 0, 0 |
| SP_C03 | C03_DrawerCabinet | 10, 0.65, 13.62 | 0, 0, 0 |
| SP_C04 | C04_DrawerCabinet | 0.8, 0.65, 40.68 | 0, 0, 0 |
| SP_C05 | C05_ChestStation | -13.8, 0.81, 34 | 0, 0, 0 |
| SP_C06 | C06_ChestStation | -14.5, 0.81, 20.4 | 0, 0, 0 |
| SP_C07 | C07_ChestStation | 14.5, 0.81, 22.8 | 0, 0, 0 |
| SP_C08 | C08_Desk | 11.2, 0.78, 30.6 | 0, 0, 0 |
| SP_C09 | C09_Desk | 8.8, 0.78, 5.7 | 0, 0, 0 |
| SP_C10 | C10_Desk | 12, 0.78, 1.3 | 0, 0, 0 |
| SP_K01 | K01_KeyBox | -6.97, 1.33, 13.7 | 0, 0, 0 |
| SP_K02 | K02_KeyBox | 1.97, 1.33, 34.5 | 0, 0, 0 |
| SP_K03 | K03_KeyBox | 15.78, 1.33, 13.8 | 0, 0, 0 |
| SP_K04 | K04_KeyBox | 10.5, 1.33, 23.78 | 0, 0, 0 |

Keep marker Scale at 1/1/1. Position supplied items using the marker's **world Position and Rotation**, with their own normalized Scale, even if you parent the visual to a scaled imported drawer. Avoid accidentally inheriting the model's import scaling.

Give your teammate the marker IDs and scene references. They register them with the spawn manager. Markers can stay children of the furniture; you do not need to configure manager arrays or write a script.

## 10. How one paper displays any random digit

Use **one reusable paper prefab**. The manager generates a digit and assigns text to that paper instance.

Give every clue a code-position label as well as its digit. For example:

```text
ELEVATOR ACCESS
DIGIT 2 OF 4

7
```

Four unlabeled digits leave their order ambiguous. The label makes it possible to recover the intended code without guessing permutations.

### Make the paper manually

1. Create Empty `PF_DigitPaper` at Position/Rotation 0/0/0, Scale 1/1/1.
2. Create a Cube child named PaperMesh.
3. Set its local Position to `(0,0.001,0)`, Rotation to `(0,0,0)`, Scale to `(0.21,0.002,0.297)`.
4. Remove its Box Collider for now. This is an A4-sized thin sheet with its bottom at the root.
5. Give it a matte, warm off-white material, for example `#D8D1B9`, Metallic 0, Smoothness 0.1.
6. Under the paper root create **3D Object > Text - TextMeshPro**, not a screen-space Canvas. Import TMP Essentials if prompted.
7. Name it DigitText. Set local Position to `(0,0.004,0)`, Rotation to `(90,180,0)`, Scale to `(0.01,0.01,0.01)`. This lays the normal 3D text face-up over the sheet, with its top pointing away from a reader standing at the furniture's local +Z front.
8. Set its text rectangle Width 19 and Height 27, so at the child scale it occupies about 0.19 × 0.27 m. Set alignment to centre/middle and colour to a dark grey.
9. Start with Auto Size enabled, minimum 2, maximum 8. Enter the example label above and frame it from above to verify that the face is visible and the text fits. Font metrics vary; visual legibility at player height matters more than one universal font-size number.
10. Save under `Assets/Prefabs/Hospital/Gameplay`.
11. Keep it as a project prefab. The spawn manager creates four runtime instances; do not permanently place all ten candidates' papers.

Unity supports 3D TextMeshPro objects without a Canvas; see [Unity's text creation documentation](https://docs.unity.cn/Packages/com.unity.textmeshpro%403.2/manual/TMPObjects.html).

Stop after the static paper and sample text look correct. A teammate changes the TextMeshPro text to the generated digit and code-position label at runtime. That implementation belongs in the separate gameplay handoff; it is not part of your world-building task.

## 11. Add the elevator doors and keypad

The saved elevator opening is **X = 3.2 to 5.0**, **Y = 0 to 2.2**, on the wall at **Z = 9**. It faces south into the lobby. The whole elevator floor is considerably larger than the door opening; do not stretch a complete cabin to the floor size and assume its door will fit.

### Door assembly

- Parent: `MainHospital/GEO_Doors`.
- Root name: `ElevatorDoors`.
- Root Position: **4.1, 0, 9**.
- Root Rotation: **0, 180, 0**, using this guide's convention that the visible front faces local +Z.
- Root Scale: **1, 1, 1**.
- Required clear opening: **1.8 m wide × 2.2 m high**.
- Keep the moving leaves/rails on the shaft side of the wall, approximately **Z = 9.15 to 9.45**, not projecting into the narrow lobby.
- If an old closed placeholder still occupies the opening, disable it only when replacing it with the working door assembly.

Inspect the JeffK model's actual door opening. Scale based on **its opening**, not its full renderer bounds. Keep its skeleton/animation parents when using `02_open`. If extracting the door meshes would break animation bindings, hide unwanted cabin renderers instead of deleting required parents. Do not enable a second cabin floor or wall over the existing geometry.

The elevator creator lists existing idle/open clips. Preview the supplied open animation and keep its imported hierarchy. If it cannot fit the opening while retaining its motion, reject it and choose another preanimated asset. **The previous fallback asking you to make sliding leaves and animate them is removed.**

Do not configure the code/key trigger. Supply the fitted visual and its existing clips to the gameplay teammate. Only an opening action is required for the final exit; no new closing or travel animation is assigned to you.

### Keypad

- Parent: `MainHospital/Props`.
- Root name: `ElevatorKeypad`.
- **Centre** Position: **5.65, 1.35, 8.85**.
- Rotation: **0, 180, 0**.
- Scale: **1, 1, 1** after fitting.
- Target body size: **0.24 wide × 0.36 high × 0.06 deep**.

The wall's south surface is Z = 8.9. With this placement the keypad back is Z = 8.88, leaving a 0.02 m offset. It sits to the right of the elevator opening as seen from the lobby.

The downloaded keypad supplies the appearance. Your programmer still needs code entry, validation and an interaction prompt. Solving the code releases the key; **it does not immediately open the elevator**.

## 12. Place the remaining room furniture

Parent: `MainHospital/Props/RoomFurniture`.

These instances are additional to the supply/clue furniture above. They do not have spawn markers. Use the same fitted prefabs and Scale 1/1/1.

| Object name | Room | Position X, Y, Z | Rotation Y | Purpose |
|---|---|---|---:|---|
| P01_Bed | Recovery | 6, 0, 2.4 | 0 | Second recovery bed |
| P02_Bed | General Ward | -10.4, 0, 10 | 0 | Second ward bed |
| P03_Bed | Anaesthesia | -11.5, 0, 22 | 0 | Preparation bed |
| P04_Bed | Pre-op | 11.5, 0, 20.4 | 0 | Pre-op bed |
| P05_Bed | Operating Room | -10.5, 0, 31.7 | 0 | Procedure bed; budget reuse instead of another operating-table asset |
| P06_Bed | ICU | 7.3, 0, 35.8 | 180 | Second ICU bed |
| P07_Bed | Diagnostics | 12, 0, 34 | 0 | Examination bed |
| P08_Toilet | WC | -3.5, 0, 41.2 | 180 | WC toilet |
| P09_Toilet | Washroom alcove | -5.5, 0, 20.3 | 0 | Washroom toilet |
| P10_Sink | WC | -4.35, 0.65, 39.4 | 90 | WC sink |
| P11_Sink | Washroom alcove | -3.5, 0.65, 19.65 | 0 | Washroom sink |
| P12_Sink | Sterile Core | 15.6, 0.65, 25.2 | 270 | Clinical sink |
| P13_Stool | Staff room | 14.8, 0, 9.3 | 270 | Staff seat |
| P14_Stool | Nurse Office | -4, 0, 12 | 90 | Office seat |
| P15_Bin | Waste | 14.8, 0, 1.3 | 0 | Waste bin |
| P16_Bin | Waste | 14.8, 0, 2.5 | 0 | Repeated waste bin |
| P17_Shelf | Storage | 12.5, 0, 16.8 | 180 | Second storage rack |

Operating/Diagnostics/ICU will read as sparse treatment rooms with this set; it is deliberately not a complete medical equipment simulation. No extra scanners, trolleys or decorative clutter are needed for the first playable version.

Leave all main and side corridors, the entrance, elevator interior and all four hiding pockets free of loose furniture. In particular, do not obstruct the vent approach from the Staff Room near X = 9.5, Z = 10.75.

## 13. Where your world-builder work stops

Your deliverable is a placed prefab, correct materials/colliders, a calibrated empty marker and an included animation that previews correctly. Hand your teammate the prefab name, animation clip name(s), moving-part names and marker IDs.

You do **not** implement E-key input, interaction raycasts, animation triggers, opening/closing state machines, random spawning, random digits, keypad validation, key release, inventory, theft logic or win conditions. Those requirements are in [Hospital_Gameplay_Developer_Handoff.md](Hospital_Gameplay_Developer_Handoff.md).

For an asset with just one opening clip, your teammate handles reverse playback and holding the end pose where the asset supports it. You do not create a new closing animation. If the supplied motion cannot perform the needed action, the asset fails the selection rule; do not repair or animate it yourself.

## 14. Your visual handoff checklist

1. All placed furniture matches the room/position schedule and fits the reserved footprint.
2. Materials render in URP and fit the existing floor/wall palette.
3. In Unity's model/clip Preview, the supplied opening motion works and stays clear of the surrounding geometry.
4. The closed and open poses both leave the paper/key support usable.
5. Markers are empty, named consistently and positioned on the actual model surfaces.
6. Leave colliders on moving parts attached to those parts; your teammate decides how gameplay handles them.
7. Give the animation and marker references to your teammate. Trigger logic is their task.
8. Keep unresolved asset slots reserved; do not fill them with a model that requires new animation work.

### What was checked for this document

The companion checker verified: all 40 saved floor positions and sizes; the 46 proposed closed furniture footprints inside their scheduled rooms; no pairwise furniture-footprint overlaps; no intersections with the saved axis-aligned wall cubes; and no furniture in the scheduled doorway clearance rectangles (opening width plus 0.25 m each side, extending 1.3 m on both sides).

The geometry check returned no errors for the reserved closed footprints, including unresolved asset slots. It does **not** certify imported prefab bounds, unsaved scene changes, open-animation sweeps, interaction line of sight, window asset protrusions or navigability with your player/enemy controller. Source model import, animation preview and marker surface calibration remain manual world-building steps; animation creation and conversion are excluded. Key-box asset selection and the drawer download format remain unresolved.

Companion files: `hospital_prop_placement_plan.json`, `Hospital_Prop_Placement_Map.png`, `Hospital_Prop_Placement_Checks.json`, `check_hospital_prop_plan.py`, and the optional `HospitalPropSizer.cs.txt`.

