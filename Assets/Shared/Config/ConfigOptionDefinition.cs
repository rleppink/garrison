using System.Collections.Generic;

namespace Garrison.Shared.Config
{
    public readonly struct ConfigOptionDefinition
    {
        public ConfigOptionDefinition(
            ConfigKey key,
            ConfigValueType type,
            string group,
            string label,
            float min = 0f,
            float max = 0f,
            bool hasRange = false
        )
        {
            Key = key;
            Type = type;
            Group = group;
            Label = label;
            Min = min;
            Max = max;
            HasRange = hasRange;
        }

        public ConfigKey Key { get; }
        public ConfigValueType Type { get; }
        public string Group { get; }
        public string Label { get; }
        public float Min { get; }
        public float Max { get; }
        public bool HasRange { get; }
    }

    public static class ConfigOptionDefinitions
    {
        public static IReadOnlyList<ConfigOptionDefinition> All { get; } =
            new[]
            {
                Int(ConfigKey.PlayerCount, "Lobby", "Player count", 1, 64),
                Float(ConfigKey.MoveSpeed, "Movement", "Walk speed", 0f),
                Float(ConfigKey.SprintSpeed, "Movement", "Sprint speed", 0f),
                Float(ConfigKey.BodyTurnSpeed, "Movement", "Body turn speed", 0f),
                Float(ConfigKey.ViewDistance, "Vision", "View distance", 0f),
                Float(ConfigKey.LosTickRate, "Vision", "LOS tick rate", 0.0001f),
                Float(ConfigKey.NpcConeArc, "Vision", "NPC cone arc", 1f, 179f),
                Float(ConfigKey.NpcConeRange, "Vision", "NPC cone range", 0f),
                Bool(ConfigKey.FogObserverWithholding, "Vision", "Fog observer withholding"),
                Float(ConfigKey.CameraZoom, "Camera", "Zoom", 0f),
                Float(ConfigKey.CameraPushExtent, "Camera", "Push extent", 0f),
                Int(ConfigKey.CameraPushShape, "Camera", "Push shape", 0),
                Float(ConfigKey.CameraPushHorizontalScale, "Camera", "Horizontal scale", 0f),
                Float(ConfigKey.CameraPushForwardScale, "Camera", "Forward scale", 0f),
                Float(ConfigKey.CameraPushBackwardScale, "Camera", "Backward scale", 0f),
                Float(
                    ConfigKey.CameraSafeViewportInset,
                    "Camera",
                    "Safe viewport inset",
                    0f,
                    0.45f
                ),
                Int(ConfigKey.CameraReturn, "Camera", "Return mode", 0),
                Float(ConfigKey.CameraReturnSpeed, "Camera", "Return speed", 0f),
                Int(ConfigKey.CameraPushCoupling, "Camera", "Push coupling", 0),
                Int(ConfigKey.DefenderSlot, "Combat", "Defender slot", 0),
                Float(ConfigKey.AimLineWidth, "Combat", "Aim line width", 0f),
                Float(ConfigKey.AimLineLength, "Combat", "Aim line length", 0f),
                Int(ConfigKey.MaxHearts, "Combat", "Attacker hearts", 1),
                Int(ConfigKey.DefenderMaxHearts, "Combat", "Defender hearts", 1),
                Float(ConfigKey.BleedOutSec, "Combat", "Bleed-out seconds", 0f),
                Float(ConfigKey.AccuracyIdleSpread, "Combat", "Idle spread", 0f),
                Float(ConfigKey.AccuracyMovingSpread, "Combat", "Moving spread", 0f),
                Float(ConfigKey.AccuracySprintSpread, "Combat", "Sprint spread", 0f),
                Int(ConfigKey.WeaponDamageHearts, "Combat", "Weapon damage", 1),
                Float(ConfigKey.WeaponBaseSpread, "Combat", "Weapon base spread", 0f),
                Float(ConfigKey.WeaponRange, "Combat", "Weapon range", 0f),
                Float(ConfigKey.WeaponFalloff, "Combat", "Weapon falloff", 0f),
                Float(ConfigKey.RecoilPerShot, "Combat", "Recoil per shot", 0f),
                Float(ConfigKey.RecoilMax, "Combat", "Recoil max", 0f),
                Float(ConfigKey.RecoilSettleTime, "Combat", "Recoil settle time", 0.0001f),
                Float(ConfigKey.SyretteReachRadius, "Combat", "Syrette reach", 0f),
            };

        private static ConfigOptionDefinition Int(
            ConfigKey key,
            string group,
            string label,
            int min,
            int max = int.MaxValue
        )
        {
            return new ConfigOptionDefinition(
                key,
                ConfigValueType.Int,
                group,
                label,
                min,
                max,
                true
            );
        }

        private static ConfigOptionDefinition Float(
            ConfigKey key,
            string group,
            string label,
            float min,
            float max = float.MaxValue
        )
        {
            return new ConfigOptionDefinition(
                key,
                ConfigValueType.Float,
                group,
                label,
                min,
                max,
                true
            );
        }

        private static ConfigOptionDefinition Bool(ConfigKey key, string group, string label)
        {
            return new ConfigOptionDefinition(key, ConfigValueType.Bool, group, label);
        }
    }
}
