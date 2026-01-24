using System;
using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Barebones base locomotion parameters shared across 2D and 3D motor backends.
    /// </summary>
    [Serializable]
    public struct MotorSettings
    {
        [Min(0f)] public float maxSpeed;
        [Min(0f)] public float groundAcceleration;
        [Min(0f)] public float groundDeceleration;
        [Min(0f)] public float airAcceleration;
        [Min(0f)] public float jumpSpeed;
        [Min(0f)] public float gravity;
        [Min(0f)] public float maxFallSpeed;
        [Range(0f, 89f)] public float maxSlopeAngle;
        [Min(0f)] public float groundStickVelocity;

        public static MotorSettings Default3D => new MotorSettings
        {
            maxSpeed = 6.5f,
            groundAcceleration = 45f,
            groundDeceleration = 55f,
            airAcceleration = 18f,
            jumpSpeed = 8.5f,
            gravity = 24f,
            maxFallSpeed = 40f,
            maxSlopeAngle = 48f,
            groundStickVelocity = 2f
        };

        public static MotorSettings DefaultPlatformer2D => new MotorSettings
        {
            maxSpeed = 8f,
            groundAcceleration = 65f,
            groundDeceleration = 75f,
            airAcceleration = 35f,
            jumpSpeed = 13f,
            gravity = 36f,
            maxFallSpeed = 30f,
            maxSlopeAngle = 45f,
            groundStickVelocity = 1.5f
        };

        public static MotorSettings DefaultTopDown2D => new MotorSettings
        {
            maxSpeed = 7f,
            groundAcceleration = 60f,
            groundDeceleration = 70f,
            airAcceleration = 60f,
            jumpSpeed = 0f,
            gravity = 0f,
            maxFallSpeed = 0f,
            maxSlopeAngle = 89f,
            groundStickVelocity = 0f
        };

        public void Clamp()
        {
            maxSpeed = Mathf.Max(0f, maxSpeed);
            groundAcceleration = Mathf.Max(0f, groundAcceleration);
            groundDeceleration = Mathf.Max(0f, groundDeceleration);
            airAcceleration = Mathf.Max(0f, airAcceleration);
            jumpSpeed = Mathf.Max(0f, jumpSpeed);
            gravity = Mathf.Max(0f, gravity);
            maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
            maxSlopeAngle = Mathf.Clamp(maxSlopeAngle, 0f, 89f);
            groundStickVelocity = Mathf.Max(0f, groundStickVelocity);
        }
    }

    /// <summary>
    /// Optional reusable ScriptableObject holding barebones base motor settings.
    /// </summary>
    [CreateAssetMenu(fileName = "MotorConfig", menuName = "CharacterMotor/Motor Config", order = 100)]
    public class MotorConfigAsset : ScriptableObject
    {
        public MotorSettings settings = MotorSettings.Default3D;

        private void OnValidate()
        {
            settings.Clamp();
        }
    }
}
