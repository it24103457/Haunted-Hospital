# Haunted Hospital — complete beginner’s skeleton-building guide

**For Unity 6000.6.2f1 • one playable floor • metres • manual Cube construction**

Build this guide one numbered step at a time. It includes the entire sequence so you can work at your own pace; you do not need to ask for “next” after each step.

## What this guide creates

A plain architectural blockout: floor slabs, the hospital’s rooms and connected corridors, a central-right elevator enclosure, a closed staircase footprint, walls, real door and window openings, simple door placeholders, window collision panes, optional ceilings, and instructions for baking/testing an initial NavMesh. No furniture, clue objects, fixed key locations, item spawns, ghost/thief behavior, horror effects, materials or final lighting.

Your later game loop is **find four digits → enter the code → a key spawns in an accessible random location → take it to unlock the elevator → escape**. The image’s numbered circles and key icon are annotations, not objects to build. The elevator is the exit; no playable upper floor is needed.

## What was checked in your project

The saved scene `Assets/Scenes/Main/MainHospital.unity` contains the seven floor pieces and identity-transform containers from your message. `F_RightLowerCorridor` is absent. `ProjectSettings/ProjectVersion.txt` specifies **6000.6.2f1**. `Packages/manifest.json` includes **AI Navigation 2.0.14** and URP. The saved hospital scene contains no NavMesh Surface, Agent, Obstacle, Link or Modifier components. Its serialized NavMeshSettings are scene settings, not evidence of a baked surface. The current project’s default agent has radius 0.5 m and height 2 m.

This is a read of saved project files, not a verification of unsaved edits in the open Editor. The instructions below do not mean the hospital or its NavMesh has already been built or tested in Unity.

The accompanying coordinate schedule was checked for overlapping floor areas, doorway-to-room adjacency and 2D connectivity with a conservative 0.35 m clearance radius on a 0.05 m grid. Every accessible floor-piece centre and both sides of each open interior doorway were reachable from the entrance in that geometric check. This is not a substitute for the Unity bake and movement tests near the end of the guide.

## Important: which blueprint measurements this guide follows

You chose **“Match the drawing’s proportions.”** Accordingly, the coordinates below are a **manually traced, grid-snapped interpretation of the picture**, not a reconstruction of its conflicting printed metre dimensions. Room positions, the stepped outline, major connections and central-right elevator are preserved. Wall details hidden by symbols and small service pockets are simplified for a Cube blockout. Door/window placements are approximate readings of the symbols, not survey measurements.

The room footprint runs from **X = -16.75 to 16** and **Z = 0 to 42**: about **32.75 × 42 m before the outside wall thickness**. Wall faces extend 0.1 m beyond exterior boundary lines. This differs from the image’s printed 28 × 42 m because fitting both those numbers would squash the drawing horizontally. The traced main corridor varies in width; it does not universally retain the printed 3 m label. The 0.25 m construction grid is intentionally approximate. Exact numbers below make the pieces meet each other; they do not make the source image dimensionally exact.

These are **floor boundary / wall-centreline dimensions**, not clear internal room sizes. A 0.2 m wall centred on a boundary occupies 0.1 m on each side. Typical clear single openings are 1.2 m in this traced blockout, the Operating opening is 1.5 m, and the entrance/elevator openings are 1.8 m. Door height is 2.2 m; windows open from Y = 1 to 2.2 m. Heights are blockout choices; the top-view image cannot establish them. Walls are 3 m high.

The original seven coordinates used the printed sizes, so **do not combine them with these traced coordinates**. First save a separate scene, then replace their transforms in the seven edit steps. The original elevator left only 0.5 m between its south edge and Recovery’s north edge. Here a distinct 2 m-deep lobby serves the elevator, with 1.8 m clear depth between opposing 0.2 m walls. The narrow strip east of the shaft is enclosed service space, not another corridor.

## Contents

1. Save a separate scene and learn the controls.
2. Correct the existing seven floor pieces and finish all floors.
3. Check the full floor layout before building walls.
4. Build wall segments with door and window openings already left empty.
5. Add plain door placeholders and window collision panes/dividers.
6. Add ceilings after inspection, then bake and validate navigation.
7. Give the team the scene, navigation data and world conventions.

![Coordinate plan for this guide](Hospital_Skeleton_Coordinate_Plan.png)

The plan above is the coordinate interpretation used by this guide. Compare it with your supplied blueprint before investing time in walls. It is not a replacement source blueprint.


## Step 1 — Open and save a separate blockout scene

1. Open this existing project in Unity Hub using **6000.6.2f1**. Do not create a different project.
2. In Unity, make sure the **Play** triangle at the top is not active. Click it to exit Play mode if necessary. Edits made during Play mode may be lost when you stop.
3. In the bottom **Project** window, open **Assets > Scenes > Main**.
4. Double-click **MainHospital**. If asked about unrelated unsaved work, save that work before changing scenes.
5. Choose **File > Save As…**. Save beside it as **Hospital_Blockout.unity**.
6. Check the open scene’s name in the Hierarchy. Work in **Hospital_Blockout**, keeping **MainHospital.unity** as the original.
7. Press **Ctrl + S** regularly. This saves the scene; it does not build a game executable.

If starting with an empty scene instead, use this same saved scene name and create the seven floor objects when their steps say “select the existing object.” Keep the default Main Camera and Directional Light for now; neither needs repositioning to construct geometry.


## Step 2 — Find the three windows you will use

- **Hierarchy:** the objects in this open scene, usually at the left. Select and rename objects here.
- **Scene:** the editor’s working view. Build here; the Game tab is the camera output and may currently look empty.
- **Inspector:** properties of the selected object, usually at the right. **Transform** contains Position, Rotation and Scale.
- **Project:** files and folders, usually at the bottom. A scene object in Hierarchy is different from an asset in Project.

If a panel is missing, use **Window > General > Hierarchy / Inspector / Scene / Project**. An Inspector padlock can pin the wrong object; unlock it if selecting a new object does not update its properties. Select one object at a time when entering coordinates. Unity has no Apply button for ordinary scene Transform edits: type the number and press Enter or Tab.


## Step 3 — Set a top-down working view

1. Click the **Scene** tab.
2. Keep **2D mode off**. This is a 3D project viewed from above; 2D mode is not the same as Top view.
3. Find the little axis gizmo at the upper-right of the Scene view. Click its **positive Y** direction to look down from above.
4. Toggle the centre cube / perspective label if necessary until the view is orthographic (**Top / Iso**, not perspective).
5. Select **MainHospital** and press **F** while the pointer is over the Scene view to frame the selection.
6. Use the mouse wheel to zoom. Middle-mouse drag pans. Avoid dragging object handles while checking the layout.
7. North/up on the plan is **+Z**, east/right is **+X**, and height is **Y**. Check the axis gizmo if the view is rotated.

If the scene is too dark, use the Scene view lighting toggle to use the editor’s preview illumination. This does not add or bake game lights.


## Step 4 — Prepare the parent objects

Your existing containers can be reused. Expand **MainHospital** using its small triangle. If one is missing, right-click the parent and choose **Create Empty**, then press **F2** to rename it.

```text
MainHospital
├── GEO_Floors
├── GEO_Walls
├── GEO_Ceilings
├── GEO_Doors
├── GEO_Windows       ← add this empty container
├── GameplayMarkers  ← leave empty
├── Props            ← leave empty
├── Lights
└── Navigation
```

Select **MainHospital**, then each container individually. Set **Position (0, 0, 0)**, **Rotation (0, 0, 0)**, **Scale (1, 1, 1)**. Do not scale a container to resize the hospital. Every coordinate in this guide relies on identity-transform parents.

Do not create duplicate containers. Do not move the existing floor cubes out of GEO_Floors. Do not recreate the deleted F_RightLowerCorridor. If your open scene differs from the saved state, use the object names and these final transforms to reconcile it before proceeding.


## Step 5 — Learn the Cube procedure before entering the first floor

For every **new** Cube in this guide:

1. Right-click its specified parent **in the Hierarchy**.
2. Select **3D Object > Cube**. If it appears at the scene root, drag its Hierarchy row onto the named parent. Do this **before** entering the numbers.
3. Press **F2**, type the exact new name and press Enter.
4. In **Inspector > Transform**, expand the component if necessary.
5. Enter all three **Position** fields, all three **Rotation** fields and all three **Scale** fields.
6. Confirm the parent’s Transform is identity. The Inspector values are local to the parent; with identity parents they also match the intended world coordinates.
7. Leave the Cube’s **Box Collider** enabled and **Is Trigger** off. Do not add a Rigidbody.
8. Press **Ctrl + S**.

For an **existing** Cube, select it and replace its Transform; do not duplicate it. Use Inspector values, not dragging or visual snapping. A standard Cube is 1 m on every side, so its Scale is its resulting size in metres when its parents have unit scale.

All floors use Position Y = -0.1 and Scale Y = 0.2, placing their upper surface at Y = 0. The following arithmetic is used throughout:

```text
Position X = (west edge + east edge) / 2
Scale X    = east edge - west edge
Position Z = (south edge + north edge) / 2
Scale Z    = north edge - south edge
```

The ASCII diagrams are orientation aids, not scale drawings. The edge numbers are the actual checks.


# Part 1 — Floors only


## Step 6 — F_MainCorridor

Select the existing **F_MainCorridor** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_MainCorridor
```

In **Inspector > Transform**, enter:

```text
Position
X = 0.625
Y = -0.1
Z = 4.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.25
Y = 0.2
Z = 9.5
```

The south end of the main spine; the entrance is at its south edge.

**Edge check:** X = -1.5 to 2.75; Z = 0 to 9.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Corridor <-- [ THIS PIECE ] --> Recovery
                            |
                         Outside
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 7 — F_PostOpRecovery

Select the existing **F_PostOpRecovery** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_PostOpRecovery
```

In **Inspector > Transform**, enter:

```text
Position
X = -8.25
Y = -0.1
Z = 3.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 10
Y = 0.2
Z = 7.25
```

Bottom-left room. The entrance apron is immediately to its east.

**Edge check:** X = -13.25 to -3.25; Z = 0 to 7.25.

```text
                         +Z / NORTH
                         General Ward
                            |
Outside <-- [ THIS PIECE ] --> Corridor
                            |
                         Outside
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 8 — F_RecoveryRoom

Select the existing **F_RecoveryRoom** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_RecoveryRoom
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.625
Y = -0.1
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.75
Y = 0.2
Z = 7
```

Bottom-right, immediately east of the main spine.

**Edge check:** X = 2.75 to 10.5; Z = 0 to 7.

```text
                         +Z / NORTH
                         Corridor
                            |
Corridor <-- [ THIS PIECE ] --> Waste
                            |
                         Outside
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 9 — F_WasteDisposal

Select the existing **F_WasteDisposal** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_WasteDisposal
```

In **Inspector > Transform**, enter:

```text
Position
X = 13.25
Y = -0.1
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.5
Y = 0.2
Z = 7
```

Immediately east of Recovery. Its south and north edges align with Recovery.

**Edge check:** X = 10.5 to 16; Z = 0 to 7.

```text
                         +Z / NORTH
                         Staff room
                            |
Recovery <-- [ THIS PIECE ] --> Outside
                            |
                         Outside
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 10 — F_StaffRoom

Select the existing **F_StaffRoom** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_StaffRoom
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.75
Y = -0.1
Z = 9.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 6.5
Y = 0.2
Z = 5.5
```

Above Waste, to the right of the elevator lobby. This is the lower Staff Room, not the small top Staff utility room.

**Edge check:** X = 9.5 to 16; Z = 7 to 12.5.

```text
                         +Z / NORTH
                         Storage
                            |
Service shaft <-- [ THIS PIECE ] --> Outside
                            |
                         Waste
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 11 — F_Storage

Select the existing **F_Storage** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_Storage
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.125
Y = -0.1
Z = 15.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.75
Y = 0.2
Z = 5.5
```

Above Staff Room. The right edges align; Storage extends farther left, as in the drawing.

**Edge check:** X = 8.25 to 16; Z = 12.5 to 18.

```text
                         +Z / NORTH
                         Pre-op
                            |
Corridor <-- [ THIS PIECE ] --> Outside
                            |
                         Staff room
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 12 — F_Elevator

Select the existing **F_Elevator** in the Hierarchy. If building from an empty scene instead, create a Cube with this name. Do not create a second copy.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_Elevator
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.5
Y = -0.1
Z = 11.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.5
Y = 0.2
Z = 5.75
```

Central-right shaft footprint. Its entrance faces south into the elevator lobby.

**Edge check:** X = 2.75 to 8.25; Z = 9 to 14.75.

```text
                         +Z / NORTH
                         Corridor
                            |
Corridor <-- [ THIS PIECE ] --> Service shaft
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 13 — F_EntranceApron

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_EntranceApron
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.375
Y = -0.1
Z = 3.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.75
Y = 0.2
Z = 7.25
```

Fills the strip between Post-op and the lower main corridor.

**Edge check:** X = -3.25 to -1.5; Z = 0 to 7.25.

