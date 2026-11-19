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
        /// <summary>Display name combining the GameObject name and motor backend type.</summary>
        string MotorName { get; }

        /// <summary>Whether this motor operates in 3D or 2D physics space.</summary>
        MotorDimension Dimension { get; }

        /// <summary>Transform owned by this motor.</summary>
        Transform Transform { get; }

        /// <summary>Latest locomotion snapshot captured after the most recent step.</summary>
        MotorState State { get; }

        /// <summary>Active locomotion parameters, clamped to valid ranges when assigned.</summary>
        MotorSettings Settings { get; set; }

        /// <summary>Attached modules sorted by <see cref="IMotorModule.ExecutionOrder"/>.</summary>
        IReadOnlyList<IMotorModule> Modules { get; }

        /// <summary>Raised when grounded state transitions; passes true on landing and false on leaving ground.</summary>
        event Action<bool> OnGroundedChanged;

        /// <summary>Raised on any step where base movement or a module triggered a jump.</summary>
        event Action OnJumped;

        /// <summary>Raised at the end of every step with the updated <see cref="MotorState"/>.</summary>
        event Action<MotorState> OnAfterMove;

        /// <summary>
        /// Supplies an explicit input snapshot for the next motor step, taking priority over any bound <see cref="ICharacterInputSource"/>.
        /// </summary>
        /// <param name="input">Input snapshot to consume on the next step.</param>
        void SetInput(in MotorInput input);

        /// <summary>
        /// Binds or clears the input source polled when no manual input is queued.
        /// </summary>
        /// <param name="source">Input provider, or null to disable automatic polling.</param>
        void SetInputSource(ICharacterInputSource source);

        /// <summary>
        /// Sets the reference frame used to project 2D directional input into world space.
        /// </summary>
        /// <param name="referenceFrame">Custom frame implementation, or null to restore the default transform frame.</param>
        void SetReferenceFrame(IMovementReferenceFrame referenceFrame);

        /// <summary>
        /// Queues an additive velocity impulse applied at the end of the next velocity solve.
        /// </summary>
        /// <param name="impulse">Velocity delta in world units per second.</param>
        void AddImpulse(Vector3 impulse);

        /// <summary>
        /// Moves the character directly to a world position, bypassing collision sweeps.
        /// </summary>
        /// <param name="worldPosition">Target world-space position.</param>
        /// <param name="resetVelocity">When true, zeroes current velocity and clears queued impulses.</param>
        void Teleport(Vector3 worldPosition, bool resetVelocity = true);

        /// <summary>
        /// Registers a locomotion module if not already present and invokes its <see cref="IMotorModule.OnAttach"/> hook.
        /// </summary>
        /// <param name="module">Module instance to attach; ignored when null or already registered.</param>
        void AddModule(IMotorModule module);

        /// <summary>
        /// Unregisters a locomotion module and invokes its <see cref="IMotorModule.OnDetach"/> hook.
        /// </summary>
        /// <param name="module">Module instance to remove.</param>
        /// <returns>True if the module was found and removed; otherwise false.</returns>
        bool RemoveModule(IMotorModule module);

        /// <summary>
        /// Returns the first registered module assignable to <typeparamref name="T"/>, or null if none is attached.
        /// </summary>
        /// <typeparam name="T">Concrete or interface module type to locate.</typeparam>
        /// <returns>The matching module instance, or null.</returns>
        T GetModule<T>() where T : class, IMotorModule;
    }
}
