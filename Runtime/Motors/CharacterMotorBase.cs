using System;
using System.Collections.Generic;
using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Motors
{
    /// <summary>
    /// Shared barebones base class for 2D and 3D CharacterMotor backends.
    /// Owns module discovery/registration, input routing, camera reference framing, and events.
    /// </summary>
    public abstract class CharacterMotorBase : MonoBehaviour, ICharacterMotor
    {
        private static readonly List<ICharacterMotor> ActiveRegistry = new List<ICharacterMotor>(16);

        /// <summary>
        /// All currently enabled motors in the scene, used by Editor debugging tools to inspect live instances.
        /// </summary>
        public static IReadOnlyList<ICharacterMotor> ActiveMotors => ActiveRegistry;

        [Header("Configuration")]
        [SerializeField] private MotorConfigAsset configAsset;
        [SerializeField] private MotorSettings inlineSettings = MotorSettings.Default3D;

        [Header("Camera / Reference Frame (Optional)")]
        [SerializeField] private Transform referenceTransform;
        [SerializeField] private bool autoDiscoverComponents = true;

        /// <summary>Deterministic locomotion pipeline shared across motor steps.</summary>
        protected readonly MotorPipeline Pipeline = new MotorPipeline();
        /// <summary>Mutable backing state exposed through <see cref="State"/>.</summary>
        protected MotorState CurrentState;
        /// <summary>Most recent manually supplied input snapshot.</summary>
        protected MotorInput CurrentInput;
        /// <summary>Active input provider polled when no manual input is pending.</summary>
        protected ICharacterInputSource InputSource;
        /// <summary>Active reference frame converting 2D move input into world-space direction.</summary>
        protected IMovementReferenceFrame ReferenceFrame;
        private bool _hasManualInput;
        private bool _wasGrounded;

        /// <inheritdoc/>
        public virtual string MotorName => $"{name} ({GetType().Name})";

        /// <inheritdoc/>
        public abstract MotorDimension Dimension { get; }

        /// <inheritdoc/>
        public Transform Transform => transform;

        /// <inheritdoc/>
        public MotorState State => CurrentState;

        /// <summary>
        /// Reads or writes active locomotion settings, preferring the assigned <see cref="MotorConfigAsset"/> over inline settings.
        /// Values are clamped via <see cref="MotorSettings.Clamp"/> on assignment.
        /// </summary>
        public MotorSettings Settings
        {
            get => configAsset != null ? configAsset.settings : inlineSettings;
            set
            {
                value.Clamp();
                if (configAsset != null) configAsset.settings = value;
                else inlineSettings = value;
            }
        }

        /// <summary>
        /// Optional transform used by the default <see cref="TransformReferenceFrame"/> (typically a Camera or Cinemachine pivot).
        /// Falls back to <see cref="Camera.main"/> when null.
        /// </summary>
        public Transform ReferenceTransform
        {
            get => referenceTransform;
            set
            {
                referenceTransform = value;
                if (ReferenceFrame is TransformReferenceFrame trf)
                {
                    trf.ReferenceTransform = value;
                }
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMotorModule> Modules => Pipeline.Modules;

        /// <inheritdoc/>
        public event Action<bool> OnGroundedChanged;

        /// <inheritdoc/>
        public event Action OnJumped;

        /// <inheritdoc/>
        public event Action<MotorState> OnAfterMove;

        /// <summary>Clears the static motor registry when entering Play Mode with Domain Reload disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ActiveRegistry.Clear();
        }

        /// <summary>Initializes inline settings from <see cref="GetDefaultSettings"/> when the component is added or reset.</summary>
        protected virtual void Reset()
        {
            inlineSettings = GetDefaultSettings();
        }

        /// <summary>Initializes reference framing, initial state, and optional same-GameObject module/input discovery.</summary>
        protected virtual void Awake()
        {
            inlineSettings.Clamp();
            ReferenceFrame = new TransformReferenceFrame(referenceTransform);
            CurrentState.Position = transform.position;
            CurrentState.GroundNormal = Vector3.up;
            CurrentState.FacingDirection = Dimension == MotorDimension.Mode2D ? Vector3.right : transform.forward;

            if (autoDiscoverComponents)
            {
                DiscoverLocalComponents();
            }
        }

        /// <summary>Registers this motor in <see cref="ActiveMotors"/> while enabled.</summary>
        protected virtual void OnEnable()
        {
            if (!ActiveRegistry.Contains(this))
            {
                ActiveRegistry.Add(this);
            }
        }

        /// <summary>Removes this motor from <see cref="ActiveMotors"/> while disabled.</summary>
        protected virtual void OnDisable()
        {
            ActiveRegistry.Remove(this);
        }

        /// <summary>Unregisters this motor and detaches all pipeline modules on destruction.</summary>
        protected virtual void OnDestroy()
        {
            ActiveRegistry.Remove(this);
            Pipeline.ClearModules(this);
        }

        /// <summary>
        /// Scans sibling <see cref="MonoBehaviour"/> components on this GameObject, attaching every
        /// <see cref="IMotorModule"/> and binding the first <see cref="ICharacterInputSource"/> if none is set.
        /// </summary>
        public void DiscoverLocalComponents()
        {
            var behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null || ReferenceEquals(b, this)) continue;

                if (b is IMotorModule mod)
                {
                    Pipeline.AddModule(mod, this);
                }

                if (InputSource == null && b is ICharacterInputSource src)
                {
                    InputSource = src;
                }
            }
        }

        /// <inheritdoc/>
        public void SetInput(in MotorInput input)
        {
            CurrentInput = input;
            _hasManualInput = true;
        }

        /// <inheritdoc/>
        public void SetInputSource(ICharacterInputSource source)
        {
            InputSource = source;
        }

        /// <inheritdoc/>
        public void SetReferenceFrame(IMovementReferenceFrame referenceFrame)
        {
            ReferenceFrame = referenceFrame ?? new TransformReferenceFrame(referenceTransform);
        }

        /// <inheritdoc/>
        public void AddImpulse(Vector3 impulse)
        {
            Pipeline.QueueImpulse(impulse);
        }

        /// <inheritdoc/>
        public abstract void Teleport(Vector3 worldPosition, bool resetVelocity = true);

        /// <inheritdoc/>
        public void AddModule(IMotorModule module) => Pipeline.AddModule(module, this);

        /// <inheritdoc/>
        public bool RemoveModule(IMotorModule module) => Pipeline.RemoveModule(module, this);

        /// <inheritdoc/>
        public T GetModule<T>() where T : class, IMotorModule => Pipeline.GetModule<T>();

        /// <summary>
        /// Returns queued manual input if <see cref="SetInput"/> was called since the last step,
        /// otherwise polls <see cref="InputSource"/>, or returns <see cref="MotorInput.Empty"/>.
        /// </summary>
        /// <returns>The <see cref="MotorInput"/> snapshot to apply during this step.</returns>
        protected MotorInput ConsumeInput()
        {
            if (_hasManualInput)
            {
                MotorInput manual = CurrentInput;
                CurrentInput.JumpPressed = false;
                _hasManualInput = false;
                return manual;
            }

            if (InputSource != null)
            {
                return InputSource.ReadInput();
            }

            return MotorInput.Empty;
        }

        /// <summary>
        /// Updates grounded flags, ground normal, slope angle, and <see cref="MotorState.TimeSinceGrounded"/>,
        /// raising <see cref="OnGroundedChanged"/> whenever grounded state flips.
        /// </summary>
        /// <param name="grounded">True when standing on a walkable surface this step.</param>
        /// <param name="groundNormal">World-space surface normal; normalized or defaulted to <see cref="Vector3.up"/>.</param>
        /// <param name="dt">Elapsed step duration in seconds.</param>
        protected void UpdateGroundedState(bool grounded, Vector3 groundNormal, float dt)
        {
            CurrentState.IsGrounded = grounded;
            CurrentState.GroundNormal = grounded && groundNormal.sqrMagnitude > 1e-4f ? groundNormal.normalized : Vector3.up;
            CurrentState.SlopeAngle = grounded ? Vector3.Angle(Vector3.up, CurrentState.GroundNormal) : 0f;
            CurrentState.TimeSinceGrounded = grounded ? 0f : CurrentState.TimeSinceGrounded + dt;

            if (grounded != _wasGrounded)
            {
                _wasGrounded = grounded;
                OnGroundedChanged?.Invoke(grounded);
            }
        }

        /// <summary>
        /// Dispatches <see cref="OnJumped"/> (when <see cref="MotorContext.JumpedThisStep"/> is true)
        /// followed by <see cref="OnAfterMove"/> with the current motor state.
        /// </summary>
        /// <param name="ctx">Completed step context.</param>
        protected void RaiseStepEvents(in MotorContext ctx)
        {
            if (ctx.JumpedThisStep)
            {
                OnJumped?.Invoke();
            }

            OnAfterMove?.Invoke(CurrentState);
        }

        /// <summary>
        /// Returns the default <see cref="MotorSettings"/> preset for this backend when the component is reset.
        /// </summary>
        /// <returns>Backend-specific default <see cref="MotorSettings"/>.</returns>
        protected virtual MotorSettings GetDefaultSettings() => MotorSettings.Default3D;
    }
}
