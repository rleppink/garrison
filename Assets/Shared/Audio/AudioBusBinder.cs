using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;

namespace Garrison.Shared.Audio
{
    // Persistent scene service that hands the inspector-wired audio bus to spawned
    // network identities that need local presentation audio. Runtime-spawned prefabs
    // cannot safely serialize references to Bootstrap scene services.
    public sealed class AudioBusBinder : MonoBehaviour
    {
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private MonoBehaviour audioBusSource;

        private HierarchyFactory clientHierarchy;

        private IAudioBus AudioBus => audioBusSource as IAudioBus;

        private void OnEnable()
        {
            if (networkManager == null)
            {
                Debug.LogError("AudioBusBinder has no NetworkManager wired; spawned audio sinks will be silent.");
                return;
            }

            if (audioBusSource == null)
                Debug.LogError("AudioBusBinder has no AudioBus source wired; spawned audio sinks will be silent.");
            else if (AudioBus == null)
                Debug.LogError("AudioBusBinder audioBusSource must implement IAudioBus.");

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
            if (identity is IAudioBusSink sink)
                sink.BindAudioBus(AudioBus);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            if (identity is IAudioBusSink sink)
                sink.BindAudioBus(null);
        }
    }
}
