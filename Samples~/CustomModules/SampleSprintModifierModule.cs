using CharacterMotor.Core;
using UnityEngine;

namespace CharacterMotor.Samples
{
    /// <summary>
    /// Example IMotorModule multiplying planar velocity while ActionMask bit 0 (e.g. Shift) is held.
    /// </summary>
    public sealed class SampleSprintModifierModule : MotorModuleBehaviour
    {
        [SerializeField] private int sprintActionBit = 0;
        [SerializeField, Min(1f)] private float speedMultiplier = 1.45f;

        /// <summary>
        /// Scales planar or horizontal <see cref="MotorContext.DesiredVelocity"/> by <c>speedMultiplier</c> while <c>sprintActionBit</c> is active.
        /// </summary>
        /// <param name="ctx">Per-step locomotion context.</param>
        public override void ModifyVelocity(ref MotorContext ctx)
        {
            if (!ctx.Input.HasAction(sprintActionBit)) return;

            if (ctx.Dimension == MotorDimension.Mode2D && ctx.IsTopDown2D)
            {
                ctx.DesiredVelocity *= speedMultiplier;
            }
            else if (ctx.Dimension == MotorDimension.Mode2D)
            {
                ctx.DesiredVelocity.x *= speedMultiplier;
            }
            else
            {
                ctx.DesiredVelocity.x *= speedMultiplier;
                ctx.DesiredVelocity.z *= speedMultiplier;
            }
        }
    }
}
