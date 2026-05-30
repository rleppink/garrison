using System;
using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;

namespace Garrison.Shared.Player
{
    // Persistent (Bootstrap) local-presentation service. Local, NOT networked.
    //
    // The local body is a runtime-spawned networked object, so it cannot be
    // inspector-wired to this persistent service, and Find/Instance/static are
    // banned. Instead this registry observes PurrNet's manager-level
    // HierarchyFactory.onIdentityAdded/onIdentityRemoved (reached through the
    // inspector-wired NetworkManager, the same TryGetModule pattern RoundController
    // uses for ScenesModule). When a spawned identity reports itself as the local
    // view (ILocalPlayerView.IsLocalView, decided by the Player slice), it becomes
    // Current. The body's OnSpawned has already run by the time onIdentityAdded
    // fires, so its assigned-player SyncVar is populated and the local check is valid.
    public sealed class LocalPlayerRegistry : MonoBehaviour, ILocalPlayerRegistry
    {
        [SerializeField] private NetworkManager networkManager;

        private HierarchyFactory clientHierarchy;
        private ILocalPlayerView current;

        public ILocalPlayerView Current => current;

        public event Action CurrentChanged;

        private void OnEnable()
        {
            if (networkManager == null)
            {
                Debug.LogError("LocalPlayerRegistry has no NetworkManager wired; the camera will not follow.");
                return;
            }

            networkManager.onClientConnectionState += OnClientConnectionState;
        }

        private void OnDisable()
        {
            if (networkManager != null)
                networkManager.onClientConnectionState -= OnClientConnectionState;

            Unsubscribe();
            SetCurrent(null);
        }

        private void OnClientConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
                Subscribe();
            else if (state == ConnectionState.Disconnected)
            {
                Unsubscribe();
                SetCurrent(null);
            }
        }

        private void Subscribe()
        {
            if (clientHierarchy != null)
                return;

            // asServer:false — this is per-client local presentation; we watch the
            // client-side hierarchy so each client follows its own body.
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
            if (identity is ILocalPlayerView view && view.IsLocalView)
                SetCurrent(view);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            if (identity is ILocalPlayerView view && ReferenceEquals(view, current))
                SetCurrent(null);
        }

        private void SetCurrent(ILocalPlayerView view)
        {
            if (ReferenceEquals(view, current))
                return;

            current = view;
            CurrentChanged?.Invoke();
        }
    }
}
