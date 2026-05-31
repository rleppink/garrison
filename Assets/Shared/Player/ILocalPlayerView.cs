using System;
using UnityEngine;

namespace Garrison.Shared.Player
{
    // Cross-slice seam: what a local-presentation follower (the camera in M1, aim
    // in C3, shoulder-spectator in M9) needs from the player's own body. Implemented
    // in the Player slice; consumed by Vision through the local-player registry so
    // the two slices stay mutually ignorant (Vision never references Player).
    public interface ILocalPlayerView
    {
        // Fired when IsLocalView may have changed after spawn, local-player-id
        // assignment, or assigned-player SyncVar replication.
        event Action LocalViewStatusChanged;

        // The transform a follower frames. For M1 this is the body itself.
        Transform ViewTarget { get; }

        // The persistent gameplay camera currently bound to this view. Presentation
        // consumers use this to stay in the same camera context as aim/camera-push
        // without doing Camera.main / scene lookups.
        Camera ViewCamera { get; }

        // True only on the client that owns this view (its own body): the body
        // decides locality from its assigned player vs the local player, keeping
        // that Player-specific check out of Shared.
        bool IsLocalView { get; }

        // The body's mouse-aim, surfaced for other slices (C4 camera-push, later M2).
        // Symmetric with ViewTarget: a consumer holding registry.Current reads it here.
        IAimSource Aim { get; }

        // The server-truth facing derived from the body's rotation. Surfaced through
        // the same seam as Aim/Movement so later combat reads posture, not client claims.
        IFacingSource Facing { get; }

        // Server-derived movement state, replicated by the body and surfaced as a seam
        // for future slices without referencing Player internals.
        IMovementState Movement { get; }

        // Injected by the local-player registry when this view becomes Current: the
        // persistent gameplay camera the aim raycast needs. The body is runtime-spawned
        // and can't be inspector-wired to the persistent camera, and Find/Camera.main/
        // Instance/static are banned — so the registry (a persistent Bootstrap object
        // that CAN be inspector-wired to the persistent Main Camera) hands it over here.
        // Camera is a UnityEngine type, so referencing it from Shared is not a slice
        // dependency. Null is passed when the camera is unavailable.
        void BindCamera(Camera camera);
    }
}
