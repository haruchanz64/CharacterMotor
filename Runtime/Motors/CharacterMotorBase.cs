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

        public static IReadOnlyList<ICharacterMotor> ActiveMotors => ActiveRegistry;

        [Header("Configuration")]
        [SerializeField] private MotorConfigAsset configAsset;
        [SerializeField] private MotorSettings inlineSettings = MotorSettings.Default3D;

        [Header("Camera / Reference Frame (Optional)")]
        [SerializeField] private Transform referenceTransform;
        [SerializeField] private bool autoDiscoverComponents = true;

        protected readonly MotorPipeline Pipeline = new MotorPipeline();
        protected MotorState CurrentState;
        protected MotorInput CurrentInput;
        protected ICharacterInputSource InputSource;
        protected IMovementReferenceFrame ReferenceFrame;
        private bool _hasManualInput;
        private bool _wasGrounded;

        public virtual string MotorName => $"{name} ({GetType().Name})";
        public abstract MotorDimension Dimension { get; }
        public Transform Transform => transform;
        public MotorState State => CurrentState;

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

        public IReadOnlyList<IMotorModule> Modules => Pipeline.Modules;

        public event Action<bool> OnGroundedChanged;
        public event Action OnJumped;
        public event Action<MotorState> OnAfterMove;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            ActiveRegistry.Clear();
        }

        protected virtual void Reset()
        {
            inlineSettings = GetDefaultSettings();
        }

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

        protected virtual void OnEnable()
        {
            if (!ActiveRegistry.Contains(this))
            {
                ActiveRegistry.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            ActiveRegistry.Remove(this);
        }

        protected virtual void OnDestroy()
        {
            ActiveRegistry.Remove(this);
            Pipeline.ClearModules(this);
        }

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

        public void SetInput(in MotorInput input)
        {
            CurrentInput = input;
            _hasManualInput = true;
        }

        public void SetInputSource(ICharacterInputSource source)
        {
            InputSource = source;
        }

        public void SetReferenceFrame(IMovementReferenceFrame referenceFrame)
        {
            ReferenceFrame = referenceFrame ?? new TransformReferenceFrame(referenceTransform);
        }

        public void AddImpulse(Vector3 impulse)
        {
            Pipeline.QueueImpulse(impulse);
        }

        public abstract void Teleport(Vector3 worldPosition, bool resetVelocity = true);

        public void AddModule(IMotorModule module) => Pipeline.AddModule(module, this);

        public bool RemoveModule(IMotorModule module) => Pipeline.RemoveModule(module, this);

        public T GetModule<T>() where T : class, IMotorModule => Pipeline.GetModule<T>();

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

        protected void RaiseStepEvents(in MotorContext ctx)
        {
            if (ctx.JumpedThisStep)
            {
                OnJumped?.Invoke();
            }

            OnAfterMove?.Invoke(CurrentState);
        }

        protected virtual MotorSettings GetDefaultSettings() => MotorSettings.Default3D;
    }
}
