using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Player
{
    // The networked player body: server-authoritative position, owned by the Player slice.
    // Implements the Shared ILocalPlayerView seam so local-presentation services (the
    // Vision camera) can follow it without referencing the Player slice. The local
    // check lives here because only the Player slice knows about AssignedPlayer.
    public sealed class PlayerBody : NetworkBehaviour, ILocalPlayerView
    {
        private readonly SyncVar<PlayerID> assignedPlayer = new(PlayerID.Server);

        public PlayerID AssignedPlayer => assignedPlayer.value;

        public Transform ViewTarget => transform;

        // True only on the client whose own body this is (mirrors PlayerInput's check).
        public bool IsLocalView => isClient && localPlayer.HasValue && assignedPlayer.value == localPlayer.Value;

        private void Awake()
        {
            EnsureVisual();
        }

        public void Assign(PlayerID player)
        {
            if (isServer)
                assignedPlayer.value = player;
        }

        private void EnsureVisual()
        {
            if (transform.childCount > 0)
                return;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(transform, false);
            visual.transform.localPosition = Vector3.up;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            if (visual.TryGetComponent(out Collider visualCollider))
                Destroy(visualCollider);
        }
    }
}
