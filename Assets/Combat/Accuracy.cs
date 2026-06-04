using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Combat
{
    public sealed class Accuracy : MonoBehaviour, IConfigConsumer
    {
        private const float DefaultIdleSpreadDegrees = 0.5f;
        private const float DefaultMovingSpreadDegrees = 2f;
        private const float DefaultSprintSpreadDegrees = 5f;

        [SerializeField] private MonoBehaviour movementStateSource;

        private IConfig config;

        private IMovementState Movement => movementStateSource as IMovementState;

        public float CurrentMovementSpreadDegrees
        {
            get { return GetMovementSpreadDegrees(); }
        }

        public float GetCurrentSpreadDegrees(float weaponBaseSpreadDegrees = 0f)
        {
            return weaponBaseSpreadDegrees + GetMovementSpreadDegrees();
        }

        public void Configure(IConfig source)
        {
            config = source;
        }

        private float GetMovementSpreadDegrees()
        {
            return Movement?.State switch
            {
                MovementState.Sprinting => config?.GetFloat(ConfigKey.AccuracySprintSpread, DefaultSprintSpreadDegrees) ?? DefaultSprintSpreadDegrees,
                MovementState.Running => config?.GetFloat(ConfigKey.AccuracyMovingSpread, DefaultMovingSpreadDegrees) ?? DefaultMovingSpreadDegrees,
                _ => config?.GetFloat(ConfigKey.AccuracyIdleSpread, DefaultIdleSpreadDegrees) ?? DefaultIdleSpreadDegrees
            };
        }
    }
}
