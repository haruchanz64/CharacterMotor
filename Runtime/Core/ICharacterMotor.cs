using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Unified contract implemented by all CharacterMotor backends (3D CharacterController,
    /// 3D Rigidbody, 2D Rigidbody2D, or custom developer motors).
    /// </summary>
    public interface ICharacterMotor
    {
        string MotorName { get; }
        MotorDimension Dimension { get; }
        Transform Transform { get; }
        MotorState State { get; }
        MotorSettings Settings { get; set; }
        IReadOnlyList<IMotorModule> Modules { get; }

        event Action<bool> OnGroundedChanged;
        event Action OnJumped;
        event Action<MotorState> OnAfterMove;

        void SetInput(in MotorInput input);
        void SetInputSource(ICharacterInputSource source);
        void SetReferenceFrame(IMovementReferenceFrame referenceFrame);
        void AddImpulse(Vector3 impulse);
        void Teleport(Vector3 worldPosition, bool resetVelocity = true);

        void AddModule(IMotorModule module);
        bool RemoveModule(IMotorModule module);
        T GetModule<T>() where T : class, IMotorModule;
    }
}
