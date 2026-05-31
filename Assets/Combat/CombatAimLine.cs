using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Combat
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class CombatAimLine : MonoBehaviour, IConfigConsumer, ILocalPlayerViewConsumer
    {
        private const float DefaultWidth = 0.025f;
        private const float DefaultLength = 7f;
        private const float ViewportMin = 0f;
        private const float ViewportMax = 1f;
        private const float ViewportEpsilon = 0.0001f;

        [SerializeField] private MonoBehaviour localViewSource;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color lineColor = new(0.5f, 0.82f, 0.96f, 0.8f);
        [SerializeField] private LayerMask collisionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        private IConfig config;
        private ILocalPlayerView localView;

        public void BindLocalView(MonoBehaviour source)
        {
            if (localView != null)
                localView.LocalViewStatusChanged -= OnLocalViewStatusChanged;

            localViewSource = source;
            localView = localViewSource as ILocalPlayerView;

            if (isActiveAndEnabled && localView != null)
                localView.LocalViewStatusChanged += OnLocalViewStatusChanged;

            UpdateVisibility();
        }

        public void Configure(IConfig source)
        {
            if (config == source)
                return;

            if (config != null)
                config.Changed -= OnConfigChanged;

            config = source;

            if (config != null)
                config.Changed += OnConfigChanged;

            ApplyConfig();
            UpdateVisibility();
        }

        private void Awake()
        {
            localView = localViewSource as ILocalPlayerView;
            ApplyRendererDefaults();
            ApplyConfig();
        }

        private void OnEnable()
        {
            if (localView != null)
                localView.LocalViewStatusChanged += OnLocalViewStatusChanged;

            if (config != null)
                config.Changed += OnConfigChanged;

            UpdateVisibility();
        }

        private void OnDisable()
        {
            if (localView != null)
                localView.LocalViewStatusChanged -= OnLocalViewStatusChanged;

            if (config != null)
                config.Changed -= OnConfigChanged;
        }

        private void LateUpdate()
        {
            if (!CanRender())
                return;

            Vector3 origin = muzzle.position;
            Vector2 planarDirection = localView.Aim != null ? localView.Aim.AimDirection : Vector2.zero;
            Vector3 direction = new(planarDirection.x, 0f, planarDirection.y);
            if (direction.sqrMagnitude <= ViewportEpsilon)
            {
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, origin);
                return;
            }

            direction.Normalize();

            float fallbackLength = config?.GetFloat(ConfigKey.AimLineLength, DefaultLength) ?? DefaultLength;
            float length = TryGetScreenEdgeLength(origin, direction, localView.ViewCamera, out float screenLength)
                ? screenLength
                : fallbackLength;

            length = ClampLengthToCollision(origin, direction, length);

            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + direction * length);
        }

        private float ClampLengthToCollision(Vector3 origin, Vector3 direction, float maxLength)
        {
            if (maxLength <= 0f)
                return 0f;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxLength, collisionMask, triggerInteraction))
                return hit.distance;

            return maxLength;
        }

        private static bool TryGetScreenEdgeLength(Vector3 origin, Vector3 direction, Camera camera, out float length)
        {
            length = 0f;

            if (camera == null || !camera.orthographic)
                return false;

            Vector3 viewportOrigin = camera.WorldToViewportPoint(origin);
            Vector3 viewportStep = camera.WorldToViewportPoint(origin + direction) - viewportOrigin;
            Vector2 viewportDelta = new(viewportStep.x, viewportStep.y);

            if (viewportDelta.sqrMagnitude <= ViewportEpsilon)
                return false;

            float candidate = float.PositiveInfinity;
            TryUseViewportBoundary(viewportOrigin.x, viewportDelta.x, ViewportMin, ref candidate);
            TryUseViewportBoundary(viewportOrigin.x, viewportDelta.x, ViewportMax, ref candidate);
            TryUseViewportBoundary(viewportOrigin.y, viewportDelta.y, ViewportMin, ref candidate);
            TryUseViewportBoundary(viewportOrigin.y, viewportDelta.y, ViewportMax, ref candidate);

            if (float.IsInfinity(candidate))
                return false;

            length = Mathf.Max(0f, candidate);
            return true;
        }

        private static void TryUseViewportBoundary(float origin, float delta, float boundary, ref float best)
        {
            if (Mathf.Abs(delta) <= ViewportEpsilon)
                return;

            float distance = (boundary - origin) / delta;
            if (distance > 0f && distance < best)
                best = distance;
        }

        private bool CanRender()
        {
            return lineRenderer != null
                && lineRenderer.enabled
                && muzzle != null
                && localView != null
                && localView.IsLocalView;
        }

        private void ApplyRendererDefaults()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            lineRenderer.textureMode = LineTextureMode.Stretch;
            lineRenderer.alignment = LineAlignment.View;
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }

        private void ApplyConfig()
        {
            if (lineRenderer == null)
                return;

            float width = config?.GetFloat(ConfigKey.AimLineWidth, DefaultWidth) ?? DefaultWidth;
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
            lineRenderer.widthMultiplier = 1f;
        }

        private void UpdateVisibility()
        {
            if (lineRenderer == null)
                return;

            bool visible = muzzle != null && localView != null && localView.IsLocalView;
            lineRenderer.enabled = visible;

            if (!visible)
            {
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.zero);
            }
        }

        private void OnConfigChanged()
        {
            ApplyConfig();
        }

        private void OnLocalViewStatusChanged()
        {
            UpdateVisibility();
        }
    }
}
