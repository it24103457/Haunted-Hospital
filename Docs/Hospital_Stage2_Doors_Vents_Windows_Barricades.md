# Hospital stage 2: vents, doors, boarded openings and windows

Manual follow-up to `Hospital_Skeleton_Beginner_Guide.md`, for Unity 6000.6.2f1 and URP. Prepared from the saved Hospital_Blockout scene and the coordinate schedule. No scene, prefab, package or gameplay script was modified during preparation. Code below is supplied for you to copy manually; it has not been compiled or play-tested in your Editor.

## Read this first

Yes: finish these structural changes and test their collision, then bake and test the NavMesh. Plan for moving doors and disappearing barricades now. **The original guide's CLOSED-door bake instructions are superseded by Part 7 below.** A doorway must exist in the underlying navigation data so opening it at runtime can reveal a route.

The original four sealed spaces were intentional service/stair footprints, not missing room doors. Your new design converts them into player hiding places. Keep the elevator separate: it must remain the final exit. A low opening alone does not implement hiding, crouching or enemy exclusion; the guide covers those separately.

I checked all 40 scheduled floor cubes and 189 wall cubes against the saved scene. One wall has a wrong coordinate and another has a leading space in its name. Check these first. Unsaved changes in the Editor were not inspected.

## Part 1 — Two small checks before adding assets

### Step 1 — Save your progress

1. Stop Play mode.
2. Press Ctrl + S in Hospital_Blockout.
3. Save a checkpoint in your version control before changing walls.
4. Keep MainHospital and its geometry containers at Position `(0,0,0)`, Rotation `(0,0,0)`, Scale `(1,1,1)`.
5. Every position in this guide is in metres under those identity parents. Enter values in the Inspector, not by eye.

### Step 2 — Correct W070_Solid01 manually

In the Hierarchy search field, type `W070_Solid01`. Select the small wall cube, then enter:

```text
Position: X = -6.75   Y = 1.5   Z = 24.625
Rotation: X = 0       Y = 0     Z = 0
Scale:    X = 0.2     Y = 3     Z = 0.45
```

Your saved scene currently has **X = +6.75**. The minus sign matters: the correct position is on the Anaesthesia side of the hospital. Clear the Hierarchy search afterward.

### Step 3 — Remove a leading space from one object name

Search for `W042_SolidEnd`. The saved object's actual name begins with a space. Press F2 and rename it exactly `W042_SolidEnd`. Its geometry already matches the schedule; do not move it.

## Part 2 — Free assets worth testing

### Step 4 — Start with these asset pages

These are creator/official listings. Availability and metadata were checked while preparing this guide. **None was imported into your Unity project, so exact native dimensions, pivot orientation and Unity 6 compatibility are not verified.** The fitting procedure below is required. No uninspected model can honestly be promised to fit without adjustment.

