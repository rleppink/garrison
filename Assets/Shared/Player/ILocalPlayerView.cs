using UnityEngine;

namespace Garrison.Shared.Player
{
    // Cross-slice seam: what a local-presentation follower (the camera in M1, aim
    // in C3, shoulder-spectator in M9) needs from the player's own body. Implemented
    // in the Player slice; consumed by Vision through the local-player registry so
    // the two slices stay mutually ignorant (Vision never references Player).
    public interface ILocalPlayerView
    {
        // The transform a follower frames. For M1 this is the body itself.
        Transform ViewTarget { get; }

        // True only on the client that owns this view (its own body): the body
        // decides locality from its assigned player vs the local player, keeping
        // that Player-specific check out of Shared.
        bool IsLocalView { get; }
    }
}
