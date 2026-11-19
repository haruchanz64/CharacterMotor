using CharacterMotor.Core;
using CharacterMotor.Motors;
using UnityEngine;

namespace CharacterMotor.Samples
{
    /// <summary>
    /// Barebones third-person orbit camera that follows a target and assigns itself
    /// as the motor's camera-relative movement reference frame.
    /// </summary>
    public sealed class SampleThirdPersonOrbitCamera : MonoBehaviour
    {
        [SerializeField] private CharacterMotorBase motor;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(1f)] private float distance = 4.5f;
        [SerializeField] private float sensitivity = 0.18f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 70f;

        private ICharacterInputSource _inputSource;
        private float _yaw;
        private float _pitch = 15f;

        /// <summary>Binds this camera transform as the motor's movement reference frame and caches its input source.</summary>
        private void Start()
        {
            if (motor != null)
            {
                motor.ReferenceTransform = transform;
                _inputSource = motor.GetComponent<ICharacterInputSource>();
            }
            _yaw = transform.eulerAngles.y;
        }

        /// <summary>Updates orbit yaw/pitch from look input and positions the camera behind the target pivot.</summary>
        private void LateUpdate()
        {
            if (motor == null) return;

            if (_inputSource != null)
            {
                Vector2 look = _inputSource.ReadInput().Look;
                _yaw += look.x * sensitivity;
                _pitch = Mathf.Clamp(_pitch - (look.y * sensitivity), minPitch, maxPitch);
            }

            Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 pivot = motor.Transform.position + pivotOffset;
            transform.SetPositionAndRotation(pivot - (rot * Vector3.forward * distance), rot);
        }
    }
}