```text
                         +Z / NORTH
                         Corridor
                            |
Post-op <-- [ THIS PIECE ] --> Corridor
                            |
                         Outside
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 14 — F_ElevatorLobby

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_ElevatorLobby
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.125
Y = -0.1
Z = 8

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 6.75
Y = 0.2
Z = 2
```

Connects the main corridor, Recovery, elevator entrance and lower Staff Room. This replaces the old conflicting corridor concept with an explicitly bounded lobby.

**Edge check:** X = 2.75 to 9.5; Z = 7 to 9.

```text
                         +Z / NORTH
                         Elevator
                            |
Corridor <-- [ THIS PIECE ] --> Staff room
                            |
                         Recovery
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 15 — F_ElevatorServiceStrip

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_ElevatorServiceStrip
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.875
Y = -0.1
Z = 10.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.25
Y = 0.2
Z = 3.5
```

The narrow strip beside the elevator is enclosed service space, not a through-corridor.

**Edge check:** X = 8.25 to 9.5; Z = 9 to 12.5.

```text
                         +Z / NORTH
                         Storage
                            |
Elevator <-- [ THIS PIECE ] --> Staff room
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 16 — F_WestLowerHall

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_WestLowerHall
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.125
Y = -0.1
Z = 8.375

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.25
Y = 0.2
Z = 2.25
```

Passage below the Office and above Post-op; joins the main spine on its east edge.

**Edge check:** X = -6.75 to -1.5; Z = 7.25 to 9.5.

```text
                         +Z / NORTH
                         Nurse Office
                            |
General Ward <-- [ THIS PIECE ] --> Corridor
                            |
                         Post-op
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 17 — F_GeneralWard

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_GeneralWard
```

In **Inspector > Transform**, enter:

```text
Position
X = -11.25
Y = -0.1
Z = 11.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 9
Y = 0.2
Z = 9.25
```

Left of the Office and west lower hall; below the west cross-corridor.

**Edge check:** X = -15.75 to -6.75; Z = 7.25 to 16.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Outside <-- [ THIS PIECE ] --> Nurse Office
                            |
                         Post-op
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 18 — F_NurseOffice

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_NurseOffice
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.75
Y = -0.1
Z = 13

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 6
Y = 0.2
Z = 7
```

Immediately right of the General Ward. Its east side makes the main passage narrower here.

**Edge check:** X = -6.75 to -0.75; Z = 9.5 to 16.5.

```text
                         +Z / NORTH
                         Corridor
                            |
General Ward <-- [ THIS PIECE ] --> Corridor
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 19 — F_MainCorridor_Office

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_MainCorridor_Office
```

In **Inspector > Transform**, enter:

```text
Position
X = 1
Y = -0.1
Z = 13

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.5
Y = 0.2
Z = 7
```

Main passage alongside the Office. Do not extend the previous corridor cube through the Office.

**Edge check:** X = -0.75 to 2.75; Z = 9.5 to 16.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Nurse Office <-- [ THIS PIECE ] --> Elevator
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 20 — F_MainCorridor_Middle

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_MainCorridor_Middle
```

In **Inspector > Transform**, enter:

```text
Position
X = 0.625
Y = -0.1
Z = 20

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.25
Y = 0.2
Z = 7
```

Connects the Office segment to the wider upper corridor.

**Edge check:** X = -1.5 to 2.75; Z = 16.5 to 23.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Washroom alcove <-- [ THIS PIECE ] --> Stair enclosure
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 21 — F_WestCrossCorridor

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_WestCrossCorridor
```

In **Inspector > Transform**, enter:

```text
Position
X = -8.625
Y = -0.1
Z = 17.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 14.25
Y = 0.2
Z = 2.75
```

The long horizontal corridor between General Ward/Office and Anaesthesia Prep.

**Edge check:** X = -15.75 to -1.5; Z = 16.5 to 19.25.

```text
                         +Z / NORTH
                         Anaesthesia
                            |
Outside <-- [ THIS PIECE ] --> Corridor
                            |
                         General Ward
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 22 — F_RightLanding

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_RightLanding
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.5
Y = -0.1
Z = 16

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.5
Y = 0.2
Z = 2.5
```

Above the elevator and below the stair footprint; leads to Storage.

**Edge check:** X = 2.75 to 8.25; Z = 14.75 to 17.25.

```text
                         +Z / NORTH
                         Stair enclosure
                            |
Corridor <-- [ THIS PIECE ] --> Storage
                            |
                         Elevator
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 23 — F_StairFootprint

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_StairFootprint
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.375
Y = -0.1
Z = 20.375

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.25
Y = 0.2
Z = 6.25
```

Preserves the staircase location. This prototype encloses it; do not build an upper floor or traversable stairs.

**Edge check:** X = 2.75 to 6; Z = 17.25 to 23.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Corridor <-- [ THIS PIECE ] --> Corridor
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 24 — F_PreOpApproach

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_PreOpApproach
```

In **Inspector > Transform**, enter:

```text
Position
X = 7.125
Y = -0.1
Z = 20.375

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.25
Y = 0.2
Z = 6.25
```

Passage east of the enclosed stairs. Joins the right landing and the Pre-op entrance.

**Edge check:** X = 6 to 8.25; Z = 17.25 to 23.5.

```text
                         +Z / NORTH
                         Sterile Core
                            |
Stair enclosure <-- [ THIS PIECE ] --> Pre-op
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 25 — F_PreOp

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_PreOp
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.125
Y = -0.1
Z = 21

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.75
Y = 0.2
Z = 6
```

Above Storage and below Sterile Core.

**Edge check:** X = 8.25 to 16; Z = 18 to 24.

```text
                         +Z / NORTH
                         Sterile Core
                            |
Corridor <-- [ THIS PIECE ] --> Outside
                            |
                         Storage
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 26 — F_AnaesthesiaPrep

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_AnaesthesiaPrep
```

In **Inspector > Transform**, enter:

```text
Position
X = -11.75
Y = -0.1
Z = 22.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 10
Y = 0.2
Z = 7.25
```

Above the west cross-corridor and below Operating Room 1.

**Edge check:** X = -16.75 to -6.75; Z = 19.25 to 26.5.

```text
                         +Z / NORTH
                         Operating Room
                            |
Outside <-- [ THIS PIECE ] --> Washroom alcove
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 27 — F_WashroomAlcove

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_WashroomAlcove
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.125
Y = -0.1
Z = 21.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.25
Y = 0.2
Z = 5.25
```

The unlabeled washroom/service area drawn east of Anaesthesia. No fixtures are added.

**Edge check:** X = -6.75 to -1.5; Z = 19.25 to 24.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Anaesthesia <-- [ THIS PIECE ] --> Corridor
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 28 — F_OperatingApproach

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingApproach
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.125
Y = -0.1
Z = 25.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.25
Y = 0.2
Z = 2
```

Short east-west connector between Anaesthesia, the washroom alcove and the main spine.

**Edge check:** X = -6.75 to -1.5; Z = 24.5 to 26.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Anaesthesia <-- [ THIS PIECE ] --> Corridor
                            |
                         Washroom alcove
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 29 — F_OperatingDoorRecess

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingDoorRecess
```

In **Inspector > Transform**, enter:

```text
Position
X = -5
Y = -0.1
Z = 27

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.5
Y = 0.2
Z = 1
```

The northward recess at Operating Room 1's doorway.

**Edge check:** X = -6.75 to -3.25; Z = 26.5 to 27.5.

```text
                         +Z / NORTH
                         Operating Room
                            |
Operating Room <-- [ THIS PIECE ] --> Corridor
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 30 — F_OperatingApproachReturn

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingApproachReturn
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.375
Y = -0.1
Z = 27

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.75
Y = 0.2
Z = 1
```

Completes the connector beside the Operating doorway recess.

**Edge check:** X = -3.25 to -1.5; Z = 26.5 to 27.5.

```text
                         +Z / NORTH
                         Operating service shaft
                            |
Corridor <-- [ THIS PIECE ] --> Corridor
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 31 — F_MainCorridor_Upper

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_MainCorridor_Upper
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.5
Y = -0.1
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 6
Y = 0.2
Z = 8
```

Wider central passage west of Sterile Core.

**Edge check:** X = -1.5 to 4.5; Z = 23.5 to 31.5.

```text
                         +Z / NORTH
                         Corridor
                            |
Outside <-- [ THIS PIECE ] --> Sterile Core
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 32 — F_SterileCore

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_SterileCore
```

In **Inspector > Transform**, enter:

```text
Position
X = 10.25
Y = -0.1
Z = 27.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 11.5
Y = 0.2
Z = 7.5
```

Below ICU/Diagnostics and above Pre-op.

**Edge check:** X = 4.5 to 16; Z = 24 to 31.5.

```text
                         +Z / NORTH
                         Diagnostics
                            |
Corridor <-- [ THIS PIECE ] --> Outside
                            |
                         Pre-op
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 33 — F_SterileCore_SouthNotch

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_SterileCore_SouthNotch
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.375
Y = -0.1
Z = 23.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.75
Y = 0.2
Z = 0.5
```

Small extension at Sterile Core's southwest corner. This is part of the same room; do not wall its seam.

**Edge check:** X = 4.5 to 8.25; Z = 23.5 to 24.

```text
                         +Z / NORTH
                         Sterile Core
                            |
Corridor <-- [ THIS PIECE ] --> Pre-op
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 34 — F_OperatingRoom1

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingRoom1
```

In **Inspector > Transform**, enter:

```text
Position
X = -11.25
Y = -0.1
Z = 31.125

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 9
Y = 0.2
Z = 9.25
```

Main west portion of Operating Room 1.

**Edge check:** X = -15.75 to -6.75; Z = 26.5 to 35.75.

```text
                         +Z / NORTH
                         Outside
                            |
Outside <-- [ THIS PIECE ] --> Operating Room
                            |
                         Anaesthesia
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 35 — F_OperatingRoom1_East

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingRoom1_East
```

In **Inspector > Transform**, enter:

```text
Position
X = -5
Y = -0.1
Z = 31.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.5
Y = 0.2
Z = 8.25
```

East portion of the same Operating Room, above its doorway recess.

**Edge check:** X = -6.75 to -3.25; Z = 27.5 to 35.75.

```text
                         +Z / NORTH
                         Operating Room
                            |
Operating Room <-- [ THIS PIECE ] --> Operating service shaft
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 36 — F_OperatingRoom1_North

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingRoom1_North
```

In **Inspector > Transform**, enter:

```text
Position
X = -7.375
Y = -0.1
Z = 36.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.25
Y = 0.2
Z = 1
```

Raised outline on the north edge of Operating Room 1. All three slabs form one continuous room.

**Edge check:** X = -10 to -4.75; Z = 35.75 to 36.75.

```text
                         +Z / NORTH
                         Outside
                            |
Outside <-- [ THIS PIECE ] --> North service alcove
                            |
                         Operating Room
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 37 — F_OperatingServiceShaft

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_OperatingServiceShaft
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.375
Y = -0.1
Z = 31.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.75
Y = 0.2
Z = 8.25
```

Simplified enclosed equipment/shaft strip between Operating Room and the main corridor.

**Edge check:** X = -3.25 to -1.5; Z = 27.5 to 35.75.

```text
                         +Z / NORTH
                         Corridor
                            |
Operating Room <-- [ THIS PIECE ] --> Corridor
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 38 — F_MainCorridor_North

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_MainCorridor_North
```

In **Inspector > Transform**, enter:

```text
Position
X = 0.125
Y = -0.1
Z = 33.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.25
Y = 0.2
Z = 4.25
```

Northward corridor leading toward WC and upper Staff utility room.

**Edge check:** X = -1.5 to 1.75; Z = 31.5 to 35.75.

```text
                         +Z / NORTH
                         Corridor
                            |
Operating service shaft <-- [ THIS PIECE ] --> ICU
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 39 — F_NorthLobby

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_NorthLobby
```

In **Inspector > Transform**, enter:

```text
Position
X = -0.75
Y = -0.1
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5
Y = 0.2
Z = 2
```

Wider top lobby directly below WC and the small Staff room.

**Edge check:** X = -3.25 to 1.75; Z = 35.75 to 37.75.

```text
                         +Z / NORTH
                         WC
                            |
North service alcove <-- [ THIS PIECE ] --> ICU
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 40 — F_NorthServiceAlcove

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_NorthServiceAlcove
```

In **Inspector > Transform**, enter:

```text
Position
X = -4
Y = -0.1
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 0.2
Z = 2
```

Small enclosed service recess west of the north lobby. Treat its ambiguous interior symbols as a closed footprint in this first blockout.

**Edge check:** X = -4.75 to -3.25; Z = 35.75 to 37.75.

```text
                         +Z / NORTH
                         WC
                            |
Outside <-- [ THIS PIECE ] --> Corridor
                            |
                         Operating Room
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 41 — F_WC

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_WC
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.5
Y = -0.1
Z = 39.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.5
Y = 0.2
Z = 4.25
```

Small top-left WC; its door faces the north lobby.

**Edge check:** X = -4.75 to -0.25; Z = 37.75 to 42.

```text
                         +Z / NORTH
                         Outside
                            |
Outside <-- [ THIS PIECE ] --> Staff utility
                            |
                         Corridor
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 42 — F_StaffUtility

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_StaffUtility
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.875
Y = -0.1
Z = 39.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.25
Y = 0.2
Z = 4.25
```

