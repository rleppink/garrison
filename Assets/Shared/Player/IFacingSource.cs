using UnityEngine;

namespace Garrison.Shared.Player
{
    // Cross-slice seam: the server-truth planar facing derived from the body's
    // rotation. Combat reads this in later milestones for shot direction instead of
    // trusting a client-claimed aim vector.
    public interface IFacingSource
    {
        // Body forward projected onto XZ and normalized. Zero only if the transform
        // has no meaningful planar forward.
        Vector2 Facing { get; }
    }
}
