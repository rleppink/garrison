using System;
using System.Collections;
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

        private readonly SyncVar<RoundState> state = new(RoundState.Lobby);
        private int appliedPlayerCount;
        private Coroutine spawnRoutine;

        public event Action Changed;

        // Server-side round-phase seam. Slices (e.g. Player) subscribe to spawn/despawn
        // their own content without Shared depending on those slices.
        public event Action<Scene> RoundStarted;
        public event Action RoundReset;

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
            spawnRoutine = StartCoroutine(LoadMapAndStart());
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

            RoundReset?.Invoke();
            UnloadMapScene();
            state.value = RoundState.Lobby;
            Debug.Log("Round reset. Config values were preserved.");
        }

        private IEnumerator LoadMapAndStart()
        {
            if (!networkManager.TryGetModule<ScenesModule>(true, out ScenesModule scenes))
            {
                Debug.LogError("Cannot start round: PurrNet ScenesModule is unavailable.");
                yield break;
            }

            if (!networkManager.TryGetModule(true, out HierarchyFactory hierarchyFactory))
            {
                Debug.LogError("Cannot start round: PurrNet HierarchyFactory is unavailable.");
                yield break;
            }

            Scene mapScene = SceneManager.GetSceneByName(mapSceneName);
            if (mapScene.isLoaded && !hierarchyFactory.TryGetHierarchy(mapScene, out _))
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(mapScene);
                if (unloadOperation != null)
                {
                    while (!unloadOperation.isDone)
                        yield return null;
                }

                mapScene = SceneManager.GetSceneByName(mapSceneName);
            }

            if (!mapScene.isLoaded)
            {
                AsyncOperation loadOperation = scenes.LoadSceneAsync(mapSceneName, LoadSceneMode.Additive);
                if (loadOperation == null)
                    yield break;

                while (!loadOperation.isDone)
                    yield return null;

                mapScene = SceneManager.GetSceneByName(mapSceneName);
            }

            if (mapScene.IsValid() && mapScene.isLoaded)
            {
                yield return WaitForNetworkHierarchy(hierarchyFactory, mapScene);
                RoundStarted?.Invoke(mapScene);
            }

            spawnRoutine = null;
        }

        private IEnumerator WaitForNetworkHierarchy(HierarchyFactory hierarchyFactory, Scene mapScene)
        {
            while (mapScene.IsValid()
                && mapScene.isLoaded
                && !hierarchyFactory.TryGetHierarchy(mapScene, out _))
            {
                yield return null;
            }
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
