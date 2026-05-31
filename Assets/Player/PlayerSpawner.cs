using System.Collections.Generic;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using Garrison.Shared.Round;
using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Garrison.Player
{
    // Server-side owner of the player body. Subscribes to the round-phase seam on
    // RoundController and spawns/despawns one server-owned PlayerBody per connected
    // player. Spawn logic lifted out of RoundController so the Player slice owns it.
    public sealed class PlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private RoundController roundController;
        [SerializeField] private MonoBehaviour configSource;
        [SerializeField] private GameObject playerBodyPrefab;

        private readonly Dictionary<PlayerID, PlayerBody> spawnedBodies = new();

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
            if (!isServer || !mapScene.IsValid() || !mapScene.isLoaded || !playerBodyPrefab)
                return;

            SpawnPoints spawnPoints = FindSpawnPoints(mapScene);
            IReadOnlyList<PlayerID> players = networkManager.players;

            // Throwaway role picker (real one is M4): the player at DefenderSlot is the
            // lone armored defender, everyone else attacks. Same index the spawn points use.
            int defenderSlot = Config != null ? Config.GetInt(ConfigKey.DefenderSlot) : 0;

            for (int i = 0; i < players.Count; i++)
            {
                PlayerID player = players[i];
                if (spawnedBodies.ContainsKey(player))
                    continue;

                Transform point = spawnPoints ? spawnPoints.GetPoint(i) : null;
                Vector3 position = point ? point.position : Vector3.zero;
                Quaternion rotation = point ? point.rotation : Quaternion.identity;

                GameObject bodyObject = UnityProxy.InstantiateDirectly(playerBodyPrefab, position, rotation, mapScene);
                if (!bodyObject || !bodyObject.TryGetComponent(out PlayerBody body))
                {
                    if (bodyObject)
                        UnityProxy.DestroyDirectly(bodyObject);

                    continue;
                }

                // Assign before PurrNet spawn so assignedPlayer is part of the
                // initial network state. Spawning first can let multiple clients see
                // the same default owner and route local input to the same body.
                body.Assign(player);
                body.AssignSide(i == defenderSlot ? Side.Defender : Side.Attacker);
                if (bodyObject.TryGetComponent(out PlayerMovement movement))
                    movement.Configure(Config);

                NetworkIdentity.Spawn(bodyObject, playerBodyPrefab, networkManager);
                spawnedBodies[player] = body;
            }
        }

        private void OnRoundReset()
        {
            if (!isServer)
                return;

            foreach (PlayerBody body in spawnedBodies.Values)
            {
                if (body)
                    body.Despawn();
            }

            spawnedBodies.Clear();
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
    }
}
