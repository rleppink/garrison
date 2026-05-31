namespace Garrison.Shared.Player
{
    // The body's role for the round: attacker or defender. Set by the Player slice
    // (PlayerSpawner) on the base body and read by Combat (armor, C7) without a
    // sideways reference. Replicated, so every client can read any body's side.
    public interface IPlayerSide
    {
        Side Side { get; }
    }
}
