namespace Garrison.Shared.Player
{
    public interface IMovementState
    {
        MovementState State { get; }
        bool IsIdle { get; }
        bool IsWalking { get; }
        bool IsSprinting { get; }
    }
}
