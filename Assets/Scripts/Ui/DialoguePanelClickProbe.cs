using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class DialoguePanelClickProbe : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private DialogueManager owner;
    private Button sourceButton;

    public void Initialize(DialogueManager manager, Button button)
    {
        owner = manager;
        sourceButton = button;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        owner?.LogDialoguePointerEvent("PointerDown", sourceButton, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        owner?.LogDialoguePointerEvent("PointerUp", sourceButton, eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.LogDialoguePointerEvent("PointerClick", sourceButton, eventData);
    }
}
