using System;
using Garrison.Shared.Audio;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Player
{
    // The networked player body: server-authoritative position, owned by the Player slice.
    // Implements the Shared ILocalPlayerView seam so local-presentation services (the
    // Vision camera) can follow it without referencing the Player slice. The local
    // check lives here because only the Player slice knows about AssignedPlayer.
    public sealed class PlayerBody : NetworkBehaviour, ILocalPlayerView, IMovementState, IAudioBusSink
    {
        [SerializeField] private PlayerAim aim;
        [SerializeField] private PlayerFootstepEmitter footstepEmitter;

        private readonly SyncVar<PlayerID> assignedPlayer = new(PlayerID.Server);
        private readonly SyncVar<int> movementState = new((int)MovementState.Idle);

        public event Action LocalViewStatusChanged;

        public PlayerID AssignedPlayer => assignedPlayer.value;

        public Transform ViewTarget => transform;

        // True only on the client whose own body this is (mirrors PlayerInput's check).
        public bool IsLocalView => isClient && localPlayer.HasValue && assignedPlayer.value == localPlayer.Value;

        // The mouse-aim source for this body, surfaced through the local-player view seam.
        public IAimSource Aim => aim;

        public IMovementState Movement => this;

        public MovementState State => DecodeMovementState(movementState.value);

        public bool IsIdle => State == MovementState.Idle;

        public bool IsWalking => State == MovementState.Walking;

        public bool IsSprinting => State == MovementState.Sprinting;

        // Registry hands over the persistent gameplay camera when this body becomes the
        // current local view; forward it to the aim component, which needs it for the
        // cursor->ground raycast. No lookup happens anywhere on the Player side.
        public void BindCamera(Camera camera)
        {
            if (aim != null)
                aim.SetCamera(camera);
        }

        public void BindAudioBus(IAudioBus audioBus)
        {
            if (footstepEmitter != null)
                footstepEmitter.BindAudioBus(audioBus);
        }

        private void Awake()
        {
            EnsureVisual();
        }

        private void OnEnable()
        {
            assignedPlayer.onChanged += OnAssignedPlayerChanged;
        }

        private void OnDisable()
        {
            assignedPlayer.onChanged -= OnAssignedPlayerChanged;
        }

        protected override void OnSpawned(bool asServer)
        {
            NotifyLocalViewStatusChanged();
        }

        protected override void OnDespawned(bool asServer)
        {
            NotifyLocalViewStatusChanged();
        }

        public void Assign(PlayerID player)
        {
            if (!isSpawned || isServer)
                assignedPlayer.value = player;

            NotifyLocalViewStatusChanged();
        }

        public void SetMovementState(MovementState state)
        {
            if (isServer)
                movementState.value = EncodeMovementState(state);
        }

        private static int EncodeMovementState(MovementState state)
        {
            return state switch
            {
                MovementState.Walking => (int)MovementState.Walking,
                MovementState.Sprinting => (int)MovementState.Sprinting,
                _ => (int)MovementState.Idle
            };
        }

        private static MovementState DecodeMovementState(int value)
        {
            return value switch
            {
                (int)MovementState.Walking => MovementState.Walking,
                (int)MovementState.Sprinting => MovementState.Sprinting,
                _ => MovementState.Idle
            };
        }

        private void OnAssignedPlayerChanged(PlayerID _)
        {
            NotifyLocalViewStatusChanged();
        }

        private void NotifyLocalViewStatusChanged()
        {
            LocalViewStatusChanged?.Invoke();
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