Small top Staff utility room, distinct from the large lower Staff Room.

**Edge check:** X = -0.25 to 4; Z = 37.75 to 42.

```text
                         +Z / NORTH
                         Outside
                            |
WC <-- [ THIS PIECE ] --> ICU
                            |
                         ICU
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 43 — F_ICUTreatment

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_ICUTreatment
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.375
Y = -0.1
Z = 34.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.25
Y = 0.2
Z = 6.25
```

ICU lower portion, above Sterile Core and left of Diagnostics.

**Edge check:** X = 1.75 to 9; Z = 31.5 to 37.75.

```text
                         +Z / NORTH
                         ICU
                            |
Corridor <-- [ THIS PIECE ] --> Diagnostics
                            |
                         Sterile Core
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 44 — F_ICUTreatment_North

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_ICUTreatment_North
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.5
Y = -0.1
Z = 39.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5
Y = 0.2
Z = 4.25
```

ICU upper extension beside the Staff utility room. No wall across the seam.

**Edge check:** X = 4 to 9; Z = 37.75 to 42.

```text
                         +Z / NORTH
                         Outside
                            |
Staff utility <-- [ THIS PIECE ] --> Outside
                            |
                         ICU
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 45 — F_DiagnosticsTests

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Floors
```

Rename the Cube:
```text
F_DiagnosticsTests
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.5
Y = -0.1
Z = 35

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7
Y = 0.2
Z = 7
```

Top-right room. Its north edge is lower than ICU's, matching the stepped silhouette.

**Edge check:** X = 9 to 16; Z = 31.5 to 38.5.

```text
                         +Z / NORTH
                         Outside
                            |
ICU <-- [ THIS PIECE ] --> Outside
                            |
                         Sterile Core
                         -Z / SOUTH
```

Neighbour labels describe the final layout at the midpoint of each edge; some neighbours are created in later steps.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 46 — Stop and check the entire floor plan

Do this before making the first wall. Do not skip this checkpoint merely because all coordinates have been typed.

1. Select MainHospital and press F in Top view. Compare against both the source image and the coordinate plan at the beginning of this guide.
2. Confirm all slabs are flat at Y = 0 on top. If one looks raised, check Position Y and Scale Y.
3. Confirm the entrance is south; Post-op is southwest; Recovery/Waste are southeast; Elevator is central-right; Storage is above Staff; Pre-op is above Storage; Sterile Core is above Pre-op; ICU and Diagnostics are at the top-right.
4. Confirm Ward is west of Office, the west cross-corridor is above both, Anaesthesia is above that corridor, and Operating Room is above Anaesthesia.
5. Look for floor gaps by zooming into each seam. The shared boundaries in this guide are exact. A line in the editor grid is not necessarily a physical crack.
6. No two floor rectangles in the schedule overlap in area. Pieces of one room deliberately meet edge-to-edge.
7. Check the continuous route: entrance → main spine → upper corridor → north lobby. Check the west cross-corridor, lower west hall, right landing and Pre-op approach too.
8. Save. This is the point to review the footprint with your team before continuing into walls.

**Specific junctions:** Recovery ends at Z = 7 and elevator lobby begins at Z = 7. Elevator lobby ends at Z = 9 and Elevator begins at Z = 9. Elevator ends at Z = 14.75 and right landing begins there. Storage starts at X = 8.25 and its landing ends there. Ward ends at Z = 16.5 and west cross-corridor starts there. Anaesthesia starts at Z = 19.25 where the cross-corridor ends.

If the traced interpretation needs changing, change the shared boundary schedule and all dependent walls together. Do not silently substitute one printed room size into the middle of this coordinate set.


# Part 2 — Walls with real openings


## Step 47 — Understand how these walls are built

A Cube cannot have a doorway cut out just by putting a door model on top of it. Build separate wall pieces around empty openings.

All full-height wall cubes run from Y = 0 to 3: **Position Y = 1.5, Scale Y = 3**. All walls are 0.2 m thick. North/south walls use thickness in Z; east/west walls use thickness in X. Rotation stays zero for both orientations.

At a door, leave Y = 0 to 2.2 empty and place only a header from Y = 2.2 to 3. At a window, keep a sill from Y = 0 to 1, an opening from Y = 1 to 2.2, and a header above it.

```text
DOOR, front view             WINDOW, front view
 +--------header-------+     +--------header-------+  Y=3
 |                     |     |                     |
 +----+-----------+----+     +----+-----------+----+  Y=2.2
 |wall| EMPTY GAP |wall|     |wall| EMPTY GAP |wall|
 |    |           |    |     +----+---sill----+----+  Y=1
 +----+-----------+----+     +---------------------+  Y=0
```

The following wall runs cover each room boundary once. **Do not add a second wall on the other room’s side.** There are no walls between slabs that are parts of the same room or corridor. Run endpoints extend 0.1 m into neighbouring corner walls to close corners; this small intentional wall overlap is different from overlapping floor slabs. Opening edges do not get extended, preserving their clear width.

Build every listed segment. A window header without its sill leaves the wrong opening. A full wall substituted for split segments closes a planned doorway. Use the wall IDs on the coordinate-plan image to find the run.


### Wall run W001 — Outside / Post-op

Boundary: **Z = 0**, from **X = -13.25 to -3.25**.

- Leave **WIN_PostOp_South** clear from X = **-9.375 to -7.125**; width **2.25 m**. Window opening starts 1 m above the floor.


## Step 48 — W001_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W001_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -11.3625
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.975
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 49 — W001_WIN_PostOp_South_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W001_WIN_PostOp_South_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -8.25
Y = 2.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.25
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 50 — W001_WIN_PostOp_South_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W001_WIN_PostOp_South_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -8.25
Y = 0.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.25
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 51 — W001_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W001_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.1375
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.975
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W002 — Corridor / Outside

Boundary: **Z = 0**, from **X = -3.25 to 2.75**.

- Leave **D_MainEntrance** clear from X = **-0.9 to 0.9**; width **1.8 m**. Door opening starts at the floor.


## Step 52 — W002_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W002_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.125
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 53 — W002_D_MainEntrance_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W002_D_MainEntrance_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 0
Y = 2.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.8
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 54 — W002_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W002_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.875
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W003 — Outside / Recovery

Boundary: **Z = 0**, from **X = 2.75 to 10.5**.

- Leave **WIN_Recovery_South** clear from X = **5.15 to 7.65**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 55 — W003_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W003_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 3.9
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 56 — W003_WIN_Recovery_South_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W003_WIN_Recovery_South_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 2.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 57 — W003_WIN_Recovery_South_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W003_WIN_Recovery_South_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 0.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 58 — W003_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W003_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.125
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W004 — Outside / Waste

Boundary: **Z = 0**, from **X = 10.5 to 16**.

- Leave **WIN_Waste_South** clear from X = **12.5 to 14**; width **1.5 m**. Window opening starts 1 m above the floor.


## Step 59 — W004_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W004_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 11.45
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.1
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 60 — W004_WIN_Waste_South_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W004_WIN_Waste_South_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 13.25
Y = 2.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 61 — W004_WIN_Waste_South_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W004_WIN_Waste_South_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 13.25
Y = 0.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 62 — W004_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W004_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 15.05
Y = 1.5
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.1
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W005 — Outside / Post-op

Boundary: **X = -13.25**, from **Z = 0 to 7.25**.

- Leave **WIN_PostOp_West** clear from Z = **2.25 to 4.75**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 63 — W005_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W005_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -13.25
Y = 1.5
Z = 1.075

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.35
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 64 — W005_WIN_PostOp_West_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W005_WIN_PostOp_West_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -13.25
Y = 2.6
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 65 — W005_WIN_PostOp_West_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W005_WIN_PostOp_West_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -13.25
Y = 0.5
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 66 — W005_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W005_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -13.25
Y = 1.5
Z = 6.05

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.6
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W006 — Outside / Waste

Boundary: **X = 16**, from **Z = 0 to 7**.

This run has no opening.


## Step 67 — W006_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W006_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 7.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W007 — Outside / Staff room

Boundary: **X = 16**, from **Z = 7 to 12.5**.

- Leave **WIN_Staff_East** clear from Z = **8.85 to 10.35**; width **1.5 m**. Window opening starts 1 m above the floor.


## Step 68 — W007_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W007_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 7.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.95
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 69 — W007_WIN_Staff_East_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W007_WIN_Staff_East_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 2.6
Z = 9.6

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 70 — W007_WIN_Staff_East_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W007_WIN_Staff_East_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 0.5
Z = 9.6

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 1.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 71 — W007_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W007_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 11.475

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.25
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W008 — General Ward / Outside

Boundary: **Z = 7.25**, from **X = -15.75 to -13.25**.

This run has no opening.


## Step 72 — W008_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W008_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -14.5
Y = 1.5
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W009 — General Ward / Outside

Boundary: **X = -15.75**, from **Z = 7.25 to 16.5**.

- Leave **WIN_Ward_West** clear from Z = **10.5 to 13**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 73 — W009_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W009_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.5
Z = 8.825

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.35
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 74 — W009_WIN_Ward_West_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W009_WIN_Ward_West_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 2.6
Z = 11.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 75 — W009_WIN_Ward_West_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W009_WIN_Ward_West_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 0.5
Z = 11.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 76 — W009_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W009_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.5
Z = 14.8

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.6
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W010 — Outside / Storage

Boundary: **X = 16**, from **Z = 12.5 to 18**.

- Leave **WIN_Storage_East** clear from Z = **14.3 to 16.3**; width **2 m**. Window opening starts 1 m above the floor.


## Step 77 — W010_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W010_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 13.35

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.9
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 78 — W010_WIN_Storage_East_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W010_WIN_Storage_East_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 2.6
Z = 15.3

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 79 — W010_WIN_Storage_East_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W010_WIN_Storage_East_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 0.5
Z = 15.3

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 80 — W010_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W010_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 17.2

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.8
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W011 — Corridor / Outside

Boundary: **X = -15.75**, from **Z = 16.5 to 19.25**.

This run has no opening.


## Step 81 — W011_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W011_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.5
Z = 17.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.95
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W012 — Outside / Pre-op

Boundary: **X = 16**, from **Z = 18 to 24**.

- Leave **WIN_PreOp_East** clear from Z = **20.15 to 22.15**; width **2 m**. Window opening starts 1 m above the floor.


## Step 82 — W012_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W012_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 19.025

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.25
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 83 — W012_WIN_PreOp_East_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W012_WIN_PreOp_East_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 2.6
Z = 21.15

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 84 — W012_WIN_PreOp_East_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W012_WIN_PreOp_East_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 0.5
Z = 21.15

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 85 — W012_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W012_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 23.125

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.95
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W013 — Anaesthesia / Outside

Boundary: **Z = 19.25**, from **X = -16.75 to -15.75**.

This run has no opening.


## Step 86 — W013_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W013_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.25
Y = 1.5
Z = 19.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W014 — Anaesthesia / Outside

Boundary: **X = -16.75**, from **Z = 19.25 to 26.5**.

- Leave **WIN_Anaesthesia_West** clear from Z = **21.75 to 24.25**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 87 — W014_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W014_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.75
Y = 1.5
Z = 20.45

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.6
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 88 — W014_WIN_Anaesthesia_West_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W014_WIN_Anaesthesia_West_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.75
Y = 2.6
Z = 23

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 89 — W014_WIN_Anaesthesia_West_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W014_WIN_Anaesthesia_West_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.75
Y = 0.5
Z = 23

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 90 — W014_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W014_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.75
Y = 1.5
Z = 25.425

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.35
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W015 — Outside / Sterile Core

Boundary: **X = 16**, from **Z = 24 to 31.5**.

- Leave **WIN_Sterile_East** clear from Z = **26.5 to 28.5**; width **2 m**. Window opening starts 1 m above the floor.


## Step 91 — W015_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W015_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 25.2

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.6
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 92 — W015_WIN_Sterile_East_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W015_WIN_Sterile_East_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 2.6
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 93 — W015_WIN_Sterile_East_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W015_WIN_Sterile_East_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 0.5
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 94 — W015_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W015_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 30.05

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.1
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W016 — Anaesthesia / Outside

Boundary: **Z = 26.5**, from **X = -16.75 to -15.75**.

This run has no opening.


## Step 95 — W016_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W016_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.25
Y = 1.5
Z = 26.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W017 — Operating Room / Outside

Boundary: **X = -15.75**, from **Z = 26.5 to 35.75**.

- Leave **WIN_Operating_West** clear from Z = **29.95 to 32.45**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 96 — W017_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W017_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.5
Z = 28.175

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.55
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 97 — W017_WIN_Operating_West_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W017_WIN_Operating_West_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 2.6
Z = 31.2

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 98 — W017_WIN_Operating_West_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W017_WIN_Operating_West_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 0.5
Z = 31.2

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 99 — W017_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W017_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.5
Z = 34.15

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.4
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W018 — Diagnostics / Outside

Boundary: **X = 16**, from **Z = 31.5 to 38.5**.

