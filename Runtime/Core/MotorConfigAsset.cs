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
        /// <summary>Target maximum planar or horizontal movement speed in units per second.</summary>
        [Min(0f)] public float maxSpeed;
        /// <summary>Acceleration rate while grounded in units per second squared.</summary>
        [Min(0f)] public float groundAcceleration;
        /// <summary>Deceleration rate toward zero while grounded with no input in units per second squared.</summary>
        [Min(0f)] public float groundDeceleration;
        /// <summary>Horizontal or planar acceleration rate while airborne in units per second squared.</summary>
        [Min(0f)] public float airAcceleration;
        /// <summary>Initial upward velocity applied on a grounded jump in units per second.</summary>
        [Min(0f)] public float jumpSpeed;
        /// <summary>Downward gravity magnitude applied while airborne in units per second squared.</summary>
        [Min(0f)] public float gravity;
        /// <summary>Terminal downward fall speed clamp in units per second; 0 disables clamping.</summary>
        [Min(0f)] public float maxFallSpeed;
        /// <summary>Maximum walkable surface angle in degrees from <see cref="Vector3.up"/> (0 to 89).</summary>
        [Range(0f, 89f)] public float maxSlopeAngle;
        /// <summary>Small downward velocity applied while grounded to keep contact on gentle downhill slopes.</summary>
        [Min(0f)] public float groundStickVelocity;

        /// <summary>Baseline locomotion preset tuned for 3D character controllers and rigidbodies.</summary>
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

        /// <summary>Baseline locomotion preset tuned for 2D side-scrolling platformers.</summary>
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

        /// <summary>Baseline locomotion preset tuned for 2D top-down movement with gravity and jumping disabled.</summary>
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

        /// <summary>
        /// Clamps all locomotion fields in place to non-negative values and limits <see cref="maxSlopeAngle"/> to [0, 89].
        /// </summary>
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
        /// <summary>Shared locomotion settings applied to any motor referencing this asset.</summary>
        public MotorSettings settings = MotorSettings.Default3D;

        private void OnValidate()
        {
            settings.Clamp();
        }
    }
}
