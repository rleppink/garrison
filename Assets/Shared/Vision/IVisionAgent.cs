using UnityEngine;

namespace Garrison.Shared.Vision
{
    // Cross-slice seam for bodies that participate in server-side LOS and fog.
    public interface IVisionAgent
    {
        Vector3 EyePosition { get; }
    }
}
