using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;

namespace Garrison.Shared.Net
{
    // Base for persistent (Bootstrap) services that hand an inspector-wired dependency to
    // runtime-spawned networked identities. Runtime-spawned prefabs cannot serialize
    // references to Bootstrap scene services, so each spawned identity is bound here as it
    // enters the hierarchy and unbound as it leaves.
    //
    // Host-mode gotcha (the reason this lives in one place): on a listen server the local
    // body is surfaced through the SERVER hierarchy, while remote clients see it through
    // the CLIENT hierarchy. A binder that observes only the client hierarchy leaves the
    // host's own sinks unbound. We observe both and let the typed Bind be idempotent, so a
    // shared host identity arriving on both hierarchies simply binds twice harmlessly. This
    // mirrors LocalPlayerRegistry, which observes both for the same reason.
    //
    // TSink is the seam interface a spawned behaviour implements to receive the dependency.
    public abstract class SpawnedIdentityBinder<TSink> : MonoBehaviour where TSink : class
    {
        [SerializeField] private NetworkManager networkManager;

        private HierarchyFactory clientHierarchy;
        private HierarchyFactory serverHierarchy;

        protected NetworkManager NetworkManager => networkManager;

        // Subclass hook for validating its own inspector-wired payload source and logging a
        // misconfiguration. Runs once on enable, after the NetworkManager check.
        protected virtual void ValidateConfiguration()
        {
        }

        // Bind (bound: true) or unbind (bound: false) the dependency on a single matching
        // sink. Must be idempotent: the host can present the same identity on both
        // hierarchies, so this can be invoked more than once per sink per state.
        protected abstract void Bind(TSink sink, bool bound);

        protected virtual void OnEnable()
        {
            if (networkManager == null)
            {
                Debug.LogError($"{GetType().Name} has no NetworkManager wired; spawned sinks will not be bound.");
                return;
            }

            ValidateConfiguration();

            networkManager.onClientConnectionState += OnClientConnectionState;
            Subscribe();
        }

        protected virtual void OnDisable()
        {
            if (networkManager != null)
                networkManager.onClientConnectionState -= OnClientConnectionState;

            Unsubscribe();
        }

        private void Update()
        {
            // Module creation can lag the connection callback, especially in host mode, and
            // the server hierarchy can surface identities before then. Keep retrying until
            // both hierarchies are captured; Subscribe is guarded, so this idles cheaply
            // once attached and re-attaches after a reconnect.
            Subscribe();
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
            if (networkManager == null)
                return;

            SubscribeHierarchy(false, ref clientHierarchy);
            SubscribeHierarchy(true, ref serverHierarchy);
        }

        private void SubscribeHierarchy(bool asServer, ref HierarchyFactory hierarchy)
        {
            if (hierarchy != null)
                return;

            if (!networkManager.TryGetModule(out HierarchyFactory module, asServer))
                return;

            hierarchy = module;
            hierarchy.onIdentityAdded += OnIdentityAdded;
            hierarchy.onIdentityRemoved += OnIdentityRemoved;
        }

        private void Unsubscribe()
        {
            UnsubscribeHierarchy(ref clientHierarchy);
            UnsubscribeHierarchy(ref serverHierarchy);
        }

        private void UnsubscribeHierarchy(ref HierarchyFactory hierarchy)
        {
            if (hierarchy == null)
                return;

            hierarchy.onIdentityAdded -= OnIdentityAdded;
            hierarchy.onIdentityRemoved -= OnIdentityRemoved;
            hierarchy = null;
        }

        private void OnIdentityAdded(NetworkIdentity identity)
        {
            BindSinks(identity, true);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            BindSinks(identity, false);
        }

        private void BindSinks(NetworkIdentity identity, bool bound)
        {
            if (identity == null)
                return;

            MonoBehaviour[] behaviours = identity.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is TSink sink)
                    Bind(sink, bound);
            }
        }
    }
}
