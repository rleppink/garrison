using System;
using PurrNet.Packing;

namespace Garrison.Shared.Config
{
    public enum ConfigValueType
    {
        Int = 0,
        Float = 1,
        Bool = 2
    }

    [Serializable]
    public struct ConfigValue : IPackedAuto
    {
        public ConfigValueType type;
        public int intValue;
        public float floatValue;
        public bool boolValue;

        public static ConfigValue Int(int value)
        {
            return new ConfigValue { type = ConfigValueType.Int, intValue = value };
        }

        public static ConfigValue Float(float value)
        {
            return new ConfigValue { type = ConfigValueType.Float, floatValue = value };
        }

        public static ConfigValue Bool(bool value)
        {
            return new ConfigValue { type = ConfigValueType.Bool, boolValue = value };
        }

        public int AsInt(int fallback = 0)
        {
            return type == ConfigValueType.Int ? intValue : fallback;
        }

        public float AsFloat(float fallback = 0f)
        {
            return type == ConfigValueType.Float ? floatValue : fallback;
        }

        public bool AsBool(bool fallback = false)
        {
            return type == ConfigValueType.Bool ? boolValue : fallback;
        }
    }
}
