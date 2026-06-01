using System;
using System.Collections.Generic;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using Garrison.Shared.Vision;
using PurrNet;
using UnityEngine;

namespace Garrison.Vision
{
    // Server-only perception seam for the throwaway M3 NPC. M5 behaviour consumes the
    // shared result interface and replaces the sweep stub that currently drives facing.
    public sealed class NpcPerception : NetworkBehaviour, IConfigConsumer, IPerception, IServerVisibilitySink
    {
        [SerializeField] private NpcBody npcBody;

        private readonly HashSet<PlayerID> currentTargets = new();
        private readonly HashSet<PlayerID> nextTargets = new();
        private readonly List<PlayerID> removedTargets = new();
        private readonly List<ServerVisibility.TrackedPlayer> trackedPlayers = new();

        private const float DefaultLosTickRate = 10f;
        private const float DefaultConeArc = 70f;
        private const float DefaultConeRange = 8f;
        private const float MaxAccumulated = 0.25f;

        private IConfig config;
        private ServerVisibility serverVisibility;
        private float tickAccumulator;

        public event Action<PlayerID> TargetAcquired;

        public event Action<PlayerID> TargetLost;

        public IReadOnlyCollection<PlayerID> CurrentTargets => currentTargets;

        public void Configure(IConfig source)
        {
            config = source;
        }

        public void BindServerVisibility(ServerVisibility visibility)
        {
            if (ReferenceEquals(serverVisibility, visibility))
                return;

            serverVisibility = visibility;

            if (serverVisibility == null)
                ClearTargets();
        }

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer)
                return;

            tickAccumulator = 0f;
            EvaluatePerception();
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer)
                return;

            ClearTargets();
            serverVisibility = null;
            tickAccumulator = 0f;
        }

        private void Update()
        {
            if (!isServer)
                return;

            float losTickRate = Mathf.Max(0.0001f, config?.GetFloat(ConfigKey.LosTickRate, DefaultLosTickRate) ?? DefaultLosTickRate);
            float tickDelta = 1f / losTickRate;
            tickAccumulator = Mathf.Min(tickAccumulator + Time.deltaTime, MaxAccumulated);

            while (tickAccumulator >= tickDelta)
            {
                tickAccumulator -= tickDelta;
                EvaluatePerception();
            }
        }

        private void EvaluatePerception()
        {
            nextTargets.Clear();
            trackedPlayers.Clear();

            if (npcBody == null || serverVisibility == null)
            {
                ApplyPerceptionChanges();
                return;
            }

            Vector2 facing = ((IFacingSource)npcBody).Facing;
            if (facing.sqrMagnitude <= Mathf.Epsilon)
            {
                ApplyPerceptionChanges();
                return;
            }

            float range = Mathf.Max(0f, config?.GetFloat(ConfigKey.NpcConeRange, DefaultConeRange) ?? DefaultConeRange);
            float maxDistanceSquared = range * range;
            float halfArc = Mathf.Clamp(config?.GetFloat(ConfigKey.NpcConeArc, DefaultConeArc) ?? DefaultConeArc, 1f, 179f) * 0.5f;
            float minFacingDot = Mathf.Cos(halfArc * Mathf.Deg2Rad);
            Vector3 origin = npcBody.EyePosition;

            serverVisibility.GetTrackedPlayers(trackedPlayers);

            for (int i = 0; i < trackedPlayers.Count; i++)
            {
                ServerVisibility.TrackedPlayer trackedPlayer = trackedPlayers[i];
                if (trackedPlayer.VisionAgent == null)
                    continue;

                Vector3 toTarget = trackedPlayer.VisionAgent.EyePosition - origin;
                Vector2 planarToTarget = new(toTarget.x, toTarget.z);
                float planarDistanceSquared = planarToTarget.sqrMagnitude;
                if (planarDistanceSquared > maxDistanceSquared)
                    continue;

                if (planarDistanceSquared > Mathf.Epsilon)
                {
                    Vector2 targetDirection = planarToTarget / Mathf.Sqrt(planarDistanceSquared);
                    if (Vector2.Dot(facing, targetDirection) < minFacingDot)
                        continue;
                }

                if (!serverVisibility.HasLineOfSight(npcBody, trackedPlayer.VisionAgent))
                    continue;

                nextTargets.Add(trackedPlayer.PlayerId);
            }

            ApplyPerceptionChanges();
        }

        private void ApplyPerceptionChanges()
        {
            removedTargets.Clear();
            foreach (PlayerID player in currentTargets)
            {
                if (!nextTargets.Contains(player))
                    removedTargets.Add(player);
            }

            for (int i = 0; i < removedTargets.Count; i++)
            {
                PlayerID player = removedTargets[i];
                currentTargets.Remove(player);
                TargetLost?.Invoke(player);
            }

            foreach (PlayerID player in nextTargets)
            {
                if (!currentTargets.Add(player))
                    continue;

                TargetAcquired?.Invoke(player);
            }
        }

        private void ClearTargets()
        {
            if (currentTargets.Count == 0)
                return;

            removedTargets.Clear();
            removedTargets.AddRange(currentTargets);
            currentTargets.Clear();

            for (int i = 0; i < removedTargets.Count; i++)
                TargetLost?.Invoke(removedTargets[i]);
        }
    }
}