- Leave **WIN_Diagnostics_East** clear from Z = **33.25 to 35.25**; width **2 m**. Window opening starts 1 m above the floor.


## Step 100 — W018_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W018_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 32.325

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.85
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 101 — W018_WIN_Diagnostics_East_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W018_WIN_Diagnostics_East_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 2.6
Z = 34.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 102 — W018_WIN_Diagnostics_East_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W018_WIN_Diagnostics_East_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 0.5
Z = 34.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 1
Z = 2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 103 — W018_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W018_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.5
Z = 36.925

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.35
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W019 — Operating Room / Outside

Boundary: **Z = 35.75**, from **X = -15.75 to -10**.

- Leave **WIN_Operating_NorthWest** clear from X = **-14 to -11.5**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 104 — W019_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W019_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -14.925
Y = 1.5
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.85
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 105 — W019_WIN_Operating_NorthWest_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W019_WIN_Operating_NorthWest_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -12.75
Y = 2.6
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 106 — W019_WIN_Operating_NorthWest_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W019_WIN_Operating_NorthWest_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -12.75
Y = 0.5
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 107 — W019_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W019_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -10.7
Y = 1.5
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.6
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W020 — Operating Room / Outside

Boundary: **X = -10**, from **Z = 35.75 to 36.75**.

This run has no opening.


## Step 108 — W020_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W020_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -10
Y = 1.5
Z = 36.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W021 — Operating Room / Outside

Boundary: **Z = 36.75**, from **X = -10 to -4.75**.

- Leave **WIN_Operating_NorthStep** clear from X = **-8.55 to -6.05**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 109 — W021_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W021_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -9.325
Y = 1.5
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.55
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 110 — W021_WIN_Operating_NorthStep_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W021_WIN_Operating_NorthStep_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -7.3
Y = 2.6
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 111 — W021_WIN_Operating_NorthStep_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W021_WIN_Operating_NorthStep_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -7.3
Y = 0.5
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 112 — W021_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W021_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.35
Y = 1.5
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.4
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W022 — North service alcove / Outside

Boundary: **X = -4.75**, from **Z = 36.75 to 37.75**.

This run has no opening.


## Step 113 — W022_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W022_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.75
Y = 1.5
Z = 37.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W023 — Outside / WC

Boundary: **X = -4.75**, from **Z = 37.75 to 42**.

This run has no opening.


## Step 114 — W023_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W023_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.75
Y = 1.5
Z = 39.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 4.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W024 — Diagnostics / Outside

Boundary: **Z = 38.5**, from **X = 9 to 16**.

- Leave **WIN_Diagnostics_North** clear from X = **10.75 to 13.25**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 115 — W024_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W024_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.825
Y = 1.5
Z = 38.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.85
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 116 — W024_WIN_Diagnostics_North_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W024_WIN_Diagnostics_North_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 12
Y = 2.6
Z = 38.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 117 — W024_WIN_Diagnostics_North_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W024_WIN_Diagnostics_North_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 12
Y = 0.5
Z = 38.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 118 — W024_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W024_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 14.675
Y = 1.5
Z = 38.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.85
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W025 — ICU / Outside

Boundary: **X = 9**, from **Z = 38.5 to 42**.

This run has no opening.


## Step 119 — W025_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W025_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 9
Y = 1.5
Z = 40.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.7
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W026 — Outside / WC

Boundary: **Z = 42**, from **X = -4.75 to -0.25**.

- Leave **WIN_WC_North** clear from X = **-3.3 to -1.8**; width **1.5 m**. Window opening starts 1 m above the floor.


## Step 120 — W026_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W026_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.075
Y = 1.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.55
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 121 — W026_WIN_WC_North_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W026_WIN_WC_North_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.55
Y = 2.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 122 — W026_WIN_WC_North_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W026_WIN_WC_North_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.55
Y = 0.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 123 — W026_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W026_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -0.975
Y = 1.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.65
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W027 — Outside / Staff utility

Boundary: **Z = 42**, from **X = -0.25 to 4**.

- Leave **WIN_StaffUtility_North** clear from X = **1.05 to 2.55**; width **1.5 m**. Window opening starts 1 m above the floor.


## Step 124 — W027_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W027_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 0.35
Y = 1.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.4
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 125 — W027_WIN_StaffUtility_North_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W027_WIN_StaffUtility_North_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.8
Y = 2.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 126 — W027_WIN_StaffUtility_North_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W027_WIN_StaffUtility_North_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.8
Y = 0.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 127 — W027_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W027_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 3.325
Y = 1.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.55
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W028 — ICU / Outside

Boundary: **Z = 42**, from **X = 4 to 9**.

- Leave **WIN_ICU_North** clear from X = **5.15 to 7.65**; width **2.5 m**. Window opening starts 1 m above the floor.


## Step 128 — W028_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W028_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.525
Y = 1.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.25
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 129 — W028_WIN_ICU_North_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W028_WIN_ICU_North_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 2.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 130 — W028_WIN_ICU_North_Sill

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W028_WIN_ICU_North_Sill
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 0.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 131 — W028_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W028_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.375
Y = 1.5
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W029 — Corridor / Post-op

Boundary: **X = -3.25**, from **Z = 0 to 7.25**.

This run has no opening.


## Step 132 — W029_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W029_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.25
Y = 1.5
Z = 3.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 7.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W030 — Corridor / Recovery

Boundary: **X = 2.75**, from **Z = 0 to 7**.

This run has no opening.


## Step 133 — W030_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W030_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 2.75
Y = 1.5
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 7.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W031 — Recovery / Waste

Boundary: **X = 10.5**, from **Z = 0 to 7**.

This run has no opening.


## Step 134 — W031_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W031_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 10.5
Y = 1.5
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 7.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W032 — Corridor / Recovery

Boundary: **Z = 7**, from **X = 2.75 to 9.5**.

- Leave **D_Recovery** clear from X = **3.5 to 4.7**; width **1.2 m**. Door opening starts at the floor.


## Step 135 — W032_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W032_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 3.075
Y = 1.5
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.85
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 136 — W032_D_Recovery_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W032_D_Recovery_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.1
Y = 2.6
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 137 — W032_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W032_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 7.15
Y = 1.5
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.9
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W033 — Recovery / Staff room

Boundary: **Z = 7**, from **X = 9.5 to 10.5**.

This run has no opening.


## Step 138 — W033_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W033_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 10
Y = 1.5
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W034 — Staff room / Waste

Boundary: **Z = 7**, from **X = 10.5 to 16**.

- Leave **D_Waste** clear from X = **11.2 to 12.4**; width **1.2 m**. Door opening starts at the floor.


## Step 139 — W034_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W034_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 10.8
Y = 1.5
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.8
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 140 — W034_D_Waste_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W034_D_Waste_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 11.8
Y = 2.6
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 141 — W034_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W034_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 14.25
Y = 1.5
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W035 — Corridor / Staff room

Boundary: **X = 9.5**, from **Z = 7 to 9**.

- Leave **D_StaffRoom** clear from Z = **7.4 to 8.6**; width **1.2 m**. Door opening starts at the floor.


## Step 142 — W035_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W035_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.5
Y = 1.5
Z = 7.15

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 143 — W035_D_StaffRoom_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W035_D_StaffRoom_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.5
Y = 2.6
Z = 8

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 144 — W035_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W035_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.5
Y = 1.5
Z = 8.85

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W036 — General Ward / Post-op

Boundary: **Z = 7.25**, from **X = -13.25 to -6.75**.

- Leave **D_Ward_PostOp** clear from X = **-12.6 to -11.4**; width **1.2 m**. Door opening starts at the floor.


## Step 145 — W036_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W036_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -12.975
Y = 1.5
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.75
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 146 — W036_D_Ward_PostOp_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W036_D_Ward_PostOp_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -12
Y = 2.6
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 147 — W036_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W036_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -9.025
Y = 1.5
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.75
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W037 — Corridor / Post-op

Boundary: **Z = 7.25**, from **X = -6.75 to -3.25**.

- Leave **D_PostOp** clear from X = **-5.75 to -4.55**; width **1.2 m**. Door opening starts at the floor.


## Step 148 — W037_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W037_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.3
Y = 1.5
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.1
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 149 — W037_D_PostOp_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W037_D_PostOp_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.15
Y = 2.6
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 150 — W037_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W037_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.85
Y = 1.5
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.4
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W038 — Corridor / General Ward

Boundary: **X = -6.75**, from **Z = 7.25 to 9.5**.

- Leave **D_Ward_East** clear from Z = **7.75 to 8.95**; width **1.2 m**. Door opening starts at the floor.


## Step 151 — W038_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W038_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 7.45

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.6
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 152 — W038_D_Ward_East_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W038_D_Ward_East_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 2.6
Z = 8.35

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 153 — W038_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W038_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 9.275

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.65
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W039 — Corridor / Elevator

Boundary: **Z = 9**, from **X = 2.75 to 8.25**.

- Leave **D_Elevator** clear from X = **3.2 to 5**; width **1.8 m**. Door opening starts at the floor.


## Step 154 — W039_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W039_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 2.925
Y = 1.5
Z = 9

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.55
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 155 — W039_D_Elevator_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W039_D_Elevator_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.1
Y = 2.6
Z = 9

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.8
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 156 — W039_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W039_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.675
Y = 1.5
Z = 9

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.35
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W040 — Corridor / Service shaft

Boundary: **Z = 9**, from **X = 8.25 to 9.5**.

This run has no opening.


## Step 157 — W040_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W040_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.875
Y = 1.5
Z = 9

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W041 — Corridor / Elevator

Boundary: **X = 2.75**, from **Z = 9 to 14.75**.

This run has no opening.


## Step 158 — W041_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W041_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 2.75
Y = 1.5
Z = 11.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 5.95
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W042 — Elevator / Service shaft

Boundary: **X = 8.25**, from **Z = 9 to 12.5**.

This run has no opening.


## Step 159 — W042_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W042_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 10.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.7
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W043 — Service shaft / Staff room

Boundary: **X = 9.5**, from **Z = 9 to 12.5**.

This run has no opening.


## Step 160 — W043_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W043_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.5
Y = 1.5
Z = 10.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 3.7
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W044 — Corridor / Nurse Office

Boundary: **Z = 9.5**, from **X = -6.75 to -0.75**.

- Leave **D_Office_South** clear from X = **-2.65 to -1.45**; width **1.2 m**. Door opening starts at the floor.


## Step 161 — W044_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W044_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.75
Y = 1.5
Z = 9.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 162 — W044_D_Office_South_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W044_D_Office_South_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.05
Y = 2.6
Z = 9.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 163 — W044_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W044_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -1.05
Y = 1.5
Z = 9.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.8
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W045 — General Ward / Nurse Office

Boundary: **X = -6.75**, from **Z = 9.5 to 16.5**.

This run has no opening.


## Step 164 — W045_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W045_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 13

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 7.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W046 — Corridor / Nurse Office

Boundary: **X = -0.75**, from **Z = 9.5 to 16.5**.

This run has no opening.


## Step 165 — W046_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W046_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -0.75
Y = 1.5
Z = 13

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 7.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W047 — Service shaft / Storage

Boundary: **Z = 12.5**, from **X = 8.25 to 9.5**.

This run has no opening.


## Step 166 — W047_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W047_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.875
Y = 1.5
Z = 12.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W048 — Staff room / Storage

Boundary: **Z = 12.5**, from **X = 9.5 to 16**.

This run has no opening.


## Step 167 — W048_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W048_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.75
Y = 1.5
Z = 12.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 6.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W049 — Elevator / Storage

Boundary: **X = 8.25**, from **Z = 12.5 to 14.75**.

This run has no opening.


## Step 168 — W049_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W049_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 13.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W050 — Corridor / Elevator

Boundary: **Z = 14.75**, from **X = 2.75 to 8.25**.

This run has no opening.


## Step 169 — W050_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W050_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.5
Y = 1.5
Z = 14.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W051 — Corridor / Storage

Boundary: **X = 8.25**, from **Z = 14.75 to 18**.

- Leave **D_Storage** clear from Z = **15.4 to 16.6**; width **1.2 m**. Door opening starts at the floor.


## Step 170 — W051_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W051_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 15.025

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.75
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 171 — W051_D_Storage_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W051_D_Storage_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 2.6
Z = 16

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 172 — W051_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W051_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 17.35

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.5
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W052 — Corridor / General Ward

Boundary: **Z = 16.5**, from **X = -15.75 to -6.75**.

- Leave **D_Ward_North** clear from X = **-9.6 to -8.4**; width **1.2 m**. Door opening starts at the floor.


## Step 173 — W052_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W052_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -12.725
Y = 1.5
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 6.25
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 174 — W052_D_Ward_North_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W052_D_Ward_North_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -9
Y = 2.6
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 175 — W052_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W052_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -7.525
Y = 1.5
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.75
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W053 — Corridor / Nurse Office

Boundary: **Z = 16.5**, from **X = -6.75 to -0.75**.

- Leave **D_Office_North** clear from X = **-6.35 to -5.15**; width **1.2 m**. Door opening starts at the floor.


