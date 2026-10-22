using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * Clears the UI selection after any button under this object is clicked.
 * Unity selects a button when it is clicked, and a selected button shows its selected colour instead of its hover colour,
 * so a button clicked once would stop turning red on hover the next time its screen opens.
 * The menus are used with the mouse, so nothing needs to stay selected; clearing it keeps the hover highlight working every time.
 * Put one on a menu's Canvas; buttons on hidden panels are included too.
 */
public class DeselectAfterClick : MonoBehaviour
{
    // Hooks every button under this object, so each click is followed by clearing the selection.
    private void Awake()
    {
        foreach (Button button in GetComponentsInChildren<Button>(true))
            button.onClick.AddListener(ClearSelection);
    }

    // Clears the selected UI element, so a clicked button doesn't stay highlighted.
    private static void ClearSelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}
