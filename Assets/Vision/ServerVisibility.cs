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

        private sealed class TrackedAgent
        {
            public TrackedAgent(NetworkIdentity identity, IVisionAgent visionAgent, IAssignedPlayer assignedPlayer)
            {
                Identity = identity;
                VisionAgent = visionAgent;
                AssignedPlayer = assignedPlayer;
            }

            public NetworkIdentity Identity { get; }

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

            TrackedAgent trackedAgent = new(identity, visionAgent, identity as IAssignedPlayer);
            trackedAgents.Add(trackedAgent);
            trackedAgentsByIdentity.Add(identity, trackedAgent);
            visibilityDirty = true;

            Debug.Log($"ServerVisibility tracked '{identity.name}' (player: {DescribeAssignedPlayer(trackedAgent.AssignedPlayer)}). Total agents: {trackedAgents.Count}.", identity);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            if (!trackedAgentsByIdentity.TryGetValue(identity, out TrackedAgent trackedAgent))
                return;

            trackedAgentsByIdentity.Remove(identity);
            trackedAgents.Remove(trackedAgent);
            RemoveFromVisibleSets(identity);
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
                        trackedAgent.Identity.RemoveBlacklistPlayer(hiddenPlayer);
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
                bool shouldObserve = target.DesiredObservers.Contains(player) || ShouldForceObservePlayer(player);
                bool isHidden = target.HiddenPlayers.Contains(player);

                if (shouldObserve)
                {
                    if (!isHidden)
                        continue;

                    target.HiddenPlayers.Remove(player);
                    target.Identity.RemoveBlacklistPlayer(player);
                    target.Identity.EvaluateVisibility(player);
                    continue;
                }

                if (isHidden)
                    continue;

                target.HiddenPlayers.Add(player);
                target.Identity.BlacklistPlayer(player);
                target.Identity.EvaluateVisibility(player);
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
                target.Identity.RemoveBlacklistPlayer(stalePlayer);
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
                    trackedAgent.Identity.RemoveBlacklistPlayer(player);
            }
        }

        private bool ShouldForceObservePlayer(PlayerID player)
        {
            return networkManagerSource != null
                && networkManagerSource.isHost
                && player == networkManagerSource.localPlayer;
        }

        private bool HasLineOfSight(Vector3 fromEyePosition, Vector3 toEyePosition)
        {
            return !Physics.Linecast(fromEyePosition, toEyePosition, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private static string DescribeAssignedPlayer(IAssignedPlayer assignedPlayer)
        {
            return assignedPlayer != null ? assignedPlayer.AssignedPlayer.ToString() : "<none>";
        }
    }
}
