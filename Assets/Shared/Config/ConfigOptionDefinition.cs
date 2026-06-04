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
            string tooltip,
            float min = 0f,
            float max = 0f,
            bool hasRange = false
        )
        {
            Key = key;
            Type = type;
            Group = group;
            Label = label;
            Tooltip = tooltip;
            Min = min;
            Max = max;
            HasRange = hasRange;
        }

        public ConfigKey Key { get; }
        public ConfigValueType Type { get; }
        public string Group { get; }
        public string Label { get; }
        public string Tooltip { get; }
        public float Min { get; }
        public float Max { get; }
        public bool HasRange { get; }
    }

    public static class ConfigOptionDefinitions
    {
        public static IReadOnlyList<ConfigOptionDefinition> All { get; } =
            new[]
            {
                Int(ConfigKey.PlayerCount, "Lobby", "Player count",
                    "Target number of players for the match (1–64). Captured as N when the round " +
                    "starts. The Defender slot indexes into this player list to pick who defends; " +
                    "everyone else is an attacker.",
                    1, 64),
                Float(ConfigKey.MoveSpeed, "Movement", "Walk speed",
                    "Default ground speed (world units per second) when moving without sprinting.",
                    0f),
                Float(ConfigKey.SprintSpeed, "Movement", "Sprint speed",
                    "Movement speed (world units per second) while holding sprint. Faster travel, but " +
                    "the heaviest accuracy penalty applies — see Sprint spread.",
                    0f),
                Float(ConfigKey.BodyTurnSpeed, "Movement", "Body turn speed",
                    "How fast the body rotates to face your aim, in degrees per second. 0 (default) " +
                    "snaps instantly to the cursor; any positive value makes the body ease toward it, " +
                    "lagging behind fast flicks.",
                    0f),
                Float(ConfigKey.ViewDistance, "Vision", "View distance",
                    "Maximum range (world units) at which a player can see another agent. Past this " +
                    "distance, or with an obstacle blocking line of sight, the target is fogged and " +
                    "withheld from that player.",
                    0f),
                Float(ConfigKey.LosTickRate, "Vision", "LOS tick rate",
                    "How many times per second (Hz) the server recomputes line of sight and fog " +
                    "visibility. Higher reveals and hides players more responsively but costs more CPU; " +
                    "lower is cheaper but laggier.",
                    0.0001f),
                Float(ConfigKey.NpcConeArc, "Vision", "NPC cone arc",
                    "Full width of the NPC's vision cone, in degrees (1–179). A target must fall " +
                    "within this arc of where the NPC faces — and within cone range, with line of " +
                    "sight — to be spotted.",
                    1f, 179f),
                Float(ConfigKey.NpcConeRange, "Vision", "NPC cone range",
                    "How far (world units) the NPC's vision cone reaches. Targets beyond this distance " +
                    "are never spotted, even when inside the arc.",
                    0f),
                Float(ConfigKey.CameraZoom, "Camera", "Zoom",
                    "Camera zoom, given as the world half-height framed at the player (smaller = closer / " +
                    "more zoomed in). Also bounds how far the aim-push can shift the view.",
                    0f),
                Float(ConfigKey.CameraPushExtent, "Camera", "Push extent",
                    "Maximum distance (world units) the camera shifts from the body toward your cursor, " +
                    "letting you see further where you aim. 0 disables aim-push entirely.",
                    0f),
                Int(ConfigKey.CameraPushShape, "Camera", "Push shape",
                    "Shape of the aim-push reach: 0 = circle (equal in every direction), 1 = ellipse " +
                    "(scaled by Horizontal and Forward scale), 2 = asymmetric (also applies Backward " +
                    "scale when you aim back toward the camera).",
                    0),
                Float(ConfigKey.CameraPushHorizontalScale, "Camera", "Horizontal scale",
                    "Ellipse and asymmetric shapes only. Multiplies the push extent sideways (screen " +
                    "left/right), so the camera can lead further horizontally than vertically.",
                    0f),
                Float(ConfigKey.CameraPushForwardScale, "Camera", "Forward scale",
                    "Ellipse and asymmetric shapes only. Multiplies the push extent when aiming forward " +
                    "(screen-up, away from the camera).",
                    0f),
                Float(ConfigKey.CameraPushBackwardScale, "Camera", "Backward scale",
                    "Asymmetric shape only. Multiplies the push extent when aiming back toward the camera " +
                    "(screen-down) — usually kept smaller so you lead less behind yourself.",
                    0f),
                Float(
                    ConfigKey.CameraSafeViewportInset,
                    "Camera",
                    "Safe viewport inset",
                    "Keeps the body on screen: the push is clamped so the body never crosses this " +
                    "fraction of the viewport edge (0–0.45). Larger insets hold the body more " +
                    "central.",
                    0f,
                    0.45f
                ),
                Int(ConfigKey.CameraReturn, "Camera", "Return mode",
                    "How the camera recenters as your aim pulls back toward the body: 0 = snap (tracks " +
                    "the aim exactly and instantly), 1 = lazy follow (eases back at Return speed, while " +
                    "still snapping outward instantly).",
                    0),
                Float(ConfigKey.CameraReturnSpeed, "Camera", "Return speed",
                    "Lazy-follow mode only. How quickly the camera eases back toward the body when your " +
                    "aim retracts (higher = snappier). No effect in snap mode.",
                    0f),
                Int(ConfigKey.CameraPushCoupling, "Camera", "Push coupling",
                    "Reserved for tuning: 0 = push follows aim, 1 = push follows a separate push input. " +
                    "Only aim input exists today, so both settings currently behave identically.",
                    0),
                Int(ConfigKey.DefenderSlot, "Combat", "Defender slot",
                    "Index into the round's player list that becomes the Defender (0 = host / first " +
                    "player); everyone else attacks. Temporary role picker until the lobby handles roles.",
                    0),
                Float(ConfigKey.AimLineWidth, "Combat", "Aim line width",
                    "Thickness (world units) of the local aim line drawn from the muzzle. Purely " +
                    "cosmetic — it shows where you point and your current spread, not the exact bullet.",
                    0f),
                Float(ConfigKey.AimLineLength, "Combat", "Aim line length",
                    "Maximum length (world units) of the local aim line. Drawn far past the screen so it " +
                    "always reaches the edge; the camera view and obstacles trim it shorter. Cosmetic only.",
                    0f),
                Int(ConfigKey.MaxHearts, "Combat", "Attacker hearts",
                    "Starting and maximum hearts for attackers (min 1). Down to the last heart the player " +
                    "is downed rather than killed; losing that final heart kills them.",
                    1),
                Int(ConfigKey.DefenderMaxHearts, "Combat", "Defender hearts",
                    "Starting and maximum hearts for the defender (min 1). Same downed/killed rules as " +
                    "attackers, tuned separately.",
                    1),
                Float(ConfigKey.BleedOutSec, "Combat", "Bleed-out seconds",
                    "How long (seconds) a downed player has before dying if no one revives them with a " +
                    "syrette. The timer resets each time they go down.",
                    0f),
                Float(ConfigKey.AccuracyIdleSpread, "Combat", "Idle spread",
                    "Extra bullet spread (degrees) while standing still, added on top of the weapon's " +
                    "base spread. Default 0 — dead-on when stationary.",
                    0f),
                Float(ConfigKey.AccuracyMovingSpread, "Combat", "Moving spread",
                    "Extra bullet spread (degrees) while walking or running, added on top of base spread.",
                    0f),
                Float(ConfigKey.AccuracySprintSpread, "Combat", "Sprint spread",
                    "Extra bullet spread (degrees) while sprinting — the largest movement penalty, " +
                    "discouraging accurate fire on the run.",
                    0f),
                Int(ConfigKey.WeaponDamageHearts, "Combat", "Weapon damage",
                    "Hearts removed per hit at close range (min 1). Reduced with distance by Weapon falloff.",
                    1),
                Float(ConfigKey.WeaponBaseSpread, "Combat", "Weapon base spread",
                    "The weapon's inherent bullet spread (degrees), always applied. Movement and recoil " +
                    "spread stack on top of it.",
                    0f),
                Float(ConfigKey.WeaponRange, "Combat", "Weapon range",
                    "Maximum distance (world units) a shot travels and can deal damage. Beyond it the " +
                    "bullet hits nothing.",
                    0f),
                Float(ConfigKey.WeaponFalloff, "Combat", "Weapon falloff",
                    "Damage drop-off distance (world units): for every this many units to the target, the " +
                    "hit loses one heart of damage (never below 1). 0 disables falloff — full damage " +
                    "at any range.",
                    0f),
                Float(ConfigKey.RecoilPerShot, "Combat", "Recoil per shot",
                    "Degrees each shot adds to the recoil 'bloom' — the envelope a random aim kick is " +
                    "rolled within. Spraying stacks bloom toward Recoil max; spaced shots stay accurate.",
                    0f),
                Float(ConfigKey.RecoilMax, "Combat", "Recoil max",
                    "Upper limit (degrees) on the recoil bloom no matter how fast you spray. Caps the " +
                    "worst-case inaccuracy.",
                    0f),
                Float(ConfigKey.RecoilSettleTime, "Combat", "Recoil settle time",
                    "Time constant (seconds) for the recoil bloom to decay back toward zero between shots. " +
                    "Larger means recoil lingers longer; firing faster than this outruns the decay and " +
                    "blooms wide.",
                    0.0001f),
                Float(ConfigKey.SyretteReachRadius, "Combat", "Syrette reach",
                    "How close (world units) an attacker must be to a downed teammate to revive them with " +
                    "a syrette. 0 disables reviving others (you can still recover yourself).",
                    0f),
            };

        private static ConfigOptionDefinition Int(
            ConfigKey key,
            string group,
            string label,
            string tooltip,
            int min,
            int max = int.MaxValue
        )
        {
            return new ConfigOptionDefinition(
                key,
                ConfigValueType.Int,
                group,
                label,
                tooltip,
                min,
                max,
                true
            );
        }

        private static ConfigOptionDefinition Float(
            ConfigKey key,
            string group,
            string label,
            string tooltip,
            float min,
            float max = float.MaxValue
        )
        {
            return new ConfigOptionDefinition(
                key,
                ConfigValueType.Float,
                group,
                label,
                tooltip,
                min,
                max,
                true
            );
        }
    }
}
