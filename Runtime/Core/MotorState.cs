using System;
using UnityEngine;

namespace CharacterMotor.Core
{
    public enum MotorDimension
    {
        Mode3D = 0,
        Mode2D = 1
    }

    public enum Motor2DMode
    {
        Platformer = 0,
        TopDown = 1
    }

    public enum CharacterRotationMode
    {
        None = 0,
        FaceMoveDirection = 1,
        FaceReferenceForward = 2
    }

    /// <summary>
    /// Observable locomotion state updated by the active motor backend.
    /// </summary>
    [Serializable]
    public struct MotorState
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public bool IsGrounded;
        public Vector3 GroundNormal;
        public float SlopeAngle;
        public float TimeSinceGrounded;
        public Vector3 FacingDirection;
    }
}