## Step 176 — W053_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W053_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.6
Y = 1.5
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.5
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 177 — W053_D_Office_North_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W053_D_Office_North_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.75
Y = 2.6
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 178 — W053_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W053_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.9
Y = 1.5
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 4.5
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W054 — Corridor / Stair enclosure

Boundary: **Z = 17.25**, from **X = 2.75 to 6**.

This run has no opening.


## Step 179 — W054_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W054_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.375
Y = 1.5
Z = 17.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W055 — Corridor / Stair enclosure

Boundary: **X = 2.75**, from **Z = 17.25 to 23.5**.

This run has no opening.


## Step 180 — W055_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W055_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 2.75
Y = 1.5
Z = 20.375

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 6.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W056 — Corridor / Stair enclosure

Boundary: **X = 6**, from **Z = 17.25 to 23.5**.

This run has no opening.


## Step 181 — W056_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W056_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 6
Y = 1.5
Z = 20.375

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 6.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W057 — Pre-op / Storage

Boundary: **Z = 18**, from **X = 8.25 to 16**.

This run has no opening.


## Step 182 — W057_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W057_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.125
Y = 1.5
Z = 18

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W058 — Corridor / Pre-op

Boundary: **X = 8.25**, from **Z = 18 to 23.5**.

- Leave **D_PreOp** clear from Z = **19.95 to 21.15**; width **1.2 m**. Door opening starts at the floor.


## Step 183 — W058_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W058_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 18.925

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.05
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 184 — W058_D_PreOp_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W058_D_PreOp_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 2.6
Z = 20.55

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 185 — W058_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W058_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 22.375

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W059 — Anaesthesia / Corridor

Boundary: **Z = 19.25**, from **X = -15.75 to -6.75**.

This run has no opening.


## Step 186 — W059_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W059_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -11.25
Y = 1.5
Z = 19.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 9.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W060 — Corridor / Washroom alcove

Boundary: **Z = 19.25**, from **X = -6.75 to -1.5**.

This run has no opening.


## Step 187 — W060_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W060_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.125
Y = 1.5
Z = 19.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 5.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W061 — Anaesthesia / Washroom alcove

Boundary: **X = -6.75**, from **Z = 19.25 to 24.5**.

This run has no opening.


## Step 188 — W061_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W061_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 21.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 5.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W062 — Corridor / Washroom alcove

Boundary: **X = -1.5**, from **Z = 19.25 to 24.5**.

This run has no opening.


## Step 189 — W062_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W062_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -1.5
Y = 1.5
Z = 21.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 5.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W063 — Corridor / Stair enclosure

Boundary: **Z = 23.5**, from **X = 2.75 to 4.5**.

This run has no opening.


## Step 190 — W063_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W063_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 3.625
Y = 1.5
Z = 23.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W064 — Stair enclosure / Sterile Core

Boundary: **Z = 23.5**, from **X = 4.5 to 6**.

This run has no opening.


## Step 191 — W064_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W064_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.25
Y = 1.5
Z = 23.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W065 — Corridor / Sterile Core

Boundary: **Z = 23.5**, from **X = 6 to 8.25**.

This run has no opening.


## Step 192 — W065_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W065_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 7.125
Y = 1.5
Z = 23.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W066 — Corridor / Sterile Core

Boundary: **X = 4.5**, from **Z = 23.5 to 31.5**.

- Leave **D_Sterile_West** clear from Z = **24.7 to 25.9**; width **1.2 m**. Door opening starts at the floor.


## Step 193 — W066_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W066_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.5
Y = 1.5
Z = 24.05

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.3
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 194 — W066_D_Sterile_West_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W066_D_Sterile_West_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.5
Y = 2.6
Z = 25.3

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 195 — W066_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W066_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.5
Y = 1.5
Z = 28.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 5.7
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W067 — Pre-op / Sterile Core

Boundary: **X = 8.25**, from **Z = 23.5 to 24**.

This run has no opening.


## Step 196 — W067_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W067_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.5
Z = 23.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.7
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W068 — Pre-op / Sterile Core

Boundary: **Z = 24**, from **X = 8.25 to 16**.

This run has no opening.


## Step 197 — W068_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W068_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.125
Y = 1.5
Z = 24

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W069 — Corridor / Washroom alcove

Boundary: **Z = 24.5**, from **X = -6.75 to -1.5**.

- Leave **D_Washroom** clear from X = **-6.3 to -5.1**; width **1.2 m**. Door opening starts at the floor.


## Step 198 — W069_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W069_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.575
Y = 1.5
Z = 24.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.55
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 199 — W069_D_Washroom_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W069_D_Washroom_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.7
Y = 2.6
Z = 24.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 200 — W069_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W069_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.25
Y = 1.5
Z = 24.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 3.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W070 — Anaesthesia / Corridor

Boundary: **X = -6.75**, from **Z = 24.5 to 26.5**.

- Leave **D_Anaesthesia** clear from Z = **24.85 to 26.05**; width **1.2 m**. Door opening starts at the floor.


## Step 201 — W070_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W070_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 24.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 202 — W070_D_Anaesthesia_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W070_D_Anaesthesia_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 2.6
Z = 25.45

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 203 — W070_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W070_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 26.325

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.55
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W071 — Anaesthesia / Operating Room

Boundary: **Z = 26.5**, from **X = -15.75 to -6.75**.

This run has no opening.


## Step 204 — W071_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W071_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -11.25
Y = 1.5
Z = 26.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 9.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W072 — Corridor / Operating Room

Boundary: **X = -6.75**, from **Z = 26.5 to 27.5**.

This run has no opening.


## Step 205 — W072_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W072_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.5
Z = 27

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W073 — Corridor / Operating Room

Boundary: **Z = 27.5**, from **X = -6.75 to -3.25**.

- Leave **D_Operating** clear from X = **-6.15 to -4.65**; width **1.5 m**. Door opening starts at the floor.


## Step 206 — W073_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W073_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.5
Y = 1.5
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 207 — W073_D_Operating_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W073_D_Operating_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.4
Y = 2.6
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 208 — W073_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W073_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.9
Y = 1.5
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W074 — Corridor / Operating service shaft

Boundary: **Z = 27.5**, from **X = -3.25 to -1.5**.

This run has no opening.


## Step 209 — W074_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W074_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.375
Y = 1.5
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W075 — Operating Room / Operating service shaft

Boundary: **X = -3.25**, from **Z = 27.5 to 35.75**.

This run has no opening.


## Step 210 — W075_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W075_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.25
Y = 1.5
Z = 31.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 8.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W076 — Corridor / Operating service shaft

Boundary: **X = -1.5**, from **Z = 27.5 to 35.75**.

This run has no opening.


## Step 211 — W076_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W076_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -1.5
Y = 1.5
Z = 31.625

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 8.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W077 — Corridor / ICU

Boundary: **Z = 31.5**, from **X = 1.75 to 4.5**.

This run has no opening.


## Step 212 — W077_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W077_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 3.125
Y = 1.5
Z = 31.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W078 — ICU / Sterile Core

Boundary: **Z = 31.5**, from **X = 4.5 to 9**.

- Leave **D_ICU_Sterile** clear from X = **5 to 6.2**; width **1.2 m**. Door opening starts at the floor.


## Step 213 — W078_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W078_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.7
Y = 1.5
Z = 31.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.6
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 214 — W078_D_ICU_Sterile_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W078_D_ICU_Sterile_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.6
Y = 2.6
Z = 31.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 215 — W078_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W078_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 7.65
Y = 1.5
Z = 31.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.9
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W079 — Diagnostics / Sterile Core

Boundary: **Z = 31.5**, from **X = 9 to 16**.

This run has no opening.


## Step 216 — W079_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W079_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 12.5
Y = 1.5
Z = 31.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 7.2
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W080 — Corridor / ICU

Boundary: **X = 1.75**, from **Z = 31.5 to 37.75**.

- Leave **D_ICU_West** clear from Z = **36.05 to 37.25**; width **1.2 m**. Door opening starts at the floor.


## Step 217 — W080_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W080_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.75
Y = 1.5
Z = 33.725

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 4.65
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 218 — W080_D_ICU_West_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W080_D_ICU_West_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.75
Y = 2.6
Z = 36.65

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 219 — W080_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W080_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.75
Y = 1.5
Z = 37.55

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 0.6
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W081 — Diagnostics / ICU

Boundary: **X = 9**, from **Z = 31.5 to 38.5**.

- Leave **D_Diagnostics** clear from Z = **32.65 to 33.85**; width **1.2 m**. Door opening starts at the floor.


## Step 220 — W081_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W081_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = 9
Y = 1.5
Z = 32.025

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.25
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 221 — W081_D_Diagnostics_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W081_D_Diagnostics_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 9
Y = 2.6
Z = 33.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 0.8
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 222 — W081_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W081_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 9
Y = 1.5
Z = 36.225

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 4.75
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W082 — North service alcove / Operating Room

Boundary: **Z = 35.75**, from **X = -4.75 to -3.25**.

This run has no opening.


## Step 223 — W082_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W082_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -4
Y = 1.5
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W083 — Corridor / Operating service shaft

Boundary: **Z = 35.75**, from **X = -3.25 to -1.5**.

This run has no opening.


## Step 224 — W083_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W083_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.375
Y = 1.5
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.95
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W084 — North service alcove / Operating Room

Boundary: **X = -4.75**, from **Z = 35.75 to 36.75**.

This run has no opening.


## Step 225 — W084_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W084_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -4.75
Y = 1.5
Z = 36.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 1.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W085 — Corridor / North service alcove

Boundary: **X = -3.25**, from **Z = 35.75 to 37.75**.

This run has no opening.


## Step 226 — W085_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W085_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -3.25
Y = 1.5
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 2.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W086 — North service alcove / WC

Boundary: **Z = 37.75**, from **X = -4.75 to -3.25**.

This run has no opening.


## Step 227 — W086_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W086_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -4
Y = 1.5
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.7
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W087 — Corridor / WC

Boundary: **Z = 37.75**, from **X = -3.25 to -0.25**.

- Leave **D_WC** clear from X = **-1.75 to -0.55**; width **1.2 m**. Door opening starts at the floor.


## Step 228 — W087_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W087_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.55
Y = 1.5
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.6
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 229 — W087_D_WC_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W087_D_WC_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = -1.15
Y = 2.6
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 230 — W087_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W087_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -0.35
Y = 1.5
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.4
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W088 — Corridor / Staff utility

Boundary: **Z = 37.75**, from **X = -0.25 to 1.75**.

- Leave **D_StaffUtility** clear from X = **0.05 to 1.25**; width **1.2 m**. Door opening starts at the floor.


## Step 231 — W088_Solid01

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W088_Solid01
```

In **Inspector > Transform**, enter:

```text
Position
X = -0.15
Y = 1.5
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.4
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 232 — W088_D_StaffUtility_Header

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W088_D_StaffUtility_Header
```

In **Inspector > Transform**, enter:

```text
Position
X = 0.65
Y = 2.6
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.2
Y = 0.8
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 233 — W088_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W088_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.55
Y = 1.5
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.6
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W089 — ICU / Staff utility

Boundary: **Z = 37.75**, from **X = 1.75 to 4**.

This run has no opening.


## Step 234 — W089_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W089_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 2.875
Y = 1.5
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.45
Y = 3
Z = 0.2
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W090 — Staff utility / WC

Boundary: **X = -0.25**, from **Z = 37.75 to 42**.

This run has no opening.


## Step 235 — W090_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W090_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = -0.25
Y = 1.5
Z = 39.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 4.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### Wall run W091 — ICU / Staff utility

Boundary: **X = 4**, from **Z = 37.75 to 42**.

This run has no opening.


## Step 236 — W091_SolidEnd

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Walls
```

Rename the Cube:
```text
W091_SolidEnd
```

In **Inspector > Transform**, enter:

```text
Position
X = 4
Y = 1.5
Z = 39.875

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.2
Y = 3
Z = 4.45
```

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 237 — Check the walls before adding placeholders

1. Switch from Top to a perspective view by dragging/orbiting the Scene view with Alt + left-mouse drag. Select a room and press F if necessary.
2. Confirm walls start on the floors, rise to Y = 3, and meet at corners. Wall tops are not ceilings.
3. Look through every intended doorway: there must be empty space from Y = 0 to 2.2. Headers above doors are expected.
4. Check there is no accidental wall across the main spine, the right landing, the corridor bends or the seams inside Operating/ICU/Sterile Core.
5. Windows must have a solid lower wall, a visible gap above it, and a header. The outside is currently empty; sky through a window is normal.
6. Confirm the elevator has one south opening into the lobby. Its west wall against the main corridor stays solid.
7. Confirm the stair footprint and enclosed service shafts are sealed. They are not playable rooms in this single-floor prototype.
8. Save before continuing.


# Part 3 — Door placeholders and windows


## Step 238 — Choose the initial door state

The architectural doorway and the door leaf are different things. The walls now contain the doorway. The next steps create a simple Cube leaf as a placeholder.

**Ordinary interior doors:** create and position each leaf, then uncheck the checkbox beside its name at the very top of the Inspector. This disables the whole GameObject, including its collider, leaving the doorway open for the first NavMesh. Disabling only Mesh Renderer would leave an invisible blocking collider and is wrong for an open door.

**Main entrance and elevator:** keep these two leaves active and their colliders enabled. They are closed prototype barriers. This prevents the entrance being an alternative escape and keeps the elevator locked for the initial world handoff. These are static placeholders; no keypad, animation, key requirement or automatic opening is implemented by the instructions.

Ordinary leaf bottoms are 0 m and tops 2.15 m, below the 2.2 m headers. Thin leaves sit on the wall centreline. Their unused width is 0.04 m clearance at each jamb. Frames are not added inside these clear openings.


## Step 239 — D_MainEntrance_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_MainEntrance_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 0
Y = 1.075
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.72
Y = 2.15
Z = 0.06
```

