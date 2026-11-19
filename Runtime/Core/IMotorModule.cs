using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Modular locomotion extension hook. Implement as a pure C# class registered via motor.AddModule()
    /// or on a MonoBehaviour attached to the same GameObject (auto-discovered on Awake).
    /// </summary>
    public interface IMotorModule
    {
        /// <summary>Stable identifier used for debugger display and deterministic sort tie-breaking.</summary>
        string ModuleId { get; }

        /// <summary>Priority controlling hook execution order; lower values run first.</summary>
        int ExecutionOrder { get; }

        /// <summary>When false, the pipeline skips all per-step hooks on this module.</summary>
        bool ModuleEnabled { get; set; }

        /// <summary>
        /// Called once when the module is registered with a motor.
        /// </summary>
        /// <param name="motor">Owning motor instance.</param>
        void OnAttach(ICharacterMotor motor);

        /// <summary>
        /// Called when the module is removed or the owning motor is destroyed.
        /// </summary>
        /// <param name="motor">Owning motor instance being detached from.</param>
        void OnDetach(ICharacterMotor motor);

        /// <summary>
        /// Runs before base movement, jump, and gravity solving. Set flags such as
        /// <see cref="MotorContext.SuppressBaseMovement"/> or <see cref="MotorContext.ApplyGravity"/> here.
        /// </summary>
        /// <param name="ctx">Mutable per-step locomotion context.</param>
        void BeforeVelocitySolve(ref MotorContext ctx);

        /// <summary>
        /// Runs after base movement and gravity solving and before external impulses are added.
        /// Adjust <see cref="MotorContext.DesiredVelocity"/> here for speed modifiers, dashes, or friction.
        /// </summary>
        /// <param name="ctx">Mutable per-step locomotion context.</param>
        void ModifyVelocity(ref MotorContext ctx);

        /// <summary>
        /// Runs after the physics backend applies movement and updates <see cref="MotorContext.State"/>.
        /// </summary>
        /// <param name="ctx">Post-move locomotion context containing the final motor state.</param>
        void AfterMove(ref MotorContext ctx);
    }

    /// <summary>
    /// Optional MonoBehaviour base class for Inspector-editable IMotorModule components.
    /// </summary>
    public abstract class MotorModuleBehaviour : MonoBehaviour, IMotorModule
    {
        [SerializeField] private int executionOrder = 0;

        /// <inheritdoc/>
        public virtual string ModuleId => GetType().Name;

        /// <inheritdoc/>
        public virtual int ExecutionOrder => executionOrder;

        /// <summary>Maps module enablement directly to <see cref="Behaviour.enabled"/> in the Inspector.</summary>
        public bool ModuleEnabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>Motor instance this module is currently attached to, or null when detached.</summary>
        protected ICharacterMotor Motor { get; private set; }

        /// <inheritdoc/>
        public virtual void OnAttach(ICharacterMotor motor) => Motor = motor;

        /// <inheritdoc/>
        public virtual void OnDetach(ICharacterMotor motor) => Motor = null;

        /// <inheritdoc/>
        public virtual void BeforeVelocitySolve(ref MotorContext ctx) { }

        /// <inheritdoc/>
        public virtual void ModifyVelocity(ref MotorContext ctx) { }

        /// <inheritdoc/>
        public virtual void AfterMove(ref MotorContext ctx) { }
    }
}
