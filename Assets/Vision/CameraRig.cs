using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Vision
{
    // Local presentation only — NOT networked. Drives the persistent Bootstrap
    // Main Camera to frame the local player's body from a fixed top-down/iso angle,
    // PERSPECTIVE, with zoom and aim-push driven by config. It reads the body only
    // through the Shared ILocalPlayerRegistry seam, so Vision never touches the
    // Player slice. The push is clamped after all shaping so the body stays inside
    // the safe viewport inset.
    //
    // Projection: a deliberately LOW field of view (telephoto), derived so the body's
    // focus plane frames the same world half-height the old orthographic size did
    // (FOV = 2·atan(zoom / distance)). `zoom` keeps its meaning — world half-height at
    // the body — and `distance` becomes the perspective-strength dial: farther back =
    // narrower FOV = subtler convergence. Subtle (not strong) perspective on purpose:
    // buildings read as solid volumes and reveal a side as you flank them, while
    // verticals barely lean and the screen↔ground mapping stays near-linear, which
    // keeps the aim-push fair and the view readable (kill-boxes, MG arcs, LOS fog).
    public sealed class CameraRig : MonoBehaviour
    {
        private const int PushShapeCircle = 0;
        private const int PushShapeEllipse = 1;
        private const int PushShapeAsymmetric = 2;
        private const int ReturnSnap = 0;
        private const int ReturnLazyFollow = 1;
        private const int PushCouplingAim = 0;
        private const int PushCouplingSeparate = 1;

        [SerializeField] private Camera targetCamera;

        // Wired from the persistent Bootstrap systems object (cast to ILocalPlayerRegistry).
        [SerializeField] private MonoBehaviour localPlayerRegistrySource;

        // Wired from ConfigService (cast to IConfig), same pattern as RoundController.
        [SerializeField] private MonoBehaviour configSource;

        // Authored camera pose. Runtime zoom and push feel are config-driven.
        // Direction the camera LOOKS. The camera sits opposite this (on the -z side,
        // south of the body) and looks down toward +z, so world +x reads as screen
        // right and world +z (W) as screen up — matching PlayerInput's world-space
        // WASD mapping. Keep z POSITIVE: flipping it puts the camera north-of-body
        // looking south, which mirrors both screen axes (W moves down, D moves left).
        [SerializeField] private Vector3 viewDirection = new(0f, -1f, 0.5f);

        // Camera setback from the focus plane AND the perspective-strength dial: the
        // FOV is derived as 2·atan(zoom / distance), so a larger distance narrows the
        // FOV for subtler convergence while preserving the body's framing. Lower it to
        // make buildings reveal their sides more aggressively (wider FOV, more lean).
        [SerializeField] private float distance = 35f;

        // Fallbacks used until config delivers values (e.g. before the client connects).
        [SerializeField] private float defaultZoom = 10f;
        [SerializeField] private float defaultPushExtent = 3.5f;
        [SerializeField] private int defaultPushShape = PushShapeCircle;
        [SerializeField] private float defaultPushHorizontalScale = 1f;
        [SerializeField] private float defaultPushForwardScale = 1f;
        [SerializeField] private float defaultPushBackwardScale = 0.7f;
        [SerializeField, Range(0f, 0.45f)] private float defaultSafeViewportInset = 0.18f;
        [SerializeField] private int defaultReturnMode = ReturnSnap;
        [SerializeField] private float defaultReturnSpeed = 8f;
        [SerializeField] private int defaultPushCoupling = PushCouplingAim;

        private ILocalPlayerRegistry registry;
        private IConfig config;
        private float zoom;
        private float pushExtent;
        private int pushShape;
        private float pushHorizontalScale;
        private float pushForwardScale;
        private float pushBackwardScale;
        private float safeViewportInset;
        private int returnMode;
        private float returnSpeed;
        private int pushCoupling;
        private Vector3 currentPush;
        private Vector3 pushVelocity;

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
            Vector3 desiredPush = ComputeAimPush(view, target.position, cameraRotation);
            Vector3 clampedPush = UpdatePush(desiredPush, cameraRotation);
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
            returnMode = Mathf.Clamp(activeConfig?.GetInt(ConfigKey.CameraReturn, defaultReturnMode) ?? defaultReturnMode, ReturnSnap, ReturnLazyFollow);
            returnSpeed = Mathf.Max(0.01f, activeConfig?.GetFloat(ConfigKey.CameraReturnSpeed, defaultReturnSpeed) ?? defaultReturnSpeed);
            pushCoupling = Mathf.Clamp(activeConfig?.GetInt(ConfigKey.CameraPushCoupling, defaultPushCoupling) ?? defaultPushCoupling, PushCouplingAim, PushCouplingSeparate);
        }

        private Vector3 ComputeAimPush(ILocalPlayerView view, Vector3 bodyPosition, Quaternion cameraRotation)
        {
            // Separate push input is exposed for tuning, but M1 only has aim input.
            // Until that input exists, both coupling modes intentionally use aim.
            _ = pushCoupling;

            IAimSource aim = view.Aim;
            if (aim == null || pushExtent <= 0f)
                return Vector3.zero;

            // PlayerAim's AimPoint is the cursor projected through the ALREADY-pushed
            // camera, so it equals (body + currentPush + cursorScreenOffset). Feeding
            // that straight back in makes the push a gain-1 integrator: every frame it
            // re-counts the push it already applied and runs away to the clamp the
            // instant the cursor leaves dead-center (the "magnets repelling" jank).
            // Subtracting the push we are currently applying recovers the cursor's
            // offset from the body as if the camera were centered on it, which is
            // camera-independent — that breaks the feedback loop and makes the push a
            // stable function of where the cursor actually is.
            Vector3 aimOffset = aim.AimPoint - bodyPosition - currentPush;
            aimOffset.y = 0f;

            float aimDistance = aimOffset.magnitude;
            if (aimDistance <= 0.0001f)
                return Vector3.zero;

            Vector3 worldDirection = aimOffset / aimDistance;
            float maxDistance = GetShapeRadius(worldDirection, cameraRotation);
            float distanceAlongAim = Mathf.Min(aimDistance, maxDistance);
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

        private Vector3 UpdatePush(Vector3 desiredPush, Quaternion cameraRotation)
        {
            Vector3 clampedTarget = ClampPushToSafeViewport(desiredPush, cameraRotation);

            if (returnMode == ReturnSnap)
            {
                currentPush = clampedTarget;
                pushVelocity = Vector3.zero;
                return currentPush;
            }

            if (clampedTarget.sqrMagnitude >= currentPush.sqrMagnitude)
            {
                currentPush = clampedTarget;
                pushVelocity = Vector3.zero;
                return currentPush;
            }

            float smoothTime = 1f / returnSpeed;
            Vector3 smoothedPush = Vector3.SmoothDamp(currentPush, clampedTarget, ref pushVelocity, smoothTime, Mathf.Infinity, Time.deltaTime);
            currentPush = ClampPushToSafeViewport(smoothedPush, cameraRotation);
            if ((currentPush - smoothedPush).sqrMagnitude > 0.0001f)
                pushVelocity = Vector3.zero;

            return currentPush;
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

            // Low-FOV perspective. The body sits exactly `distance` along the view axis
            // from the camera, so a frustum half-height of `zoom` at that depth matches
            // the old orthographicSize framing. Guard distance so a degenerate 0 can't
            // blow the FOV up to 180°.
            float safeDistance = Mathf.Max(0.01f, distance);
            targetCamera.orthographic = false;
            targetCamera.fieldOfView = 2f * Mathf.Atan2(zoom, safeDistance) * Mathf.Rad2Deg;
        }
    }
}
