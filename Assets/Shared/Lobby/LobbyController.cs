using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Garrison.Shared.Lobby
{
    public readonly struct LobbyPlayerView
    {
        public LobbyPlayerView(PlayerID playerId, string displayName, bool isHost)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            IsHost = isHost;
        }

        public PlayerID PlayerId { get; }
        public string DisplayName { get; }
        public bool IsHost { get; }
    }

    public sealed class LobbyController : NetworkBehaviour
    {
        private readonly SyncDictionary<PlayerID, string> players = new();
        private readonly SyncVar<PlayerID> hostPlayer = new(PlayerID.Server);

        public event Action LobbyChanged;

        public IReadOnlyList<LobbyPlayerView> PlayerViews
        {
            get
            {
                List<LobbyPlayerView> views = new(players.Count);
                PlayerID host = hostPlayer.value;

                foreach (KeyValuePair<PlayerID, string> player in players)
                    views.Add(new LobbyPlayerView(player.Key, player.Value, player.Key == host));

                views.Sort((left, right) => string.CompareOrdinal(left.PlayerId.ToString(), right.PlayerId.ToString()));
                return views;
            }
        }

        public bool IsLocalHost => localPlayer.HasValue && localPlayer.Value == hostPlayer.value;

        protected override void OnSpawned(bool asServer)
        {
            players.onChanged += OnPlayersChanged;
            hostPlayer.onChanged += OnHostChanged;

            if (asServer)
            {
                networkManager.onPlayerJoined += OnPlayerJoined;
                networkManager.onPlayerLeft += OnPlayerLeft;
                RebuildServerList();
            }

            RaiseLobbyChanged();
        }

        protected override void OnDespawned(bool asServer)
        {
            players.onChanged -= OnPlayersChanged;
            hostPlayer.onChanged -= OnHostChanged;

            if (asServer && networkManager)
            {
                networkManager.onPlayerJoined -= OnPlayerJoined;
                networkManager.onPlayerLeft -= OnPlayerLeft;
            }
        }

        public void StartRound()
        {
            if (!isServer)
                return;

            Debug.Log("Lobby StartRound requested. RoundController will take over in C6.");
        }

        private void RebuildServerList()
        {
            players.Clear();

            IReadOnlyList<PlayerID> connectedPlayers = networkManager.players;
            for (int i = 0; i < connectedPlayers.Count; i++)
                AddPlayer(connectedPlayers[i]);
        }

        private void OnPlayerJoined(PlayerID player, bool isReconnect, bool asServer)
        {
            if (!asServer)
                return;

            AddPlayer(player);
            Debug.Log($"Lobby player joined: {player}");
        }

        private void OnPlayerLeft(PlayerID player, bool asServer)
        {
            if (!asServer)
                return;

            players.Remove(player);
            Debug.Log($"Lobby player left: {player}");
        }

        private void AddPlayer(PlayerID player)
        {
            if (players.Count == 0)
                hostPlayer.value = player;

            players[player] = $"Player {player}";
        }

        private void OnPlayersChanged(SyncDictionaryChange<PlayerID, string> change)
        {
            RaiseLobbyChanged();
        }

        private void OnHostChanged(PlayerID newHost)
        {
            RaiseLobbyChanged();
        }

        private void RaiseLobbyChanged()
        {
            LobbyChanged?.Invoke();
        }
    }
}
