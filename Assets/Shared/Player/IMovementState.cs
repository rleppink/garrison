namespace Garrison.Shared.Player
{
    public interface IMovementState
    {
        MovementState State { get; }
        bool IsIdle { get; }
        bool IsRunning { get; }
        bool IsSprinting { get; }
    }
}