South main entrance. Closed placeholder for the one-exit prototype.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 240 — D_PostOp_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_PostOp_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.15
Y = 1.075
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

North side of Post-op, opening into the lower west hall.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 241 — D_Ward_PostOp_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Ward_PostOp_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -12
Y = 1.075
Z = 7.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Door drawn at the northwest of Post-op into the Ward.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 242 — D_Recovery_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Recovery_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.1
Y = 1.075
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Recovery opens north into the elevator lobby.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 243 — D_Waste_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Waste_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 11.8
Y = 1.075
Z = 7

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Waste is reached from the Staff Room, as the drawn north doorway indicates.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 244 — D_StaffRoom_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_StaffRoom_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 9.5
Y = 1.075
Z = 8

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

West side of Staff Room, facing the elevator lobby.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 245 — D_Elevator_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Elevator_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.1
Y = 1.075
Z = 9

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.72
Y = 2.15
Z = 0.06
```

South elevator opening. Closed placeholder; keypad and unlock behavior belong to later gameplay work.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 246 — D_Storage_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Storage_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.075
Z = 16

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

West side of Storage, facing the landing above the elevator.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 247 — D_Ward_North_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Ward_North_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -9
Y = 1.075
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

North Ward doorway off the west cross-corridor.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 248 — D_Ward_East_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Ward_East_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.075
Z = 8.35

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

Lower east Ward doorway into the lower west hall.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 249 — D_Office_North_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Office_North_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.75
Y = 1.075
Z = 16.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Office entrance on its north edge.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 250 — D_Office_South_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Office_South_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.05
Y = 1.075
Z = 9.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Second Office entrance into the lower west hall.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 251 — D_PreOp_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_PreOp_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 8.25
Y = 1.075
Z = 20.55

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

Pre-op door faces the passage east of the stair enclosure.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 252 — D_Anaesthesia_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Anaesthesia_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -6.75
Y = 1.075
Z = 25.45

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

Upper east side of Anaesthesia, opening toward the Operating approach.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 253 — D_Washroom_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Washroom_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.7
Y = 1.075
Z = 24.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Door on the north side of the unlabeled washroom.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 254 — D_Operating_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Operating_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -5.4
Y = 1.075
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.42
Y = 2.15
Z = 0.06
```

Operating Room door faces south into its recessed approach.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 255 — D_Sterile_West_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Sterile_West_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 4.5
Y = 1.075
Z = 25.3

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

West side of Sterile Core, facing the wide main corridor.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 256 — D_ICU_Sterile_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_ICU_Sterile_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 5.6
Y = 1.075
Z = 31.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Connection drawn between ICU and Sterile Core.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 257 — D_ICU_West_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_ICU_West_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.75
Y = 1.075
Z = 36.65

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

Upper west ICU doorway into the north lobby.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 258 — D_Diagnostics_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_Diagnostics_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 9
Y = 1.075
Z = 33.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 2.15
Z = 1.12
```

Diagnostics opens west into ICU; no invented corridor through ICU.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 259 — D_WC_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_WC_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = -1.15
Y = 1.075
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

WC door faces south into the north lobby.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 260 — D_StaffUtility_Leaf

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Doors
```

Rename the Cube:
```text
D_StaffUtility_Leaf
```

In **Inspector > Transform**, enter:

```text
Position
X = 0.65
Y = 1.075
Z = 37.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.12
Y = 2.15
Z = 0.06
```

Small Staff utility door faces south into the north lobby.

**Disable GameObject after creation.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 261 — Understand the window collision setup

For each window, the wall already supplies its sill, header and side jambs. Add the two listed Cubes: an invisible collision pane and a visible centre divider.

For a **CollisionPane**, find the **Mesh Renderer** component in the Inspector and uncheck its component checkbox. **Do not** uncheck the object’s top-level checkbox. Leave its **Box Collider** enabled and **Is Trigger** off. You can now see through the window but a character cannot walk/jump through it. This is a collision-only placeholder, not finished glass.

For a **Mullion**, leave Mesh Renderer enabled. It is the small vertical bar in the middle. Its standard grey surface is sufficient. No transparent material, texture or lighting setup is needed.


### WIN_PostOp_South — on wall run W001


## Step 262 — WIN_PostOp_South_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_PostOp_South_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -8.25
Y = 1.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.25
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 263 — WIN_PostOp_South_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_PostOp_South_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -8.25
Y = 1.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_PostOp_West — on wall run W005


## Step 264 — WIN_PostOp_West_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_PostOp_West_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -13.25
Y = 1.6
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2.5
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 265 — WIN_PostOp_West_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_PostOp_West_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -13.25
Y = 1.6
Z = 3.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Recovery_South — on wall run W003


## Step 266 — WIN_Recovery_South_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Recovery_South_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 1.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 267 — WIN_Recovery_South_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Recovery_South_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 1.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Waste_South — on wall run W004


## Step 268 — WIN_Waste_South_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Waste_South_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 13.25
Y = 1.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 269 — WIN_Waste_South_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Waste_South_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 13.25
Y = 1.6
Z = 0

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Staff_East — on wall run W007


## Step 270 — WIN_Staff_East_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Staff_East_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 9.6

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 1.5
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 271 — WIN_Staff_East_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Staff_East_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 9.6

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Storage_East — on wall run W010


## Step 272 — WIN_Storage_East_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Storage_East_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 15.3

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 273 — WIN_Storage_East_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Storage_East_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 15.3

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_PreOp_East — on wall run W012


## Step 274 — WIN_PreOp_East_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_PreOp_East_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 21.15

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 275 — WIN_PreOp_East_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_PreOp_East_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 21.15

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Sterile_East — on wall run W015


## Step 276 — WIN_Sterile_East_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Sterile_East_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 277 — WIN_Sterile_East_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Sterile_East_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 27.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Diagnostics_East — on wall run W018


## Step 278 — WIN_Diagnostics_East_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Diagnostics_East_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 34.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 279 — WIN_Diagnostics_East_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Diagnostics_East_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 16
Y = 1.6
Z = 34.25

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Diagnostics_North — on wall run W024


## Step 280 — WIN_Diagnostics_North_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Diagnostics_North_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 12
Y = 1.6
Z = 38.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 281 — WIN_Diagnostics_North_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Diagnostics_North_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 12
Y = 1.6
Z = 38.5

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_ICU_North — on wall run W028


## Step 282 — WIN_ICU_North_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_ICU_North_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 1.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 283 — WIN_ICU_North_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_ICU_North_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 6.4
Y = 1.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_StaffUtility_North — on wall run W027


## Step 284 — WIN_StaffUtility_North_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_StaffUtility_North_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.8
Y = 1.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 285 — WIN_StaffUtility_North_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_StaffUtility_North_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = 1.8
Y = 1.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_WC_North — on wall run W026


## Step 286 — WIN_WC_North_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_WC_North_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.55
Y = 1.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 1.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 287 — WIN_WC_North_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_WC_North_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -2.55
Y = 1.6
Z = 42

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Operating_NorthWest — on wall run W019


## Step 288 — WIN_Operating_NorthWest_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Operating_NorthWest_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -12.75
Y = 1.6
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 289 — WIN_Operating_NorthWest_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Operating_NorthWest_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -12.75
Y = 1.6
Z = 35.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Operating_NorthStep — on wall run W021


## Step 290 — WIN_Operating_NorthStep_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Operating_NorthStep_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -7.3
Y = 1.6
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 2.5
Y = 1.2
Z = 0.08
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 291 — WIN_Operating_NorthStep_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Operating_NorthStep_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -7.3
Y = 1.6
Z = 36.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.06
Y = 1.2
Z = 0.24
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Operating_West — on wall run W017


## Step 292 — WIN_Operating_West_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Operating_West_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.6
Z = 31.2

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2.5
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 293 — WIN_Operating_West_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Operating_West_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.6
Z = 31.2

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Anaesthesia_West — on wall run W014


## Step 294 — WIN_Anaesthesia_West_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Anaesthesia_West_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.75
Y = 1.6
Z = 23

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2.5
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 295 — WIN_Anaesthesia_West_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Anaesthesia_West_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -16.75
Y = 1.6
Z = 23

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


### WIN_Ward_West — on wall run W009


## Step 296 — WIN_Ward_West_CollisionPane

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Ward_West_CollisionPane
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.6
Z = 11.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.08
Y = 1.2
Z = 2.5
```

Invisible collision pane: an opening that admits a view but does not let characters leave the building.

**Disable Mesh Renderer only; keep GameObject and Box Collider enabled.**

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


## Step 297 — WIN_Ward_West_Mullion

Right-click the parent below in the **Hierarchy**, then choose **3D Object > Cube**.

Parent:
```text
MainHospital
└── GEO_Windows
```

Rename the Cube:
```text
WIN_Ward_West_Mullion
```

In **Inspector > Transform**, enter:

```text
Position
X = -15.75
Y = 1.6
Z = 11.75

Rotation
X = 0
Y = 0
Z = 0

