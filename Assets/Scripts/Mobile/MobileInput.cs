using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileInput : MonoBehaviour
{
    // -1 = left, 0 = none, +1 = right
    public float MoveX { get; private set; }

    private bool jumpPressed;
    private bool pausePressed;
    private bool rescuePressed;
    public bool AttackHeld { get; private set; }
    public bool JumpReleasedThisFrame { get; private set; }

    private bool jumpHeld;
    private bool movementInputEnabled = true;
    private readonly List<Selectable> movementInputButtons = new();
    private readonly List<EventTrigger> movementInputTriggers = new();

    // Track button input separately
    private float buttonMoveX = 0f;

private bool inventoryPressed;

public void PressInventory()
{
    inventoryPressed = true;
}
public bool ConsumeInventoryPressed()
{
    bool val = inventoryPressed;
    inventoryPressed = false;
    return val;
}
    private void Awake()
    {
        CacheMovementButtons();
        ApplyMovementButtonState();
    }

    private void LateUpdate()
    {
        JumpReleasedThisFrame = false; // reset every frame
    }

    private void Update()
    {
        if (!movementInputEnabled)
        {
            MoveX = 0f;
            return;
        }

        // Keyboard input
        float keyboard = Input.GetAxisRaw("Horizontal");

        // Combine: if button is pressed, use it; otherwise use keyboard
        if (buttonMoveX != 0f)
        {
            MoveX = buttonMoveX;
            
        }
        else
        {
            if (Mathf.Abs(keyboard) > 0.1f)
                MoveX = keyboard;
            else
                MoveX = 0f;
        }
    }

    /* =========================
       LEFT / RIGHT (BUTTONS)
       ========================= */
    public void LeftDown()
    {
        if (!movementInputEnabled)
            return;

        buttonMoveX = -1f;
    }

    public void LeftUp()
    {
        if (buttonMoveX < 0f)
            buttonMoveX = 0f;
    }

    public void RightDown()
    {
        if (!movementInputEnabled)
            return;

        buttonMoveX = 1f;
    }

    public void RightUp()
    {
        if (buttonMoveX > 0f)
            buttonMoveX = 0f;
    }

    /* =========================
       JUMP (TAP)
       ========================= */
    public void JumpDown()
    {
        if (!movementInputEnabled)
            return;

        jumpPressed = true;
        jumpHeld = true;
    }

    public void JumpUp()
    {
        if (!movementInputEnabled)
        {
            jumpHeld = false;
            return;
        }

        jumpHeld = false;
        JumpReleasedThisFrame = true;
    }

    public bool JumpHeld() => jumpHeld;

    public bool ConsumeJumpPressed()
    {
        if (!jumpPressed) return false;
        jumpPressed = false;
        return true;
    }

    /* =========================
       ATTACK (HOLD)
       ========================= */
    public void AttackDown() => AttackHeld = true;
    public void AttackUp() => AttackHeld = false;

    public void SetMovementInputEnabled(bool enabled)
    {
        movementInputEnabled = enabled;

        if (!enabled)
        {
            ResetMovementState();
        }

        CacheMovementButtons();
        ApplyMovementButtonState();
    }

    private void ResetMovementState()
    {
        MoveX = 0f;
        buttonMoveX = 0f;
        jumpPressed = false;
        jumpHeld = false;
        JumpReleasedThisFrame = false;
    }

    private void CacheMovementButtons()
    {
        if (movementInputButtons.Count > 0)
            return;

        EventTrigger[] triggers = GetComponentsInChildren<EventTrigger>(true);
        foreach (EventTrigger trigger in triggers)
        {
            if (trigger == null || !CallsMovementMethod(trigger))
                continue;

            if (!movementInputTriggers.Contains(trigger))
            {
                movementInputTriggers.Add(trigger);
            }

            Selectable selectable = trigger.GetComponent<Selectable>();
            if (selectable != null && !movementInputButtons.Contains(selectable))
            {
                movementInputButtons.Add(selectable);
            }
        }
    }

    private void ApplyMovementButtonState()
    {
        foreach (Selectable selectable in movementInputButtons)
        {
            if (selectable != null)
                selectable.interactable = movementInputEnabled;
        }

        foreach (EventTrigger trigger in movementInputTriggers)
        {
            if (trigger != null)
                trigger.enabled = movementInputEnabled;
        }
    }

    private static bool CallsMovementMethod(EventTrigger trigger)
    {
        foreach (EventTrigger.Entry entry in trigger.triggers)
        {
            if (entry == null || entry.callback == null)
                continue;

            for (int i = 0; i < entry.callback.GetPersistentEventCount(); i++)
            {
                string methodName = entry.callback.GetPersistentMethodName(i);
                if (methodName == nameof(LeftDown)
                    || methodName == nameof(LeftUp)
                    || methodName == nameof(RightDown)
                    || methodName == nameof(RightUp)
                    || methodName == nameof(JumpDown)
                    || methodName == nameof(JumpUp))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /* =========================
       PAUSE (TAP)
       ========================= */
    public void PausePress()
    {
        pausePressed = true;
    }

    public bool ConsumePausePressed()
    {
        if (!pausePressed) return false;
        pausePressed = false;
        return true;
    }

    /* =========================
       RESCUE (TAP)
       ========================= */
    public void RescuePress()
    {
        rescuePressed = true;
    }

    public bool ConsumeRescuePressed()
    {
        if (!rescuePressed) return false;
        rescuePressed = false;
        return true;
    }
}
