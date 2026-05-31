using Garrison.Shared.Audio;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using SharedLifeState = Garrison.Shared.Player.LifeState;

namespace Garrison.Combat
{
    public sealed class Syrette : NetworkBehaviour, IConfigConsumer, IAudioBusSink
    {
        private const float DefaultReachRadius = 2.5f;
        private const int MaxNearbyColliders = 16;
        private const float GotUpVolume = 0.65f;

        private static readonly Collider[] NearbyColliders = new Collider[MaxNearbyColliders];

        [Header("Seams")]
        [SerializeField] private MonoBehaviour assignedPlayerSource;
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private MonoBehaviour playerSideSource;
        [SerializeField] private LifeState lifeState;

        [Header("Targeting")]
        [SerializeField] private LayerMask targetMask = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Presentation")]
        [SerializeField] private AudioClip gotUpClip;

        private IConfig config;
        private IAudioBus audioBus;

        private IAssignedPlayer AssignedPlayerSource => assignedPlayerSource as IAssignedPlayer;
        private ISyretteInput InputSource => inputSource as ISyretteInput;
        private IPlayerSide PlayerSide => playerSideSource as IPlayerSide;

        public void Configure(IConfig source)
        {
            config = source;
        }

        public void BindAudioBus(IAudioBus bus)
        {
            audioBus = bus;
        }

        private void OnEnable()
        {
            if (lifeState != null)
                lifeState.GotUp += OnGotUp;
        }

        private void OnDisable()
        {
            if (lifeState != null)
                lifeState.GotUp -= OnGotUp;
        }

        private void Update()
        {
            if (!CanReadLocalInput())
                return;

            if (!(InputSource?.UseSyrettePressedThisFrame ?? false))
                return;

            RequestUseSyrette();
        }

        [ServerRpc(channel: Channel.ReliableOrdered, requireOwnership: false)]
        private void RequestUseSyrette(RPCInfo info = default)
        {
            if (!isServer)
                return;

            IAssignedPlayer assignedPlayer = AssignedPlayerSource;
            if (assignedPlayer == null || info.sender != assignedPlayer.AssignedPlayer)
                return;

            if (lifeState == null || PlayerSide?.Side != Side.Attacker)
                return;

            LifeState target = ResolveTarget();
            if (target == null)
                return;

            target.TryRecoverFromDowned();
        }

        private bool CanReadLocalInput()
        {
            if (!isClient || !localPlayer.HasValue)
                return false;

            IAssignedPlayer assignedPlayer = AssignedPlayerSource;
            if (assignedPlayer == null || assignedPlayer.AssignedPlayer != localPlayer.Value)
                return false;

            if (PlayerSide?.Side != Side.Attacker || lifeState == null)
                return false;

            return lifeState.State != SharedLifeState.Dead;
        }

        private LifeState ResolveTarget()
        {
            if (lifeState.State == SharedLifeState.Downed)
                return lifeState;

            if (!lifeState.CanAct)
                return null;

            float reachRadius = Mathf.Max(0f, config?.GetFloat(ConfigKey.SyretteReachRadius, DefaultReachRadius) ?? DefaultReachRadius);
            if (reachRadius <= 0f)
                return null;

            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, reachRadius, NearbyColliders, targetMask, triggerInteraction);
            LifeState nearestTarget = null;
            float nearestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                Collider candidateCollider = NearbyColliders[i];
                NearbyColliders[i] = null;

                LifeState candidate = ResolveLifeState(candidateCollider);
                if (candidate == null || candidate == lifeState)
                    continue;

                if (candidate.Side != Side.Attacker || candidate.State != SharedLifeState.Downed)
                    continue;

                float distanceSquared = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distanceSquared >= nearestDistanceSquared)
                    continue;

                nearestDistanceSquared = distanceSquared;
                nearestTarget = candidate;
            }

            return nearestTarget;
        }

        private void OnGotUp()
        {
            if (!isClient || audioBus == null || gotUpClip == null)
                return;

            audioBus.Play(AudioChannel.Weapons, gotUpClip, transform.position, GotUpVolume, 1f);
        }

        private static LifeState ResolveLifeState(Collider collider)
        {
            if (collider == null)
                return null;

            if (collider.TryGetComponent(out LifeState targetLifeState))
                return targetLifeState;

            return collider.GetComponentInParent<LifeState>();
        }
    }
}
