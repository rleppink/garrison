using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;

namespace Garrison.Shared.Config
{
    // Persistent scene service that hands the inspector-wired config source to
    // spawned client-side behaviours without those behaviours depending on a concrete
    // slice.
    //
    // Caveat: keep this as a narrow runtime-spawn bridge, not a general service
    // locator. Author-time references still belong in prefabs/scenes, and
    // server-authoritative systems should be configured by their owning server path.
    public sealed class ConfigConsumerBinder : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private MonoBehaviour configSource;

        private HierarchyFactory clientHierarchy;

        private IConfig Config => configSource as IConfig;

        private void OnEnable()
        {
            if (networkManager == null)
            {
                Debug.LogError("ConfigConsumerBinder has no NetworkManager wired; spawned config consumers will use fallbacks.");
                return;
            }

            if (configSource == null)
                Debug.LogError("ConfigConsumerBinder has no config source wired; spawned config consumers will use fallbacks.");
            else if (Config == null)
                Debug.LogError("ConfigConsumerBinder configSource must implement IConfig.");

            networkManager.onClientConnectionState += OnClientConnectionState;
            Subscribe();
        }

        private void OnDisable()
        {
            if (networkManager != null)
                networkManager.onClientConnectionState -= OnClientConnectionState;

            Unsubscribe();
        }

        private void OnClientConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
                Subscribe();
            else if (state == ConnectionState.Disconnected)
                Unsubscribe();
        }

        private void Subscribe()
        {
            if (clientHierarchy != null || networkManager == null)
                return;

            if (!networkManager.TryGetModule(out HierarchyFactory hierarchy, false))
                return;

            clientHierarchy = hierarchy;
            clientHierarchy.onIdentityAdded += OnIdentityAdded;
            clientHierarchy.onIdentityRemoved += OnIdentityRemoved;
        }

        private void Unsubscribe()
        {
            if (clientHierarchy == null)
                return;

            clientHierarchy.onIdentityAdded -= OnIdentityAdded;
            clientHierarchy.onIdentityRemoved -= OnIdentityRemoved;
            clientHierarchy = null;
        }

        private void OnIdentityAdded(NetworkIdentity identity)
        {
            BindConfigConsumers(identity, Config);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            BindConfigConsumers(identity, null);
        }

        private static void BindConfigConsumers(NetworkIdentity identity, IConfig config)
        {
            if (identity == null)
                return;

            MonoBehaviour[] behaviours = identity.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IConfigConsumer consumer)
                    consumer.Configure(config);
            }
        }
    }
}
