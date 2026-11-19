using CharacterMotor.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CharacterMotor.InputSystemBridge
{
    /// <summary>
    /// Barebones Unity Input System adapter implementing ICharacterInputSource.
    /// Uses assigned InputActionReferences when provided, or falls back to Keyboard/Mouse polling.
    /// </summary>
    public sealed class NewInputSystemSource : MonoBehaviour, ICharacterInputSource
    {
        [Header("Optional InputActionReferences (Falls Back to WASD / Space / Mouse When Empty)")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference action0;
        [SerializeField] private InputActionReference action1;

        private bool _jumpQueued;

        private void OnEnable()
        {
            moveAction?.action?.Enable();
            lookAction?.action?.Enable();
            jumpAction?.action?.Enable();
            action0?.action?.Enable();
            action1?.action?.Enable();
        }

        private void Update()
        {
            // Latch jump presses in Update so FixedUpdate-driven Rigidbody motors never miss a single-frame press.
            if (jumpAction != null && jumpAction.action != null)
            {
                if (jumpAction.action.WasPressedThisFrame()) _jumpQueued = true;
            }
            else if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                _jumpQueued = true;
            }
        }

        /// <summary>
        /// Reads assigned <see cref="InputActionReference"/> actions (or falls back to WASD, arrows, Space, Mouse delta,
        /// Left Shift for action 0, and Left Ctrl for action 1) and consumes any latched jump press.
        /// </summary>
        /// <returns>The current <see cref="MotorInput"/> snapshot.</returns>
        public MotorInput ReadInput()
        {
            var input = MotorInput.Empty;

            if (moveAction != null && moveAction.action != null)
            {
                input.Move = moveAction.action.ReadValue<Vector2>();
            }
            else if (Keyboard.current != null)
            {
                var kb = Keyboard.current;
                float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) -
                          (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
                float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) -
                          (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
                input.Move = new Vector2(x, y);
            }

            if (lookAction != null && lookAction.action != null)
            {
                input.Look = lookAction.action.ReadValue<Vector2>();
            }
            else if (Mouse.current != null)
            {
                input.Look = Mouse.current.delta.ReadValue();
            }

            input.JumpPressed = _jumpQueued;
            _jumpQueued = false;

            if (jumpAction != null && jumpAction.action != null)
            {
                input.JumpHeld = jumpAction.action.IsPressed();
            }
            else if (Keyboard.current != null)
            {
                input.JumpHeld = Keyboard.current.spaceKey.isPressed;
            }

            if (action0 != null && action0.action != null)
            {
                input.SetAction(0, action0.action.IsPressed());
            }
            else if (Keyboard.current != null)
            {
                input.SetAction(0, Keyboard.current.leftShiftKey.isPressed);
            }

            if (action1 != null && action1.action != null)
            {
                input.SetAction(1, action1.action.WasPressedThisFrame());
            }
            else if (Keyboard.current != null)
            {
                input.SetAction(1, Keyboard.current.leftCtrlKey.wasPressedThisFrame);
            }

            return input;
        }
    }
}
