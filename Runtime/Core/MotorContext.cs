using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Mutable per-step context passed through every enabled IMotorModule during a tick.
    /// </summary>
    public struct MotorContext
    {
        public float DeltaTime;
        public MotorDimension Dimension;
        public bool IsTopDown2D;
        public MotorInput Input;
        public Vector3 MoveDirectionWorld;
        public MotorState State;
        public MotorSettings Settings;

        /// <summary>Velocity solved during this tick and applied by the physics backend.</summary>
        public Vector3 DesiredVelocity;

        /// <summary>Whether base gravity applies during this step.</summary>
        public bool ApplyGravity;

        /// <summary>Multiplier applied to base gravity for this step.</summary>
        public float GravityScale;

        /// <summary>When set by a module in BeforeVelocitySolve, skips base horizontal/planar velocity solving.</summary>
        public bool SuppressBaseMovement;

        /// <summary>When set by a module in BeforeVelocitySolve, skips base grounded jump execution.</summary>
        public bool SuppressBaseJump;

        /// <summary>True if either base jump or a module triggered a jump during this step.</summary>
        public bool JumpedThisStep;
    }
}
