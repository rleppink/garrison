using PurrNet;
using UnityEngine;

namespace Garrison.Player
{
    // The networked player body: server-authoritative position, owned by the Player slice.
    public sealed class PlayerBody : NetworkBehaviour
    {
        private readonly SyncVar<PlayerID> assignedPlayer = new(PlayerID.Server);

        public PlayerID AssignedPlayer => assignedPlayer.value;

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
