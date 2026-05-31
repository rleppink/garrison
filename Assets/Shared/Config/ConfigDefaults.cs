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
