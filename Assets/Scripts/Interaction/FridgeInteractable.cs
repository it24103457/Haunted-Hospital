using UnityEngine;

// The old fridges that hold a clue note (PF_OldFridge). Press E on the door to swing the TOP door open.
// Only the top door opens: the note sits just above the door split. The door colliders are children of the
// prefab root, so PlayerInteractor finds this script when the player looks at the door.
//
// Collider note: the Auto_Colliders box on PF_OldFridge must only cover the back half of the fridge,
// otherwise the E ray hits that box and never reaches the note inside (see the fridge setup notes).
public class FridgeInteractable : OpenableInteractable
{
    protected override string DefaultOpenParameter => "OpenTop";
    protected override string DefaultDisplayName => "the fridge";
}
