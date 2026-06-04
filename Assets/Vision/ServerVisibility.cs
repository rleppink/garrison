using System;
using System.Collections.Generic;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using Garrison.Shared.Vision;
using PurrNet;
using PurrNet.Modules;
using UnityEngine;

namespace Garrison.Vision
{
    // Server-side LOS service. C2 computes per-viewer visible sets; C3 consumes
    // those sets to drive PurrNet observer withholding.
    public sealed class ServerVisibility : NetworkBehaviour
    {
        [SerializeField] private MonoBehaviour configSource;
        [SerializeField] private NetworkManager networkManagerSource;
        [SerializeField] private LayerMask obstacleMask = 1;

        private readonly List<TrackedAgent> trackedAgents = new();
        private readonly Dictionary<NetworkIdentity, TrackedAgent> trackedAgentsByIdentity = new();
        private readonly HashSet<PlayerID> connectedPlayers = new();
        private readonly List<PlayerID> staleHiddenPlayers = new();

        private const float DefaultViewDistance = 25f;
        private const float DefaultLosTickRate = 10f;
        private const float MaxAccumulated = 0.25f;

        private HierarchyFactory serverHierarchy;
        private float tickAccumulator;
        private bool visibilityDirty;

        public readonly struct TrackedPlayer
        {
            public TrackedPlayer(PlayerID playerId, IVisionAgent visionAgent)
            {
                PlayerId = playerId;
                VisionAgent = visionAgent;
            }

            public PlayerID PlayerId { get; }

            public IVisionAgent VisionAgent { get; }
        }

        private sealed class TrackedAgent
        {
            public TrackedAgent(NetworkIdentity identity, NetworkIdentity[] identities, Renderer[] renderers, IVisionAgent visionAgent, IAssignedPlayer assignedPlayer)
            {
                Identity = identity;
                Identities = identities;
                Renderers = renderers;
                VisionAgent = visionAgent;
                AssignedPlayer = assignedPlayer;
            }

            public NetworkIdentity Identity { get; }

            public NetworkIdentity[] Identities { get; }

            public Renderer[] Renderers { get; }

            public IVisionAgent VisionAgent { get; }

            public IAssignedPlayer AssignedPlayer { get; }

            public HashSet<NetworkIdentity> VisibleTargets { get; } = new();

            public HashSet<PlayerID> DesiredObservers { get; } = new();

            public HashSet<PlayerID> HiddenPlayers { get; } = new();
        }

        private IConfig Config => configSource as IConfig;

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer)
                return;

            if (networkManagerSource == null)
            {
                Debug.LogError("ServerVisibility has no NetworkManager wired; vision agents will not be tracked.", this);
                return;
            }

