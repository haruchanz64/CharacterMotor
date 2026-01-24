namespace CharacterMotor.Core
{
    /// <summary>
    /// Implement to feed player, AI, replay, or networked inputs into any ICharacterMotor.
    /// </summary>
    public interface ICharacterInputSource
    {
        MotorInput ReadInput();
    }
}
