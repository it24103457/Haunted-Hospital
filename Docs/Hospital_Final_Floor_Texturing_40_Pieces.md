# Haunted Hospital — final flooring for all 40 floor pieces

**Unity 6000.6.2f1 · MainHospital scene · 1 Unity unit = 1 metre**

This guide covers all 40 floor Cubes currently saved in Assets/Scenes/Main/MainHospital.unity. Their X and Z sizes were checked against Docs/hospital_skeleton_coordinate_schedule.json. These steps change only their materials: leave each Cube's Position, Rotation, Scale, collider, and parent as they are. The scene has not been edited for you.

## How the floors match your walls

Your existing wall materials use worn plaster in off-white, muted sage (#CBD2C4), and warm ivory (#E8E3D7). The flooring palette is grey, sage-grey, warm stone, and charcoal. The blueprint's blue/green/pink fills identify rooms; they are not literal flooring colours. All parts of one room use the same floor family, while finishes change at room boundaries.

## Download and import the missing textures

Two texture sets are already in the project: Worn Tile Floor and Tiles 107. Keep these existing files. Download the remaining three 2K sets. For each Poly Haven asset, download only Diffuse and Normal (GL). Extract the files, then drag those two images into Assets/Art/Textures/Hospital/Floors in Unity's Project window.

| Texture set | Source | Rooms | Image coverage |
|---|---|---|---|
| Worn Tile Floor | [Poly Haven](https://polyhaven.com/a/worn_tile_floor) | Corridors | 2 × 2 m |
| Tiles 107 | [ambientCG](https://ambientcg.com/view?id=Tiles107) | Clinical, diagnostics, bathrooms, waste | 1 × 1 m |
| Old Mosaic Floor | [Poly Haven](https://polyhaven.com/a/old_mosaic_floor) | Wards, recovery, staff | 2 × 2 m |
| Concrete Floor Worn 02 | [Poly Haven](https://polyhaven.com/a/concrete_floor_worn_02) | Service, storage, stairs | 2 × 2 m |
| Rubber Tiles | [Poly Haven](https://polyhaven.com/a/rubber_tiles) | Elevator cabin | 2 × 2 m |

All listed source assets are CC0. For each newly imported Diffuse image, select it, set Texture Type = Default and Wrap Mode = Repeat, then click Apply. For each Normal (GL) image, select it, set Texture Type = Normal map and Wrap Mode = Repeat, leave Create from Grayscale off, then click Apply. Use Max Size = 2048 for these 2K images. The existing Tiles 107 images are in Assets/Art/Textures/Hospital/Floors/Clinical.

## Create the eight master materials

In the Project window, open Assets/Art/Materials/Floors. Right-click empty space → Create → Material. Create the eight materials below. For every material select Shader = Universal Render Pipeline/Lit, Workflow Mode = Metallic, Surface Type = Opaque, Metallic = 0, Emission = Off, Base Map Tiling = (1, 1), and Base Map Offset = (0, 0). Drag that texture set's Diffuse/Color image into Base Map and its Normal (GL) image into Normal Map. Enter the exact tint in the colour swatch next to Base Map, with alpha fully opaque.

| Material name | Texture set | Tint | Smoothness | Normal strength |
|---|---|---:|---:|---:|
| MAT_Floor_MASTER_Corridor | Worn Tile Floor | #FFFFFF | 0.25 | 0.3 |
| MAT_Floor_MASTER_Ward | Old Mosaic Floor | #E2DDD2 | 0.22 | 0.2 |
| MAT_Floor_MASTER_Clinical | Tiles 107 | #DCE4DF | 0.3 | 0.2 |
| MAT_Floor_MASTER_Diagnostics | Tiles 107 | #DCE2E5 | 0.3 | 0.2 |
| MAT_Floor_MASTER_WetRoom | Tiles 107 | #E1E5DF | 0.25 | 0.25 |
| MAT_Floor_MASTER_Waste | Tiles 107 | #ADB7B0 | 0.17 | 0.25 |
| MAT_Floor_MASTER_Service | Concrete Floor Worn 02 | #C8C8C4 | 0.12 | 0.25 |
| MAT_Floor_MASTER_Elevator | Rubber Tiles | #C9D0CA | 0.14 | 0.2 |

The clinical and diagnostics masters can use the Tiles 107 images already imported. They differ in tint. The wet-room and waste masters use those same images with smaller-looking tiles. The masters are templates: each floor gets its own named duplicate below so its exact tiling is independent.

## Follow this procedure for every numbered floor

1. Expand MainHospital > GEO_Floors in the Hierarchy. Select the exact F_ object named in the numbered step.
2. In Assets/Art/Materials/Floors, select the named MASTER material and press Ctrl + D to duplicate it. Rename the copy to the exact final material name in that step. Always duplicate the master, since another room's material may already have different tiling.
3. Select the copy. In its Inspector, find Surface Inputs > Base Map > Tiling. Enter the two numbers given in that step. Keep Offset X and Offset Y at 0.
4. Select the floor Cube again. In Mesh Renderer > Materials, drag the final material into Element 0. This replaces only that floor's visible material.
5. Continue to the next numbered step. Press Ctrl + S frequently to save the scene.

The values below come from each Cube's Scale X and Scale Z. Worn corridor tile, old mosaic, concrete, and rubber repeat once per 2 metres. Clinical tile repeats once per 1 metre; the wet-room version repeats once per 0.5 metres. For example, F_MainCorridor is 4.25 × 9.5 m, so its tiling is (4.25 ÷ 2, 9.5 ÷ 2) = (2.125, 4.75). Do not resize a Cube to change its texture.

## All 40 floors, one by one

### 1. F_MainCorridor — Corridor

Select MainHospital > GEO_Floors > **F_MainCorridor**. The top face is **4.25 m along X × 9.5 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_MainCorridor**. Set:

```text
Base Map Tiling X = 2.125
Base Map Tiling Y = 4.75
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_MainCorridor** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 2. F_PostOpRecovery — Post-op

Select MainHospital > GEO_Floors > **F_PostOpRecovery**. The top face is **10 m along X × 7.25 m along Z**. Duplicate **MAT_Floor_MASTER_Ward** and rename it **MAT_Floor_Final_PostOpRecovery**. Set:

```text
Base Map Tiling X = 5
Base Map Tiling Y = 3.625
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_PostOpRecovery** into this floor's **Mesh Renderer > Materials > Element 0**. Use muted warm terrazzo beside warm ivory and off-white plaster.

### 3. F_RecoveryRoom — Recovery

Select MainHospital > GEO_Floors > **F_RecoveryRoom**. The top face is **7.75 m along X × 7 m along Z**. Duplicate **MAT_Floor_MASTER_Ward** and rename it **MAT_Floor_Final_RecoveryRoom**. Set:

```text
Base Map Tiling X = 3.875
Base Map Tiling Y = 3.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_RecoveryRoom** into this floor's **Mesh Renderer > Materials > Element 0**. Use muted warm terrazzo beside warm ivory and off-white plaster.

### 4. F_WasteDisposal — Waste

Select MainHospital > GEO_Floors > **F_WasteDisposal**. The top face is **5.5 m along X × 7 m along Z**. Duplicate **MAT_Floor_MASTER_Waste** and rename it **MAT_Floor_Final_WasteDisposal**. Set:

```text
Base Map Tiling X = 11
Base Map Tiling Y = 14
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_WasteDisposal** into this floor's **Mesh Renderer > Materials > Element 0**. Use the same small tile pattern with a darker grey-green tint.

### 5. F_StaffRoom — Staff room

Select MainHospital > GEO_Floors > **F_StaffRoom**. The top face is **6.5 m along X × 5.5 m along Z**. Duplicate **MAT_Floor_MASTER_Ward** and rename it **MAT_Floor_Final_StaffRoom**. Set:

```text
Base Map Tiling X = 3.25
Base Map Tiling Y = 2.75
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_StaffRoom** into this floor's **Mesh Renderer > Materials > Element 0**. Use muted warm terrazzo beside warm ivory and off-white plaster.

### 6. F_Storage — Storage

Select MainHospital > GEO_Floors > **F_Storage**. The top face is **7.75 m along X × 5.5 m along Z**. Duplicate **MAT_Floor_MASTER_Service** and rename it **MAT_Floor_Final_Storage**. Set:

```text
Base Map Tiling X = 3.875
Base Map Tiling Y = 2.75
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_Storage** into this floor's **Mesh Renderer > Materials > Element 0**. Use worn grey concrete for the enclosed service footprint.

### 7. F_Elevator — Elevator

Select MainHospital > GEO_Floors > **F_Elevator**. The top face is **5.5 m along X × 5.75 m along Z**. Duplicate **MAT_Floor_MASTER_Elevator** and rename it **MAT_Floor_Final_Elevator**. Set:

```text
Base Map Tiling X = 2.75
Base Map Tiling Y = 2.875
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_Elevator** into this floor's **Mesh Renderer > Materials > Element 0**. Use charcoal rubber to distinguish the exit cabin.

### 8. F_EntranceApron — Corridor

Select MainHospital > GEO_Floors > **F_EntranceApron**. The top face is **1.75 m along X × 7.25 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_EntranceApron**. Set:

```text
Base Map Tiling X = 0.875
Base Map Tiling Y = 3.625
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_EntranceApron** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 9. F_ElevatorLobby — Corridor

Select MainHospital > GEO_Floors > **F_ElevatorLobby**. The top face is **6.75 m along X × 2 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_ElevatorLobby**. Set:

```text
Base Map Tiling X = 3.375
Base Map Tiling Y = 1
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_ElevatorLobby** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 10. F_ElevatorServiceStrip — Service shaft

Select MainHospital > GEO_Floors > **F_ElevatorServiceStrip**. The top face is **1.25 m along X × 3.5 m along Z**. Duplicate **MAT_Floor_MASTER_Service** and rename it **MAT_Floor_Final_ElevatorServiceStrip**. Set:

```text
Base Map Tiling X = 0.625
Base Map Tiling Y = 1.75
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_ElevatorServiceStrip** into this floor's **Mesh Renderer > Materials > Element 0**. Use worn grey concrete for the enclosed service footprint.

### 11. F_WestLowerHall — Corridor

Select MainHospital > GEO_Floors > **F_WestLowerHall**. The top face is **5.25 m along X × 2.25 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_WestLowerHall**. Set:

```text
Base Map Tiling X = 2.625
Base Map Tiling Y = 1.125
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_WestLowerHall** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 12. F_GeneralWard — General Ward

Select MainHospital > GEO_Floors > **F_GeneralWard**. The top face is **9 m along X × 9.25 m along Z**. Duplicate **MAT_Floor_MASTER_Ward** and rename it **MAT_Floor_Final_GeneralWard**. Set:

```text
Base Map Tiling X = 4.5
Base Map Tiling Y = 4.625
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_GeneralWard** into this floor's **Mesh Renderer > Materials > Element 0**. Use muted warm terrazzo beside warm ivory and off-white plaster.

### 13. F_NurseOffice — Nurse Office

Select MainHospital > GEO_Floors > **F_NurseOffice**. The top face is **6 m along X × 7 m along Z**. Duplicate **MAT_Floor_MASTER_Ward** and rename it **MAT_Floor_Final_NurseOffice**. Set:

```text
Base Map Tiling X = 3
Base Map Tiling Y = 3.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_NurseOffice** into this floor's **Mesh Renderer > Materials > Element 0**. Use muted warm terrazzo beside warm ivory and off-white plaster.

### 14. F_MainCorridor_Office — Corridor

Select MainHospital > GEO_Floors > **F_MainCorridor_Office**. The top face is **3.5 m along X × 7 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_MainCorridor_Office**. Set:

```text
Base Map Tiling X = 1.75
Base Map Tiling Y = 3.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_MainCorridor_Office** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 15. F_MainCorridor_Middle — Corridor

Select MainHospital > GEO_Floors > **F_MainCorridor_Middle**. The top face is **4.25 m along X × 7 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_MainCorridor_Middle**. Set:

```text
Base Map Tiling X = 2.125
Base Map Tiling Y = 3.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_MainCorridor_Middle** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 16. F_WestCrossCorridor — Corridor

Select MainHospital > GEO_Floors > **F_WestCrossCorridor**. The top face is **14.25 m along X × 2.75 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_WestCrossCorridor**. Set:

```text
Base Map Tiling X = 7.125
Base Map Tiling Y = 1.375
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_WestCrossCorridor** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 17. F_RightLanding — Corridor

Select MainHospital > GEO_Floors > **F_RightLanding**. The top face is **5.5 m along X × 2.5 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_RightLanding**. Set:

```text
Base Map Tiling X = 2.75
Base Map Tiling Y = 1.25
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_RightLanding** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 18. F_StairFootprint — Stair enclosure

Select MainHospital > GEO_Floors > **F_StairFootprint**. The top face is **3.25 m along X × 6.25 m along Z**. Duplicate **MAT_Floor_MASTER_Service** and rename it **MAT_Floor_Final_StairFootprint**. Set:

```text
Base Map Tiling X = 1.625
Base Map Tiling Y = 3.125
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_StairFootprint** into this floor's **Mesh Renderer > Materials > Element 0**. Use worn grey concrete for the enclosed service footprint.

### 19. F_PreOpApproach — Corridor

Select MainHospital > GEO_Floors > **F_PreOpApproach**. The top face is **2.25 m along X × 6.25 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_PreOpApproach**. Set:

```text
Base Map Tiling X = 1.125
Base Map Tiling Y = 3.125
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_PreOpApproach** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 20. F_PreOp — Pre-op

Select MainHospital > GEO_Floors > **F_PreOp**. The top face is **7.75 m along X × 6 m along Z**. Duplicate **MAT_Floor_MASTER_Ward** and rename it **MAT_Floor_Final_PreOp**. Set:

```text
Base Map Tiling X = 3.875
Base Map Tiling Y = 3
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_PreOp** into this floor's **Mesh Renderer > Materials > Element 0**. Use muted warm terrazzo beside warm ivory and off-white plaster.

### 21. F_AnaesthesiaPrep — Anaesthesia

Select MainHospital > GEO_Floors > **F_AnaesthesiaPrep**. The top face is **10 m along X × 7.25 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_AnaesthesiaPrep**. Set:

```text
Base Map Tiling X = 10
Base Map Tiling Y = 7.25
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_AnaesthesiaPrep** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 22. F_WashroomAlcove — Washroom alcove

Select MainHospital > GEO_Floors > **F_WashroomAlcove**. The top face is **5.25 m along X × 5.25 m along Z**. Duplicate **MAT_Floor_MASTER_WetRoom** and rename it **MAT_Floor_Final_WashroomAlcove**. Set:

```text
Base Map Tiling X = 10.5
Base Map Tiling Y = 10.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_WashroomAlcove** into this floor's **Mesh Renderer > Materials > Element 0**. Use a tighter white tile grid for washrooms and utility.

### 23. F_OperatingApproach — Corridor

Select MainHospital > GEO_Floors > **F_OperatingApproach**. The top face is **5.25 m along X × 2 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_OperatingApproach**. Set:

```text
Base Map Tiling X = 2.625
Base Map Tiling Y = 1
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingApproach** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 24. F_OperatingDoorRecess — Corridor

Select MainHospital > GEO_Floors > **F_OperatingDoorRecess**. The top face is **3.5 m along X × 1 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_OperatingDoorRecess**. Set:

```text
Base Map Tiling X = 1.75
Base Map Tiling Y = 0.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingDoorRecess** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 25. F_OperatingApproachReturn — Corridor

Select MainHospital > GEO_Floors > **F_OperatingApproachReturn**. The top face is **1.75 m along X × 1 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_OperatingApproachReturn**. Set:

```text
Base Map Tiling X = 0.875
Base Map Tiling Y = 0.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingApproachReturn** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 26. F_MainCorridor_Upper — Corridor

Select MainHospital > GEO_Floors > **F_MainCorridor_Upper**. The top face is **6 m along X × 8 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_MainCorridor_Upper**. Set:

```text
Base Map Tiling X = 3
Base Map Tiling Y = 4
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_MainCorridor_Upper** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 27. F_SterileCore — Sterile Core

Select MainHospital > GEO_Floors > **F_SterileCore**. The top face is **11.5 m along X × 7.5 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_SterileCore**. Set:

```text
Base Map Tiling X = 11.5
Base Map Tiling Y = 7.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_SterileCore** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 28. F_SterileCore_SouthNotch — Sterile Core

Select MainHospital > GEO_Floors > **F_SterileCore_SouthNotch**. The top face is **3.75 m along X × 0.5 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_SterileCore_SouthNotch**. Set:

```text
Base Map Tiling X = 3.75
Base Map Tiling Y = 0.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_SterileCore_SouthNotch** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 29. F_OperatingRoom1 — Operating Room

Select MainHospital > GEO_Floors > **F_OperatingRoom1**. The top face is **9 m along X × 9.25 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_OperatingRoom1**. Set:

```text
Base Map Tiling X = 9
Base Map Tiling Y = 9.25
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingRoom1** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 30. F_OperatingRoom1_East — Operating Room

Select MainHospital > GEO_Floors > **F_OperatingRoom1_East**. The top face is **3.5 m along X × 8.25 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_OperatingRoom1_East**. Set:

```text
Base Map Tiling X = 3.5
Base Map Tiling Y = 8.25
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingRoom1_East** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 31. F_OperatingRoom1_North — Operating Room

Select MainHospital > GEO_Floors > **F_OperatingRoom1_North**. The top face is **5.25 m along X × 1 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_OperatingRoom1_North**. Set:

```text
Base Map Tiling X = 5.25
Base Map Tiling Y = 1
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingRoom1_North** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 32. F_OperatingServiceShaft — Operating service shaft

Select MainHospital > GEO_Floors > **F_OperatingServiceShaft**. The top face is **1.75 m along X × 8.25 m along Z**. Duplicate **MAT_Floor_MASTER_Service** and rename it **MAT_Floor_Final_OperatingServiceShaft**. Set:

```text
Base Map Tiling X = 0.875
Base Map Tiling Y = 4.125
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_OperatingServiceShaft** into this floor's **Mesh Renderer > Materials > Element 0**. Use worn grey concrete for the enclosed service footprint.

### 33. F_MainCorridor_North — Corridor

Select MainHospital > GEO_Floors > **F_MainCorridor_North**. The top face is **3.25 m along X × 4.25 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_MainCorridor_North**. Set:

```text
Base Map Tiling X = 1.625
Base Map Tiling Y = 2.125
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_MainCorridor_North** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 34. F_NorthLobby — Corridor

Select MainHospital > GEO_Floors > **F_NorthLobby**. The top face is **5 m along X × 2 m along Z**. Duplicate **MAT_Floor_MASTER_Corridor** and rename it **MAT_Floor_Final_NorthLobby**. Set:

```text
Base Map Tiling X = 2.5
Base Map Tiling Y = 1
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_NorthLobby** into this floor's **Mesh Renderer > Materials > Element 0**. Keep the public corridor finish consistent.

### 35. F_NorthServiceAlcove — North service alcove

Select MainHospital > GEO_Floors > **F_NorthServiceAlcove**. The top face is **1.5 m along X × 2 m along Z**. Duplicate **MAT_Floor_MASTER_Service** and rename it **MAT_Floor_Final_NorthServiceAlcove**. Set:

```text
Base Map Tiling X = 0.75
Base Map Tiling Y = 1
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_NorthServiceAlcove** into this floor's **Mesh Renderer > Materials > Element 0**. Use worn grey concrete for the enclosed service footprint.

### 36. F_WC — WC

Select MainHospital > GEO_Floors > **F_WC**. The top face is **4.5 m along X × 4.25 m along Z**. Duplicate **MAT_Floor_MASTER_WetRoom** and rename it **MAT_Floor_Final_WC**. Set:

```text
Base Map Tiling X = 9
Base Map Tiling Y = 8.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_WC** into this floor's **Mesh Renderer > Materials > Element 0**. Use a tighter white tile grid for washrooms and utility.

### 37. F_StaffUtility — Staff utility

Select MainHospital > GEO_Floors > **F_StaffUtility**. The top face is **4.25 m along X × 4.25 m along Z**. Duplicate **MAT_Floor_MASTER_WetRoom** and rename it **MAT_Floor_Final_StaffUtility**. Set:

```text
Base Map Tiling X = 8.5
Base Map Tiling Y = 8.5
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_StaffUtility** into this floor's **Mesh Renderer > Materials > Element 0**. Use a tighter white tile grid for washrooms and utility.

### 38. F_ICUTreatment — ICU

Select MainHospital > GEO_Floors > **F_ICUTreatment**. The top face is **7.25 m along X × 6.25 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_ICUTreatment**. Set:

```text
Base Map Tiling X = 7.25
Base Map Tiling Y = 6.25
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_ICUTreatment** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 39. F_ICUTreatment_North — ICU

Select MainHospital > GEO_Floors > **F_ICUTreatment_North**. The top face is **5 m along X × 4.25 m along Z**. Duplicate **MAT_Floor_MASTER_Clinical** and rename it **MAT_Floor_Final_ICUTreatment_North**. Set:

```text
Base Map Tiling X = 5
Base Map Tiling Y = 4.25
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_ICUTreatment_North** into this floor's **Mesh Renderer > Materials > Element 0**. Use pale sage-grey clinical tile beside off-white and sage plaster.

### 40. F_DiagnosticsTests — Diagnostics

Select MainHospital > GEO_Floors > **F_DiagnosticsTests**. The top face is **7 m along X × 7 m along Z**. Duplicate **MAT_Floor_MASTER_Diagnostics** and rename it **MAT_Floor_Final_DiagnosticsTests**. Set:

```text
Base Map Tiling X = 7
Base Map Tiling Y = 7
Base Map Offset X = 0
Base Map Offset Y = 0
```

Drag **MAT_Floor_Final_DiagnosticsTests** into this floor's **Mesh Renderer > Materials > Element 0**. Use a slightly cooler variation of the clinical tile.

## Final checks

1. Count 40 F_ objects under GEO_Floors. Each should have its matching MAT_Floor_Final_ material in Mesh Renderer Element 0.
2. From the entrance to the north lobby, all public corridor and landing pieces use the same worn tile set. The elevator lobby is corridor tile; only the elevator cabin uses rubber.
3. The three F_OperatingRoom1 pieces, two F_ICUTreatment pieces, and two F_SterileCore pieces share the clinical family within each room. Do not change material at the seam inside a room.
4. F_WC, F_WashroomAlcove, and F_StaffUtility use the denser wet-room tile. F_WasteDisposal uses its darker tint. Storage, stairs, and service alcoves use grey concrete.
5. View the floor from Top and from an angled Scene view. Check that warm stone beside warm plaster, clinical tile beside sage/off-white plaster, and darker service floors beside worn plaster look coherent. Press Ctrl + S.

**Grout-line note:** Separate Cube meshes start their texture patterns independently. These exact Tiling values keep physical repeat size consistent, but a grout line may jump at an open join between two Cubes. If this appears, align that join using the assigned material's Offset fields while looking at it in Unity. The direction of the offset depends on the top-face UV orientation of the Cube. Keep these Tiling values and all floor transforms unchanged.
