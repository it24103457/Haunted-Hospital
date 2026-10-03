# Hospital gameplay handoff — interaction/spawn developer

The world builder imports and places models, previews existing animation clips, sets colliders and places empty markers. They do not author animation motion, write gameplay scripts or configure interaction triggers. This file is for the teammate implementing gameplay.

Assets must already contain the required motion. Do not send animation-authoring or Blender work back to the world builder. Runtime playback, reversing a supplied opening clip where supported, holding the open pose, E-key/raycast logic, pickup logic, random selection and code/key state are your responsibility.

## 13. Random spawning: programmer handoff

This section describes the required behavior. Placing Empty markers alone does not implement it. Your world-builder deliverable is the furniture, colliders, calibrated markers, opening previews and a manager reference list.

### A. On a genuinely new run

1. Reset the four key boxes to empty and clear the previous run's spawned items.
2. Reset codeSolved, hasExitKey and keyReleased to false.
3. Generate four integers from 0 to 9 and store them in order.
4. Treat the full code as a **four-character string**: `0472` must retain its leading zero. Repeated digits are allowed.
5. Choose **2 distinct** clue markers from `C03,C04,C07,C10` (barricaded).
6. Choose **2 distinct** from `C01,C02,C05,C06,C08,C09` (ordinary access).
7. Shuffle the four selected markers, then assign code indices 0,1,2,3. Instantiate one PF_DigitPaper at each assigned marker and set its text from the stored digit/index.
8. Choose **3 distinct** supply markers from `S04,S08,S09,S10,S12` (barricaded).
9. Choose **6 distinct** from the other ten supply markers.
10. Shuffle the selected barricaded markers and put one health, one ammo and one sanity pickup among them.
11. Shuffle the selected ordinary-access markers and put two of each item type among them.
12. Exactly nine supply items now exist on nine different supports; six supports remain empty.

This gives every run useful loot behind boards, including ammunition, without putting every ammunition pickup behind boards. Do **not** discard a candidate merely because the current NavMesh route is blocked by an intact barricade. Validate reachability with that barricade removed as well as the current blocked state.

For a simpler fully uniform mode, shuffle all 15 candidates and assign the nine items to the first nine; shuffle all ten clue candidates and use four. That does not guarantee barricaded clues every run. The split selection above is the recommended harder version.

### B. Picking up supplies and the thief

Each marker has one occupancy record: empty, reserved for spawn, or occupied by a specific instance.

- Picking up a health item removes that world instance and clears its marker.
- A thief can steal supply items; it must claim the same occupancy record so player and thief cannot both receive one item.
- Keep clues, exit key and first-aid key boxes outside the thief's supply target list.
- A thief cannot steal through boards or walls. It needs an actual valid route and interaction range.
- If the thief physically carries a stolen case that can be dropped, count that live case toward the corresponding limit of three. Moving it from shelf to thief is not a new free slot for duplication.
- The simplest first version spawns nine once, with no automatic replenishment. “Maximum nine” does not mean the game must instantly refill every pickup.
- If adding replenishment, check the global per-type count **and** reserve a currently empty marker before creating a replacement. Never replace a stolen item while another recoverable copy still exists.

### C. Correct keypad entry

1. Compare the entered four-character string against the stored code.
2. Wrong entries do not change the code, papers or key selection.
3. On the first correct entry, set codeSolved = true.
4. If keyReleased is false, choose one of `K01,K02,K03,K04` at random.
5. Instantiate exactly one PF_ExitKey inside that box and set keyReleased = true.
6. All four choices are valid even if the selected room still has intact boards.
7. Do not choose again when the player re-enters the code or opens an empty box.
8. If the selected box was already open, the key still appears at its fixed interior marker; do not wait for a fresh “open” event.
9. On pickup, remove the world key and set hasExitKey = true.
10. The elevator may open only when **codeSolved AND hasExitKey**. Consume the key when unlocking if desired.
11. Entering the elevator ends the run.

