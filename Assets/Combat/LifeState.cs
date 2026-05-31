using System;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;
using SharedLifeState = Garrison.Shared.Player.LifeState;

namespace Garrison.Combat
{
    public sealed class LifeState : NetworkBehaviour, ILifeState, IConfigConsumer
    {
        private const int DefaultMaxHearts = 3;
        private const int DefaultDefenderMaxHearts = 4;
        private const float DefaultBleedOutSec = 12f;

        [SerializeField] private MonoBehaviour playerSideSource;

        private readonly SyncVar<int> hearts = new(DefaultMaxHearts);
        private readonly SyncVar<int> lifeState = new((int)SharedLifeState.Healthy);

        private IConfig config;
        private SharedLifeState observedState = SharedLifeState.Healthy;
        private float bleedOutRemaining;
        private int maxHearts = DefaultMaxHearts;

        public event Action<SharedLifeState> StateChanged;
        public event Action BecameDowned;
        public event Action Died;

        public int Hearts => hearts.value;
        public int MaxHearts => maxHearts;

        public SharedLifeState State => DecodeLifeState(lifeState.value);

        public bool CanAct => State is SharedLifeState.Healthy or SharedLifeState.Up;

        public float BleedOutRemaining => bleedOutRemaining;

        private IPlayerSide PlayerSide => playerSideSource as IPlayerSide;

        public void Configure(IConfig source)
        {
            config = source;
            maxHearts = Mathf.Max(GetConfiguredMaxHearts(), hearts.value);
        }

        private void OnEnable()
        {
            observedState = State;
            lifeState.onChanged += OnLifeStateChanged;
        }

        private void OnDisable()
        {
            lifeState.onChanged -= OnLifeStateChanged;
        }

        protected override void OnSpawned(bool asServer)
        {
            maxHearts = Mathf.Max(GetConfiguredMaxHearts(), hearts.value);

            if (!asServer)
                return;

            maxHearts = GetConfiguredMaxHearts();
            hearts.value = maxHearts;

            SharedLifeState initialState = maxHearts > 1
                ? SharedLifeState.Healthy
                : SharedLifeState.Downed;

            SetState(initialState);
        }

        private void Update()
        {
            if (!isServer || State != SharedLifeState.Downed)
                return;

            bleedOutRemaining -= Time.deltaTime;
            if (bleedOutRemaining <= 0f)
                Die();
        }

        public void ApplyHit(PlayerID attacker)
        {
            _ = attacker;

            if (!isServer || State == SharedLifeState.Dead)
                return;

            // Armor-first plugs in here in C7. C4 applies damage straight to hearts.
            if (hearts.value <= 1)
            {
                Die();
                return;
            }

            hearts.value = Mathf.Max(0, hearts.value - 1);
            if (hearts.value <= 1)
                EnterDowned();
        }

        public bool TryRecoverFromDowned()
        {
            if (!isServer || State != SharedLifeState.Downed || hearts.value != 1)
                return false;

            SetState(SharedLifeState.Up);
            return true;
        }

        [ContextMenu("Debug/Apply Test Hit (Server Only)")]
        private void DebugApplyTestHit()
        {
            if (isServer)
                ApplyHit(PlayerID.Server);
        }

        private void EnterDowned()
        {
            if (State == SharedLifeState.Dead)
                return;

            SetState(SharedLifeState.Downed);
        }

        private int GetConfiguredMaxHearts()
        {
            if (PlayerSide?.Side == Side.Defender)
                return Mathf.Max(1, config?.GetInt(ConfigKey.DefenderMaxHearts, DefaultDefenderMaxHearts) ?? DefaultDefenderMaxHearts);

            return Mathf.Max(1, config?.GetInt(ConfigKey.MaxHearts, DefaultMaxHearts) ?? DefaultMaxHearts);
        }

        private void Die()
        {
            hearts.value = 0;
            SetState(SharedLifeState.Dead);
        }

        private void SetState(SharedLifeState nextState)
        {
            lifeState.value = EncodeLifeState(nextState);
            bleedOutRemaining = nextState == SharedLifeState.Downed
                ? Mathf.Max(0f, config?.GetFloat(ConfigKey.BleedOutSec, DefaultBleedOutSec) ?? DefaultBleedOutSec)
                : 0f;
        }

        private void OnLifeStateChanged(int value)
        {
            SharedLifeState nextState = DecodeLifeState(value);
            SharedLifeState previousState = observedState;

            observedState = nextState;
            StateChanged?.Invoke(nextState);

            if (nextState == SharedLifeState.Downed && previousState != SharedLifeState.Downed)
                BecameDowned?.Invoke();

            if (nextState == SharedLifeState.Dead && previousState != SharedLifeState.Dead)
                Died?.Invoke();
        }

        private static int EncodeLifeState(SharedLifeState state)
        {
            return state switch
            {
                SharedLifeState.Downed => (int)SharedLifeState.Downed,
                SharedLifeState.Up => (int)SharedLifeState.Up,
                SharedLifeState.Dead => (int)SharedLifeState.Dead,
                _ => (int)SharedLifeState.Healthy
            };
        }

        private static SharedLifeState DecodeLifeState(int value)
        {
            return value switch
            {
                (int)SharedLifeState.Downed => SharedLifeState.Downed,
                (int)SharedLifeState.Up => SharedLifeState.Up,
                (int)SharedLifeState.Dead => SharedLifeState.Dead,
                _ => SharedLifeState.Healthy
            };
        }
    }
}