            networkManagerSource.onPlayerJoined += OnPlayerJoined;
            networkManagerSource.onPlayerLeft += OnPlayerLeft;
            SubscribeHierarchy(true);
            visibilityDirty = true;
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer)
                return;

            if (networkManagerSource != null)
            {
                networkManagerSource.onPlayerJoined -= OnPlayerJoined;
                networkManagerSource.onPlayerLeft -= OnPlayerLeft;
            }

            UnsubscribeHierarchy();
            ClearTrackedAgents();
        }

        private void Update()
        {
            if (!isServer)
                return;

            if (serverHierarchy == null && networkManagerSource != null)
                SubscribeHierarchy(false);

            float losTickRate = Mathf.Max(0.0001f, Config?.GetFloat(ConfigKey.LosTickRate, DefaultLosTickRate) ?? DefaultLosTickRate);
            float tickDelta = 1f / losTickRate;
            tickAccumulator = Mathf.Min(tickAccumulator + Time.deltaTime, MaxAccumulated);
            bool rebuiltThisFrame = false;
            while (tickAccumulator >= tickDelta)
            {
                tickAccumulator -= tickDelta;
                RebuildVisibleSets();
                ReconcileObservers();
                visibilityDirty = false;
                rebuiltThisFrame = true;
            }

            if (!rebuiltThisFrame && visibilityDirty)
            {
                RebuildVisibleSets();
                ReconcileObservers();
                visibilityDirty = false;
            }
        }

        public bool HasLineOfSight(IVisionAgent from, IVisionAgent to)
        {
            if (from == null || to == null)
                return false;

            return HasLineOfSight(from.EyePosition, to.EyePosition);
        }

        public bool IsVisibleToViewer(NetworkIdentity viewer, NetworkIdentity target)
        {
            return viewer != null
                && target != null
                && trackedAgentsByIdentity.TryGetValue(viewer, out TrackedAgent trackedViewer)
                && trackedViewer.VisibleTargets.Contains(target);
        }

        public bool TryGetVisibleTargets(NetworkIdentity viewer, out IReadOnlyCollection<NetworkIdentity> visibleTargets)
        {
            if (viewer != null && trackedAgentsByIdentity.TryGetValue(viewer, out TrackedAgent trackedViewer))
            {
                visibleTargets = trackedViewer.VisibleTargets;
                return true;
            }

            visibleTargets = null;
            return false;
        }

        public int GetTrackedPlayers(List<TrackedPlayer> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();

            for (int i = 0; i < trackedAgents.Count; i++)
            {
                TrackedAgent trackedAgent = trackedAgents[i];
                if (trackedAgent.AssignedPlayer == null || trackedAgent.VisionAgent == null)
                    continue;

                results.Add(new TrackedPlayer(trackedAgent.AssignedPlayer.AssignedPlayer, trackedAgent.VisionAgent));
            }

            return results.Count;
        }

        private void SubscribeHierarchy(bool logFailure)
        {
            if (serverHierarchy != null)
                return;

            if (!networkManagerSource.TryGetModule(out HierarchyFactory module, true))
            {
                if (logFailure)
                    Debug.LogError("ServerVisibility could not resolve the server HierarchyFactory; vision agents will not be tracked.", this);

                return;
            }

            serverHierarchy = module;
            serverHierarchy.onIdentityAdded += OnIdentityAdded;
            serverHierarchy.onIdentityRemoved += OnIdentityRemoved;
        }

        private void UnsubscribeHierarchy()
        {
            if (serverHierarchy == null)
                return;

            serverHierarchy.onIdentityAdded -= OnIdentityAdded;
            serverHierarchy.onIdentityRemoved -= OnIdentityRemoved;
            serverHierarchy = null;
        }

        private void OnIdentityAdded(NetworkIdentity identity)
        {
            if (identity is not IVisionAgent visionAgent || trackedAgentsByIdentity.ContainsKey(identity))
                return;

            NetworkIdentity[] identities = identity.GetComponents<NetworkIdentity>();
            Renderer[] renderers = identity.GetComponentsInChildren<Renderer>(true);
            TrackedAgent trackedAgent = new(identity, identities, renderers, visionAgent, identity as IAssignedPlayer);
            trackedAgents.Add(trackedAgent);
            for (int i = 0; i < identities.Length; i++)
            {
                if (identities[i] != null)
                    trackedAgentsByIdentity[identities[i]] = trackedAgent;
            }

            visibilityDirty = true;

            Debug.Log($"ServerVisibility tracked '{identity.name}' (player: {DescribeAssignedPlayer(trackedAgent.AssignedPlayer)}). Total agents: {trackedAgents.Count}.", identity);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            if (!trackedAgentsByIdentity.TryGetValue(identity, out TrackedAgent trackedAgent))
                return;

            for (int i = 0; i < trackedAgent.Identities.Length; i++)
            {
                if (trackedAgent.Identities[i] != null)
                    trackedAgentsByIdentity.Remove(trackedAgent.Identities[i]);
            }

            trackedAgents.Remove(trackedAgent);
            RemoveFromVisibleSets(trackedAgent.Identity);
            visibilityDirty = true;

            Debug.Log($"ServerVisibility untracked '{identity.name}' (player: {DescribeAssignedPlayer(trackedAgent.AssignedPlayer)}). Total agents: {trackedAgents.Count}.", identity);
        }

        private void ClearTrackedAgents()
        {
            for (int i = 0; i < trackedAgents.Count; i++)
            {
                TrackedAgent trackedAgent = trackedAgents[i];
                if (trackedAgent.Identity != null)
                {
                    foreach (PlayerID hiddenPlayer in trackedAgent.HiddenPlayers)
                    {
                        RemoveBlacklistPlayer(trackedAgent, hiddenPlayer);
                        SetHostPresentationVisible(trackedAgent, hiddenPlayer, true);
                    }
                }

                trackedAgent.VisibleTargets.Clear();
                trackedAgent.DesiredObservers.Clear();
                trackedAgent.HiddenPlayers.Clear();
            }

            trackedAgents.Clear();
            trackedAgentsByIdentity.Clear();
            connectedPlayers.Clear();
            staleHiddenPlayers.Clear();
            visibilityDirty = false;
        }

        private void RebuildVisibleSets()
        {
            float viewDistance = Mathf.Max(0f, Config?.GetFloat(ConfigKey.ViewDistance, DefaultViewDistance) ?? DefaultViewDistance);
            float maxDistanceSquared = viewDistance * viewDistance;

            for (int viewerIndex = 0; viewerIndex < trackedAgents.Count; viewerIndex++)
            {
                TrackedAgent viewer = trackedAgents[viewerIndex];
                if (viewer.Identity == null || viewer.VisionAgent == null)
                    continue;

                HashSet<NetworkIdentity> visibleTargets = viewer.VisibleTargets;
                visibleTargets.Clear();
                visibleTargets.Add(viewer.Identity);

                Vector3 viewerEyePosition = viewer.VisionAgent.EyePosition;

                for (int targetIndex = 0; targetIndex < trackedAgents.Count; targetIndex++)
                {
                    TrackedAgent target = trackedAgents[targetIndex];
                    if (target.Identity == null || target.VisionAgent == null || target.Identity == viewer.Identity)
                        continue;

                    Vector3 toTarget = target.VisionAgent.EyePosition - viewerEyePosition;
                    if (toTarget.sqrMagnitude > maxDistanceSquared)
                        continue;

                    if (HasLineOfSight(viewerEyePosition, target.VisionAgent.EyePosition))
                        visibleTargets.Add(target.Identity);
                }
            }
        }

        private void ReconcileObservers()
        {
            if (networkManagerSource == null)
                return;

            connectedPlayers.Clear();
            IReadOnlyList<PlayerID> players = networkManagerSource.players;
            for (int i = 0; i < players.Count; i++)
                connectedPlayers.Add(players[i]);

            for (int targetIndex = 0; targetIndex < trackedAgents.Count; targetIndex++)
            {
                TrackedAgent target = trackedAgents[targetIndex];
                target.DesiredObservers.Clear();

                if (target.AssignedPlayer != null)
                    target.DesiredObservers.Add(target.AssignedPlayer.AssignedPlayer);
            }

            for (int viewerIndex = 0; viewerIndex < trackedAgents.Count; viewerIndex++)
            {
                TrackedAgent viewer = trackedAgents[viewerIndex];
                if (viewer.AssignedPlayer == null)
                    continue;

                PlayerID viewerPlayer = viewer.AssignedPlayer.AssignedPlayer;

                // M9 spectator mode repoints this observer source from the body to the followed teammate.
                foreach (NetworkIdentity visibleTarget in viewer.VisibleTargets)
                {
                    if (visibleTarget != null && trackedAgentsByIdentity.TryGetValue(visibleTarget, out TrackedAgent trackedTarget))
                        trackedTarget.DesiredObservers.Add(viewerPlayer);
                }
            }

            for (int targetIndex = 0; targetIndex < trackedAgents.Count; targetIndex++)
                ReconcileTargetObservers(trackedAgents[targetIndex]);
        }

        private void ReconcileTargetObservers(TrackedAgent target)
        {
            if (target.Identity == null)
                return;

            foreach (PlayerID player in connectedPlayers)
            {
                bool shouldObserve = target.DesiredObservers.Contains(player);
                bool isHidden = target.HiddenPlayers.Contains(player);

                if (shouldObserve)
                {
                    if (!isHidden)
                        continue;

                    target.HiddenPlayers.Remove(player);
                    RemoveBlacklistPlayer(target, player);
                    target.Identity.EvaluateVisibility(player);
                    SetHostPresentationVisible(target, player, true);
                    Debug.Log($"ServerVisibility revealed '{target.Identity.name}' to {player}.", target.Identity);
                    continue;
                }

                if (isHidden)
                    continue;

                target.HiddenPlayers.Add(player);
                BlacklistPlayer(target, player);
                target.Identity.EvaluateVisibility(player);
                SetHostPresentationVisible(target, player, false);
                Debug.Log($"ServerVisibility hid '{target.Identity.name}' from {player}.", target.Identity);
            }

            staleHiddenPlayers.Clear();
            foreach (PlayerID hiddenPlayer in target.HiddenPlayers)
            {
                if (!connectedPlayers.Contains(hiddenPlayer))
                    staleHiddenPlayers.Add(hiddenPlayer);
            }

            for (int i = 0; i < staleHiddenPlayers.Count; i++)
            {
                PlayerID stalePlayer = staleHiddenPlayers[i];
                target.HiddenPlayers.Remove(stalePlayer);
                RemoveBlacklistPlayer(target, stalePlayer);
                SetHostPresentationVisible(target, stalePlayer, true);
            }
        }

        private void RemoveFromVisibleSets(NetworkIdentity identity)
        {
            for (int i = 0; i < trackedAgents.Count; i++)
                trackedAgents[i].VisibleTargets.Remove(identity);
        }

        private void OnPlayerJoined(PlayerID player, bool _, bool asServer)
        {
            if (asServer)
                visibilityDirty = true;
        }

        private void OnPlayerLeft(PlayerID player, bool asServer)
        {
            if (!asServer)
                return;

            ClearPlayerVisibilityState(player);
            visibilityDirty = true;
        }

        private void ClearPlayerVisibilityState(PlayerID player)
        {
            for (int i = 0; i < trackedAgents.Count; i++)
            {
                TrackedAgent trackedAgent = trackedAgents[i];
                trackedAgent.DesiredObservers.Remove(player);

                if (trackedAgent.HiddenPlayers.Remove(player) && trackedAgent.Identity != null)
                {
                    RemoveBlacklistPlayer(trackedAgent, player);
                    SetHostPresentationVisible(trackedAgent, player, true);
                }
            }
        }

        private bool HasLineOfSight(Vector3 fromEyePosition, Vector3 toEyePosition)
        {
            return !Physics.Linecast(fromEyePosition, toEyePosition, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private void SetHostPresentationVisible(TrackedAgent target, PlayerID player, bool visible)
        {
            if (networkManagerSource == null || !networkManagerSource.isHost || player != networkManagerSource.localPlayer)
                return;

            for (int i = 0; i < target.Renderers.Length; i++)
            {
                if (target.Renderers[i] != null)
                    target.Renderers[i].enabled = visible;
            }
        }

        private static void BlacklistPlayer(TrackedAgent target, PlayerID player)
        {
            for (int i = 0; i < target.Identities.Length; i++)
            {
                if (target.Identities[i] != null)
                    target.Identities[i].BlacklistPlayer(player);
            }
        }

        private static void RemoveBlacklistPlayer(TrackedAgent target, PlayerID player)
        {
            for (int i = 0; i < target.Identities.Length; i++)
            {
                if (target.Identities[i] != null)
                    target.Identities[i].RemoveBlacklistPlayer(player);
            }
        }

        private static string DescribeAssignedPlayer(IAssignedPlayer assignedPlayer)
        {
            return assignedPlayer != null ? assignedPlayer.AssignedPlayer.ToString() : "<none>";
        }
    }
}
