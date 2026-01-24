using CharacterMotor.Core;
using CharacterMotor.Motors;
using UnityEngine;

namespace CharacterMotor.Samples
{
    /// <summary>
    /// Barebones first-person camera controller that consumes MotorInput.Look from an ICharacterInputSource
    /// and binds the camera transform as the motor's movement reference frame.
    /// </summary>
    public sealed class SampleFirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private CharacterMotorBase motor;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float sensitivity = 0.15f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private bool lockCursor = true;

        private ICharacterInputSource _inputSource;
        private float _yaw;
        private float _pitch;

        private void Awake()
        {
            if (motor == null) motor = GetComponent<CharacterMotorBase>();
            if (cameraPivot == null) cameraPivot = transform;
            _inputSource = GetComponent<ICharacterInputSource>();
            _yaw = transform.eulerAngles.y;

            if (motor != null && cameraPivot != null)
            {
                motor.ReferenceTransform = cameraPivot;
            }

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LateUpdate()
        {
            if (_inputSource == null) return;
            Vector2 look = _inputSource.ReadInput().Look;
            _yaw += look.x * sensitivity;
            _pitch = Mathf.Clamp(_pitch - (look.y * sensitivity), minPitch, maxPitch);

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (cameraPivot != null && cameraPivot != transform)
            {
                cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            }
        }
    }
}