Scale
X = 0.24
Y = 1.2
Z = 0.06
```

Plain central window divider. Sill and header were already built with the wall.

Keep the Box Collider enabled and **Is Trigger** unchecked. Press **Ctrl + S**. Finish this step before continuing.


# Part 4 — Ceilings and navigation


## Step 298 — Inspect before adding ceilings

Ceilings hide the layout from a normal Top view, so leave them until floors, walls and openings pass the checks. You can bake an initial NavMesh without ceilings while making corrections. If you do add them now, use the following repeatable method; it covers irregular rooms without drawing one giant rectangular roof over exterior recesses.

1. Select **GEO_Ceilings**. Keep its Transform at position/rotation zero and scale one.
2. For each floor Cube listed in Part 1, select only that Cube and press **Ctrl + D**.
3. Drag the duplicate onto **GEO_Ceilings** in the Hierarchy, then rename its `F_` prefix to `C_` and remove Unity’s `(1)` suffix. Example: `F_RecoveryRoom` becomes `C_RecoveryRoom`.
4. Keep its Position X/Z, Rotation, and Scale X/Z exactly the same as the floor.
5. Set **Position Y = 3.1**, **Scale Y = 0.2**. The ceiling underside is now at Y = 3, meeting the wall tops.
6. Leave Box Collider enabled, Is Trigger off, and add no Rigidbody.
7. Repeat for every floor slab, including the multi-piece corridors/rooms and sealed stair/shaft footprints.
8. Check that the originals remain under GEO_Floors with Y = -0.1. Accidentally moving the original instead of a duplicate removes the walkable floor.
9. Use the Hierarchy’s **Scene visibility eye** on GEO_Ceilings to hide ceilings while editing. If its visibility controls are not shown, hover to the left of the object row. The eye affects editor visibility; it is not the object’s active checkbox. Restore visibility for the final visual check.
10. Save.

You should have the same number of ceiling slabs as floor slabs: **40**. Optional ceilings use the identical X/Z positions and sizes already given per floor, so no new floor-plan arithmetic is needed.


## Step 299 — Verify the installed AI Navigation package

1. Choose **Window > Package Management > Package Manager**.
2. Set the package list to **In Project** and select **AI Navigation**.
3. This project already specifies **2.0.14**. Let Unity finish importing/resolving if it is still busy. There is no need to upgrade it for this guide.
4. If it is unexpectedly missing in a separate copy of the project, choose **Unity Registry**, search **AI Navigation**, then install the version offered as compatible with your Editor. Do not install the old GitHub NavMeshComponents package on top of this one.
5. Open **Window > General > Console**. Resolve red compile/import errors before relying on the Bake button.

The package’s local documentation confirms the NavMesh Surface, Modifier and Navigation window controls used below. This guide uses the current component-based bake, not a legacy Navigation window “Bake” tab.


## Step 300 — Create a blockout agent type

1. Open **Window > AI > Navigation** and select the **Agents** tab.
2. Click **+** to add an agent type; name it **Hospital_Blockout**. Do not overwrite an existing teammate’s agent type.
3. Set **Radius = 0.3**, **Height = 1.8**, **Step Height = 0.2**, **Max Slope = 45**.
4. Set **Drop Height = 0** and **Jump Distance = 0** where the generated-link settings are displayed.
5. These are provisional human-sized navigation settings for the skeleton. Tell your teammates the chosen type and measurements. Their NavMeshAgent components must use this type to walk on this bake. A larger creature will need a suitable agent type, its own bake and clearance checks.

Why these settings: a 0.3 m radius means a 0.6 m diameter body fits comfortably within the 1.2 m doorways. The project’s existing default 0.5 m radius is wider. The bake radius, rather than only changing a runtime Agent radius, determines how far the navigation surface is kept from walls.


## Step 301 — Add the NavMesh Surface to the correct parent

1. Select **MainHospital** in the Hierarchy — not the empty Navigation container and not GEO_Floors alone.
2. Click **Add Component** in the Inspector, search **NavMesh Surface**, and add it.
3. Set **Agent Type = Hospital_Blockout**.
4. Set **Default Area = Walkable**.
5. Set **Use Geometry = Physics Colliders**. This uses the enabled Box Colliders of floors, walls, panes and the two closed door placeholders.
6. Expand **Object Collection**. Set **Collect Objects = Current Object Hierarchy** and **Include Layers = Everything** for this geometry-only prototype.
7. Leave **Generate Links** off. There are no jumps, second-floor stairs or teleports in this blockout.
8. Expand **Advanced**. Enable **Override Voxel Size** and set **Voxel Size = 0.05**. Leave tile size at its default. Use **Minimum Region Area = 2** initially and leave **Build Height Mesh** off.

The Surface is on MainHospital so its current hierarchy includes both floors and blocking walls. Putting it on the empty Navigation child with “Current Object Hierarchy” would collect no hospital geometry. Putting it on GEO_Floors alone would omit walls and could generate paths through them.


## Step 302 — Keep blocking geometry solid but not walkable on top

Do the following on each of **GEO_Walls**, **GEO_Doors**, **GEO_Windows**, and **GEO_Ceilings**:

1. Select the container.
2. Choose **Add Component > NavMesh Modifier**.
3. Set **Mode = Add or Modify Object**.
4. Enable **Apply To Children**.
5. Set **Affected Agents = All**.
6. Enable **Override Area**, then choose **Not Walkable**.

This keeps their collision geometry in the bake as blockers, while preventing walkable patches on their tops. **Do not choose Remove Object** for walls: that would make the bake ignore them. Do not mark GEO_Floors or MainHospital Not Walkable.

Next, add the same Modifier settings directly to these five floor Cubes, which are intentionally inaccessible in this version:

```text
F_Elevator
F_ElevatorServiceStrip
F_StairFootprint
F_OperatingServiceShaft
F_NorthServiceAlcove
```

The Washroom alcove is an accessible room and should stay Walkable. The elevator floor is excluded for this initial **closed-elevator** bake. When the team implements unlocking, they must plan the navigation update too; simply removing a closed door after baking does not generate the missing walkable surface.


## Step 303 — Bake the first NavMesh

1. Confirm all ordinary interior door leaves are **inactive**, while D_MainEntrance_Leaf and D_Elevator_Leaf are **active**.
2. Confirm all floors, walls and window collision panes are active. Mesh Renderer can be off on the window panes, but their Box Colliders must be on.
3. Select **MainHospital** and click **Bake** on its NavMesh Surface component. Wait for the operation to finish.
4. Check the **NavMesh Data** field now references an asset instead of None.
5. Show the blue navigation overlay in the Scene view. If hidden, keep the Surface selected, open the Scene view **Overlays** menu, enable the **AI Navigation** overlay and enable its NavMesh display. Hide ceilings with the Scene visibility eye if they obscure it.
6. Press **Ctrl + S**. The generated NavMesh asset and its `.meta` file are part of the handoff, not just a temporary blue drawing.

The blue area should cover accessible floors and continue through all open doorways. It should stay inset from walls, have no walkable wall/ceiling tops, and exclude the locked elevator interior and closed stair/service enclosures. An isolated blue island is not proof a room can be reached from the entrance.


## Step 304 — Inspect every route, not only the main corridor

Use Top view with ceilings hidden. Follow the connected blue surface across each route below. A narrow dark gap across a doorway can mean the room is disconnected.

| Start | Destination | Expected route |
|---|---|---|
| Inside main entrance | Post-op | Main spine → west lower hall → north Post-op doorway |
| Main entrance | Recovery | Main spine → elevator lobby → Recovery north doorway |
| Elevator lobby | Waste | Staff west doorway → Staff Room → Waste north doorway |
| Main spine | Storage | Right landing above Elevator → Storage west doorway |
| Main spine | Pre-op | Right landing → passage east of stairs → Pre-op west doorway |
| Main spine | General Ward | West cross-corridor → Ward north doorway, or lower west hall → Ward east doorway |
| Main spine | Nurse Office | West cross-corridor → Office north doorway, or lower west hall → south doorway |
| Main spine | Anaesthesia | Operating approach → Anaesthesia east doorway |
| Main spine | Operating Room | Operating approach → door recess → Operating south doorway |
| Operating approach | Washroom alcove | Washroom north doorway |
| Main spine | Sterile Core | Sterile west doorway |
| Main spine | ICU | North lobby → ICU west doorway, or Sterile Core → ICU south doorway |
| ICU | Diagnostics | Diagnostics west doorway |
| North lobby | WC / Staff utility | Their south doorways |
| Elevator lobby | Elevator cabin | **Blocked in this first bake** |
| Any corridor | Stair enclosure / service shafts | **Blocked** |

Also inspect the main spine where its width changes beside the Office and near ICU. Corridors that only meet at a point are not usable routes; this schedule gives shared edges, not point-only connections.


## Step 305 — Run an actual path check with a temporary navigation probe

A blue overlay alone is not a complete movement test. Use the small disposable probe below to confirm paths in Play mode. It is test tooling, not ghost, thief or gameplay logic. If a teammate already has a working navigation tester, they can perform the equivalent route checks instead.

1. In Project, make **Assets > Tests > Manual** (right-click a folder > Create > Folder for each missing folder).
2. Inside Manual, choose **Create > Scripting > MonoBehaviour Script** (or **Create > C# Script**, depending on menu presentation). Name it **HospitalNavProbe**.
3. Double-click it. Replace its contents with the complete code below and save. Return to Unity and wait for compilation.
4. Create an empty GameObject under **Navigation** named **TEST_NavTarget**, with Position **(-8, 0, 3)**, Rotation zero and Scale one. This is only a test destination in Post-op, not an item spawn.
5. Create another empty under Navigation named **TEST_NavProbeRoot**, Position **(0, 0, 2)**, Rotation zero and Scale one. Add **NavMesh Agent** and set **Agent Type = Hospital_Blockout**, **Radius = 0.3**, **Height = 1.8**, **Base Offset = 0**, **Speed = 2**, **Stopping Distance = 0.1**.
6. Add **HospitalNavProbe** to TEST_NavProbeRoot. Drag TEST_NavTarget from Hierarchy to the script’s **Target** field.
7. Create a **Capsule** as a child of TEST_NavProbeRoot named **TEST_Visual**. Set its local Position **(0, 0.9, 0)**, Rotation zero, Scale **(0.6, 0.9, 0.6)**. Remove its Capsule Collider using the component menu > Remove Component. The capsule is only a visible body; the root’s feet stay at Y = 0.
8. Enter Play mode. The Console should report **PathComplete**, and the capsule should move through the hall and doorway into Post-op without crossing walls.
9. Stop Play mode. Move TEST_NavTarget to each coordinate in the next table; enter Play mode again for each test. Do not bake with the test objects present as geometry. The root has a NavMeshAgent and the visible child has no collider.
10. For the moving-agent team’s acceptance, repeat representative routes in reverse and from room to room, including ICU → Diagnostics and Staff → Waste. Check both path result and visible wall clearance.

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HospitalNavProbe : MonoBehaviour
{
    public Transform target;

    void Start()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (target == null || !agent.isOnNavMesh)
        {
            Debug.LogError("Assign a target and place the probe on the matching baked NavMesh.", this);
            return;
        }

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };

        if (!NavMesh.SamplePosition(target.position, out NavMeshHit hit, 0.25f, filter))
        {
            Debug.LogWarning("Target is not on reachable-type navigation geometry within 0.25 m.", target);
            return;
        }

        NavMeshPath path = new NavMeshPath();
        bool found = agent.CalculatePath(hit.position, path);
        Debug.Log("Navigation result: " + path.status, this);
        if (found && path.status == NavMeshPathStatus.PathComplete)
            agent.SetPath(path);
        else
            Debug.LogWarning("No complete route: check the doorway, blockers and bake.", this);
    }
}
```

The 0.25 m sampling radius is deliberately small. A very large search radius can snap a target through a wall into a different room and produce a misleading success. Each target below is well inside its room, at floor height. Stop Play mode before editing test positions so changes persist.

| Target room | Position X | Position Y | Position Z | Expected |
|---|---:|---:|---:|---|
| Post-op | -8 | 0 | 3 | Complete |
| Recovery | 6 | 0 | 3 | Complete |
| Waste | 13 | 0 | 3 | Complete |
| Staff room | 12 | 0 | 10 | Complete |
| Storage | 12 | 0 | 15 | Complete |
| Pre-op | 12 | 0 | 21 | Complete |
| General Ward | -11 | 0 | 12 | Complete |
| Nurse Office | -4 | 0 | 12 | Complete |
| Anaesthesia | -11 | 0 | 22 | Complete |
| Washroom alcove | -4 | 0 | 22 | Complete |
| Operating Room | -10 | 0 | 31 | Complete |
| Sterile Core | 10 | 0 | 28 | Complete |
| ICU | 6 | 0 | 35 | Complete |
| Diagnostics | 12 | 0 | 35 | Complete |
| WC | -2.5 | 0 | 40 | Complete |
| Staff utility | 1.5 | 0 | 40 | Complete |
| Elevator interior | 5 | 0 | 12 | No sampled NavMesh / no complete route |
| Stair enclosure | 4 | 0 | 20 | No sampled NavMesh / no complete route |

These are starting test instructions; this guide has not executed the probe in your Editor. Record actual pass/fail results after constructing and baking the scene.


## Step 306 — Fix common navigation failures

| Symptom | What to check, in order |
|---|---|
| No blue floor anywhere | Package imported; no Console errors; Surface on MainHospital; Current Object Hierarchy selected; geometry active; colliders enabled; selected agent type correct; click Bake. |
| Blue floor ignores walls | Surface must collect walls as well as floors. Use Physics Colliders. Walls must have Box Colliders and Is Trigger off. Modifier must be Add or Modify Object, not Remove Object. Re-bake. |
| Doorway disconnected | Interior leaf must be inactive, not merely invisible. Check no full wall crosses the opening, door clear width, agent type radius, header bottom at 2.2 and voxel size 0.05. Re-bake. |
| Some corridors missing | Check floor transforms and whether a Not Walkable Modifier was accidentally placed on MainHospital/GEO_Floors. Check clear width after subtracting wall thickness. |
| Agent says it is not on NavMesh | Match Hospital_Blockout on Surface and Agent. Put root at (0,0,2); visual capsule alone is raised. Ensure bake exists and Surface/MainHospital are active. |
| PathPartial / PathInvalid | Check the failed route from the route table, disabled door leaves, a shifted slab, a stray collider, or a Not Walkable modifier on the wrong object. |
| Blue on roofs or walls | Apply the Not Walkable Modifier to ceilings/walls with Apply To Children enabled; keep geometry included. Re-bake. |
| Agent clips wall corners | Check Box Collider sizes against meshes; do not shrink radius just to hide a broken doorway. Agree on the actual character size with the agent developer. |
| Invisible wall in a doorway | Look for an enabled leaf whose Mesh Renderer alone was switched off, or an extra old wall cube. |
| Window can be walked through | Its CollisionPane GameObject and Box Collider must remain enabled; only its Mesh Renderer should be off. |
| Door was opened but path remains blocked | Initial bake included the closed leaf. Re-bake for the test, or have the team implement the agreed runtime obstacle/bake strategy. |

Do not add NavMesh Links to conceal accidental floor gaps or door-bake failures. This is a continuous ground floor; normal doorways should have continuous navigation.


## Step 307 — Remove the temporary probe and save a clean handoff

1. Stop Play mode.
2. Delete only **TEST_NavProbeRoot** (with its visual child) and **TEST_NavTarget** from the scene. The optional probe script can stay in Tests/Manual, or be removed if no longer needed.
3. Restore the intended initial door states: ordinary interior leaves inactive; entrance/elevator leaves active.
4. If any geometry or navigation settings changed during testing, select MainHospital and Bake again. Save the scene.
5. Restore ceiling visibility for a final perspective inspection, then hide it with the editor eye if that helps teammates inspect the layout.
6. The scene is ready for agent development only after the route tests pass, not just because every construction step has been entered.


# Part 5 — Team handoff and later work


## Step 308 — Give your teammates the world contract

Send the team the saved scene and these facts:

```text
Scene: Assets/Scenes/Main/Hospital_Blockout.unity
Unity: 6000.6.2f1
AI Navigation: 2.0.14
Units: 1 Unity unit = 1 metre
Walkable floor height: Y = 0
North: +Z; east: +X
Walls: 0.2 m thick; 3 m high
Door clear height: 2.2 m
Typical door clear width: 1.2 m
NavMesh type: Hospital_Blockout
Bake agent radius: 0.3 m; height: 1.8 m; step: 0.2 m
Initial state: interior door leaves inactive/open
Initial state: entrance and elevator active/closed
Excluded: elevator cabin, stair enclosure and sealed service shafts
No fixed clue, key, resource or enemy spawn positions have been created
```

