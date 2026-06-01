using System.Collections.Generic;
using Garrison.Shared.Player;
using Garrison.Shared.Vision;
using PurrNet;
using PurrNet.Modules;
using UnityEngine;

namespace Garrison.Vision
{
    // Server-side scaffold for future LOS-driven observer withholding. C1 only
    // tracks vision-capable identities as they enter/leave the network hierarchy.
    public sealed class ServerVisibility : NetworkBehaviour
    {
        [SerializeField] private NetworkManager networkManagerSource;

        private readonly List<TrackedAgent> trackedAgents = new();
        private readonly Dictionary<NetworkIdentity, TrackedAgent> trackedAgentsByIdentity = new();

        private HierarchyFactory serverHierarchy;

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
        }

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer)
                return;

            if (networkManagerSource == null)
            {
                Debug.LogError("ServerVisibility has no NetworkManager wired; vision agents will not be tracked.", this);
                return;
            }

            SubscribeHierarchy(true);
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer)
                return;

            UnsubscribeHierarchy();
            ClearTrackedAgents();
        }

        private void Update()
        {
            if (isServer && serverHierarchy == null && networkManagerSource != null)
                SubscribeHierarchy(false);
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

            Debug.Log($"ServerVisibility tracked '{identity.name}' (player: {DescribeAssignedPlayer(trackedAgent.AssignedPlayer)}). Total agents: {trackedAgents.Count}.", identity);
        }

        private void OnIdentityRemoved(NetworkIdentity identity)
        {
            if (!trackedAgentsByIdentity.TryGetValue(identity, out TrackedAgent trackedAgent))
                return;

            trackedAgentsByIdentity.Remove(identity);
            trackedAgents.Remove(trackedAgent);

            Debug.Log($"ServerVisibility untracked '{identity.name}' (player: {DescribeAssignedPlayer(trackedAgent.AssignedPlayer)}). Total agents: {trackedAgents.Count}.", identity);
        }

        private void ClearTrackedAgents()
        {
            trackedAgents.Clear();
            trackedAgentsByIdentity.Clear();
        }

        private static string DescribeAssignedPlayer(IAssignedPlayer assignedPlayer)
        {
            return assignedPlayer != null ? assignedPlayer.AssignedPlayer.ToString() : "<none>";
        }
    }
}
