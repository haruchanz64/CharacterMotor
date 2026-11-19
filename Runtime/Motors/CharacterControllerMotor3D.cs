using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Motors
{
    /// <summary>
    /// Barebones 3D motor backend built on Unity's CharacterController.
    /// Supports camera-relative movement, slope-limit grounding, gravity, jumping, and IMotorModule hooks.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public sealed class CharacterControllerMotor3D : CharacterMotorBase
    {
        [Header("3D CharacterController")]
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;
        [SerializeField] private CharacterRotationMode rotationMode = CharacterRotationMode.FaceMoveDirection;
        [SerializeField, Min(0f)] private float rotationSharpness = 14f;

        private UnityEngine.CharacterController _controller;

        /// <inheritdoc/>
        public override MotorDimension Dimension => MotorDimension.Mode3D;

        /// <summary>Underlying Unity <see cref="UnityEngine.CharacterController"/> driven by this motor.</summary>
        public UnityEngine.CharacterController Controller => _controller;

        /// <inheritdoc/>
        protected override void Awake()
        {
            _controller = GetComponent<UnityEngine.CharacterController>();
            base.Awake();
            if (_controller != null)
            {
                _controller.slopeLimit = Settings.maxSlopeAngle;
            }
        }

        private void Update()
        {
            Step(Time.deltaTime);
        }

        /// <summary>
        /// Executes one locomotion tick: probes ground, consumes input, solves velocity through <see cref="CharacterMotorBase.Pipeline"/>,
        /// moves the <see cref="UnityEngine.CharacterController"/>, updates rotation, and raises post-move callbacks.
        /// </summary>
        /// <param name="deltaTime">Step duration in seconds; non-positive values are ignored.</param>
        public void Step(float deltaTime)
        {
            if (_controller == null || deltaTime <= 0f) return;

            MotorSettings settings = Settings;
            _controller.slopeLimit = settings.maxSlopeAngle;

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

            CollisionFlags flags = _controller.Move(ctx.DesiredVelocity * deltaTime);

            if ((flags & CollisionFlags.Above) != 0 && ctx.DesiredVelocity.y > 0f)
            {
                // Cancel upward velocity on ceiling impact so the character does not stick to overhead geometry.
                ctx.DesiredVelocity.y = 0f;
            }

            CurrentState.Position = transform.position;
            CurrentState.Velocity = ctx.DesiredVelocity;

            UpdateRotation(moveWorld, deltaTime);
            ctx.State = CurrentState;

            Pipeline.NotifyAfterMove(ref ctx);
            RaiseStepEvents(in ctx);
        }

        /// <inheritdoc/>
        public override void Teleport(Vector3 worldPosition, bool resetVelocity = true)
        {
            if (_controller != null)
            {
                // CharacterController caches transform position internally and can revert direct assignments while enabled.
                bool wasEnabled = _controller.enabled;
                _controller.enabled = false;
                transform.position = worldPosition;
                _controller.enabled = wasEnabled;
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

        /// <summary>
        /// Casts a sphere below the capsule bottom to detect walkable ground and surface normals,
        /// falling back to <see cref="UnityEngine.CharacterController.isGrounded"/> when the cast misses.
        /// </summary>
        private void ProbeGround(float maxSlopeAngle, out bool grounded, out Vector3 normal)
        {
            normal = Vector3.up;
            float radius = Mathf.Max(0.05f, _controller.radius * 0.9f);
            Vector3 bottomCenter = transform.TransformPoint(_controller.center) +
                                   Vector3.down * Mathf.Max(0f, (_controller.height * 0.5f) - radius);

            if (Physics.SphereCast(
                bottomCenter + Vector3.up * 0.05f,
                radius,
                Vector3.down,
                out RaycastHit hit,
                groundProbeDistance + 0.05f,
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

            grounded = _controller.isGrounded;
        }

        /// <summary>
        /// Smoothly rotates the transform toward the active <see cref="CharacterRotationMode"/> target using exponential damping.
        /// </summary>
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
                transform.rotation = rotationSharpness > 0f
                    ? Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSharpness * dt))
                    : targetRot;
            }
        }
    }
}
