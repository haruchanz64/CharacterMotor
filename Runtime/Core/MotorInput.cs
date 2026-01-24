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
        public Vector2 Move;
        public Vector2 Look;
        public bool JumpPressed;
        public bool JumpHeld;

        /// <summary>
        /// Custom 32-bit action bitmask for developer-defined IMotorModule abilities
        /// (for example bit 0 = Sprint, bit 1 = Dash, bit 2 = Crouch).
        /// </summary>
        public uint ActionMask;

        public bool HasAction(int bitIndex)
        {
            return bitIndex >= 0 && bitIndex < 32 && (ActionMask & (1u << bitIndex)) != 0u;
        }

        public void SetAction(int bitIndex, bool active)
        {
            if (bitIndex < 0 || bitIndex >= 32) return;
            uint mask = 1u << bitIndex;
            if (active) ActionMask |= mask;
            else ActionMask &= ~mask;
        }

        public static MotorInput Empty => default;
    }
}
