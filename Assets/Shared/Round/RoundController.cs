using System;
using System.Collections;
using System.Collections.Generic;
using Garrison.Shared.Player;
using Garrison.Shared.Config;
using PurrNet;
using PurrNet.Modules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Garrison.Shared.Round
{
    public sealed class RoundController : NetworkBehaviour
    {
        [SerializeField] private MonoBehaviour configSource;
        [SerializeField] private string mapSceneName = "Greybox";
        [SerializeField] private GameObject playerCapsulePrefab;

        private readonly SyncVar<RoundState> state = new(RoundState.Lobby);
        private readonly Dictionary<PlayerID, PlayerCapsule> spawnedCapsules = new();
        private int appliedPlayerCount;
        private Coroutine spawnRoutine;

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
            spawnRoutine = StartCoroutine(LoadMapAndSpawnPlayers());
            Debug.Log($"Round started with N={appliedPlayerCount}.");
        }

        public void ResetRound()
        {
            if (!isServer || state.value != RoundState.InRound)
                return;

            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }

            DespawnCapsules();
            UnloadMapScene();
            state.value = RoundState.Lobby;
            Debug.Log("Round reset. Config values were preserved.");
        }

        private IEnumerator LoadMapAndSpawnPlayers()
        {
            if (!networkManager.TryGetModule<ScenesModule>(true, out ScenesModule scenes))
            {
                Debug.LogError("Cannot start round: PurrNet ScenesModule is unavailable.");
                yield break;
            }

            Scene mapScene = SceneManager.GetSceneByName(mapSceneName);
            if (!mapScene.isLoaded)
            {
                AsyncOperation loadOperation = scenes.LoadSceneAsync(mapSceneName, LoadSceneMode.Additive);
                if (loadOperation == null)
                    yield break;

                while (!loadOperation.isDone)
                    yield return null;

                mapScene = SceneManager.GetSceneByName(mapSceneName);
            }

            SpawnPlayers(mapScene);
            spawnRoutine = null;
        }

        private void SpawnPlayers(Scene mapScene)
        {
            if (!mapScene.IsValid() || !mapScene.isLoaded || !playerCapsulePrefab)
                return;

            SpawnPoints spawnPoints = FindSpawnPoints(mapScene);
            IReadOnlyList<PlayerID> players = networkManager.players;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerID player = players[i];
                if (spawnedCapsules.ContainsKey(player))
                    continue;

                Transform point = spawnPoints ? spawnPoints.GetPoint(i) : null;
                Vector3 position = point ? point.position : Vector3.zero;
                Quaternion rotation = point ? point.rotation : Quaternion.identity;

                GameObject capsuleObject = UnityProxy.Instantiate(playerCapsulePrefab, position, rotation, mapScene);
                if (!capsuleObject || !capsuleObject.TryGetComponent(out PlayerCapsule capsule))
                    continue;

                capsule.Assign(player);
                spawnedCapsules[player] = capsule;
            }
        }

        private static SpawnPoints FindSpawnPoints(Scene mapScene)
        {
            GameObject[] roots = mapScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].TryGetComponent(out SpawnPoints spawnPoints))
                    return spawnPoints;

                spawnPoints = roots[i].GetComponentInChildren<SpawnPoints>(true);
                if (spawnPoints)
                    return spawnPoints;
            }

            return null;
        }

        private void DespawnCapsules()
        {
            foreach (PlayerCapsule capsule in spawnedCapsules.Values)
            {
                if (capsule)
                    capsule.Despawn();
            }

            spawnedCapsules.Clear();
        }

        private void UnloadMapScene()
        {
            Scene mapScene = SceneManager.GetSceneByName(mapSceneName);
            if (!mapScene.isLoaded)
                return;

            if (networkManager.TryGetModule<ScenesModule>(true, out ScenesModule scenes))
                scenes.UnloadSceneAsync(mapScene);
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
