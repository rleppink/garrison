using System;
using System.Collections;
using System.Collections.Generic;
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
    // uses for ScenesModule). Spawn and SyncVar/local-player-id ordering differs
    // between host and remote clients, so this registry keeps candidate views and
    // re-evaluates whenever a view says its local status may have changed.
    //
    // Caveat: consumer binding here is only for local-view presentation components on
    // the same spawned identity. It is deliberately not a general dependency-injection
    // layer for gameplay state.
    public sealed class LocalPlayerRegistry : MonoBehaviour, ILocalPlayerRegistry
    {
        [SerializeField] private NetworkManager networkManager;

        // The persistent Bootstrap Main Camera. Both this registry and the camera are
        // persistent Bootstrap objects, so they ARE inspector-wireable to each other.
        // The runtime-spawned local body can't be, so we hand the camera to it via
        // ILocalPlayerView.BindCamera when it becomes Current — letting the body's aim
        // raycast use the gameplay camera without any Find/Camera.main/Instance lookup.
        [SerializeField] private Camera gameplayCamera;

        private readonly List<ILocalPlayerView> candidates = new();

        private HierarchyFactory clientHierarchy;
        private HierarchyFactory serverHierarchy;
        private PlayersManager clientPlayers;
        private ILocalPlayerView current;
        private Coroutine refreshRoutine;

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
            Subscribe();
            ReevaluateCurrent();
            refreshRoutine = StartCoroutine(RefreshLocalViewLoop());
        }

        private void OnDisable()
        {
            if (networkManager != null)
                networkManager.onClientConnectionState -= OnClientConnectionState;

            Unsubscribe();
            SetCurrent(null);

            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        private IEnumerator RefreshLocalViewLoop()
        {
            while (enabled)
            {
                // Module creation and local-player-id receipt can lag connection
                // callbacks, especially in host mode.
                Subscribe();

                if (current == null || !IsLiveLocalView(current))
                    ReevaluateCurrent();

                yield return null;
            }
        }

        private void OnClientConnectionState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
            {
                Subscribe();
                ReevaluateCurrent();
            }
            else if (state == ConnectionState.Disconnected)
            {
                Unsubscribe();
                SetCurrent(null);
            }
        }

        private void Subscribe()
        {
            if (networkManager == null)
                return;

            // Client hierarchy is the normal path. In host mode, PurrNet can surface
            // the already-instantiated local body through the server hierarchy before
            // the client-side spawn path has the assigned-player SyncVar ready, so we
            // observe both and still only select views whose IsLocalView is true.
            SubscribeHierarchy(false, ref clientHierarchy);
            SubscribeHierarchy(true, ref serverHierarchy);
            SubscribeClientPlayers();
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
            UnsubscribeClientPlayers();

            for (int i = candidates.Count - 1; i >= 0; i--)
                RemoveCandidate(candidates[i]);
        }

        private void UnsubscribeHierarchy(ref HierarchyFactory hierarchy)
        {
            if (hierarchy == null)
                return;

            hierarchy.onIdentityAdded -= OnIdentityAdded;
            hierarchy.onIdentityRemoved -= OnIdentityRemoved;
            hierarchy = null;
        }

        private void SubscribeClientPlayers()
        {
            if (clientPlayers != null)
                return;

            if (!networkManager.TryGetModule(out PlayersManager players, false))
                return;

            clientPlayers = players;
            clientPlayers.onLocalPlayerReceivedID += OnLocalPlayerReceivedID;
        }

        private void UnsubscribeClientPlayers()
        {
            if (clientPlayers == null)
                return;

            clientPlayers.onLocalPlayerReceivedID -= OnLocalPlayerReceivedID;
            clientPlayers = null;
        }

        private void OnIdentityAdded(NetworkIdentity identity)
        {
            if (identity is ILocalPlayerView view)
            {
                AddCandidate(view);
                BindLocalViewConsumers(identity, view);
            }
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            if (identity is ILocalPlayerView view)
            {
                bool wasCurrent = ReferenceEquals(view, current);
                RemoveCandidate(view);

                if (wasCurrent)
                    SetCurrent(null);

                BindLocalViewConsumers(identity, null);
                ReevaluateCurrent();
            }
        }

        private static void BindLocalViewConsumers(NetworkIdentity identity, ILocalPlayerView view)
        {
            if (identity == null)
                return;

            MonoBehaviour source = view as MonoBehaviour;
            MonoBehaviour[] behaviours = identity.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILocalPlayerViewConsumer consumer)
                    consumer.BindLocalView(source);
            }
        }

        private void OnLocalPlayerReceivedID(PlayerID _)
        {
            ReevaluateCurrent();
        }

        private void OnCandidateLocalViewStatusChanged()
        {
            ReevaluateCurrent();
        }

        private void AddCandidate(ILocalPlayerView view)
        {
            if (candidates.Contains(view))
                return;

            candidates.Add(view);
            view.LocalViewStatusChanged += OnCandidateLocalViewStatusChanged;
            ReevaluateCurrent();
        }

        private void RemoveCandidate(ILocalPlayerView view)
        {
            if (!candidates.Remove(view))
                return;

            view.LocalViewStatusChanged -= OnCandidateLocalViewStatusChanged;
        }

        private void ReevaluateCurrent()
        {
            if (current != null && IsLiveLocalView(current))
                return;

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                ILocalPlayerView candidate = candidates[i];
                if (IsDestroyed(candidate))
                {
                    candidates.RemoveAt(i);
                    continue;
                }

                if (IsLiveLocalView(candidate))
                {
                    SetCurrent(candidate);
                    return;
                }
            }

            SetCurrent(null);
        }

        private static bool IsLiveLocalView(ILocalPlayerView view)
        {
            return view != null && !IsDestroyed(view) && view.ViewTarget != null && view.IsLocalView;
        }

        private static bool IsDestroyed(ILocalPlayerView view)
        {
            return view is UnityEngine.Object unityObject && unityObject == null;
        }

        private void SetCurrent(ILocalPlayerView view)
        {
            if (ReferenceEquals(view, current))
                return;

            // Release the camera from the outgoing view, hand it to the incoming one.
            if (!IsDestroyed(current))
                current?.BindCamera(null);

            current = view;

            if (!IsDestroyed(current))
                current?.BindCamera(gameplayCamera);

            CurrentChanged?.Invoke();
        }
    }
}
