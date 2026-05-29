using System;
using UnityEngine;

namespace Garrison.Shared.Config
{
    [CreateAssetMenu(menuName = "Garrison/Config Defaults", fileName = "ConfigDefaults")]
    public sealed class ConfigDefaults : ScriptableObject
    {
        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public ReadOnlySpan<Entry> Entries => entries;

        [Serializable]
        public struct Entry
        {
            public ConfigKey key;
            public ConfigValue value;
        }
    }
}
