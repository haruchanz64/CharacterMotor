using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Motors
{
    /// <summary>
    /// Barebones 2D motor backend built on Unity's Rigidbody2D (FixedUpdate + Unity 6 linearVelocity).
    /// Supports both side-scrolling Platformer mode and omnidirectional TopDown mode.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class RigidbodyMotor2D : CharacterMotorBase
    {
        [Header("2D Rigidbody2D")]
        [SerializeField] private Motor2DMode mode = Motor2DMode.Platformer;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private Vector2 groundBoxSize = new Vector2(0.75f, 0.1f);
        [SerializeField] private Vector2 groundBoxOffset = new Vector2(0f, -0.52f);
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.08f;

        private Rigidbody2D _rb2d;

        /// <inheritdoc/>
        public override MotorDimension Dimension => MotorDimension.Mode2D;

        /// <summary>Active 2D locomotion mode (side-scrolling <see cref="Motor2DMode.Platformer"/> or <see cref="Motor2DMode.TopDown"/>).</summary>
        public Motor2DMode Mode
        {
            get => mode;
            set => mode = value;
        }

        /// <summary>Underlying Unity <see cref="Rigidbody2D"/> driven by this motor.</summary>
        public Rigidbody2D Body2D => _rb2d;

        /// <inheritdoc/>
        protected override MotorSettings GetDefaultSettings()
        {
            return mode == Motor2DMode.TopDown
                ? MotorSettings.DefaultTopDown2D
                : MotorSettings.DefaultPlatformer2D;
        }

        /// <summary>
        /// Caches the <see cref="Rigidbody2D"/>, zeroes its gravity scale and freezes Z rotation so the motor owns both,
        /// and initializes base state.
        /// </summary>
        protected override void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            if (_rb2d != null)
            {
                _rb2d.gravityScale = 0f;
                _rb2d.freezeRotation = true;
            }

            base.Awake();
        }

        private void FixedUpdate()
        {
            Step(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Executes one 2D physics step: probes ground in platformer mode (always grounded in top-down mode),
        /// solves velocity via <see cref="CharacterMotorBase.Pipeline"/>, applies linear velocity, updates horizontal facing, and raises step events.
        /// </summary>
        /// <param name="deltaTime">Step duration in seconds (typically <see cref="Time.fixedDeltaTime"/>); non-positive values are ignored.</param>
        public void Step(float deltaTime)
        {
            if (_rb2d == null || deltaTime <= 0f) return;

            MotorSettings settings = Settings;
            Vector2 currentVel = GetLinearVelocity2D();
            CurrentState.Position = _rb2d.position;
            CurrentState.Velocity = new Vector3(currentVel.x, currentVel.y, 0f);

            bool isTopDown = mode == Motor2DMode.TopDown;
            if (isTopDown)
            {
                UpdateGroundedState(true, Vector3.up, deltaTime);
            }
            else
            {
                ProbeGround2D(settings.maxSlopeAngle, out bool grounded, out Vector2 groundNormal);
                UpdateGroundedState(grounded, new Vector3(groundNormal.x, groundNormal.y, 0f), deltaTime);
            }

            MotorInput input = ConsumeInput();
            Vector3 moveWorld = new Vector3(input.Move.x, isTopDown ? input.Move.y : 0f, 0f);

            var ctx = new MotorContext
            {
                DeltaTime = deltaTime,
                Dimension = MotorDimension.Mode2D,
                IsTopDown2D = isTopDown,
                Input = input,
                MoveDirectionWorld = moveWorld,
                State = CurrentState,
                Settings = settings
            };

            Pipeline.SolveVelocity(ref ctx);

            Vector2 finalVel = new Vector2(ctx.DesiredVelocity.x, ctx.DesiredVelocity.y);
            SetLinearVelocity2D(finalVel);

            CurrentState.Velocity = new Vector3(finalVel.x, finalVel.y, 0f);
            CurrentState.Position = _rb2d.position;
            if (Mathf.Abs(input.Move.x) > 0.01f)
            {
                CurrentState.FacingDirection = input.Move.x > 0f ? Vector3.right : Vector3.left;
            }

            ctx.State = CurrentState;
            Pipeline.NotifyAfterMove(ref ctx);
            RaiseStepEvents(in ctx);
        }

        /// <inheritdoc/>
        public override void Teleport(Vector3 worldPosition, bool resetVelocity = true)
        {
            if (_rb2d != null)
            {
                _rb2d.position = new Vector2(worldPosition.x, worldPosition.y);
                if (resetVelocity) SetLinearVelocity2D(Vector2.zero);
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
        /// Performs a downward 2D box cast beneath the body, ignoring triggers and the body's own collider,
        /// to determine walkable platformer grounding and surface normal.
        /// </summary>
        private void ProbeGround2D(float maxSlopeAngle, out bool grounded, out Vector2 normal)
        {
            normal = Vector2.up;
            Vector2 origin = _rb2d.position + groundBoxOffset;
            RaycastHit2D hit = Physics2D.BoxCast(origin, groundBoxSize, 0f, Vector2.down, groundProbeDistance, groundLayers);
            if (hit.collider != null && !hit.collider.isTrigger && hit.rigidbody != _rb2d)
            {
                float angle = Vector2.Angle(Vector2.up, hit.normal);
                if (angle <= maxSlopeAngle)
                {
                    grounded = true;
                    normal = hit.normal;
                    return;
                }
            }

            grounded = false;
        }

        /// <summary>Reads 2D rigidbody linear velocity using Unity 6's <c>linearVelocity</c> or legacy <c>velocity</c>.</summary>
        private Vector2 GetLinearVelocity2D()
        {
#if UNITY_6000_0_OR_NEWER
            return _rb2d.linearVelocity;
#else
            return _rb2d.velocity;
#endif
        }

        /// <summary>Writes 2D rigidbody linear velocity using Unity 6's <c>linearVelocity</c> or legacy <c>velocity</c>.</summary>
        private void SetLinearVelocity2D(Vector2 value)
        {
#if UNITY_6000_0_OR_NEWER
            _rb2d.linearVelocity = value;
#else
            _rb2d.velocity = value;
#endif
        }
    }
}
