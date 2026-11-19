using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Mutable per-step context passed through every enabled IMotorModule during a tick.
    /// </summary>
    public struct MotorContext
    {
        /// <summary>Elapsed step duration in seconds (Update deltaTime or FixedUpdate fixedDeltaTime).</summary>
        public float DeltaTime;
        /// <summary>Physics dimension of the active motor backend.</summary>
        public MotorDimension Dimension;
        /// <summary>True when running <see cref="Motor2DMode.TopDown"/> with gravity and jumping disabled.</summary>
        public bool IsTopDown2D;
        /// <summary>Input snapshot consumed for this step.</summary>
        public MotorInput Input;
        /// <summary>Directional input projected into world space by the active reference frame.</summary>
        public Vector3 MoveDirectionWorld;
        /// <summary>Locomotion state captured before solving and refreshed after movement completes.</summary>
        public MotorState State;
        /// <summary>Active locomotion settings for this step.</summary>
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
