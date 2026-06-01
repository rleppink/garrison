using Garrison.Shared.Config;
using Garrison.Shared.Round;
using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Garrison.Vision
{
    // Vision-owned round-start spawn for the throwaway C4 NPC. Kept separate from
    // PlayerSpawner so the Vision slice owns its own temporary scaffolding.
    public sealed class NpcSpawner : NetworkBehaviour
    {
        [SerializeField] private RoundController roundController;
        [SerializeField] private MonoBehaviour configSource;
        [SerializeField] private GameObject npcBodyPrefab;

        private NetworkIdentity spawnedNpc;

        private IConfig Config => configSource as IConfig;

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer || roundController == null)
                return;

            roundController.RoundStarted += OnRoundStarted;
            roundController.RoundReset += OnRoundReset;
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer || roundController == null)
                return;

            roundController.RoundStarted -= OnRoundStarted;
            roundController.RoundReset -= OnRoundReset;
        }

        private void OnRoundStarted(Scene mapScene)
        {
            if (!isServer || spawnedNpc != null || !mapScene.IsValid() || !mapScene.isLoaded || !npcBodyPrefab)
                return;

            NpcSpawnPoint spawnPoint = FindSpawnPoint(mapScene);
            if (spawnPoint == null)
            {
                Debug.LogWarning("NpcSpawner could not find an NpcSpawnPoint in the loaded map scene.", this);
                return;
            }

            GameObject npcObject = UnityProxy.InstantiateDirectly(npcBodyPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, mapScene);
            if (!npcObject || !npcObject.TryGetComponent(out NetworkIdentity identity))
            {
                if (npcObject)
                    UnityProxy.DestroyDirectly(npcObject);

                Debug.LogWarning("NpcSpawner failed to instantiate a networked NPC body.", this);
                return;
            }

            ConfigureConsumers(npcObject, Config);
            NetworkIdentity.Spawn(npcObject, npcBodyPrefab, networkManager);
            spawnedNpc = identity;
        }

        private void OnRoundReset()
        {
            if (!isServer)
                return;

            if (spawnedNpc)
                spawnedNpc.Despawn();

            spawnedNpc = null;
        }

        private static NpcSpawnPoint FindSpawnPoint(Scene mapScene)
        {
            GameObject[] roots = mapScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].TryGetComponent(out NpcSpawnPoint spawnPoint))
                    return spawnPoint;

                spawnPoint = roots[i].GetComponentInChildren<NpcSpawnPoint>(true);
                if (spawnPoint)
                    return spawnPoint;
            }

            return null;
        }

        private static void ConfigureConsumers(GameObject root, IConfig config)
        {
            if (!root)
                return;

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IConfigConsumer consumer)
                    consumer.Configure(config);
            }
        }
    }
}
