using System;
using System.Collections;
using System.Collections.Generic;
using Garrison.Shared.Audio;
using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Garrison.Combat
{
    public sealed class WeaponFire : NetworkBehaviour, IConfigConsumer, IAudioBusSink
    {
        private const float DefaultWeaponFireRate = 2.5f;
        private const int DefaultWeaponDamageHearts = 1;
        private const float DefaultWeaponBaseSpread = 0.35f;
        private const float DefaultWeaponRange = 20f;
        private const float DefaultWeaponFalloff = 20f;
        private const float DefaultTracerWidth = 0.075f;
        private const float DefaultMuzzleFlashWidth = 0.12f;
        private const float DefaultImpactFlashWidth = 0.1f;
        private const float TracerDuration = 0.06f;
        private const float MuzzleFlashDuration = 0.04f;
        private const float ImpactFlashDuration = 0.06f;
        private const float DirectionEpsilon = 0.0001f;
        private const float MuzzleFlashLength = 0.32f;
        private const float ImpactFlashLength = 0.18f;
        private const int MaxRaycastHits = 8;

        private static readonly Color TracerColor = new(1f, 0.88f, 0.52f, 0.95f);
        private static readonly Color MuzzleFlashColor = new(1f, 0.74f, 0.34f, 1f);
        private static readonly Color ImpactFlashColor = new(1f, 0.96f, 0.8f, 1f);
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[MaxRaycastHits];
        private static readonly Comparison<RaycastHit> HitDistanceComparison = static (left, right) => left.distance.CompareTo(right.distance);

        [Header("Seams")]
        [SerializeField] private MonoBehaviour assignedPlayerSource;
        [SerializeField] private MonoBehaviour facingSource;
        [SerializeField] private LifeState lifeState;
        [SerializeField] private Accuracy accuracy;
        [SerializeField] private Transform muzzle;

        [Header("Collision")]
        [SerializeField] private LayerMask hitMask = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Presentation")]
        [SerializeField] private LineRenderer tracerRenderer;
        [SerializeField] private LineRenderer muzzleFlashRenderer;
        [SerializeField] private LineRenderer impactFlashRenderer;
        [SerializeField] private AudioClip gunfireClip;

        private IConfig config;
        private IAudioBus audioBus;
        private Coroutine tracerRoutine;
        private Coroutine muzzleFlashRoutine;
        private Coroutine impactFlashRoutine;
        private float nextAllowedShotTime;

        private IAssignedPlayer AssignedPlayerSource => assignedPlayerSource as IAssignedPlayer;
        private IFacingSource FacingSource => facingSource as IFacingSource;

        public void Configure(IConfig source)
        {
            config = source;
        }

        public void BindAudioBus(IAudioBus bus)
        {
            audioBus = bus;
        }

        private void Awake()
        {
            ConfigureLineRenderer(tracerRenderer, DefaultTracerWidth, TracerColor);
            ConfigureLineRenderer(muzzleFlashRenderer, DefaultMuzzleFlashWidth, MuzzleFlashColor);
            ConfigureLineRenderer(impactFlashRenderer, DefaultImpactFlashWidth, ImpactFlashColor);
        }

        private void Update()
        {
            if (!CanReadLocalFireInput())
                return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            RequestFire();
        }

        [ServerRpc(channel: Channel.ReliableOrdered, requireOwnership: false)]
        private void RequestFire(RPCInfo info = default)
        {
            if (!isServer)
                return;

            IAssignedPlayer assignedPlayer = AssignedPlayerSource;
            if (assignedPlayer == null || info.sender != assignedPlayer.AssignedPlayer)
                return;

            if (lifeState == null || !lifeState.CanAct || muzzle == null)
                return;

            float fireRate = Mathf.Max(0.01f, config?.GetFloat(ConfigKey.WeaponFireRate, DefaultWeaponFireRate) ?? DefaultWeaponFireRate);
            if (Time.time < nextAllowedShotTime)
                return;

            nextAllowedShotTime = Time.time + (1f / fireRate);

            Vector2 planarFacing = FacingSource != null ? FacingSource.Facing : Vector2.zero;
            if (planarFacing.sqrMagnitude <= DirectionEpsilon)
                return;

            Vector3 origin = muzzle.position;
            Vector3 intendedDirection = new(planarFacing.x, 0f, planarFacing.y);
            Vector3 shotDirection = ApplySpread(intendedDirection, accuracy != null ? accuracy.GetCurrentSpreadDegrees(GetWeaponBaseSpread()) : GetWeaponBaseSpread());

            float weaponRange = Mathf.Max(0f, config?.GetFloat(ConfigKey.WeaponRange, DefaultWeaponRange) ?? DefaultWeaponRange);
            bool hitTarget = ResolveShot(origin, shotDirection, weaponRange, info.sender, out Vector3 endPoint);
            BroadcastShot(origin, endPoint, hitTarget);
        }

        [ObserversRpc(channel: Channel.ReliableOrdered)]
        private void BroadcastShot(Vector3 origin, Vector3 endPoint, bool hitTarget)
        {
            RenderShotFeedback(origin, endPoint, hitTarget);
        }

        private bool ResolveShot(Vector3 origin, Vector3 direction, float range, PlayerID attacker, out Vector3 endPoint)
        {
            if (range <= 0f)
            {
                endPoint = origin;
                return false;
            }

            int hitCount = Physics.RaycastNonAlloc(origin, direction, RaycastHits, range, hitMask, triggerInteraction);
            if (hitCount <= 0)
            {
                endPoint = origin + direction * range;
                return false;
            }

            Array.Sort(RaycastHits, 0, hitCount, Comparer<RaycastHit>.Create(HitDistanceComparison));

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = RaycastHits[i];
                if (hit.collider == null)
                    continue;

                LifeState targetLifeState = ResolveLifeState(hit.collider);
                if (targetLifeState == lifeState)
                    continue;

                endPoint = hit.point;

                if (targetLifeState == null)
                    return false;

                ApplyDamage(targetLifeState, attacker, hit.distance);
                return true;
            }

            endPoint = origin + direction * range;
            return false;
        }

        private void ApplyDamage(LifeState targetLifeState, PlayerID attacker, float hitDistance)
        {
            if (targetLifeState == null)
                return;

            int damageHearts = Mathf.Max(1, config?.GetInt(ConfigKey.WeaponDamageHearts, DefaultWeaponDamageHearts) ?? DefaultWeaponDamageHearts);
            float falloff = Mathf.Max(0f, config?.GetFloat(ConfigKey.WeaponFalloff, DefaultWeaponFalloff) ?? DefaultWeaponFalloff);

            if (falloff > 0f && damageHearts > 1)
                damageHearts = Mathf.Max(1, damageHearts - Mathf.FloorToInt(hitDistance / falloff));

            for (int i = 0; i < damageHearts; i++)
                targetLifeState.ApplyHit(attacker);
        }

        private void RenderShotFeedback(Vector3 origin, Vector3 endPoint, bool hitTarget)
        {
            Vector3 shotVector = endPoint - origin;
            Vector3 shotDirection = shotVector.sqrMagnitude > DirectionEpsilon ? shotVector.normalized : transform.forward;

            ShowLine(tracerRenderer, origin, endPoint, TracerDuration, ref tracerRoutine);
            ShowLine(muzzleFlashRenderer, origin, origin + shotDirection * MuzzleFlashLength, MuzzleFlashDuration, ref muzzleFlashRoutine);

            if (hitTarget)
            {
                Vector3 impactPerpendicular = Vector3.Cross(shotDirection, Vector3.up);
                if (impactPerpendicular.sqrMagnitude <= DirectionEpsilon)
                    impactPerpendicular = Vector3.right;

                impactPerpendicular.Normalize();
                Vector3 halfImpact = impactPerpendicular * (ImpactFlashLength * 0.5f);
                ShowLine(impactFlashRenderer, endPoint - halfImpact, endPoint + halfImpact, ImpactFlashDuration, ref impactFlashRoutine);
            }
            else
            {
                HideLine(impactFlashRenderer, ref impactFlashRoutine);
            }

            if (audioBus != null && gunfireClip != null)
                audioBus.Play(AudioChannel.Weapons, gunfireClip, origin);
        }

        private bool CanReadLocalFireInput()
        {
            if (!isClient || !localPlayer.HasValue)
                return false;

            IAssignedPlayer assignedPlayer = AssignedPlayerSource;
            if (assignedPlayer == null || assignedPlayer.AssignedPlayer != localPlayer.Value)
                return false;

            return lifeState == null || lifeState.CanAct;
        }

        private float GetWeaponBaseSpread()
        {
            return Mathf.Max(0f, config?.GetFloat(ConfigKey.WeaponBaseSpread, DefaultWeaponBaseSpread) ?? DefaultWeaponBaseSpread);
        }

        private static Vector3 ApplySpread(Vector3 direction, float spreadDegrees)
        {
            Vector3 normalized = direction.sqrMagnitude > DirectionEpsilon ? direction.normalized : Vector3.forward;
            if (spreadDegrees <= 0f)
                return normalized;

            float yawOffset = UnityEngine.Random.Range(-spreadDegrees, spreadDegrees);
            return Quaternion.AngleAxis(yawOffset, Vector3.up) * normalized;
        }

        private static LifeState ResolveLifeState(Collider collider)
        {
            if (collider == null)
                return null;

            if (collider.TryGetComponent(out LifeState targetLifeState))
                return targetLifeState;

            // Body-part colliders may live on child objects later; keep the fallback
            // tightly scoped to the hit collider's parent chain rather than a scene search.
            return collider.GetComponentInParent<LifeState>();
        }

        private void ShowLine(LineRenderer renderer, Vector3 start, Vector3 end, float duration, ref Coroutine routine)
        {
            if (renderer == null)
                return;

            renderer.enabled = true;
            renderer.positionCount = 2;
            renderer.SetPosition(0, start);
            renderer.SetPosition(1, end);

            if (routine != null)
                StopCoroutine(routine);

            routine = StartCoroutine(HideAfterDelay(renderer, duration));
        }

        private void HideLine(LineRenderer renderer, ref Coroutine routine)
        {
            if (renderer == null)
                return;

            renderer.enabled = false;
            renderer.positionCount = 2;
            renderer.SetPosition(0, Vector3.zero);
            renderer.SetPosition(1, Vector3.zero);

            if (routine == null)
                return;

            StopCoroutine(routine);
            routine = null;
        }

        private IEnumerator HideAfterDelay(LineRenderer renderer, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (renderer != null)
            {
                renderer.enabled = false;
                renderer.SetPosition(0, Vector3.zero);
                renderer.SetPosition(1, Vector3.zero);
            }
        }

        private static void ConfigureLineRenderer(LineRenderer renderer, float width, Color color)
        {
            if (renderer == null)
                return;

            renderer.enabled = false;
            renderer.positionCount = 2;
            renderer.useWorldSpace = true;
            renderer.loop = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.alignment = LineAlignment.View;
            renderer.widthMultiplier = 1f;
            renderer.startWidth = width;
            renderer.endWidth = width;
            renderer.startColor = color;
            renderer.endColor = color;
            renderer.SetPosition(0, Vector3.zero);
            renderer.SetPosition(1, Vector3.zero);
        }
    }
}