Include **Hospital_Blockout.unity**, its `.meta`, the generated NavMesh data asset and its `.meta`, relevant project navigation settings (`ProjectSettings/NavMeshAreas.asset`), and any new assets/scripts actually used. Preserve existing version control conventions. Do not share Library/Temp as project source. Use the NavMesh Surface’s NavMesh Data field to locate the generated data asset instead of guessing its folder.

Do not move or scale MainHospital after integration without coordinating with teammates: their positions, paths and metadata will depend on the world coordinates.

| Agent from your document | What this skeleton provides | What comes later |
|---|---|---|
| Sound Hunter | Rooms, physical walls, open passages and validated NavMesh | Sound events, hearing/perception, states, search/chase behavior, spawn location |
| Resource Thief | Connected rooms and navigation routes | Item registry, utility/risk logic, agreed stash and spawn locations |
| Environment / Horror Director | Stable room names and future door locations | Event points, actual lights/audio, scare logic |
| Item Spawn Director | Accessible room areas and a basis for reachability checks | Candidate spawn points, CSP data, resource rules and randomized placements |

Your attachment describes design requirements, not executable instructions for this blockout. Examples in it such as a basement, pharmacy, fuse, ammo or batteries do not add rooms/items to your blueprint. Your current resource list is health, armour and sanity pills. A baked NavMesh also does not by itself implement or demonstrate the custom A*, risk-aware search, utility AI or CSP requested for the agents.

**Later elevator integration:** keep the cabin geometry. Agree whether AI ever enters it. If it should become navigable after unlocking, remove its Not Walkable exclusion and use an appropriate bake/runtime obstacle update strategy. For a dynamic door, a common later approach is to bake the doorway open, then use the closed door’s collider plus a carving obstacle; that is not the closed-door bake made in this guide. Never leave a permanently baked blocker and assume moving its mesh removes the baked obstruction.

**Later randomized item placement:** the key must not spawn in the locked elevator, sealed shafts or stair enclosure, nor behind the lock it opens. Candidate locations need complete-route validation, not only a nearby NavMesh sample. Do not treat the test target coordinates as a final item-placement design.

The two non-moving directors can already be developed with placeholder data. The hunter and thief developers can begin with this validated greybox; they do not need final textures, furniture or lighting.


## Step 309 — Final completion checklist

- [ ] Working in Hospital_Blockout, with original MainHospital scene preserved.
- [ ] All seven original floor transforms reconciled; no old corridor or duplicate floors left over.
- [ ] Forty floor slabs use the specified shared boundaries and all have top Y = 0.
- [ ] Room positions and connections checked against the source blueprint and the traced-plan image.
- [ ] Every wall run built once; same-room slab seams remain open.
- [ ] All doorway and window openings exist physically, not just visually.
- [ ] Ordinary interior doors inactive/open for initial navigation.
- [ ] Entrance and elevator placeholders active/closed.
- [ ] Window panes collide while their Mesh Renderers are off.
- [ ] Stair footprint retained but sealed; no upper floor.
- [ ] Optional ceilings meet wall tops, without replacing/removing floors.
- [ ] Hospital_Blockout agent type and NavMesh Surface configured and baked.
- [ ] No walkable roofs, wall tops, locked cabin or service-shaft islands.
- [ ] Actual complete-path and movement tests passed for every accessible room.
- [ ] Temporary probe objects removed; clean scene and NavMesh data saved.
- [ ] Team informed of dimensions, coordinates, agent size and door state.
- [ ] No fixed digit/key/resource spawns or gameplay agents added in this pass.

## Reference notes

- Main architectural reference: the hospital blueprint you supplied in this conversation.
- Agent context: `haunted_hospital_four_is_agents.md`, supplied by you. Its examples are not additions to the floor plan.
- Unity’s [Scene view navigation](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-windows-views-reference/using-the-scene-view/scene-view-navigation) documents the editor viewing controls.
- Unity’s [AI Navigation package documentation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html) covers the component-based navigation workflow. Controls in this guide were also checked against the installed 2.0.14 package’s `Documentation~/NavMeshSurface.md`, `NavigationWindow.md` and `NavMeshModifier.md`.
- The temporary test uses Unity’s documented [NavMesh.SamplePosition](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMesh.SamplePosition.html) and [NavMeshAgent.CalculatePath](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshAgent.CalculatePath.html) APIs. Test the supplied script in your Editor as instructed; it has not been compiled or run there during preparation of this document.

## Coordinate schedule — quick lookup after learning the steps

This table is for checking work, not a substitute for the individual steps above. Floor values are world/local values under identity parents. All floors have Y = -0.1, thickness 0.2 and zero rotation.

| Floor | X position | Z position | X size | Z size | West/east edges | South/north edges |
|---|---:|---:|---:|---:|---|---|

| F_MainCorridor | 0.625 | 4.75 | 4.25 | 9.5 | -1.5 / 2.75 | 0 / 9.5 |
| F_PostOpRecovery | -8.25 | 3.625 | 10 | 7.25 | -13.25 / -3.25 | 0 / 7.25 |
| F_RecoveryRoom | 6.625 | 3.5 | 7.75 | 7 | 2.75 / 10.5 | 0 / 7 |
| F_WasteDisposal | 13.25 | 3.5 | 5.5 | 7 | 10.5 / 16 | 0 / 7 |
| F_StaffRoom | 12.75 | 9.75 | 6.5 | 5.5 | 9.5 / 16 | 7 / 12.5 |
| F_Storage | 12.125 | 15.25 | 7.75 | 5.5 | 8.25 / 16 | 12.5 / 18 |
| F_Elevator | 5.5 | 11.875 | 5.5 | 5.75 | 2.75 / 8.25 | 9 / 14.75 |
| F_EntranceApron | -2.375 | 3.625 | 1.75 | 7.25 | -3.25 / -1.5 | 0 / 7.25 |
| F_ElevatorLobby | 6.125 | 8 | 6.75 | 2 | 2.75 / 9.5 | 7 / 9 |
| F_ElevatorServiceStrip | 8.875 | 10.75 | 1.25 | 3.5 | 8.25 / 9.5 | 9 / 12.5 |
| F_WestLowerHall | -4.125 | 8.375 | 5.25 | 2.25 | -6.75 / -1.5 | 7.25 / 9.5 |
| F_GeneralWard | -11.25 | 11.875 | 9 | 9.25 | -15.75 / -6.75 | 7.25 / 16.5 |
| F_NurseOffice | -3.75 | 13 | 6 | 7 | -6.75 / -0.75 | 9.5 / 16.5 |
| F_MainCorridor_Office | 1 | 13 | 3.5 | 7 | -0.75 / 2.75 | 9.5 / 16.5 |
| F_MainCorridor_Middle | 0.625 | 20 | 4.25 | 7 | -1.5 / 2.75 | 16.5 / 23.5 |
| F_WestCrossCorridor | -8.625 | 17.875 | 14.25 | 2.75 | -15.75 / -1.5 | 16.5 / 19.25 |
| F_RightLanding | 5.5 | 16 | 5.5 | 2.5 | 2.75 / 8.25 | 14.75 / 17.25 |
| F_StairFootprint | 4.375 | 20.375 | 3.25 | 6.25 | 2.75 / 6 | 17.25 / 23.5 |
| F_PreOpApproach | 7.125 | 20.375 | 2.25 | 6.25 | 6 / 8.25 | 17.25 / 23.5 |
| F_PreOp | 12.125 | 21 | 7.75 | 6 | 8.25 / 16 | 18 / 24 |
| F_AnaesthesiaPrep | -11.75 | 22.875 | 10 | 7.25 | -16.75 / -6.75 | 19.25 / 26.5 |
| F_WashroomAlcove | -4.125 | 21.875 | 5.25 | 5.25 | -6.75 / -1.5 | 19.25 / 24.5 |
| F_OperatingApproach | -4.125 | 25.5 | 5.25 | 2 | -6.75 / -1.5 | 24.5 / 26.5 |
| F_OperatingDoorRecess | -5 | 27 | 3.5 | 1 | -6.75 / -3.25 | 26.5 / 27.5 |
| F_OperatingApproachReturn | -2.375 | 27 | 1.75 | 1 | -3.25 / -1.5 | 26.5 / 27.5 |
| F_MainCorridor_Upper | 1.5 | 27.5 | 6 | 8 | -1.5 / 4.5 | 23.5 / 31.5 |
| F_SterileCore | 10.25 | 27.75 | 11.5 | 7.5 | 4.5 / 16 | 24 / 31.5 |
| F_SterileCore_SouthNotch | 6.375 | 23.75 | 3.75 | 0.5 | 4.5 / 8.25 | 23.5 / 24 |
| F_OperatingRoom1 | -11.25 | 31.125 | 9 | 9.25 | -15.75 / -6.75 | 26.5 / 35.75 |
| F_OperatingRoom1_East | -5 | 31.625 | 3.5 | 8.25 | -6.75 / -3.25 | 27.5 / 35.75 |
| F_OperatingRoom1_North | -7.375 | 36.25 | 5.25 | 1 | -10 / -4.75 | 35.75 / 36.75 |
| F_OperatingServiceShaft | -2.375 | 31.625 | 1.75 | 8.25 | -3.25 / -1.5 | 27.5 / 35.75 |
| F_MainCorridor_North | 0.125 | 33.625 | 3.25 | 4.25 | -1.5 / 1.75 | 31.5 / 35.75 |
| F_NorthLobby | -0.75 | 36.75 | 5 | 2 | -3.25 / 1.75 | 35.75 / 37.75 |
| F_NorthServiceAlcove | -4 | 36.75 | 1.5 | 2 | -4.75 / -3.25 | 35.75 / 37.75 |
| F_WC | -2.5 | 39.875 | 4.5 | 4.25 | -4.75 / -0.25 | 37.75 / 42 |
| F_StaffUtility | 1.875 | 39.875 | 4.25 | 4.25 | -0.25 / 4 | 37.75 / 42 |
| F_ICUTreatment | 5.375 | 34.625 | 7.25 | 6.25 | 1.75 / 9 | 31.5 / 37.75 |
| F_ICUTreatment_North | 6.5 | 39.875 | 5 | 4.25 | 4 / 9 | 37.75 / 42 |
| F_DiagnosticsTests | 12.5 | 35 | 7 | 7 | 9 / 16 | 31.5 / 38.5 |

### Door opening lookup

All openings have bottom Y = 0 and top Y = 2.2. “Centre along run” means X for a horizontal wall and Z for a vertical wall.

| Opening | Wall run | Direction | Fixed coordinate | Centre along run | Clear width | Connects |
|---|---|---|---|---:|---:|---|
| D_MainEntrance | W002 | H | Z = 0 | 0 | 1.8 | Corridor / Outside |
| D_PostOp | W037 | H | Z = 7.25 | -5.15 | 1.2 | Post-op / Corridor |
| D_Ward_PostOp | W036 | H | Z = 7.25 | -12 | 1.2 | Post-op / General Ward |
| D_Recovery | W032 | H | Z = 7 | 4.1 | 1.2 | Recovery / Corridor |
| D_Waste | W034 | H | Z = 7 | 11.8 | 1.2 | Waste / Staff room |
| D_StaffRoom | W035 | V | X = 9.5 | 8 | 1.2 | Staff room / Corridor |
| D_Elevator | W039 | H | Z = 9 | 4.1 | 1.8 | Elevator / Corridor |
| D_Storage | W051 | V | X = 8.25 | 16 | 1.2 | Storage / Corridor |
| D_Ward_North | W052 | H | Z = 16.5 | -9 | 1.2 | General Ward / Corridor |
| D_Ward_East | W038 | V | X = -6.75 | 8.35 | 1.2 | General Ward / Corridor |
| D_Office_North | W053 | H | Z = 16.5 | -5.75 | 1.2 | Nurse Office / Corridor |
| D_Office_South | W044 | H | Z = 9.5 | -2.05 | 1.2 | Nurse Office / Corridor |
| D_PreOp | W058 | V | X = 8.25 | 20.55 | 1.2 | Pre-op / Corridor |
| D_Anaesthesia | W070 | V | X = -6.75 | 25.45 | 1.2 | Anaesthesia / Corridor |
| D_Washroom | W069 | H | Z = 24.5 | -5.7 | 1.2 | Washroom alcove / Corridor |
| D_Operating | W073 | H | Z = 27.5 | -5.4 | 1.5 | Operating Room / Corridor |
| D_Sterile_West | W066 | V | X = 4.5 | 25.3 | 1.2 | Sterile Core / Corridor |
| D_ICU_Sterile | W078 | H | Z = 31.5 | 5.6 | 1.2 | ICU / Sterile Core |
| D_ICU_West | W080 | V | X = 1.75 | 36.65 | 1.2 | ICU / Corridor |
| D_Diagnostics | W081 | V | X = 9 | 33.25 | 1.2 | Diagnostics / ICU |
| D_WC | W087 | H | Z = 37.75 | -1.15 | 1.2 | WC / Corridor |
| D_StaffUtility | W088 | H | Z = 37.75 | 0.65 | 1.2 | Staff utility / Corridor |