using Garrison.Shared.Player;
using Garrison.Shared.Vision;
using PurrNet;
using UnityEngine;

namespace Garrison.Vision
{
    // Throwaway C4 NPC body: server-auth networked target for fog plus a facing seam
    // the visible cone can mirror. M5 replaces the sweep driver with real behaviour.
    public sealed class NpcBody : NetworkBehaviour, IVisionAgent, IFacingSource
    {
        [SerializeField] private Transform facingAnchor;
        [SerializeField, Min(0f)] private float eyeHeight = 1.6f;

        public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;

        public Transform FacingAnchor => facingAnchor;

        Vector2 IFacingSource.Facing
        {
            get
            {
                Vector3 planarForward = (facingAnchor ? facingAnchor.forward : transform.forward);
                planarForward.y = 0f;

                float magnitude = planarForward.magnitude;
                return magnitude > Mathf.Epsilon
                    ? new Vector2(planarForward.x, planarForward.z) / magnitude
                    : Vector2.zero;
            }
        }
    }
}
