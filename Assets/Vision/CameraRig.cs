using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Vision
{
    // Local presentation only — NOT networked. Drives the persistent Bootstrap
    // Main Camera to frame the local player's body from a fixed top-down/iso angle,
    // orthographic, with zoom and aim-push driven by config. It reads the body only
    // through the Shared ILocalPlayerRegistry seam, so Vision never touches the
    // Player slice. The push is clamped after all shaping so the body stays inside
    // the safe viewport inset.
    public sealed class CameraRig : MonoBehaviour
    {
        private const int PushShapeCircle = 0;
        private const int PushShapeEllipse = 1;
        private const int PushShapeAsymmetric = 2;

        [SerializeField] private Camera targetCamera;

        // Wired from the persistent Bootstrap systems object (cast to ILocalPlayerRegistry).
        [SerializeField] private MonoBehaviour localPlayerRegistrySource;

        // Wired from ConfigService (cast to IConfig), same pattern as RoundController.
        [SerializeField] private MonoBehaviour configSource;

        // Authored camera pose. Runtime zoom and push feel are config-driven.
        [SerializeField] private Vector3 viewDirection = new(0f, -1f, -0.5f);
        [SerializeField] private float distance = 20f;

        // Fallbacks used until config delivers values (e.g. before the client connects).
        [SerializeField] private float defaultZoom = 10f;
        [SerializeField] private float defaultPushExtent = 3.5f;
        [SerializeField] private int defaultPushShape = PushShapeCircle;
        [SerializeField] private float defaultPushHorizontalScale = 1f;
        [SerializeField] private float defaultPushForwardScale = 1f;
        [SerializeField] private float defaultPushBackwardScale = 0.7f;
        [SerializeField, Range(0f, 0.45f)] private float defaultSafeViewportInset = 0.18f;

        private ILocalPlayerRegistry registry;
        private IConfig config;
        private float zoom;
        private float pushExtent;
        private int pushShape;
        private float pushHorizontalScale;
        private float pushForwardScale;
        private float pushBackwardScale;
        private float safeViewportInset;

        private ILocalPlayerRegistry Registry => registry ??= localPlayerRegistrySource as ILocalPlayerRegistry;
        private IConfig Config => config ??= configSource as IConfig;

        private void OnEnable()
        {
            ReadConfig();

            if (Config != null)
                Config.Changed += OnConfigChanged;

            ApplyProjection();
        }

        private void OnDisable()
        {
            if (config != null)
                config.Changed -= OnConfigChanged;
        }

        private void OnConfigChanged()
        {
            ReadConfig();
            ApplyProjection();
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
                return;

            ILocalPlayerView view = Registry?.Current;
            if (view == null)
                return;

            Transform target = view.ViewTarget;
            if (target == null)
                return;

            ApplyProjection();

            Vector3 backwards = viewDirection.sqrMagnitude > 0f ? -viewDirection.normalized : Vector3.up;
            Quaternion cameraRotation = Quaternion.LookRotation(-backwards, Vector3.up);
            Vector3 desiredPush = ComputeAimPush(view, cameraRotation);
            Vector3 clampedPush = ClampPushToSafeViewport(desiredPush, cameraRotation);
            Vector3 frameTarget = target.position + clampedPush;

            Transform camTransform = targetCamera.transform;
            camTransform.position = frameTarget + backwards * distance;
            camTransform.rotation = cameraRotation;
        }

        private void ReadConfig()
        {
            IConfig activeConfig = Config;
            zoom = activeConfig?.GetFloat(ConfigKey.CameraZoom, defaultZoom) ?? defaultZoom;
            pushExtent = Mathf.Max(0f, activeConfig?.GetFloat(ConfigKey.CameraPushExtent, defaultPushExtent) ?? defaultPushExtent);
            pushShape = Mathf.Clamp(activeConfig?.GetInt(ConfigKey.CameraPushShape, defaultPushShape) ?? defaultPushShape, PushShapeCircle, PushShapeAsymmetric);
            pushHorizontalScale = Mathf.Max(0.01f, activeConfig?.GetFloat(ConfigKey.CameraPushHorizontalScale, defaultPushHorizontalScale) ?? defaultPushHorizontalScale);
            pushForwardScale = Mathf.Max(0.01f, activeConfig?.GetFloat(ConfigKey.CameraPushForwardScale, defaultPushForwardScale) ?? defaultPushForwardScale);
            pushBackwardScale = Mathf.Max(0.01f, activeConfig?.GetFloat(ConfigKey.CameraPushBackwardScale, defaultPushBackwardScale) ?? defaultPushBackwardScale);
            safeViewportInset = Mathf.Clamp(activeConfig?.GetFloat(ConfigKey.CameraSafeViewportInset, defaultSafeViewportInset) ?? defaultSafeViewportInset, 0f, 0.45f);
        }

        private Vector3 ComputeAimPush(ILocalPlayerView view, Quaternion cameraRotation)
        {
            IAimSource aim = view.Aim;
            if (aim == null || pushExtent <= 0f || aim.AimDistance <= 0f)
                return Vector3.zero;

            Vector2 aimDirection = aim.AimDirection;
            if (aimDirection.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            Vector3 worldDirection = new Vector3(aimDirection.x, 0f, aimDirection.y).normalized;
            float maxDistance = GetShapeRadius(worldDirection, cameraRotation);
            float distanceAlongAim = Mathf.Min(aim.AimDistance, maxDistance);
            return worldDirection * distanceAlongAim;
        }

        private float GetShapeRadius(Vector3 worldDirection, Quaternion cameraRotation)
        {
            if (pushShape == PushShapeCircle)
                return pushExtent;

            Vector3 screenRight = cameraRotation * Vector3.right;
            Vector3 screenUp = cameraRotation * Vector3.up;
            float screenX = Vector3.Dot(worldDirection, screenRight);
            float screenY = Vector3.Dot(worldDirection, screenUp);
            float horizontalRadius = pushExtent * pushHorizontalScale;
            float verticalScale = pushShape == PushShapeAsymmetric && screenY < 0f
                ? pushBackwardScale
                : pushForwardScale;
            float verticalRadius = pushExtent * verticalScale;
            float denominator = (screenX * screenX) / (horizontalRadius * horizontalRadius)
                + (screenY * screenY) / (verticalRadius * verticalRadius);

            return denominator > 0.0001f ? 1f / Mathf.Sqrt(denominator) : pushExtent;
        }

        private Vector3 ClampPushToSafeViewport(Vector3 desiredPush, Quaternion cameraRotation)
        {
            if (desiredPush.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            float halfHeight = Mathf.Max(0f, zoom);
            float halfWidth = halfHeight * Mathf.Max(0.01f, targetCamera.aspect);
            float safeScale = 1f - safeViewportInset * 2f;
            float maxX = halfWidth * safeScale;
            float maxY = halfHeight * safeScale;

            Vector3 localBodyOffset = Quaternion.Inverse(cameraRotation) * -desiredPush;
            float clampedX = Mathf.Clamp(localBodyOffset.x, -maxX, maxX);
            float clampedY = Mathf.Clamp(localBodyOffset.y, -maxY, maxY);

            if (Mathf.Approximately(localBodyOffset.x, clampedX) && Mathf.Approximately(localBodyOffset.y, clampedY))
                return desiredPush;

            return GroundPushForScreenOffset(-clampedX, -clampedY, cameraRotation);
        }

        private static Vector3 GroundPushForScreenOffset(float screenX, float screenY, Quaternion cameraRotation)
        {
            Vector3 screenRight = cameraRotation * Vector3.right;
            Vector3 screenUp = cameraRotation * Vector3.up;
            float determinant = screenRight.x * screenUp.z - screenRight.z * screenUp.x;

            if (Mathf.Abs(determinant) <= 0.0001f)
                return Vector3.zero;

            float pushX = (screenX * screenUp.z - screenRight.z * screenY) / determinant;
            float pushZ = (screenRight.x * screenY - screenX * screenUp.x) / determinant;
            return new Vector3(pushX, 0f, pushZ);
        }

        private void ApplyProjection()
        {
            if (targetCamera == null)
                return;

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = zoom;
        }
    }
}
