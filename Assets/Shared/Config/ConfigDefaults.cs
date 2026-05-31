using System.Collections.Generic;
using UnityEngine;

namespace Garrison.Shared.Config
{
    [CreateAssetMenu(menuName = "Garrison/Config Defaults", fileName = "ConfigDefaults")]
    public sealed class ConfigDefaults : ScriptableObject
    {
        [Header("Lobby")]
        [SerializeField, Range(1, 64)] private int playerCount = 6;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 5.8f;
        [SerializeField] private float bodyTurnSpeed;

        [Header("Combat")]
        // Index into the round's player list that gets Side.Defender (0 = host/first
        // player); everyone else is an attacker. Throwaway role picker until M4's lobby.
        [SerializeField] private int defenderSlot;
        [SerializeField] private float aimLineWidth = 0.025f;
        // Max aim-line reach. Far past the screen so it always reaches the edge at any
        // zoom; the camera frustum clips it and collision/blockers trim it shorter.
        [SerializeField] private float aimLineLength = 1000f;
        [SerializeField, Min(1)] private int maxHearts = 3;
        [SerializeField, Min(0f)] private float bleedOutSec = 12f;
        [SerializeField, Min(0f)] private float accuracyIdleSpread;
        [SerializeField, Min(0f)] private float accuracyMovingSpread = 2f;
        [SerializeField, Min(0f)] private float accuracySprintSpread = 5f;
        [SerializeField, Min(0.01f)] private float weaponFireRate = 2.5f;
        [SerializeField, Min(1)] private int weaponDamageHearts = 1;
        [SerializeField, Min(0f)] private float weaponBaseSpread = 0.35f;
        [SerializeField, Min(0f)] private float weaponRange = 60f;
        [SerializeField, Min(0f)] private float weaponFalloff = 20f;

        [Header("Camera")]
        [SerializeField] private float cameraZoom = 10f;
        [SerializeField] private float cameraPushExtent = 8f;
        [SerializeField] private int cameraPushShape;
        [SerializeField] private float cameraPushHorizontalScale = 1f;
        [SerializeField] private float cameraPushForwardScale = 1f;
        [SerializeField] private float cameraPushBackwardScale = 0.7f;
        [SerializeField, Range(0f, 0.45f)] private float cameraSafeViewportInset = 0.05f;
        [SerializeField] private int cameraReturn;
        [SerializeField] private float cameraReturnSpeed = 8f;
        [SerializeField] private int cameraPushCoupling;

        /// <summary>
        /// Maps the typed authoring fields onto the neutral key/value pairs that seed
        /// <see cref="ConfigService"/>'s runtime dictionary.
        /// </summary>
        public IEnumerable<KeyValuePair<ConfigKey, ConfigValue>> Entries()
        {
            yield return Pair(ConfigKey.PlayerCount, ConfigValue.Int(playerCount));
            yield return Pair(ConfigKey.MoveSpeed, ConfigValue.Float(moveSpeed));
            yield return Pair(ConfigKey.SprintSpeed, ConfigValue.Float(sprintSpeed));
            yield return Pair(ConfigKey.BodyTurnSpeed, ConfigValue.Float(bodyTurnSpeed));
            yield return Pair(ConfigKey.DefenderSlot, ConfigValue.Int(defenderSlot));
            yield return Pair(ConfigKey.AimLineWidth, ConfigValue.Float(aimLineWidth));
            yield return Pair(ConfigKey.AimLineLength, ConfigValue.Float(aimLineLength));
            yield return Pair(ConfigKey.MaxHearts, ConfigValue.Int(maxHearts));
            yield return Pair(ConfigKey.BleedOutSec, ConfigValue.Float(bleedOutSec));
            yield return Pair(ConfigKey.AccuracyIdleSpread, ConfigValue.Float(accuracyIdleSpread));
            yield return Pair(ConfigKey.AccuracyMovingSpread, ConfigValue.Float(accuracyMovingSpread));
            yield return Pair(ConfigKey.AccuracySprintSpread, ConfigValue.Float(accuracySprintSpread));
            yield return Pair(ConfigKey.WeaponFireRate, ConfigValue.Float(weaponFireRate));
            yield return Pair(ConfigKey.WeaponDamageHearts, ConfigValue.Int(weaponDamageHearts));
            yield return Pair(ConfigKey.WeaponBaseSpread, ConfigValue.Float(weaponBaseSpread));
            yield return Pair(ConfigKey.WeaponRange, ConfigValue.Float(weaponRange));
            yield return Pair(ConfigKey.WeaponFalloff, ConfigValue.Float(weaponFalloff));
            yield return Pair(ConfigKey.CameraZoom, ConfigValue.Float(cameraZoom));
            yield return Pair(ConfigKey.CameraPushExtent, ConfigValue.Float(cameraPushExtent));
            yield return Pair(ConfigKey.CameraPushShape, ConfigValue.Int(cameraPushShape));
            yield return Pair(ConfigKey.CameraPushHorizontalScale, ConfigValue.Float(cameraPushHorizontalScale));
            yield return Pair(ConfigKey.CameraPushForwardScale, ConfigValue.Float(cameraPushForwardScale));
            yield return Pair(ConfigKey.CameraPushBackwardScale, ConfigValue.Float(cameraPushBackwardScale));
            yield return Pair(ConfigKey.CameraSafeViewportInset, ConfigValue.Float(cameraSafeViewportInset));
            yield return Pair(ConfigKey.CameraReturn, ConfigValue.Int(cameraReturn));
            yield return Pair(ConfigKey.CameraReturnSpeed, ConfigValue.Float(cameraReturnSpeed));
            yield return Pair(ConfigKey.CameraPushCoupling, ConfigValue.Int(cameraPushCoupling));
        }

        private static KeyValuePair<ConfigKey, ConfigValue> Pair(ConfigKey key, ConfigValue value)
        {
            return new KeyValuePair<ConfigKey, ConfigValue>(key, value);
        }
    }
}
