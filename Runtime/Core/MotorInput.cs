using System;
using UnityEngine;

namespace CharacterMotor.Core
{
    /// <summary>
    /// Minimal per-frame or per-tick input snapshot passed to any ICharacterMotor.
    /// Decouples locomotion from specific input devices so player, AI, replay, or network
    /// controllers can drive the same motor.
    /// </summary>
    [Serializable]
    public struct MotorInput
    {
        /// <summary>2D movement axes (X = horizontal/strafe, Y = vertical/forward), typically in [-1, 1].</summary>
        public Vector2 Move;
        /// <summary>Optional look or aim delta exposed for camera scripts.</summary>
        public Vector2 Look;
        /// <summary>True on the step a jump press is consumed.</summary>
        public bool JumpPressed;
        /// <summary>True while the jump input remains held.</summary>
        public bool JumpHeld;

        /// <summary>
        /// Custom 32-bit action bitmask for developer-defined IMotorModule abilities
        /// (for example bit 0 = Sprint, bit 1 = Dash, bit 2 = Crouch).
        /// </summary>
        public uint ActionMask;

        /// <summary>
        /// Returns whether the custom action bit at <paramref name="bitIndex"/> is active.
        /// </summary>
        /// <param name="bitIndex">Zero-based bit index in [0, 31]; out-of-range values return false.</param>
        /// <returns>True when the bit is set in <see cref="ActionMask"/>; otherwise false.</returns>
        public bool HasAction(int bitIndex)
        {
            return bitIndex >= 0 && bitIndex < 32 && (ActionMask & (1u << bitIndex)) != 0u;
        }

        /// <summary>
        /// Sets or clears the custom action bit at <paramref name="bitIndex"/> in <see cref="ActionMask"/>.
        /// </summary>
        /// <param name="bitIndex">Zero-based bit index in [0, 31]; out-of-range indices are ignored.</param>
        /// <param name="active">True to set the bit, false to clear it.</param>
        public void SetAction(int bitIndex, bool active)
        {
            if (bitIndex < 0 || bitIndex >= 32) return;
            uint mask = 1u << bitIndex;
            if (active) ActionMask |= mask;
            else ActionMask &= ~mask;
        }

        /// <summary>Zeroed input snapshot with no movement, jump, or action bits active.</summary>
        public static MotorInput Empty => default;
    }
}