A successful code should give a clear message such as “Emergency key released — search the first-aid boxes.” It should not silently imply that the elevator itself has opened.

### D. Saving, restart and multiplayer

Save the generated code, selected marker IDs, key selection/release state, taken items, opened containers and broken boards. Loading a saved run restores them; it does not reroll.

If multiplayer is added, only the host/server makes random choices and owns pickup/key state. Replicate those results so players see the same digits and key box. Do not generate a separate code on each client.

## 14. Barricade difficulty without accidental impossible seeds

**Your requested change is included: mandatory digits and the final key can spawn behind barricades.**

Under the previous eight-barricade plan, these five rooms have genuinely gated content: Waste, Storage, Pre-op, Washroom and Staff Utility. The other three boards are shortcuts into rooms with another ordinary entrance.

If each gate has 4 planks and every plank takes 2 successful hits, each gate costs 8 hits. Clearing all five loot-gated rooms costs **40 successful shots**. Two selected barred clue rooms plus a different barred key room require at most three of those gates, or **24 successful shots**, if the player knew exactly where to go.

For the first playable test, **40 starting standard rounds** is a concrete baseline covering every loot-gated room before random ammo or thief behavior is considered. This is a proposed balancing setting for your programmer, not a change made to the project. Resource cases still follow the three-ammo-case maximum; carried starting rounds are not extra world pickup cases.

Misses, combat and optional shortcuts can still exhaust that ammunition. If running out is an intended failure condition, make restart clear. If you want recovery instead of a lost run, an optional slow melee method for breaking boards can provide it, but that changes the shoot-only rule and should be a deliberate team decision.

Do not claim “two accessible ammo spawns guarantee success”: the thief may steal them, and their refill amount may be too low. Test the worst case with every random ammo case stolen. Keep the actual plank damage cost and starting ammunition documented together when balancing.

## 15. Practical checks before handing this to the agent developers

1. Walk from the entrance to every ordinary-access room with the actual player controller.
2. Break each of the five gated-room barricades and verify the item support is reachable afterward.
3. Open every filing drawer, tool chest and first-aid box. Check that neither the mesh nor its collider crosses a wall, another prop or the player's usable standing space.
4. Check drawer papers while closed and open. Interaction must require line of sight, range and the container's open state; no reading through walls or the back of the cabinet.
5. Check that bed/shelf pickups rest on the real mesh. Correct the prefab marker once, not each duplicate.
6. Verify exactly 15 supply IDs, 10 clue IDs and 4 key IDs are registered, with no duplicates.
7. Run several new games, then use debug selection to force each individual candidate. Random sampling alone does not prove all 29 positions work.
8. Test codes beginning with zero and codes containing repeated digits.
9. Test: enter correct code twice; open boxes before entering code; select a key behind boards; pick the key up twice; save/load after code entry. There must remain at most one exit key.
10. Verify the thief cannot take clues/key or reach through closed barricades.
11. With final furniture colliders in place, rebake/update navigation using the established moving-door/barricade approach. The saved project already contains NavMesh assets, so this is an update, not proof that existing navigation fits the new props.
12. Check each enemy's clearance with its own radius. The empty-floor plan check is not a substitute for controller/NavMesh testing.


## Runtime paper text assignment

Use the prepared paper prefab and its TextMeshPro component. Generate and store the digits once; assign each paper its code-position label and stored digit. The world builder supplies a sample visual only. No per-digit texture or animation authoring is needed.

## Animation asset status

The Ulf cabinet, Poly Haven tool chest and RayznGames case have been removed from the active animated-prop selection. The proposed drawer replacement has a creator-listed drawer animation but its download format is not yet verified. The NOT_Lonely chest includes opening motion; closing uses reverse playback of that supplied motion. First-aid wall boxes remain unresolved under the free/preanimated/no-Blender requirements. Do not treat reserved K markers as completed interactive cabinets.
