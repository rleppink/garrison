using PurrNet;
using UnityEngine;

namespace Garrison.Shared.Player
{
    // M0 throwaway skeleton: M1/M2 can replace this when movement and combat own the player body.
    public sealed class PlayerCapsule : NetworkBehaviour
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
