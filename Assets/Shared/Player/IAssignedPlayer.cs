using PurrNet;

namespace Garrison.Shared.Player
{
    // Cross-slice seam for the body's authoritative owning player id. Player writes
    // it on the base body; other slices validate owner-issued RPCs through it without
    // referencing Player types directly.
    public interface IAssignedPlayer
    {
        PlayerID AssignedPlayer { get; }
    }
}
