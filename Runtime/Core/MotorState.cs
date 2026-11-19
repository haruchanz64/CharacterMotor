using System;
using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>Physics dimension used by a motor backend.</summary>
    public enum MotorDimension
    {
        /// <summary>3D locomotion on the XZ plane with Y as vertical.</summary>
        Mode3D = 0,
        /// <summary>2D locomotion on the XY plane.</summary>
        Mode2D = 1
    }

    /// <summary>Locomotion style used by <see cref="CharacterMotor.Motors.RigidbodyMotor2D"/>.</summary>
    public enum Motor2DMode
    {
        /// <summary>Side-scrolling movement along X with gravity and jumping on Y.</summary>
        Platformer = 0,
        /// <summary>Omnidirectional movement on XY with gravity and jumping disabled.</summary>
        TopDown = 1
    }

    /// <summary>Controls how 3D motors orient the character transform each step.</summary>
    public enum CharacterRotationMode
    {
        /// <summary>Leave transform rotation untouched so external scripts or cameras control facing.</summary>
        None = 0,
        /// <summary>Rotate smoothly toward the current planar movement direction.</summary>
        FaceMoveDirection = 1,
        /// <summary>Rotate smoothly toward the active reference frame's planar forward vector.</summary>
        FaceReferenceForward = 2
    }

    /// <summary>
    /// Observable locomotion state updated by the active motor backend.
    /// </summary>
    [Serializable]
    public struct MotorState
    {
        /// <summary>Current world-space position of the character.</summary>
        public Vector3 Position;
        /// <summary>Current world-space velocity in units per second.</summary>
        public Vector3 Velocity;
        /// <summary>True when standing on a walkable surface within <see cref="MotorSettings.maxSlopeAngle"/>.</summary>
        public bool IsGrounded;
        /// <summary>Normalized surface normal of the ground contact, or <see cref="Vector3.up"/> while airborne.</summary>
        public Vector3 GroundNormal;
        /// <summary>Angle in degrees between <see cref="GroundNormal"/> and <see cref="Vector3.up"/> while grounded.</summary>
        public float SlopeAngle;
        /// <summary>Elapsed airborne duration in seconds since the last grounded step; 0 while grounded.</summary>
        public float TimeSinceGrounded;
        /// <summary>Latest unit facing direction tracked by the motor.</summary>
        public Vector3 FacingDirection;
    }
}
