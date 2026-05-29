using System;
using Garrison.Shared.Config;
using PurrNet;
using UnityEngine;

namespace Garrison.Shared.Round
{
    public sealed class RoundController : NetworkBehaviour
    {
        [SerializeField] private MonoBehaviour configSource;

        private readonly SyncVar<RoundState> state = new(RoundState.Lobby);
        private int appliedPlayerCount;

        public event Action Changed;

        public RoundState State => state.value;

        public int AppliedPlayerCount => appliedPlayerCount;

        private IConfig Config => configSource as IConfig;

        protected override void OnSpawned(bool asServer)
        {
            state.onChanged += OnStateChanged;

            if (asServer)
                state.value = RoundState.Lobby;

            RaiseChanged();
        }

        protected override void OnDespawned(bool asServer)
        {
            state.onChanged -= OnStateChanged;
        }

        public void StartRound()
        {
            if (!isServer || state.value != RoundState.Lobby)
                return;

            appliedPlayerCount = Config?.GetInt(ConfigKey.PlayerCount) ?? 0;
            state.value = RoundState.InRound;
            Debug.Log($"Round started with N={appliedPlayerCount}.");
        }

        public void ResetRound()
        {
            if (!isServer || state.value != RoundState.InRound)
                return;

            state.value = RoundState.Lobby;
            Debug.Log("Round reset. Config values were preserved.");
        }

        private void OnStateChanged(RoundState newState)
        {
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