| Purpose | Free asset | What was verified | Remaining work |
|---|---|---|---|
| Main hospital door candidate | [Hospital Door Old Free — AMMediaGames](https://sketchfab.com/3d-models/hospital-door-old-free-15b147ae76ef40da9ad23831ca7225dd) | Creator describes it for hospital/asylum horror use. Public metadata reports downloadable, **one animation**, CC Attribution. | Inspect the motion: one animation does not guarantee separate Open/Close clips or a Unity interaction script. Measure and fit its opening. |
| Alternative door visuals | [Free Wood Door Pack — Biostart](https://assetstore.unity.com/packages/3d/props/interior/free-wood-door-pack-280509) | Listed FREE; compatibility table includes URP for its listed 2021.3 version. | Animation inclusion was not established. Do not choose this expecting guaranteed ready-made animation. Good visual alternative if the hospital model does not suit a narrow opening. |
| Alternative interaction reference | [Interactive Physical Door Pack — Art Notes](https://assetstore.unity.com/packages/tools/physics/interactive-physical-door-pack-163249) | Listed FREE. [Publisher description](https://artnotesblog.blogspot.com/2020/02/interactive-physical-door-pack.html) describes working physics interactions and doors. | Older Unity 2019-era package; physics-driven motion, not confirmed baked clips. Test separately; do not replace your project/controller with its demo. |
| Window candidate | [Old window — SebastianSosnowski](https://sketchfab.com/3d-models/old-window-771cc689144f46ae84186ddc7fae4b90) | Public model metadata reports downloadable, CC Attribution, zero animations. | Inspect appearance and measure it. Static is appropriate here. Add an opaque backing as described below. |
| Closed industrial window alternative | [Rollershutter Window 01 — MP / Poly Haven](https://polyhaven.com/a/rollershutter_window_01) | Free CC0 model; FBX option, weathered metal finish. Listing says 5.1 m wide. | It is a shutter, not glass. It is oversized for this hospital; scale to the aperture using measured dimensions. |
| Board models | [Wooden Planks — Raineo.Dayz](https://sketchfab.com/3d-models/wooden-planks-30ead9be2077420dadb498e44a72f2ed) | Downloadable model, 396 triangles, CC Attribution. | No destruction system promised. Check whether boards are separate meshes; split loose parts in Blender if necessary, or use one separate board per gameplay object. |
| Wood texture fallback | [Wooden Planks — Poly Haven](https://polyhaven.com/a/wooden_planks) | Free CC0 texture set, not a 3D model. | Only a fallback material for simple board geometry; it does not supply breakage or animation. |

For the Phasmophobia-like visual direction, start with worn painted doors, dull handles, dirty opaque window panes and rough boards. This is an art-direction recommendation; these are not assets from Phasmophobia.

For CC Attribution downloads, keep the creator name, model title, source link, license link and a note of modifications in your project credits. Follow the license actually included with the download. Unity Asset Store downloads use their listed Unity license; keep them in your team's project rather than redistributing an asset collection publicly.

### Step 5 — Import ONE door, ONE window and ONE board first

1. Download through the linked official/creator page. Sign in if required.
2. Extract the download to a folder outside Assets so you can inspect what is included.
3. Prefer a supplied FBX with its textures for Unity. **OBJ does not carry the door animation.** If only a Blender source is supplied, export an FBX with animation enabled from Blender. If only glTF is supplied, you need a compatible importer; do not assume Unity imports `.glb` natively in this project.
4. In Unity's Project window create `Assets/Art/ThirdParty/HospitalStage2`. Copy the chosen model and its texture files into a subfolder there. Preserve the original relative texture folders when possible.
5. For an Asset Store pack: Add to My Assets on its store page; in Unity open Window > Package Management > Package Manager > My Assets, find it, Download, then Import. Review the import list; do not import sample scenes or project-setting replacements into your hospital workflow.
6. Create a separate test scene yourself, save it as `Assets/Scenes/Main/DoorAsset_Test.unity`, and test the asset there before placing 12 copies.
7. If materials are pink, make your own material using **Universal Render Pipeline/Lit**, assign the model's base-color map to Base Map, and use that material on your prefab variant. Do not change the hospital's render pipeline.
8. Start with 1K or 2K textures. Use Metallic = 0 for wood; use low Smoothness for weathered wood. Import a normal map as Normal Map before assigning it. A roughness image is not directly a Unity smoothness map.

### Step 6 — Check the included door animation before committing to it

1. Select the imported door FBX in Project.
2. In its Inspector, use **Rig > Animation Type = Generic** for a non-humanoid door and Apply, unless the supplied asset explicitly uses another suitable setup.
3. Open its **Animation** tab and enable Import Animation if required.
4. Expand its clip list and preview/scrub the motion. Identify a time at which it is fully CLOSED and a time at which it is fully OPEN. Record those times in seconds.
5. Check whether the animation moves one leaf, two leaves, the handle, or something else. The published count of one animation does not establish its content.
6. If a full open-close cycle is supplied, record only the closed-to-open interval. If separate clips are supplied in your download, keep the supplied controller or construct an Open/Close controller using those clips.
7. Do not make up clip names or frame numbers from this guide: inspect the actual download.
8. If it lacks useful door motion, treat it as a visual-only model and test the interaction pack instead. The script in Appendix A can reuse a useful interval of an included clip, but cannot invent a missing animation.

## Part 3 — Exact opening sizes and the 60/40 split

### Step 7 — Know what must fit

| Existing opening | Width | Height | Vertical range |
|---|---:|---:|---|
| 19 ordinary room openings | 1.2 m | 2.2 m | Y = 0 to 2.2 |
| Operating Room opening | 1.5 m | 2.2 m | Y = 0 to 2.2 |
| Main entrance | 1.8 m | 2.2 m | Y = 0 to 2.2 |
| Elevator | 1.8 m | 2.2 m | Y = 0 to 2.2 |
| Windows | 1.5 / 2 / 2.25 / 2.5 m | 1.2 m | Y = 1 to 2.2 |

There are **20 ordinary room openings**. Use **12 openable doors + 8 breakable boarded openings**, exactly 60/40. Entrance and elevator remain separate special doors, not breakable bypasses of the endgame. Four vents and 18 windows do not count toward that ratio. All installed ordinary door leaves should open; boarded openings have boards **instead of a door leaf** in this version.

### Step 8 — Fit the model to the opening, not to its Inspector Scale values

1. In the test scene create an empty `DoorPlacementRoot`, position/rotation zero, scale one.
2. Put a copy/variant of the imported door beneath it, with the closed door facing along local Z and its width along local X. Keep animation on the imported hierarchy; rotate/scale a wrapper above it.
3. Make a temporary Cube named `FIT_Opening`, Position `(0,1.1,0)`, Scale `(1.2,2.2,0.2)`. Disable its Box Collider and use its selected wireframe as an aperture reference. It is a ruler, not the finished door. Delete it after fitting.
4. Measure the closed asset: if its frame's OUTSIDE width is `W` and height `H`, a conservative scale is `min(1.2/W, 2.2/H)`. Uniform scaling preserves proportions. If you fit the outer frame, check the resulting INNER clear opening still admits a 0.6 m-diameter character with margin; aim for at least 0.9 m clear.
5. If using a bare leaf without an imported frame, aim initially for about **1.12 m wide, 2.12 m high**, with its bottom at Y = 0.04. Do not simply set an arbitrary FBX Scale to `(1.12,2.12,0.06)`; those are target world dimensions, not universal imported model scale values.
6. Avoid stretching a 0.9 m door to a 1.5 or 1.8 m opening. Prefer a suitable double-door variant, or a 1.2 m usable assembly with stationary infill panels on either side. Infill must meet the wall, preserve useful passage width and be included as static geometry in the bake.
7. A 1.2 m assembly centred in the 1.5 m Operating opening leaves **0.15 m per side** for infill. The root remains at the centre below. This is an option, not an asset fit already verified.
8. Keep the frame fixed while the leaf moves. If opening the door moves its frame too, the asset hierarchy/animation binding is wrong.
9. Keep `DoorPlacementRoot` at Scale `(1,1,1)`. Apply visual fit beneath it. Place the root at floor level; do not use the old placeholder leaf's Y = 1.075.
10. Save the tested hierarchy as your own prefab by dragging it into `Assets/Prefabs/Hospital/Doors`. Keep the source asset unchanged.

For measurements without a modelling tool, a temporary Box Collider on a simple MeshRenderer object can help inspect its local mesh extents. Its Size is local, not necessarily the world size of a multi-mesh prefab. For a complete assembly, compare its closed silhouette to the aperture ruler or inspect combined bounds in the modelling tool. Do not pretend the root Transform Scale tells you the mesh's metre dimensions.

### Step 9 — Place the 12 openable doors

Create each root under `MainHospital/GEO_Doors`. All roots use **Position Y = 0, Rotation X/Z = 0, Scale = (1,1,1)**. The table gives Position X/Z and Rotation Y. Local X runs across the aperture; local Z runs through it. A 90-degree turn aligns a door with an east/west wall. Fine-tune the imported visual's facing beneath this root, not the scheduled root position.

| Root name | X | Z | Y rotation | Opening width | Corresponding wall |
|---|---:|---:|---:|---:|---|
| Door_PostOp | -5.15 | 7.25 | 0 | 1.2 | W037 |
| Door_Recovery | 4.1 | 7 | 0 | 1.2 | W032 |
| Door_StaffRoom | 9.5 | 8 | 90 | 1.2 | W035 |
| Door_Ward_North | -9 | 16.5 | 0 | 1.2 | W052 |
| Door_Office_North | -5.75 | 16.5 | 0 | 1.2 | W053 |
| Door_Anaesthesia | -6.75 | 25.45 | 90 | 1.2 | W070 |
| Door_Operating | -5.4 | 27.5 | 0 | 1.5 | W073 |
| Door_Sterile_West | 4.5 | 25.3 | 90 | 1.2 | W066 |
| Door_ICU_Sterile | 5.6 | 31.5 | 0 | 1.2 | W078 |
| Door_ICU_West | 1.75 | 36.65 | 90 | 1.2 | W080 |
| Door_Diagnostics | 9 | 33.25 | 90 | 1.2 | W081 |
| Door_WC | -1.15 | 37.75 | 0 | 1.2 | W087 |

1. Place and test **Door_PostOp first**. Duplicate only after its motion/collision is correct.
2. If you previously made a `D_..._Leaf` Cube at that opening, disable/remove that old placeholder before installing the final assembly. Do not leave a hidden collider behind it.
3. Check the leaf does not swing into an adjacent wall, another door or a narrow circulation path. Reverse the hinge/opening direction or choose a suitable asset variant where needed; do not move the room walls to compensate casually.
4. Opening should be initiated by an interaction ray from the player's camera within about 2 m, hitting the nearest solid object. Raycasts that only check a Door layer can interact through intervening walls; include walls in the ray mask.
5. Use the asset's supplied working interaction if compatible. If only a useful animation is supplied, Appendix A provides a simple interaction bridge which samples that existing animation; it does not require you to author keyframes.
6. Main entrance root is `(0,0,0)` and elevator root is `(4.1,0,9)`, Y rotation 0, width 1.8. Fit those separately. Keep the entrance unavailable during a run and elevator gated by the eventual code/key state. A debug open button is not the final escape logic.

### Step 10 — Use eight boarded openings

Create roots under a new identity-transform `MainHospital/GEO_Barricades` container. Position Y = 0 and Scale = one for every root. Do not put an ordinary door behind these boards in this version.

| Root name | X | Z | Y rotation | Existing opening | Purpose |
|---|---:|---:|---:|---|---|
| Boards_Ward_PostOp | -12 | 7.25 | 0 | D_Ward_PostOp / W036 | Optional shortcut; rooms have other entries |
| Boards_Ward_East | -6.75 | 8.35 | 90 | D_Ward_East / W038 | Optional shortcut; Ward north door remains |
| Boards_Office_South | -2.05 | 9.5 | 0 | D_Office_South / W044 | Optional shortcut; Office north door remains |
| Boards_Waste | 11.8 | 7 | 0 | D_Waste / W034 | Room requires clearing |
| Boards_Storage | 8.25 | 16 | 90 | D_Storage / W051 | Room requires clearing |
| Boards_PreOp | 8.25 | 20.55 | 90 | D_PreOp / W058 | Room requires clearing |
| Boards_Washroom | -5.7 | 24.5 | 0 | D_Washroom / W069 | Room requires clearing |
| Boards_StaffUtility | 0.65 | 37.75 | 0 | D_StaffUtility / W088 | Room requires clearing |

All eight apertures are 1.2 x 2.2 m. This is a proposed distribution, not a rule about random clue locations. The main corridor and routes between its branches remain unboarded. Do not place the player's first weapon or all ammunition behind these barriers. With the example two hits per plank and four planks per opening, all eight barriers require **64 successful hits**. Unlimited test ammunition is fine during construction; final resource design needs guaranteed access or a reliable fallback way to break boards. A resource thief must not make the game unwinnable.

### Step 11 — Build one reusable barricade prefab

Create this under `Boards_Ward_PostOp` first:

```text
Boards_Ward_PostOp              (root, scale 1)
├── PassageBlocker              (invisible full-opening movement collider)
├── Board_01                   (health script + fitted Box Collider)
│   └── ImportedBoardVisual
├── Board_02
├── Board_03
└── Board_04
```

Make every Board child an EMPTY object at Scale one. Fit its imported visual below it to the target sizes. Position/rotation below are **LOCAL to the barricade root**:

| Board | Position X,Y,Z | Rotation X,Y,Z | Target visual length x height x depth |
|---|---|---|---|
| Board_01 | 0, 0.45, 0 | 0, 0, 0 | 1.5 x 0.18 x 0.06 |
| Board_02 | 0, 0.9, 0.02 | 0, 0, 0 | 1.5 x 0.18 x 0.06 |
| Board_03 | 0, 1.35, 0 | 0, 0, 0 | 1.5 x 0.18 x 0.06 |
| Board_04 | 0, 1.1, 0.09 | 0, 0, 35 | 1.9 x 0.18 x 0.06 |

The horizontal boards extend 0.15 m onto the wall on each side. The diagonal has an approximately 1.66 m-wide rotated bounding box, giving overlap too. Create variety by changing its Z rotation to -35 for the second prefab variant. Keep Rotation Y on the overall root from the placement table; do not individually reinterpret every board's world axes.

1. Add a Box Collider to each Board empty. Set Center `(0,0,0)` and Size to that board's target dimensions. Is Trigger off. Remove duplicate colliders from the imported visual.
2. No Rigidbody is needed for the first version. Do not add a live Rigidbody to an intact board, or it will fall before being shot.
3. Create an EMPTY `PassageBlocker` child at local Position `(0,1.1,0)`, Rotation zero, Scale one. Add Box Collider, Center zero, Size `(1.2,2.2,0.15)`, Is Trigger off. No renderer is needed.
4. Assign this blocker to a new **MovementOnly** layer. Player and enemies collide with it. The gun ray mask must **exclude MovementOnly** while including walls, doors and the actual board colliders. Otherwise every shot hits the invisible sheet instead of a plank.
5. The whole opening stays impassable until the last board breaks. This avoids a crouching player slipping between the decorative boards before clearing them.
6. Add the scripts from Appendix B. Set every plank to 50 health; the test hit supplies 25 damage. Assign all four plank references and the PassageBlocker collider to the root's barricade component.
7. For the navigation stage, put a Box NavMeshObstacle on the ROOT: Center `(0,1.1,0)`, Size `(1.2,2.2,0.3)`, Carve on, Carve Only Stationary on. Assign it to the barricade script. It is not a physical collider.
8. Shoot one actual board twice. It should disappear; the portal should still block movement. Repeat on the remaining boards. Only after the last board breaks should the portal collider and obstacle turn off.
9. Save as your own prefab in `Assets/Prefabs/Hospital/Barricades`. Place it using the eight-row table.

This initial break effect removes individual boards. Optional later debris can spawn separate fragments with Rigidbody, short lifetimes and collision rules that do not block the cleared path. No model download automatically supplies your damage rules. Do not introduce runtime fracture simulation just to get the first version working.

## Part 4 — Convert the four sealed pockets into hiding places

### Step 12 — Identify the correct four spaces

| Hiding place | Existing floor | Floor bounds X | Floor bounds Z | Entrance side |
|---|---|---|---|---|
| Hide_ElevatorSide | F_ElevatorServiceStrip | 8.25 to 9.5 | 9 to 12.5 | East wall, from Staff Room |
| Hide_StairPocket | F_StairFootprint | 2.75 to 6 | 17.25 to 23.5 | South wall, from right landing |
| Hide_OperatingSide | F_OperatingServiceShaft | -3.25 to -1.5 | 27.5 to 35.75 | East wall, from main corridor |
| Hide_NorthPocket | F_NorthServiceAlcove | -4.75 to -3.25 | 35.75 to 37.75 | East wall, from north lobby |

The elevator-side pocket has only about **1.05 m clear internal width** after wall thickness. It is suitable for a small crouching player, not a large model. The north pocket is about 1.3 x 1.8 m clear. Do not confuse the accessible Washroom alcove with these four spaces; it already has a north doorway, now proposed as a boarded opening.

### Step 13 — Use a consistent crouch aperture

Use a clear opening **0.9 m wide and 1.1 m high**, from floor Y = 0 to Y = 1.1. This is a deliberately generous game vent for crouch movement, not a literal small ventilation grille.

Assumed player body: standing height 1.8 m; crouched height 0.9 m; radius 0.25 m. A CharacterController with radius 0.5 m cannot fit a 0.9 m-wide vent. Confirm the actual player's collider before choosing artwork. Lowering only the camera does not shrink the body.

```text
Before:                  After, front view:
                         +-----------------+ Y=3
  +-----------------+    |  header, 1.9 m   |
  |   solid wall    |    +----+-------+----+ Y=1.1
  |                 |    |wall| 0.9 m |wall|
  +-----------------+    +----+-------+----+ Y=0
                               opening
```

No raised sill: the player should crouch and walk through, without needing a jump. Do not put a solid decorative vent grille across the opening; use an open frame, or a removable cover later. Mount trim on the wall face so it does not reduce the 0.9 x 1.1 clear space.

### Step 14 — Replace one wall with three Cubes for Hide_ElevatorSide

1. Select `W043_SolidEnd` and uncheck its top-level active checkbox. Keep it temporarily as a reversible backup; **not merely its Mesh Renderer**, which would leave invisible collision.
2. Under GEO_Walls create three Cubes using right-click > 3D Object > Cube. Rename and enter these transforms. Rotation is `(0,0,0)` for each. Keep Box Colliders enabled, Is Trigger off.

| Name | Position X,Y,Z | Scale X,Y,Z |
|---|---|---|
| W043_Vent_South | 9.5, 1.5, 9.6 | 0.2, 3, 1.4 |
| W043_Vent_North | 9.5, 1.5, 11.9 | 0.2, 3, 1.4 |
| W043_Vent_Header | 9.5, 2.05, 10.75 | 0.2, 1.9, 0.9 |

Check: the gap runs along Z = **10.3 to 11.2**, on X = 9.5. The header bottom is `2.05 - 1.9/2 = 1.1`. The player enters westward from Staff into the service strip. Do not cut the elevator's wall instead.

### Step 15 — Replace one wall for Hide_StairPocket

Disable `W054_SolidEnd`. Add these Cubes under GEO_Walls, each with Rotation `(0,0,0)`:

| Name | Position X,Y,Z | Scale X,Y,Z |
|---|---|---|
| W054_Vent_West | 3.275, 1.5, 17.25 | 1.25, 3, 0.2 |
| W054_Vent_East | 5.45, 1.5, 17.25 | 1.3, 3, 0.2 |
| W054_Vent_Header | 4.35, 2.05, 17.25 | 0.9, 1.9, 0.2 |

Check: gap X = **3.9 to 4.8** at Z = 17.25. The player enters northward from the landing above the elevator. Keep the area as a ground-level pocket; no stairs leading to an upper floor are added.

### Step 16 — Replace one wall for Hide_OperatingSide

Disable `W076_SolidEnd`. Add these Cubes under GEO_Walls, each with Rotation `(0,0,0)`:

| Name | Position X,Y,Z | Scale X,Y,Z |
|---|---|---|
| W076_Vent_South | -1.5, 1.5, 28.975 | 0.2, 3, 3.15 |
| W076_Vent_North | -1.5, 1.5, 33.65 | 0.2, 3, 4.4 |
| W076_Vent_Header | -1.5, 2.05, 31 | 0.2, 1.9, 0.9 |

Check: gap Z = **30.55 to 31.45** at X = -1.5. The player enters westward from the main corridor. The far wall against Operating Room stays intact.

### Step 17 — Replace one wall for Hide_NorthPocket

Disable `W085_SolidEnd`. Add these Cubes under GEO_Walls, each with Rotation `(0,0,0)`:

| Name | Position X,Y,Z | Scale X,Y,Z |
|---|---|---|
| W085_Vent_South | -3.25, 1.5, 35.975 | 0.2, 3, 0.65 |
| W085_Vent_North | -3.25, 1.5, 37.525 | 0.2, 3, 0.65 |
| W085_Vent_Header | -3.25, 2.05, 36.75 | 0.2, 1.9, 0.9 |

Check: gap Z = **36.3 to 37.2** at X = -3.25. The player enters westward from the north lobby.

These four replacements preserve the original wall endpoint extensions, so corners still meet their neighbouring cubes. Do not re-enable the original solid wall afterward. Press Ctrl + S after every completed pocket.

### Step 18 — Make entry physically player-only

The enemy NavMesh restrictions later are important, but also add physical exclusion so an enemy using direct movement cannot accidentally enter.

1. In Inspector > Layer > Add Layer, create **Player**, **Enemy**, **EnemyOnlyBlocker** and **MovementOnly** if they do not exist. Leave other layer slots alone.
2. Assign Player to the player's body and collider children, Enemy to enemy bodies/collider children. Tags and layers are different; a Player tag alone is insufficient.
3. Open Edit > Project Settings > Physics. In the 3D Layer Collision Matrix, make EnemyOnlyBlocker collide with Enemy and not with Player. Restrict its other pairs as appropriate so it acts only as the enemy gate. Do not use Physics 2D.
4. Under a new identity container `MainHospital/GEO_Vents`, create four EMPTY gate objects. Add Box Collider, Size as below, Center zero, Is Trigger off. No Mesh Renderer and no Rigidbody.
5. Assign **EnemyOnlyBlocker** layer to those gate objects. Exclude that layer from player interaction/gun rays and stand-up clearance queries.

| Gate | Position X,Y,Z | Rotation | Box Collider Size X,Y,Z |
|---|---|---|---|
| Gate_Hide_ElevatorSide | 9.5, 0.55, 10.75 | 0,0,0 | 0.2, 1.1, 0.9 |
| Gate_Hide_StairPocket | 4.35, 0.55, 17.25 | 0,0,0 | 0.9, 1.1, 0.2 |
| Gate_Hide_OperatingSide | -1.5, 0.55, 31 | 0,0,0 | 0.2, 1.1, 0.9 |
| Gate_Hide_NorthPocket | -3.25, 0.55, 36.75 | 0,0,0 | 0.2, 1.1, 0.9 |

Physical colliders do not constrain an enemy whose script teleports or writes its Transform through walls. The enemy developer must respect the NavMesh/obstacles and hiding-area rules. A NavMeshAgent's planned motion is not made player-only simply by changing the Physics collision matrix.

### Step 19 — Implement crouching and headroom checks

If a player controller already exists, integrate these changes into it instead of adding a second movement controller:

1. Keep the controller root at the feet, with Scale one and no tilt.
2. Standing: CharacterController Height 1.8, Radius 0.25, Center `(0,0.9,0)`.
3. Crouched: Height 0.9, Radius 0.25, Center `(0,0.45,0)`.
4. Set Step Offset around 0.15, Min Move Distance 0, Skin Width around 0.025. Use CharacterController.Move; do not drive the same player with a Rigidbody too.
5. Lower the camera to around local Y = 0.75 while crouched, and restore to about 1.6 while standing. Both camera and collider must change.
6. Before returning to standing, test the intended standing capsule against solid environment geometry. Ignore the player's own layer and trigger-only sensors. If blocked by the vent header, keep crouching even if the player releases the crouch key.
7. Preserve the feet position when changing height. Do not grow the collider equally downward through the floor.
8. Test entering and backing out of all four vents. Test releasing crouch while under each header. Test standing inside the pocket if there is sufficient headroom; the design only requires crouching at entry, not necessarily throughout the room.

The optional tester in Appendix C supplies this behavior for an otherwise empty scene. It is not a complete final player/weapon system.

### Step 20 — Keep hiding separate from collision

An inaccessible enemy room is not automatically a full hiding mechanic. Later, give each pocket an interior trigger and notify the hunter when the player is fully inside. Suggested trigger boxes below are inset 0.4 m from floor boundaries, so they do not extend into the corridor:

| Interior trigger | Position X,Y,Z | Box Size X,Y,Z |
|---|---|---|
| HideZone_ElevatorSide | 8.875, 0.9, 10.75 | 0.45, 1.8, 2.7 |
| HideZone_StairPocket | 4.375, 0.9, 20.375 | 2.45, 1.8, 5.45 |
| HideZone_OperatingSide | -2.375, 0.9, 31.625 | 0.95, 1.8, 7.45 |
| HideZone_NorthPocket | -4, 0.9, 36.75 | 0.7, 1.8, 1.2 |

Use these as trigger volumes, not solid colliders. A trigger touch is only a candidate for hiding: require the player's whole body to be behind the vent plane before granting hidden status. With multiple child colliders, track the player root and occupancy correctly; one OnTriggerExit is not necessarily the entire player leaving.

Tell the hunter developer to keep the last-heard location at a reachable point outside the vent and search there. Do not grant a silent invisibility cheat merely because the player touches the threshold: footsteps/shots from inside can still emit sound, according to your final design. The hunter's attack/raycast logic must not hit through the wall just because it has an old target reference. These are agent integration requirements, not behavior already provided by a vent model.

## Part 5 — Static, opaque windows

### Step 21 — Build one window variant

1. Create an identity-transform `MainHospital/GEO_Windows` container if missing.
2. In your asset test scene create an empty `WindowRoot`, Scale one. Add the imported window beneath it.
3. Fit the imported window's visible outer frame to a **1.5 m x 1.2 m** hole first. Keep width along local X, height along local Y, depth along local Z.
4. Centre the visible frame about local Y = 0. The placement root will go at world Y = **1.6**, midway between the sill at 1 and header at 2.2.
5. Use the same measured-dimensions procedure as for doors. Static models may be adjusted horizontally more easily, but avoid visibly stretched handles or frame profiles. Make separate tested variants for widths 1.5, 2, 2.25 and 2.5 m.
6. Create a dark opaque backing Cube as a child. Local Position `(0,0,0)`, Scale `(window width,1.2,0.08)`. The final decorative asset can be placed slightly toward the interior face, leaving the backing behind its panes. Keep the backing on the wall centreline and make sure it does not poke through the frame.
7. Give the backing an **URP/Lit Opaque** material, dark grey rather than transparent. Keep its Box Collider enabled and Is Trigger off. This seals both views and movement to the unbuilt outdoors.
8. If the imported window uses transparent glass, replace that glass material on your own variant with a dirty opaque material, or let the backing supply the opaque surface.
9. If you already created `WIN_..._CollisionPane` from the first guide, reuse its collider/backing rather than stacking another coincident collider. Disable the old plain Mullion if the new asset includes its own divider.
10. No Animator or opening script is needed. Save the fitted assembly as a prefab. Mark it static only after fitting; these windows do not move.

### Step 22 — Place all 18 windows

All roots below use **Position Y = 1.6**, Rotation X/Z = 0, Scale one. The width is the target APERTURE, not a raw FBX scale. The listed Y rotation aligns width/depth axes. Reverse the model's facing beneath the root if the decorative side faces outdoors.

| Root | X | Z | Y rotation | Width |
|---|---:|---:|---:|---:|
| Window_PostOp_South | -8.25 | 0 | 0 | 2.25 |
| Window_PostOp_West | -13.25 | 3.5 | 90 | 2.5 |
| Window_Recovery_South | 6.4 | 0 | 0 | 2.5 |
| Window_Waste_South | 13.25 | 0 | 0 | 1.5 |
| Window_Staff_East | 16 | 9.6 | 90 | 1.5 |
| Window_Storage_East | 16 | 15.3 | 90 | 2 |
| Window_PreOp_East | 16 | 21.15 | 90 | 2 |
| Window_Sterile_East | 16 | 27.5 | 90 | 2 |
| Window_Diagnostics_East | 16 | 34.25 | 90 | 2 |
| Window_Diagnostics_North | 12 | 38.5 | 0 | 2.5 |
| Window_ICU_North | 6.4 | 42 | 0 | 2.5 |
| Window_StaffUtility_North | 1.8 | 42 | 0 | 1.5 |
| Window_WC_North | -2.55 | 42 | 0 | 1.5 |
| Window_Operating_NorthWest | -12.75 | 35.75 | 0 | 2.5 |
| Window_Operating_NorthStep | -7.3 | 36.75 | 0 | 2.5 |
| Window_Operating_West | -15.75 | 31.2 | 90 | 2.5 |
| Window_Anaesthesia_West | -16.75 | 23 | 90 | 2.5 |
| Window_Ward_West | -15.75 | 11.75 | 90 | 2.5 |

Do not enlarge/reposition the wall cutouts just because an asset's default size differs. The test-fit step exists to settle one asset before placing it repeatedly.

## Part 6 — Test the player interaction before navigation

### Step 23 — Run this test list

- Standing player cannot enter any vent; crouched player can enter and leave all four.
- Releasing crouch beneath a header does not force the camera/body into the wall.
- Enemy-sized test body is blocked at the vent; Player is not blocked by EnemyOnlyBlocker.
- Every ordinary door visibly opens and closes, blocks when closed, and admits the player when fully open.
- The open leaf still has correct physical collision where it rests. For a skinned asset, colliders must follow animated bones/hinges; a static root collider does not follow vertex deformation.
- Door closing is prevented while a player/enemy occupies the doorway or swing region. Do not crush or trap a player with an instantaneous invisible collider.
- Interact cannot reach a door through a wall.
- Board hits affect only the nearest hit board; no damage through a wall or from merely looking at the model.
- Individual boards disappear after their intended damage. Invisible full-opening collision remains until the last board breaks, then goes away.
- Windows remain opaque and impassable, including when the player jumps.
- Entrance/elevator have their intended special restrictions. Shooting boards must not bypass the elevator code/key sequence.

Keep the first test limited to one door and one barricade. Reuse the tested prefab for the rest.

## Part 7 — Then bake a NavMesh that supports those changes

The saved scene currently has AI Navigation **2.0.14 installed**, but no NavMeshSurface, NavMeshAgent, NavMeshObstacle, NavMeshLink or NavMeshModifier components in the saved hospital scene. No custom movement/weapon scripts were found among the project's C# files at inspection time. This is why the appendices offer a temporary tester and explicit integration points.

### Step 24 — Preserve walkable geometry under dynamic barriers

Use the original guide's Surface-on-MainHospital setup, Physics Colliders, Current Object Hierarchy and Hospital_Blockout agent type. Keep the prototype enemy agent at radius 0.3, height 1.8, step 0.2, voxel size 0.05 unless your teammates agree otherwise.

**Change the original bake procedure:** bake the potential traversable doorways without their dynamic blockers. Include floors, fixed walls, fixed door frames/infill and window backing colliders. Exclude moving door leaves, portal blockers, board colliders and debris from static input. Otherwise opening/breaking them leaves a permanently cut hole in the baked NavMesh.

One explicit workflow for the first bake:

1. Put fixed door frames/infill under a static geometry branch collected by MainHospital. Do not put them below a modifier which removes the entire animated assembly from the bake.
2. Put each animated model and portal collider beneath a `Dynamic` branch on that door root. Add **NavMesh Modifier > Mode = Remove Object, Apply To Children on** to that Dynamic branch. This removes it only from bake input, not from physics/rendering.
3. Add the same Remove Object modifier to each entire barricade root (boards and movement blocker are dynamic), while leaving surrounding fixed walls included.
4. Temporarily disable only the NavMeshObstacle components on doors/barricades for the underlying open-route bake and inspection. Also preview door visuals open if helpful; their modifiers should already exclude them from bake input.
5. Bake. Verify a continuous underlying route through every ordinary opening.
6. Re-enable runtime obstacle components in the proper initial closed/intact state before testing Play mode. A carving obstacle cuts the already baked mesh at runtime; opening a door or removing boards disables/updates that obstacle and allows the surface to recover on a subsequent navigation update.

Do not select Remove Object for the fixed walls or window backing; they must remain blockers. The old blanket Not Walkable modifier on GEO_Doors must not be your only dynamic-door setup. Use the more specific child Remove Object configuration for moving pieces, and a Not Walkable override on fixed frame geometry to stop walkable patches on top.

### Step 25 — Door and barricade obstacle states

For a hinged door, use a portal Box NavMeshObstacle on the placement root: Center `(0,1.1,0)`, Size `(opening width,2.2,0.3)`, Carve on, Carve Only Stationary on. Root Scale must be one. Disable this portal obstacle only when the door has reached its fully open, passable pose. Re-enable when safely starting to close. Appendix A performs that basic state change.

The portal obstacle represents passage availability, not the open leaf's actual occupied space. If the open leaf projects into walkable space, give the moving leaf an additional correctly sized obstacle following its hinge/bone, or choose a swing pose against a wall where it does not obstruct circulation. Test both sides. Do not leave an always-active portal obstacle after opening, or enemies can never use the opening.

For boards, one root obstacle blocks the entire doorway until all planks are gone. Disable it together with the passage collider on final destruction, as Appendix B does. Do not put an obstacle on each flying fragment for this first version.

Neither collider removal nor NavMeshObstacle carving is guaranteed to update a path in the same frame. Allow the next navigation update and repath when the barrier state changes.

### Step 26 — Keep enemies out of hiding places

1. Keep the four hiding floor Cubes marked **Not Walkable** for the enemy bake using NavMesh Modifier > Add or Modify Object > Override Area = Not Walkable. The player uses CharacterController physics and does not require those floors to be on an enemy NavMesh.
2. The 1.1 m headers are too low for a 1.8 m-tall enemy bake, but do not rely only on agent size. Explicitly exclude the interiors too.
3. Include the EnemyOnlyBlocker gate geometry in the enemy Surface's collected layers if using it as a bake blocker. The physics collision matrix does not determine a Surface's Include Layers.
4. Add no NavMeshLinks into the hiding pockets. Keep Generate Links off for this single-floor prototype.
5. Leave the elevator excluded from enemy navigation unless your design explicitly allows AI into it. The player can later enter using physics after the game unlocks its door; player movement does not require an enemy NavMesh there.
6. Confirm the hunter can path to a reachable vent approach, not inside. Confirm the thief never chooses resources in a player-only hiding pocket unless its logic knows those items are unavailable to it.

### Step 27 — Decide how enemies use normal doors

A carved closed door makes the route unreachable. **Auto Repath does not teach an enemy to open a door.** For the initial test, enemies can simply use currently open routes and wait/search when closed barriers block a path. If the design lets the ghost/thief open doors, their agent developer must add door interaction and a higher-level route/door plan, or a carefully controlled link-based door traversal. That is agent behavior, not an asset-import feature.

Test at least these state changes: closed door = blocked; opening = still blocked until passable; open door = complete route; safe closing = blocked again; intact barricade = blocked; last board breaks = complete route; hiding pocket = never a valid enemy destination.

### Step 28 — Prevent progression problems

Retest routes with all normal doors open and all boards cleared; every ordinary room should be connected. Then test the initial closed/intact state. Waste, Storage, Pre-op, Washroom and upper Staff utility are intentionally blocked until clearing. The other three barricades are alternate routes. Random digits and the key must use valid candidate locations for the intended progression stage; do not put mandatory items inside locked elevator geometry or enemy-only assumptions. Your gun/ammo access must be possible before any compulsory barricade.

Do not rewrite your whole original build guide from the beginning. Finish this stage, then use its existing NavMesh testing and team-handoff sections **with the dynamic-barrier changes above**.

## Appendix A — Reuse the downloaded door animation

Use this only if the asset contains a useful opening animation and does not already provide compatible interaction. This plays an interval of the existing clip forward to open and backward to close. It does not create a new animation, retarget an incompatible rig or reproduce Phasmophobia's drag-to-open system. Begin with press-E-to-toggle.

### A1 — Prepare the hierarchy and components

```text
Door_PostOp                       (placement root; HospitalClipDoor)
├── Frame                         (fixed; included in static bake)
├── Dynamic                       (Remove Object NavMeshModifier later)
│   ├── ImportedAnimatedModel     (Animator on original animation root)
│   └── PortalCollider            (Box Collider; opening-sized)
└── CloseCheck                    (empty; centre of closing safety region)
```

If the imported frame cannot be separated without breaking animation paths, keep the imported hierarchy intact, exclude that entire visual from bake input, and provide separate fixed frame colliders under Frame to represent its actual narrowing of the opening. Do not drag animated bones around and break their paths.

1. Make PortalCollider an EMPTY at local `(0,1.1,0)`, Scale one. Add Box Collider, Center zero, Size `(opening width,2.2,0.12)`, Is Trigger off. It blocks the closed/opening doorway and switches off when fully open. It is not a visible model.
2. Add a NavMeshObstacle to the placement root as described in Step 25; it can wait until the navigation stage. Do not put it on the Animator if the animation moves that root.
3. Create CloseCheck at local `(0,1.1,0)`. The script's default half-extents cover a generous region on both sides. Adjust that box to cover the full leaf sweep, especially a wide/double door. Its actor mask must include Player and Enemy, not the door itself.
4. Keep a fitted leaf collider attached to the actual moving hinge/leaf or appropriate animation bone. The script's portal collider is not a substitute for collision with the open leaf. Use a kinematic Rigidbody only if the imported physics setup needs it; do not combine uncontrolled physics with animation driving the same transform.
5. Disable other scripts/Animator Controllers that drive this same door if using the script below. Set its Animator Controller to None; keep the correct imported Avatar if its Generic rig uses one. Turn Apply Root Motion off.
6. In Project create `Assets/Scripts/World/HospitalClipDoor.cs` using Create > Scripting > MonoBehaviour Script, name it exactly, open it and replace its contents with the code below. Save and return to Unity.
7. Add HospitalClipDoor to Door_PostOp. Assign Animator, the actual imported Animation Clip, PortalCollider and CloseCheck. Set Actor Layers to Player and Enemy. Set Closed Time/Open Time to the measured clip times from Step 6. Seconds = 0.8 is a starting motion duration, independent of the clip's original duration.
8. Keep Locked off for ordinary doors. Keep it on for the special entrance/elevator until the real game-state code authorizes opening. The prototype does not implement that authorization.
9. Test the one door before saving a prefab. The original clip must not animate DoorPlacementRoot away from its doorway. If bindings do not match, correct the imported animation root/rig rather than randomly moving the placement root.

```csharp
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

public sealed class HospitalClipDoor : MonoBehaviour
{
    public Animator animator;
    public AnimationClip clip;
    public float closedTime;
    public float openTime = 1f;
    [Min(0.1f)] public float seconds = 0.8f;
    public bool locked;
    public BoxCollider portalCollider;
    public NavMeshObstacle portalObstacle;
    public Transform closeCheck;
    public Vector3 closeHalfExtents = new Vector3(0.85f, 1.15f, 1.5f);
    public LayerMask actorLayers;

    PlayableGraph graph;
    AnimationClipPlayable motion;
    float openness;
    bool targetOpen;

    void OnEnable()
    {
        if (!animator || !clip || !portalCollider || !closeCheck ||
            actorLayers.value == 0 || clip.legacy ||
            closedTime < 0 || openTime < 0 ||
            closedTime > clip.length || openTime > clip.length ||
            Mathf.Approximately(closedTime, openTime))
        {
            Debug.LogError("Door needs a non-Legacy clip, valid pose times, references and actor layers.", this);
            enabled = false;
            return;
        }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("HospitalDoorClip");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        motion = AnimationClipPlayable.Create(graph, clip);
        motion.SetSpeed(0);
        var output = AnimationPlayableOutput.Create(graph, "Door", animator);
        output.SetSourcePlayable(motion);
        openness = 0;
        targetOpen = false;
        graph.Play();
        Sample();
        SetBlocked(true);
    }

    public void Toggle()
    {
        if (!enabled || !graph.IsValid() || locked) return;
        if (targetOpen && Occupied()) return;
        targetOpen = !targetOpen;
        if (!targetOpen) SetBlocked(true);
    }

    bool Occupied()
    {
        return Physics.CheckBox(closeCheck.position, closeHalfExtents,
            closeCheck.rotation, actorLayers, QueryTriggerInteraction.Ignore);
    }

    void Update()
    {
        if (!graph.IsValid()) return;
        if (!targetOpen && openness > 0 && Occupied()) targetOpen = true;
        openness = Mathf.MoveTowards(openness, targetOpen ? 1 : 0,
            Time.deltaTime / Mathf.Max(0.1f, seconds));
        Sample();
        SetBlocked(openness < 0.999f);
    }

    void Sample()
    {
        motion.SetTime(Mathf.Lerp(closedTime, openTime, openness));
        graph.Evaluate(0);
    }

    void SetBlocked(bool value)
    {
        portalCollider.enabled = value;
        if (portalObstacle) portalObstacle.enabled = value;
    }

    void OnDisable()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
```

This intentionally blocks passage during the transition until the open pose is reached. The separate leaf collider must also move correctly. Check the open pose provides real body clearance before allowing the portal to switch off. On re-enabling this prototype component, it resets closed; persistent saved door states are later gameplay work. If you use the publisher's door system instead, connect its open/close events to equivalent collider/obstacle state changes; do not run both animation drivers.

## Appendix B — Four shootable boards, one passage blocker

Create two separate C# files under `Assets/Scripts/World`. The filenames must match the public class names. The first belongs on the barricade ROOT. The second belongs on each Board_01 through Board_04 empty, above its visual/collider.

### B1 — HospitalBarricade.cs

```csharp
using UnityEngine;
using UnityEngine.AI;

public sealed class HospitalBarricade : MonoBehaviour
{
    public HospitalPlank[] planks;
    public Collider passageBlocker;
    public NavMeshObstacle obstacle;
    public bool IsCleared { get; private set; }

    void Start()
    {
        if (planks == null || planks.Length == 0 || !passageBlocker)
            Debug.LogError("Assign all four planks and the passage blocker.", this);
    }

    public void Recheck()
    {
        if (IsCleared || planks == null || planks.Length == 0) return;
        foreach (var plank in planks)
            if (!plank || !plank.IsBroken) return;

        IsCleared = true;
        if (passageBlocker) passageBlocker.enabled = false;
        if (obstacle) obstacle.enabled = false;
        Debug.Log("Barricade cleared: " + name, this);
    }
}
```

### B2 — HospitalPlank.cs

```csharp
using UnityEngine;

public sealed class HospitalPlank : MonoBehaviour
{
    [Min(1)] public float maximumHealth = 50;
    public HospitalBarricade owner;
    public bool IsBroken { get; private set; }
    float health;

    void Awake()
    {
        health = maximumHealth;
        if (!owner) owner = GetComponentInParent<HospitalBarricade>();
    }

    public void TakeDamage(float amount)
    {
        if (IsBroken || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        health -= amount;
        if (health > 0) return;
        IsBroken = true;
        if (owner) owner.Recheck();
        gameObject.SetActive(false);
    }
}
```

In HospitalBarricade's Inspector set Planks array Size = 4, drag Board_01 through Board_04 into its four slots, and assign PassageBlocker. Assign the root's NavMeshObstacle once added. Ensure Board_04's visual and collider are both below Board_04 so one disabling action removes both.

The gun developer should perform a nearest-hit raycast including solid world geometry, then call `hit.collider.GetComponentInParent<HospitalPlank>()?.TakeDamage(damage)` only when a plank is actually hit. Do not broadcast damage to every board in range. Exclude the invisible MovementOnly blocker from the shot mask. Health/state persistence, real gun ammunition, sound events and fragment effects are separate work.

## Appendix C — Optional temporary WASD/crouch/interaction tester

Use this only if you do not already have a player controller. It uses the Input System package already present in this project. It offers WASD, mouse look, left Ctrl to crouch, E to toggle the test door and left click for a 25-damage test ray. There is no weapon model, ammunition, recoil, jump, health or final combat logic.

### C1 — Set it up manually

1. Create `Assets/Scripts/Tests/HospitalStage2Tester.cs` with the code below.
2. In Hierarchy create an EMPTY `TEST_Player` at `(0,0,2)`, Rotation zero, Scale one. Set its layer to Player.
3. Add CharacterController: Height 1.8, Radius 0.25, Center `(0,0.9,0)`, Skin Width 0.025, Step Offset 0.15, Min Move Distance 0. No Rigidbody.
4. Create a Camera child at local `(0,1.6,0)`, Rotation zero, Scale one. Temporarily disable the old Main Camera so only one camera/Audio Listener is active.
5. Add HospitalStage2Tester to TEST_Player and assign the Camera to View.
6. Set World Mask to actual solid environment layers, including Default and MovementOnly, but excluding Player, EnemyOnlyBlocker and trigger-only layers. This mask is for the stand-up collision test; it must include headers and door blockers.
7. Set Ray Mask to actual visible/solid world and interactable layers, including Default and your board/door layers, excluding Player, MovementOnly and EnemyOnlyBlocker. It must include walls, so E/shots cannot work through them.
8. Make sure the Player layer collides with Default and MovementOnly in the Physics matrix. EnemyOnlyBlocker must not collide with Player.
9. Your project must use Input System Package (New) or Both under Player settings > Active Input Handling for this tester. The package is installed; inspect the setting before changing it, and coordinate with teammates if they use another controller.
10. Enter Play mode. Click the Game view to capture the cursor. Aim using the centre of the screen (there is no crosshair UI). Use E on a door, click on a board. Press Esc to release the cursor.
11. After testing, stop Play mode, remove TEST_Player and restore the intended Main Camera. The tester's infinite test hits should not become the game's final weapon system accidentally.

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class HospitalStage2Tester : MonoBehaviour
{
    public Camera view;
    public LayerMask worldMask;
    public LayerMask rayMask;
    public float mouseSensitivity = 0.1f;
    CharacterController body;
    float pitch;
    float verticalSpeed;
    bool crouched;

    void Start()
    {
        body = GetComponent<CharacterController>();
        if (!view || worldMask.value == 0 || rayMask.value == 0)
        {
            Debug.LogError("Assign the camera and both masks before testing.", this);
            enabled = false;
        }
    }

    void Update()
    {
        var keys = Keyboard.current;
        var mouse = Mouse.current;
        if (keys == null || mouse == null) return;
        if (keys.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
        transform.Rotate(0, look.x, 0);
        pitch = Mathf.Clamp(pitch - look.y, -85, 85);
        view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);

        if (keys.leftCtrlKey.isPressed) crouched = true;
        else if (crouched && CanStand()) crouched = false;
        float height = crouched ? 0.9f : 1.8f;
        body.height = height;
        body.center = new Vector3(0, height / 2, 0);
        view.transform.localPosition = new Vector3(0, crouched ? 0.75f : 1.6f, 0);

        Vector2 input = new Vector2(
            (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
            (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
        input = Vector2.ClampMagnitude(input, 1);
        Vector3 movement = (transform.right * input.x + transform.forward * input.y)
            * (crouched ? 1.2f : 2.5f);
        if (body.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
        verticalSpeed += -9.81f * Time.deltaTime;
        movement.y = verticalSpeed;
        body.Move(movement * Time.deltaTime);

        Ray ray = new Ray(view.transform.position, view.transform.forward);
        if (keys.eKey.wasPressedThisFrame && Physics.Raycast(ray, out RaycastHit useHit,
            2f, rayMask, QueryTriggerInteraction.Ignore))
        {
            var door = useHit.collider.GetComponentInParent<HospitalClipDoor>();
            if (door) door.Toggle();
        }
        if (mouse.leftButton.wasPressedThisFrame && Physics.Raycast(ray, out RaycastHit shotHit,
            30f, rayMask, QueryTriggerInteraction.Ignore))
        {
            var plank = shotHit.collider.GetComponentInParent<HospitalPlank>();
            if (plank) plank.TakeDamage(25);
        }
    }

    bool CanStand()
    {
        Vector3 bottom = transform.position + Vector3.up * (body.radius + 0.05f);
        Vector3 top = transform.position + Vector3.up * (1.8f - body.radius);
        return !Physics.CheckCapsule(bottom, top, body.radius,
            worldMask, QueryTriggerInteraction.Ignore);
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
```

Create all four referenced script files before expecting the tester to compile. Check the Console for errors after copying. The code above is a supplied prototype, not a claim that your game now has these features installed.

## Final acceptance and handoff

- [ ] W070_Solid01 X corrected to -6.75; W042 name cleaned up.
- [ ] One downloaded door's actual animation, pivots, dimensions and URP material tested before duplication.
- [ ] 12 ordinary openable doors and 8 boarded openings placed; special entrance/elevator separate.
- [ ] 18 opaque static windows fitted without changing wall openings.
- [ ] Four vent gaps are 0.9 x 1.1 m; disabled original wall colliders are not blocking them.
- [ ] Crouch changes the body and camera; stand-up checks avoid clipping into headers.
- [ ] Enemy physical exclusion and enemy NavMesh exclusion are both configured.
- [ ] Boards take damage individually; final break removes the full passage blocker and obstacle.
- [ ] Underlying navigation baked through potential open routes; dynamic barriers change runtime availability.
- [ ] Door motion, blockers and repathing tested in all states; no routes through windows or hiding pockets.
- [ ] Progression resource access cannot be made impossible by boarded rooms or the thief.
- [ ] Download licenses/credits recorded; imported assets and their .meta files included in team handoff.

## Technical references

- [Unity NavMesh obstacles and carving](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AboutObstacles.html): runtime obstruction behavior and updates.
- [Unity AnimationClipPlayable](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Animations.AnimationClipPlayable.html) and [PlayableGraph.Evaluate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Playables.PlayableGraph.Evaluate.html): the supplied door prototype evaluates the existing clip interval directly.
- [Unity Physics.CheckCapsule](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics.CheckCapsule.html): the prototype's stand-up clearance query.
- Creator model metadata checked through the public Sketchfab model records: [hospital door](https://api.sketchfab.com/v3/models/15b147ae76ef40da9ad23831ca7225dd) and [old window](https://api.sketchfab.com/v3/models/771cc689144f46ae84186ddc7fae4b90). Exact Unity import bounds and clip contents remain to be checked from the actual downloaded files.
