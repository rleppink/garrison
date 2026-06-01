using PurrNet;
using UnityEngine;

namespace Garrison.Vision
{
    // Placeholder C4 tell-driver only. M5's real AI replaces this server-auth sweep.
    public sealed class NpcSweepDriver : NetworkBehaviour
    {
        [SerializeField, Min(0f)] private float sweepHalfAngle = 45f;
        [SerializeField, Min(0.1f)] private float sweepPeriodSeconds = 4f;

        private double spawnedAtTime;
        private float centerYaw;

        protected override void OnSpawned(bool asServer)
        {
            if (!asServer)
                return;

            centerYaw = transform.eulerAngles.y;
            spawnedAtTime = Time.timeAsDouble;
            ApplySweep();
        }

        private void Update()
        {
            if (!isServer)
                return;

            ApplySweep();
        }

        private void ApplySweep()
        {
            if (sweepPeriodSeconds <= 0f)
                return;

            float phase = (float)((Time.timeAsDouble - spawnedAtTime) / sweepPeriodSeconds * Mathf.PI * 2f);
            float yawOffset = Mathf.Sin(phase) * sweepHalfAngle;
            transform.rotation = Quaternion.Euler(0f, centerYaw + yawOffset, 0f);
        }
    }
}
