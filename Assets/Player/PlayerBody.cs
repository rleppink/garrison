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
    public sealed class PlayerBody : NetworkBehaviour, IAssignedPlayer, ILocalPlayerView, IFacingSource, IMovementState, IPlayerSide, IAudioBusSink
    {
        [SerializeField] private PlayerAim aim;
        [SerializeField] private PlayerFootstepEmitter footstepEmitter;

        private readonly SyncVar<PlayerID> assignedPlayer = new(PlayerID.Server);
        private readonly SyncVar<int> movementState = new((int)MovementState.Idle);

        // Replicated as a primitive int (not the Side enum directly): a custom enum in
        // SyncVar<T> is the M1 hasher crash. Decode back to Side through the seam.
        private readonly SyncVar<int> side = new((int)Shared.Player.Side.Attacker);
        private Camera viewCamera;

        public event Action LocalViewStatusChanged;

        public PlayerID AssignedPlayer => assignedPlayer.value;

        public Transform ViewTarget => transform;

        public Camera ViewCamera => viewCamera;

        // True only on the client whose own body this is (mirrors PlayerInput's check).
        public bool IsLocalView => isClient && localPlayer.HasValue && assignedPlayer.value == localPlayer.Value;

        // The mouse-aim source for this body, surfaced through the local-player view seam.
        public IAimSource Aim => aim;

        IFacingSource ILocalPlayerView.Facing => this;

        public IMovementState Movement => this;

        // Role for the round (attacker/defender), decoded from the replicated int.
        public Side Side => DecodeSide(side.value);

        public MovementState State => DecodeMovementState(movementState.value);

        public bool IsIdle => State == MovementState.Idle;

        public bool IsRunning => State == MovementState.Running;

        public bool IsSprinting => State == MovementState.Sprinting;

        Vector2 IFacingSource.Facing
        {
            get
            {
                Vector3 planarForward = transform.forward;
                planarForward.y = 0f;

                float magnitude = planarForward.magnitude;
                return magnitude > Mathf.Epsilon
                    ? new Vector2(planarForward.x, planarForward.z) / magnitude
                    : Vector2.zero;
            }
        }

        // Registry hands over the persistent gameplay camera when this body becomes the
        // current local view; forward it to the aim component, which needs it for the
        // cursor->ground raycast. No lookup happens anywhere on the Player side.
        public void BindCamera(Camera camera)
        {
            viewCamera = camera;

            if (aim != null)
                aim.SetCamera(camera);
        }

        public void BindAudioBus(IAudioBus audioBus)
        {
            if (footstepEmitter != null)
                footstepEmitter.BindAudioBus(audioBus);
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

        // Server-only, symmetric with Assign(PlayerID): set before NetworkIdentity.Spawn
        // so the side ships in the initial network state. DefenderSlot (config) picks who.
        public void AssignSide(Side side)
        {
            if (!isSpawned || isServer)
                this.side.value = (int)side;
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
                MovementState.Running => (int)MovementState.Running,
                MovementState.Sprinting => (int)MovementState.Sprinting,
                _ => (int)MovementState.Idle
            };
        }

        private static MovementState DecodeMovementState(int value)
        {
            return value switch
            {
                (int)MovementState.Running => MovementState.Running,
                (int)MovementState.Sprinting => MovementState.Sprinting,
                _ => MovementState.Idle
            };
        }

        private static Side DecodeSide(int value)
        {
            return value == (int)Shared.Player.Side.Defender
                ? Shared.Player.Side.Defender
                : Shared.Player.Side.Attacker;
        }

        private void OnAssignedPlayerChanged(PlayerID _)
        {
            NotifyLocalViewStatusChanged();
        }

        private void NotifyLocalViewStatusChanged()
        {
            LocalViewStatusChanged?.Invoke();
        }
    }
}
