using System.Collections.Generic;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Combat
{
    public sealed class DefenderArmor : NetworkBehaviour, IConfigConsumer
    {
        private const int DefaultMaxHearts = 3;
        private const float DefaultFocusFireWindowSec = 2f;
        private const int DefaultFocusFireThreshold = 2;
        private const int MaxSupportedHearts = 30;

        [SerializeField] private MonoBehaviour playerSideSource;

        // Primitive bitmask for replicated per-heart armor flags. Bit N mirrors heart N+1.
        private readonly SyncVar<int> armorMask = new(0);

        private readonly Dictionary<PlayerID, double> recentHitTimes = new();
        private readonly List<PlayerID> expiredAttackers = new();

        private IConfig config;

        private IPlayerSide PlayerSide => playerSideSource as IPlayerSide;

        public int ArmorMask => armorMask.value;

        public bool IsEnabled => PlayerSide?.Side == Side.Defender;

        public void Configure(IConfig source)
        {
            config = source;
        }

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer)
                return;

            recentHitTimes.Clear();
            expiredAttackers.Clear();
            armorMask.value = IsEnabled
                ? CreateInitialArmorMask(GetConfiguredMaxHearts())
                : 0;
        }

        protected override void OnDespawned(bool asServer)
        {
            if (!asServer)
                return;

            recentHitTimes.Clear();
            expiredAttackers.Clear();
        }

        public bool TryConsumeHeartHit(PlayerID attacker, int currentHearts)
        {
            if (!isServer || !IsEnabled || currentHearts <= 0)
                return true;

            RegisterRecentHit(attacker);

            int heartIndex = currentHearts - 1;
            if (!HasArmorAtHeartIndex(heartIndex))
                return true;

            SpendArmor(heartIndex);
            return GetRecentAttackerCount() >= GetFocusFireThreshold();
        }

        public bool HasArmorAtHeartIndex(int heartIndex)
        {
            if (heartIndex < 0 || heartIndex >= MaxSupportedHearts)
                return false;

            int mask = 1 << heartIndex;
            return (armorMask.value & mask) != 0;
        }

        private int GetConfiguredMaxHearts()
        {
            return Mathf.Clamp(
                config?.GetInt(ConfigKey.MaxHearts, DefaultMaxHearts) ?? DefaultMaxHearts,
                0,
                MaxSupportedHearts);
        }

        private int GetFocusFireThreshold()
        {
            return Mathf.Max(1, config?.GetInt(ConfigKey.FocusFireThreshold, DefaultFocusFireThreshold) ?? DefaultFocusFireThreshold);
        }

        private float GetFocusFireWindowSec()
        {
            return Mathf.Max(0f, config?.GetFloat(ConfigKey.FocusFireWindowSec, DefaultFocusFireWindowSec) ?? DefaultFocusFireWindowSec);
        }

        private void RegisterRecentHit(PlayerID attacker)
        {
            double now = Time.timeAsDouble;
            double cutoff = now - GetFocusFireWindowSec();

            expiredAttackers.Clear();
            foreach (KeyValuePair<PlayerID, double> hit in recentHitTimes)
            {
                if (hit.Value < cutoff)
                    expiredAttackers.Add(hit.Key);
            }

            for (int i = 0; i < expiredAttackers.Count; i++)
                recentHitTimes.Remove(expiredAttackers[i]);

            recentHitTimes[attacker] = now;
        }

        private int GetRecentAttackerCount()
        {
            return recentHitTimes.Count;
        }

        private void SpendArmor(int heartIndex)
        {
            if (heartIndex < 0 || heartIndex >= MaxSupportedHearts)
                return;

            armorMask.value &= ~(1 << heartIndex);
        }

        private static int CreateInitialArmorMask(int maxHearts)
        {
            if (maxHearts <= 0)
                return 0;

            return (1 << maxHearts) - 1;
        }
    }
}
