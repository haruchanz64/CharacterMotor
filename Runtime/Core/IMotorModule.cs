using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Modular locomotion extension hook. Implement as a pure C# class registered via motor.AddModule()
    /// or on a MonoBehaviour attached to the same GameObject (auto-discovered on Awake).
    /// </summary>
    public interface IMotorModule
    {
        string ModuleId { get; }
        int ExecutionOrder { get; }
        bool ModuleEnabled { get; set; }

        void OnAttach(ICharacterMotor motor);
        void OnDetach(ICharacterMotor motor);
        void BeforeVelocitySolve(ref MotorContext ctx);
        void ModifyVelocity(ref MotorContext ctx);
        void AfterMove(ref MotorContext ctx);
    }

    /// <summary>
    /// Optional MonoBehaviour base class for Inspector-editable IMotorModule components.
    /// </summary>
    public abstract class MotorModuleBehaviour : MonoBehaviour, IMotorModule
    {
        [SerializeField] private int executionOrder = 0;

        public virtual string ModuleId => GetType().Name;
        public virtual int ExecutionOrder => executionOrder;

        public bool ModuleEnabled
        {
            get => enabled;
            set => enabled = value;
        }

        protected ICharacterMotor Motor { get; private set; }

        public virtual void OnAttach(ICharacterMotor motor) => Motor = motor;
        public virtual void OnDetach(ICharacterMotor motor) => Motor = null;
        public virtual void BeforeVelocitySolve(ref MotorContext ctx) { }
        public virtual void ModifyVelocity(ref MotorContext ctx) { }
        public virtual void AfterMove(ref MotorContext ctx) { }
    }
}
