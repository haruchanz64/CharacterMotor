using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Motors
{
    /// <summary>
    /// Barebones 3D motor backend built on Unity's Rigidbody (FixedUpdate + Unity 6 linearVelocity).
    /// Supports camera-relative movement, slope projection, ground probing, gravity, jumping, and IMotorModule hooks.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyMotor3D : CharacterMotorBase
    {
        [Header("3D Rigidbody")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0.05f)] private float groundProbeRadius = 0.35f;
        [SerializeField, Min(0.05f)] private float groundProbeDistance = 0.25f;
        [SerializeField] private Vector3 groundProbeOriginOffset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private CharacterRotationMode rotationMode = CharacterRotationMode.FaceMoveDirection;
        [SerializeField, Min(0f)] private float rotationSharpness = 14f;

        private Rigidbody _rb;

        public override MotorDimension Dimension => MotorDimension.Mode3D;
        public Rigidbody Body => _rb;

        protected override void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
            {
                _rb.useGravity = false;
                _rb.freezeRotation = true;
            }

            base.Awake();
        }

        private void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }

        public void Step(float deltaTime)
        {
            if (_rb == null || deltaTime <= 0f) return;

            MotorSettings settings = Settings;
            CurrentState.Position = _rb.position;
            CurrentState.Velocity = GetLinearVelocity();

            ProbeGround(settings.maxSlopeAngle, out bool grounded, out Vector3 groundNormal);
            UpdateGroundedState(grounded, groundNormal, deltaTime);

            MotorInput input = ConsumeInput();
            Vector3 moveWorld = ReferenceFrame != null
                ? ReferenceFrame.TransformMoveInput(input.Move, Vector3.up)
                : new Vector3(input.Move.x, 0f, input.Move.y);

            var ctx = new MotorContext
            {
                DeltaTime = deltaTime,
                Dimension = MotorDimension.Mode3D,
                IsTopDown2D = false,
                Input = input,
                MoveDirectionWorld = moveWorld,
                State = CurrentState,
                Settings = settings
            };

            Pipeline.SolveVelocity(ref ctx);

            // Project planar velocity onto walkable slopes when grounded and not jumping
            Vector3 finalVelocity = ctx.DesiredVelocity;
            if (CurrentState.IsGrounded && !ctx.JumpedThisStep && CurrentState.SlopeAngle > 0.1f)
            {
                Vector3 planar = new Vector3(finalVelocity.x, 0f, finalVelocity.z);
                Vector3 projected = Vector3.ProjectOnPlane(planar, CurrentState.GroundNormal);
                finalVelocity.x = projected.x;
                finalVelocity.z = projected.z;
                finalVelocity.y = Mathf.Min(projected.y, finalVelocity.y);
            }

            SetLinearVelocity(finalVelocity);
            CurrentState.Velocity = finalVelocity;
            CurrentState.Position = _rb.position;

            UpdateRotation(moveWorld, deltaTime);
            ctx.State = CurrentState;

            Pipeline.NotifyAfterMove(ref ctx);
            RaiseStepEvents(in ctx);
        }

        public override void Teleport(Vector3 worldPosition, bool resetVelocity = true)
        {
            if (_rb != null)
            {
                _rb.position = worldPosition;
                if (resetVelocity) SetLinearVelocity(Vector3.zero);
            }
            else
            {
                transform.position = worldPosition;
            }

            CurrentState.Position = worldPosition;
            if (resetVelocity)
            {
                CurrentState.Velocity = Vector3.zero;
                Pipeline.ClearPendingImpulse();
            }
        }

        private void ProbeGround(float maxSlopeAngle, out bool grounded, out Vector3 normal)
        {
            normal = Vector3.up;
            Vector3 origin = _rb.position + groundProbeOriginOffset;
            if (Physics.SphereCast(
                origin,
                groundProbeRadius,
                Vector3.down,
                out RaycastHit hit,
                groundProbeDistance,
                groundLayers,
                QueryTriggerInteraction.Ignore))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle <= maxSlopeAngle)
                {
                    grounded = true;
                    normal = hit.normal;
                    return;
                }
            }

            grounded = false;
        }

        private void UpdateRotation(Vector3 moveWorld, float dt)
        {
            Vector3 desiredForward = Vector3.zero;
            if (rotationMode == CharacterRotationMode.FaceMoveDirection && moveWorld.sqrMagnitude > 1e-4f)
            {
                desiredForward = new Vector3(moveWorld.x, 0f, moveWorld.z).normalized;
            }
            else if (rotationMode == CharacterRotationMode.FaceReferenceForward && ReferenceFrame != null)
            {
                desiredForward = ReferenceFrame.GetForwardOnPlane(Vector3.up);
            }

            if (desiredForward.sqrMagnitude > 1e-4f)
            {
                CurrentState.FacingDirection = desiredForward;
                Quaternion targetRot = Quaternion.LookRotation(desiredForward, Vector3.up);
                Quaternion nextRot = rotationSharpness > 0f
                    ? Quaternion.Slerp(_rb.rotation, targetRot, 1f - Mathf.Exp(-rotationSharpness * dt))
                    : targetRot;
                _rb.MoveRotation(nextRot);
            }
        }

        private Vector3 GetLinearVelocity()
        {
#if UNITY_6000_0_OR_NEWER
            return _rb.linearVelocity;
#else
            return _rb.velocity;
#endif
        }

        private void SetLinearVelocity(Vector3 value)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = value;
#else
            _rb.velocity = value;
#endif
        }
    }
}
