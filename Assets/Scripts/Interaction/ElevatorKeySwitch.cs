using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// The key switch inside the elevator. Press E to turn the key: this needs the code to have been accepted
/// (the elevator is open) and the player to hold the exit key. Turning it ends the game.
///
/// Put it on PF_ElevatorKeySwitch together with a solid (non-trigger) Collider that covers the part the
/// player looks at, because PlayerInteractor ignores triggers.
/// It tells ObjectiveState what happened and does nothing else: the win screen is ObjectiveHud's job.
/// </summary>
public class ElevatorKeySwitch : MonoBehaviour, IInteractable
{
    [Tooltip("Seconds between turning the key and the game being won. 0 = win immediately.")]
    [SerializeField, Min(0f)] private float escapeDelay = 0f;

    [Tooltip("Optional extra things to do when the key is turned (a sound, a light, ...).")]
    [SerializeField] private UnityEvent onKeyTurned;

    private ObjectiveState objectives;
    private PlayerStats player;
    private bool used;

    public string GetPrompt()
    {
        if (used) return string.Empty;

        FindReferences();
        if (objectives == null || player == null) return string.Empty;

        if (!objectives.IsCodeAccepted) return "The switch is dead";
        if (!player.HasExitKey) return "The switch needs the exit key";
        return "Press E to turn the key";
    }

    public void Interact(PlayerInteractor interactor)
    {
        if (used) return;

        FindReferences();
        if (objectives == null || player == null || player.IsDead) return;

        if (!objectives.IsCodeAccepted)
        {
            Say("The switch has no power.");
            return;
        }

        if (!player.HasExitKey)
        {
            Say("You need the exit key.");
            return;
        }

        used = true; // from here on the key cannot be used a second time
        onKeyTurned.Invoke();

        if (escapeDelay <= 0f)
        {
            Finish();
        }
        else
        {
            objectives.BeginEscape();
            StartCoroutine(FinishAfterDelay());
        }
    }

    private IEnumerator FinishAfterDelay()
    {
        yield return new WaitForSeconds(escapeDelay);

        // If the player died during the delay, there is no escape.
        if (player != null && player.IsDead) yield break;

        Finish();
    }

    private void Finish()
    {
        // ObjectiveState does not freeze the controls, so do it here: the win screen needs a free cursor.
        PlayerControlLock.Set(true);
        objectives.CompleteEscape();
    }

    private void FindReferences()
    {
        if (objectives == null) objectives = FindAnyObjectByType<ObjectiveState>();
        if (player == null) player = FindAnyObjectByType<PlayerStats>();
    }

    private static void Say(string text)
    {
        ObjectiveHud hud = ObjectiveHud.Instance;
        if (hud != null) hud.ShowMessage(text);
    }
}
