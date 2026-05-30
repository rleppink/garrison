using PurrNet;
using UnityEngine;

namespace Garrison.Shared.Config
{
    public sealed class ConfigService : NetworkBehaviour, IConfig
    {
        [SerializeField] private ConfigDefaults defaults;

        private readonly SyncDictionary<ConfigKey, ConfigValue> values = new();

        public event System.Action Changed;

        protected override void OnSpawned(bool asServer)
        {
            values.onChanged += OnValuesChanged;

            if (asServer && values.Count == 0)
                ApplyDefaults();

            RaiseChanged();
        }

        protected override void OnDespawned(bool asServer)
        {
            values.onChanged -= OnValuesChanged;
        }

        public int GetInt(ConfigKey key, int fallback = 0)
        {
            return values.TryGetValue(key, out ConfigValue value) ? value.AsInt(fallback) : fallback;
        }

        public float GetFloat(ConfigKey key, float fallback = 0f)
        {
            return values.TryGetValue(key, out ConfigValue value) ? value.AsFloat(fallback) : fallback;
        }

        public bool GetBool(ConfigKey key, bool fallback = false)
        {
            return values.TryGetValue(key, out ConfigValue value) ? value.AsBool(fallback) : fallback;
        }

        public void SetInt(ConfigKey key, int value)
        {
            if (isServer)
                values[key] = ConfigValue.Int(value);
        }

        public void SetFloat(ConfigKey key, float value)
        {
            if (isServer)
                values[key] = ConfigValue.Float(value);
        }

        public void SetBool(ConfigKey key, bool value)
        {
            if (isServer)
                values[key] = ConfigValue.Bool(value);
        }

        private void ApplyDefaults()
        {
            if (!defaults)
                return;

            foreach (var entry in defaults.Entries())
                values[entry.Key] = entry.Value;
        }

        private void OnValuesChanged(SyncDictionaryChange<ConfigKey, ConfigValue> change)
        {
            RaiseChanged();
        }

        private void RaiseChanged()
        {
            Changed?.Invoke();
        }
    }
}
