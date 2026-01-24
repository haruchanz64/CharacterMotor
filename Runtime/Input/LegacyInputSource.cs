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
            if (UnityEngine.Input.GetKeyDown(jumpKey))
            {
                _jumpQueued = true;
            }
#endif
        }

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
