namespace Garrison.Shared.Player
{
    // Cross-slice seam for the local "use syrette" intent. Player owns the actual key
    // read; Combat only queries whether the owning client pressed it this frame.
    public interface ISyretteInput
    {
        bool UseSyrettePressedThisFrame { get; }
    }
}
