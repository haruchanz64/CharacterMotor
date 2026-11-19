using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Samples
{
    /// <summary>
    /// Example IMotorModule adding a timed directional dash triggered by ActionMask bit 1.
    /// Demonstrates BeforeVelocitySolve + SuppressBaseMovement + ModifyVelocity.
    /// </summary>
    public sealed class SampleDashModule : MotorModuleBehaviour
    {
        [SerializeField] private int actionBit = 1;
        [SerializeField, Min(1f)] private float dashSpeed = 18f;
        [SerializeField, Min(0.05f)] private float dashDuration = 0.18f;
        [SerializeField, Min(0f)] private float cooldown = 0.6f;

        private float _dashRemaining;
        private float _cooldownRemaining;
        private Vector3 _dashDirection;

        /// <summary>
        /// Updates dash timers, starts a dash along move or facing direction when the configured action bit fires,
        /// and suppresses base movement and gravity while dashing.
        /// </summary>
        /// <param name="ctx">Per-step locomotion context.</param>
        public override void BeforeVelocitySolve(ref MotorContext ctx)
        {
            if (_cooldownRemaining > 0f) _cooldownRemaining -= ctx.DeltaTime;

            if (_dashRemaining <= 0f && _cooldownRemaining <= 0f && ctx.Input.HasAction(actionBit))
            {
                Vector3 dir = ctx.MoveDirectionWorld.sqrMagnitude > 1e-4f
                    ? ctx.MoveDirectionWorld.normalized
                    : ctx.State.FacingDirection.normalized;
                if (dir.sqrMagnitude < 1e-4f) dir = Vector3.right;

                _dashDirection = dir;
                _dashRemaining = dashDuration;
                _cooldownRemaining = cooldown;
            }

            if (_dashRemaining > 0f)
            {
                _dashRemaining -= ctx.DeltaTime;
                ctx.SuppressBaseMovement = true;
                ctx.ApplyGravity = false;
            }
        }

        /// <summary>
        /// Overrides <see cref="MotorContext.DesiredVelocity"/> with the locked dash direction and speed while a dash is active.
        /// </summary>
        /// <param name="ctx">Per-step locomotion context.</param>
        public override void ModifyVelocity(ref MotorContext ctx)
        {
            if (_dashRemaining > 0f)
            {
                ctx.DesiredVelocity = _dashDirection * dashSpeed;
            }
        }
    }
}
