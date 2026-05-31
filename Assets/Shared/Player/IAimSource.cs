using UnityEngine;

namespace Garrison.Shared.Player
{
    // Cross-slice seam: the player's mouse-derived aim, in world space. Produced in
    // the Player slice (local body only) and consumed by other slices through the
    // local-player registry (ILocalPlayerView.Aim), the same way ViewTarget is
    // surfaced. M1 use: the camera-push (C4) reads this. In M2, PlayerInput streams
    // AimDirection to the server as part of the existing owner-input packet.
    public interface IAimSource
    {
        // World point on the ground math-plane (y = body.y) under the cursor.
        Vector3 AimPoint { get; }

        // (AimPoint - bodyPos) flattened to the XZ plane, normalized. Zero when the
        // cursor sits on the body (no meaningful direction).
        Vector2 AimDirection { get; }

        // Planar distance from the body to AimPoint; grows as the cursor moves away.
        float AimDistance { get; }
    }
}
