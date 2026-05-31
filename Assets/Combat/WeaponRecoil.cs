using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Combat
{
    // Server-authoritative weapon recoil. Every shot grows a bloom that re-rolls a
    // random yaw offset within it; the bloom (and offset) decay back to zero as the
    // player holds fire. The server owns this so a client cannot spray accurately —
    // PlayerMovement yaws the body by YawOffsetDegrees (synced to everyone) and the
    // shot follows that facing.
    //
    // The local client runs an independent oscillator with the same tuning, exposed as
    // LocalYawOffsetDegrees, purely so the aim line kicks the instant you click without
    // waiting on the server. It is cosmetic feedback, not the authority.
    public sealed class WeaponRecoil : NetworkBehaviour, IConfigConsumer, IAimRecoil
    {
        private const float DefaultPerShotDegrees = 6f;
        private const float DefaultMaxDegrees = 40f;
        private const float DefaultSettleSeconds = 0.4f;

        private IConfig config;
        private RecoilState serverRecoil;
        private RecoilState localRecoil;

        public float YawOffsetDegrees => serverRecoil.Offset;
        public float LocalYawOffsetDegrees => localRecoil.Offset;

        public void Configure(IConfig source)
        {
            config = source;
        }

        // Server: a shot landed. Grows the authoritative bloom and re-rolls the offset
        // the body yaws to (and the bullet follows on the next movement tick).
        public void Kick()
        {
            if (isServer)
                serverRecoil.Kick(PerShotDegrees, MaxDegrees);
        }

        // Local owner clicked. Mirrors the kick for the aim line only.
        public void KickLocal()
        {
            localRecoil.Kick(PerShotDegrees, MaxDegrees);
        }

        private void Update()
        {
            // Exponential decay with SettleSeconds as the time constant. Crucially this is
            // frame-rate independent AND yields a fire-rate-dependent equilibrium: faster
            // spam outruns the decay so bloom climbs toward Max, while spaced taps decay
            // back to ~zero between shots and stay accurate. A linear "shed N deg/sec"
            // decay can't do both — it either resets between clicks or never recovers.
            float retained = Mathf.Exp(-Time.deltaTime / SettleSeconds);

            if (isServer)
                serverRecoil.Decay(retained);

            if (isClient)
                localRecoil.Decay(retained);
        }

        private float PerShotDegrees => Mathf.Max(0f, config?.GetFloat(ConfigKey.RecoilPerShot, DefaultPerShotDegrees) ?? DefaultPerShotDegrees);
        private float MaxDegrees => Mathf.Max(0f, config?.GetFloat(ConfigKey.RecoilMax, DefaultMaxDegrees) ?? DefaultMaxDegrees);
        private float SettleSeconds => Mathf.Max(0.0001f, config?.GetFloat(ConfigKey.RecoilSettleTime, DefaultSettleSeconds) ?? DefaultSettleSeconds);

        // Bloom is the envelope (how wide a kick can be, grows with spam); offset is the
        // current signed kick within it. Both decay toward zero between shots.
        private struct RecoilState
        {
            private const float SnapEpsilon = 0.01f;

            private float bloom;
            private float offset;

            public readonly float Offset => offset;

            public void Kick(float perShotDegrees, float maxDegrees)
            {
                // Roll this shot's offset from the bloom accumulated SO FAR, then grow it
                // for the shots that follow. A rested shot fires from ~zero bloom (dead on
                // target); only sustained fire that hasn't decayed yet bites into accuracy.
                offset = Random.Range(-bloom, bloom);
                bloom = Mathf.Min(bloom + perShotDegrees, maxDegrees);
            }

            public void Decay(float retained)
            {
                bloom *= retained;
                offset *= retained;

                // Exponential decay never reaches zero; snap small residuals so the aim
                // line and body settle cleanly back onto the cursor.
                if (bloom < SnapEpsilon)
                    bloom = 0f;
                if (Mathf.Abs(offset) < SnapEpsilon)
                    offset = 0f;
            }
        }
    }
}
