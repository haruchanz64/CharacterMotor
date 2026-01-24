using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Samples
{
    /// <summary>
    /// Example IMotorModule adding coyote time and jump input buffering using MotorState.TimeSinceGrounded.
    /// </summary>
    public sealed class SampleCoyoteJumpModule : MotorModuleBehaviour
    {
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        private float _bufferRemaining;

        public override void BeforeVelocitySolve(ref MotorContext ctx)
        {
            if (ctx.Input.JumpPressed)
            {
                _bufferRemaining = jumpBufferTime;
            }
            else if (_bufferRemaining > 0f)
            {
                _bufferRemaining -= ctx.DeltaTime;
            }

            bool canCoyoteJump = !ctx.State.IsGrounded && ctx.State.TimeSinceGrounded <= coyoteTime;
            if ((ctx.State.IsGrounded || canCoyoteJump) && _bufferRemaining > 0f && ctx.Settings.jumpSpeed > 0f)
            {
                ctx.SuppressBaseJump = true;
                ctx.DesiredVelocity.y = ctx.Settings.jumpSpeed;
                ctx.JumpedThisStep = true;
                _bufferRemaining = 0f;
            }
        }
    }
}
