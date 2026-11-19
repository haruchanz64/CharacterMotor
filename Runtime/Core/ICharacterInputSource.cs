namespace CharacterMotor.Core
{
    /// <summary>
    /// Implement to feed player, AI, replay, or networked inputs into any ICharacterMotor.
    /// </summary>
    public interface ICharacterInputSource
    {
        /// <summary>
        /// Reads the current input snapshot for a motor step and consumes any buffered one-shot triggers such as jump.
        /// </summary>
        /// <returns>The current <see cref="MotorInput"/> snapshot.</returns>
        MotorInput ReadInput();
    }
}
