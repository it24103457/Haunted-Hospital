# Animate the hospital first-aid key box in Unity

For Unity 6000.6.2f1. This is a manual beginner guide; no scenes have been edited for you.

**Your new exception:** you will create the first-aid box's opening and closing motion. All other opening props still need supplied animations. Your teammate handles when animations play, interaction, random key spawning and collection. You do not need Blender or gameplay scripts for this guide.

Make **one reusable prefab**, with two motion clips and two still poses, then use it at all four key-box locations. These are the boxes that may contain the exit key, not the health pickup briefcases.

## 1. Download the source model

Use [Emergency MedKit — RayznGames](https://rayzngames.itch.io/emergency-medkit-3d-model).

1. Open the page and select **Download Now**.
2. Use the free/name-your-price download option.
3. Download **Emergency_MedKit.unitypackage**, rather than the ZIP that might need a different import workflow.
4. Keep an attribution entry with the asset: `Emergency MedKit by Ramon Serrate / RayznGames — CC BY 4.0`, plus the source link.

The creator describes modular mechanical parts that can be animated. That does **not** establish that ready-made opening clips are supplied. The instructions below create our own motion. The package is not currently verified inside your project, so its internal object names, exact lid construction and hinge coordinates must be checked after import. This case is a portable medkit adapted to a wall mounting, rather than a verified purpose-built wall cabinet.

## 2. Prepare a separate work scene

1. Save your current hospital scene with **Ctrl+S**.
2. In the Project window, create these folders if they do not already exist:
   - `Assets/Animations/HospitalProps/FirstAidBox`
   - `Assets/Prefabs/HospitalProps`
   - `Assets/Scenes/Sandbox`
3. Choose **File > New Scene** and use a basic scene.
4. Save it as `Assets/Scenes/Sandbox/FirstAidBox_AnimationTest.unity`.
5. Import the downloaded file using **Assets > Import Package > Custom Package**. Keep its model and material dependencies selected.
6. Find its prefab in the imported folders and drag one copy into the Hierarchy. Actual imported names may differ; do not search for names invented by this guide.
7. Select that instance and press **F** while your pointer is over the Scene view to frame it.

Keep the Editor's large Play button off while building and recording clips. The Animation window has its own playback controls for previewing motion.

## 3. Confirm the lid can move independently

1. Expand the imported instance in the Hierarchy.
2. Select its child objects individually. Observe which surfaces highlight in the Scene view.
3. Identify the fixed case body, moving lid and any separate locks/handles.
4. With animation recording off, temporarily rotate the suspected lid by about 10 degrees in the Inspector.
5. Confirm only the intended moving assembly changes. Immediately press **Ctrl+Z** to restore it.

You need either a separate rigid lid object or a usable existing lid joint. The steps below use a separate rigid lid object. **Unpacking a prefab does not split a combined mesh.** If selecting the lid always selects the entire case as one mesh, or it is a skinned mesh whose joint setup is unclear, pause here and show the imported hierarchy before continuing. Do not manufacture a hinge by rotating the whole box. The asset's public description is not enough to guarantee its exact hierarchy.

For an ordinary prefab instance that prevents reorganizing its parts, right-click the scene instance and choose **Prefab > Unpack Completely**. This affects that instance, not the downloaded source. Do not delete bones or reorganize a skinned rig using these rigid-object instructions.

## 4. Establish the box orientation and size

Our placement convention is:

| Property | Target |
|---|---|
| Closed outer width | 0.55 m, along local X |
| Closed outer height | 0.45 m, along local Y |
| Closed outer depth | 0.18 m, along local Z |
| Origin | Centre of the closed box |
| Front/opening direction | Local +Z |
| Back against wall | Local -Z |
| Hinge axis | Vertical, parallel to local Y |

1. Create an Empty at the scene root. Rename it **PF_KeyBox**.
2. In its Transform menu, choose **Reset**: Position `(0,0,0)`, Rotation `(0,0,0)`, Scale `(1,1,1)`.
3. Orient the source case so its closed front faces +Z and its physical hinge runs vertically. You may rotate the source model 90 degrees to stand its original hinge upright. This is a static model adjustment, not an animation.
4. Create a temporary Cube named **SizeReference** at the scene root.
5. Set its Position and Rotation to `(0,0,0)` and Scale to `(0.55,0.45,0.18)`.
6. Use the Scene view's wireframe display to compare the case with this reference from front, side and top. Centre and resize the closed case until it fits the envelope. Ignore optional medical contents when measuring the outer shell.
7. Prefer uniform scaling if the case's proportions already fit. If different scale values are required, apply them to the visual parts, not the final animated hinge or PF_KeyBox root.

The target dimensions are exact design dimensions. A source model's required scale multiplier depends on its original size; `Scale = (0.55,0.45,0.18)` is **not** a universal instruction for imported meshes. For a measured original width W, a uniform width fit is `0.55 / W`; check the resulting height and depth as well.

## 5. Build the final hierarchy before making clips

Under PF_KeyBox, create these Empty objects using right-click > **Create Empty**. Reset their local transforms first:

```text
PF_KeyBox                 Animator will go here
├── Body                  fixed shell and fixed hardware
│   └── [fixed model parts]
├── LidHinge              the only transform we animate
│   └── [lid and attached moving hardware]
└── KeySpawn              empty marker inside the fixed box
```

1. Parent fixed visual parts under **Body**.
2. Leave the lid temporarily outside **LidHinge** until the next step positions the hinge.
3. Preserve the parts' visible world positions while reparenting. Unity normally preserves their world pose when dragging between parents; verify that nothing jumps or changes size.
4. Keep PF_KeyBox, Body and LidHinge at Scale `(1,1,1)`. Fit dimensions on their visual children. Do not leave a nonuniformly scaled ancestor above LidHinge: it can distort rotation.
5. Hide removable medical contents that occupy the intended key space. Keep enough fixed interior detail to look plausible.
6. If the case has separate lock catches, arrange them in a visibly released pose while recording is off. Body-mounted parts stay with Body; parts physically attached to the lid move with the lid. This simple animation swings an already-unlatched lid; it does not simulate a locking mechanism.
7. Remove an unused empty source wrapper only after all necessary parts have been accounted for. Do not remove material/model files from the Project window.

Finalize these names now. Animation clips refer to hierarchy paths; renaming or reparenting the hinge after recording can break those bindings.

## 6. Put LidHinge on the physical hinge line

The imported lid's pivot might be at its centre. Our Empty supplies the correct pivot without editing the mesh.

1. Select **LidHinge**. Keep Rotation `(0,0,0)` and Scale `(1,1,1)`.
2. Set the Scene toolbar to **Pivot** rather than Center, and **Local** rather than Global.
3. Press **W** for the Move tool.
4. Look straight at the closed front using the Scene axis gizmo. Move the Empty sideways to the physical hinge pins.
5. Look from the side/top and adjust its Z position onto the centre line of the hinge pins, not the outside edge of the front cover.
6. Its Y position can remain `0` if the vertical hinge line passes through the box's mid-height.
7. Now drag the lid and all lid-attached parts under LidHinge. They must remain in their original closed pose.
8. Temporarily set LidHinge Rotation Y to `-15` and inspect the result.

For a hinge on the **negative-X edge**, negative Y rotation should move the lid outward toward +Z. Use `-100` degrees as the target open angle. If the mechanical hinge is on the **positive-X edge**, use positive rotation and a target of `+100` degrees instead.

The hinge's exact X and Z coordinates come from the imported model; they cannot be inferred from the box envelope alone. Do not assume the pivot is exactly on the outer bounding edge.

Check that the hinge edge stays in place while the free edge swings outward. If the lid orbits away from the case, Undo the test, move the lid temporarily out of LidHinge while preserving its pose, correct the Empty's position, and parent the lid back. Moving an Empty while the lid is inside it would also displace the lid.

After checking, restore LidHinge Rotation to `(0,0,0)`. Test the full 100-degree opening once, then restore zero again. If the mechanical model collides with itself before 100 degrees, choose a smaller clear angle and use that same angle everywhere below.

## 7. Create the closed pose clip

1. Select **PF_KeyBox**, not the lid, in the Hierarchy.
2. Open **Window > Animation > Animation**. This is the timeline window; **Animator** is a different window.
3. Click **Create** and save the clip as `Assets/Animations/HospitalProps/FirstAidBox/FirstAid_Closed.anim`.
4. Unity creates an Animator/controller for an object that does not already have one. Keep the Animator on PF_KeyBox. Name the controller **FirstAidBox_Preview.controller** if desired.
5. Set the clip's **Samples** to `60`. If this field is hidden, look in the Animation window's options for **Show Sample Rate**. Widen the window if its controls are hidden.
6. Click **Add Property** and expand **LidHinge > Transform > Rotation**, then click the plus beside Rotation.
7. Keep only the hinge's Rotation property in this clip. Do not add the root's Position, Rotation or Scale.
8. Expand the Rotation row into its X, Y and Z channels.
9. Make two keys: frame `0` and frame `1`, both with X=`0`, Y=`0`, Z=`0`. Use the timeline's Add Keyframe button at the selected frame if necessary. Remove any automatically created later key by selecting its diamond and pressing Delete.
10. Turn **Record** off when finished. It is the round recording button in the Animation window.

At 60 samples per second, frame 1 means 1/60 second. It does not mean one second. This very short clip simply supplies the closed pose.

## 8. Create the opening animation

1. Keep PF_KeyBox selected. In the Animation window's clip-name dropdown, choose **Create New Clip**.
2. Save it as **FirstAid_Open.anim** in the same folder.
3. Set Samples to `60`.
4. Add **LidHinge > Transform > Rotation**.
5. Delete automatically generated endpoint keys if they are at other times. We want only these two key times:

| Frame | Time | X | Y, negative-X hinge | Z |
|---|---|---|---|---|
| 0 | 0.00 s | 0 | 0 | 0 |
| 36 | 0.60 s | 0 | -100 | 0 |

For a positive-X hinge, the second Y value is `+100`. Substitute your tested smaller angle if required.

6. Set the playhead/current frame field to `0`.
7. Turn Record on, select LidHinge in the Hierarchy, and enter Rotation `(0,0,0)` in the Inspector. If it was already zero, explicitly use **Add Keyframe** for the Rotation row to ensure there is a key.
8. If changing selection makes the Animation window switch targets, reselect PF_KeyBox and use the Animation window's lock control before selecting LidHinge again.
9. Move the playhead to frame `36`. At 60 fps a seconds:frames ruler may label this `0:36`.
10. Enter Rotation `(0,-100,0)` on LidHinge, or your approved positive/smaller angle. A key diamond should appear at frame 36.
11. Turn Record off immediately.
12. Press the small **Play** button inside the Animation window. Watch the lid move; the case body must stay fixed.
13. Stop playback and drag the playhead manually between the two endpoints. Check the complete swing, not only the final pose.

If you accidentally record body/root movement, delete that unwanted property row from the clip. Do not try to counteract it by changing your wall placement coordinates.

## 9. Give the motion a gentle start and stop

1. At the bottom of the Animation window, switch from **Dopesheet** to **Curves**.
2. Select the hinge's Y rotation curve.
3. Select the first and last keys. Right-click and choose **Clamped Auto** tangents if available.
4. Inspect the curve: it should move smoothly between zero and the open angle without passing beyond either endpoint.
5. Preview again. If it overshoots, use **Linear** tangents as a simple predictable fallback.

Do not add wobble, bounce or extra position keys. A steady 0.6-second swing suits this small mechanical prop. Unity may display an equivalent Euler angle, such as 260 instead of -100; judge the previewed path. If it takes a long spin, return to just the two intended local rotation keys and re-enter the tested short opening rotation.

## 10. Create the closing and open-pose clips

Repeat the Create New Clip workflow, adding only LidHinge Rotation each time. Use 60 Samples for all clips.

**FirstAid_Close.anim**:

| Frame | X | Y, negative-X hinge | Z |
|---|---|---|---|
| 0 | 0 | -100 | 0 |
| 36 | 0 | 0 | 0 |

Use the same tangent treatment as the opening clip. Closing should start at exactly the opening clip's final pose.

**FirstAid_OpenIdle.anim**:

| Frame | X | Y, negative-X hinge | Z |
|---|---|---|---|
| 0 | 0 | -100 | 0 |
| 1 | 0 | -100 | 0 |

Use your positive or reduced angle consistently in all three affected clips. OpenIdle is a still pose, not another opening action.

Select each `.anim` asset in the Project window and turn **Loop Time off** in its Inspector. The Animation window itself may repeat its preview; that is separate from runtime clip looping.

Finish with Record off, playback stopped and **Preview off**. Confirm the non-preview LidHinge rotation is `(0,0,0)` so the saved prefab is closed.

## 11. Leave a safe preview controller for your teammate

1. Select PF_KeyBox. On its Animator, leave **Apply Root Motion** off.
2. Open **Window > Animation > Animator** to view the assigned controller.
3. Find the **FirstAid_Closed** state. Right-click it and choose **Set as Layer Default State**. It should be orange.
4. Other clip states may have been added automatically. If one is absent, drag that `.anim` into the Animator window.
5. Do not add transitions, input handling or parameters. There should be no automatic transition from Closed into Open.
6. Use the **Animation** window to preview individual clips when testing.

The teammate can replace or extend this preview controller. Supplying four clips does not itself make the box respond to the player; that connection is intentionally their task.

## 12. Add the stationary key marker

1. Select **KeySpawn**, which is a child of PF_KeyBox and a sibling of Body/LidHinge.
2. Start with local Position `(0,-0.12,0.01)`, Rotation `(0,0,0)`, Scale `(1,1,1)`.
3. Preview the open pose and inspect the actual interior. Adjust the marker onto a usable fixed shelf or the interior base, with enough space for the key model.
4. Treat that starting coordinate as a layout suggestion, not a measured shelf position. Leave the key slightly above the supporting surface, based on its actual pivot and thickness.
5. If needed, use a temporary Cube to represent a key: dimensions approximately `(0.12,0.008,0.04)`. Remove this test Cube from the final prefab; retain the Empty marker.
6. Preview opening and closing again. The marker must stay inside the fixed body and must not travel with the lid.

Your teammate will spawn a key at this marker in one randomly selected box after the correct elevator code. All four boxes should initially be visually empty of the exit key.

## 13. Fit colliders without sealing the interior

1. Check the imported model for existing colliders before adding more.
2. A single solid BoxCollider covering the entire case also covers its empty interior. Replace that on your working copy with simple fitted colliders for the fixed back and frame where needed.
3. To create one, make a child Empty under Body, name it for the part (for example **BackCollider**), and use **Add Component > Box Collider**.
4. Click **Edit Collider** and fit its handles to that fixed surface. Repeat for the frame rails as needed. Actual dimensions depend on the imported shell.
5. Add a separate fitted BoxCollider for the lid under LidHinge. It must move with the lid, not remain attached to Body.
6. Preview all motion and check the lid collider follows the mesh. Avoid a default one-metre collider around this small case.
7. Leave moving parts' **Static** flag off. If Unity asks whether to apply Static to children, do not mark the entire hierarchy static.

You do not need a Hinge Joint to play a transform animation. Do not add a dynamic Rigidbody to make the lid fall or swing physically. The gameplay teammate should decide final Rigidbody, interaction-layer and collision-response settings for their controller and physics system.

## 14. Save the reusable prefab

1. Stop preview and turn Record off.
2. Restore the closed base pose and confirm root Position/Rotation `(0,0,0)`, Scale `(1,1,1)`.
3. Remove SizeReference and temporary test objects from the prefab hierarchy, if any were placed there.
4. Drag PF_KeyBox from the Hierarchy into `Assets/Prefabs/HospitalProps` to create **PF_KeyBox.prefab**.
5. Save the work scene.
6. Drag a fresh copy of that prefab into the work scene and preview its opening clip. Confirm the saved copy has the same materials, hinge motion and marker.

If the fresh instance works, use this one prefab for all four locations. Do not create four separate animation sets.

## 15. Place the four copies in the hospital

Open your hospital scene. Under `MainHospital > Props`, create **KeyBoxes** if needed and reset its Transform. Place one PF_KeyBox instance at each scheduled position:

| Instance name | Room / mounting wall | Position X, Y, Z | Rotation X, Y, Z | Scale |
|---|---|---|---|---|
| K01_KeyBox | General Ward / east wall | -6.96, 1.45, 13.7 | 0, 270, 0 | 1, 1, 1 |
| K02_KeyBox | ICU / west wall | 1.96, 1.45, 34.5 | 0, 90, 0 | 1, 1, 1 |
| K03_KeyBox | Storage / east wall | 15.79, 1.45, 13.8 | 0, 270, 0 | 1, 1, 1 |
| K04_KeyBox | Pre-op / north wall | 10.5, 1.45, 23.79 | 0, 180, 0 | 1, 1, 1 |

These use the existing placement schedule and the normalized, centre-origin 0.55 × 0.45 × 0.18 m box. They assume identity transforms on MainHospital/Props/KeyBoxes. With Y=1.45, the closed box extends from Y=1.225 to Y=1.675. Its back has the scheduled approximately 0.02 m wall clearance. Do not use these root coordinates with an uncentred or incorrectly oriented source model.

Preview each instance. Its lid must open into its room. Root yaw rotates the whole assembly; the clips should still animate only local LidHinge rotation. Check nearby walls, props and the player's approach area at intermediate angles. The placement schedule checked closed footprints, not this imported lid's sweep.

K03 and K04 are intentionally in rooms behind barricades. Keep those locations eligible for the random key pool; your teammate handles reachability and avoiding gameplay deadlocks.

Save with **Ctrl+S**. If you improve the hinge or collider later, edit the shared prefab so all four instances receive the same correction; retain their individual scene positions.

## 16. What to hand over

- Prefab: **PF_KeyBox**; scene instances: **K01–K04**.
- Animator root: PF_KeyBox; animated path: **LidHinge**; property: local Rotation.
- Clips: **FirstAid_Closed**, **FirstAid_Open**, **FirstAid_OpenIdle**, **FirstAid_Close**.
- Opening/closing duration: **0.60 seconds** each.
- Actual final open angle and any changed hinge orientation.
- Key attachment: each instance's **KeySpawn**, calibrated to its actual interior.
- Four visible boxes; one randomly selected key after code success; no permanent key placed by the world builder.
- Preview controller starts closed and has no interaction transitions. Teammate supplies triggers, state handling, pickup logic and final collider/physics integration.

## Troubleshooting

| What you see | What to check |
|---|---|
| Entire box rotates | Clip is targeting root Rotation instead of LidHinge Rotation. |
| Lid circles around the box | Empty pivot is not on the hinge line. Correct it with the lid temporarily unparented. |
| Lid deforms as it turns | A parent has nonuniform scale. Keep the animated hinge and its ancestors at unit scale. |
| Handle stays behind | That moving part is outside LidHinge. Reparent before finalizing clips. |
| Key marker moves with lid | Move KeySpawn to the fixed root/body hierarchy. |
| Lid opens into wall | Verify +Z points into the room, hinge side and rotation sign. |
| Animation changes nothing | Check selected root, clip property path and whether another Animator is controlling the same part. |
| Import is read-only | Create new `.anim` files and work on your own scene instance/prefab. Do not edit embedded source clips. |
| Box opens immediately in Play mode | Set Closed as the controller's default; remove unintended automatic transitions. |
| Case is pink | Its material shader needs a compatible URP setup; this is separate from the hinge motion. |
| Closed lid catches the frame | Correct pivot, hardware pose or collider fit; do not hide the problem with a larger open angle. |

## Reference links

- [RayznGames source asset and license](https://rayzngames.itch.io/emergency-medkit-3d-model).
- [Unity: creating an Animation Clip](https://docs.unity3d.com/6000.0/Documentation/Manual/animeditor-CreatingANewAnimationClip.html).
- [Unity: animating a GameObject](https://docs.unity3d.com/6000.0/Documentation/Manual/animeditor-AnimatingAGameObject.html).

The timing, hierarchy and mounting arrangement above are our project-specific design. The source package has not been imported or its animation tested by this guide's author. Only the manual documentation files were changed.
