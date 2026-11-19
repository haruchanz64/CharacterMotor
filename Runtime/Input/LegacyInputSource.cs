using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Input
{
    /// <summary>
    /// Barebones Legacy Input Manager adapter implementing ICharacterInputSource.
    /// Safe when Legacy Input is disabled (returns empty input).
    /// </summary>
    public sealed class LegacyInputSource : MonoBehaviour, ICharacterInputSource
    {
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode action0Key = KeyCode.LeftShift;
        [SerializeField] private KeyCode action1Key = KeyCode.LeftControl;

        private bool _jumpQueued;

        private void Update()
        {
#if !CHARACTER_MOTOR_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            // Latch jump presses in Update so FixedUpdate-driven Rigidbody motors never miss a single-frame key press.
            if (UnityEngine.Input.GetKeyDown(jumpKey))
            {
                _jumpQueued = true;
            }
#endif
        }

        /// <summary>
        /// Polls Legacy Input Manager axes and configured keys, returning a <see cref="MotorInput"/> snapshot
        /// and clearing any latched jump press. Returns <see cref="MotorInput.Empty"/> when Legacy Input is disabled.
        /// </summary>
        /// <returns>The current frame's <see cref="MotorInput"/> snapshot.</returns>
        public MotorInput ReadInput()
        {
            var input = MotorInput.Empty;
#if !CHARACTER_MOTOR_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            input.Move = new Vector2(
                UnityEngine.Input.GetAxisRaw("Horizontal"),
                UnityEngine.Input.GetAxisRaw("Vertical"));
            input.Look = new Vector2(
                UnityEngine.Input.GetAxisRaw("Mouse X"),
                UnityEngine.Input.GetAxisRaw("Mouse Y"));
            input.JumpPressed = _jumpQueued;
            input.JumpHeld = UnityEngine.Input.GetKey(jumpKey);
            input.SetAction(0, UnityEngine.Input.GetKey(action0Key));
            input.SetAction(1, UnityEngine.Input.GetKeyDown(action1Key));
            _jumpQueued = false;
#endif
            return input;
        }
    }
}
