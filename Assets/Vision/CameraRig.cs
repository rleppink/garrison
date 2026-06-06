using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Vision
{
    // Local presentation only — NOT networked. Drives the persistent Bootstrap
    // Main Camera to frame the local player's body from a fixed straight-down (top-down)
    // angle, PERSPECTIVE, with zoom and aim-push driven by config. It reads the body only
    // through the Shared ILocalPlayerRegistry seam, so Vision never touches the
    // Player slice. The push is clamped so the body stays inside the safe viewport
    // inset.
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
        [SerializeField] private Camera targetCamera;

        // Wired from the persistent Bootstrap systems object (cast to ILocalPlayerRegistry).
        [SerializeField] private MonoBehaviour localPlayerRegistrySource;

        // Wired from ConfigService (cast to IConfig), same pattern as RoundController.
        [SerializeField] private MonoBehaviour configSource;

        // The camera looks straight DOWN with its up axis pinned to world +z (north),
        // so world +x reads as screen-right and world +z (north) as screen-up —
        // matching PlayerInput's world-space WASD mapping. Straight-down is the
        // fully-fair view (see Docs/design/camera-fairness.md, Option 1b): north and
        // south are mirror images, so sightlines are symmetric in every direction.
        private static readonly Quaternion TopDownRotation =
            Quaternion.LookRotation(Vector3.down, Vector3.forward);

        // Camera setback from the focus plane AND the perspective-strength dial: the
        // FOV is derived as 2·atan(zoom / distance), so a larger distance narrows the
        // FOV for subtler convergence while preserving the body's framing. Lower it to
        // make buildings reveal their sides more aggressively (wider FOV, more lean).
        [SerializeField] private float distance = 35f;

        // Fallbacks used until config delivers values (e.g. before the client connects).
        [SerializeField] private float defaultZoom = 15f;
        [SerializeField] private float defaultPushExtent = 10f;
        [SerializeField, Range(0f, 0.45f)] private float defaultSafeViewportInset = 0.18f;

        private ILocalPlayerRegistry registry;
        private IConfig config;
        private float zoom;
        private float pushExtent;
        private float safeViewportInset;
        private Vector3 currentPush;

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

            // The push slides the body's screen *position* toward aim (body to the
            // bottom when aiming north → full-screen forward view). It is orthogonal to
            // the fixed top-down viewing angle and rotationally symmetric, so it never
            // disturbs fairness.
            Vector3 desiredPush = ComputeAimPush(view, target.position);
            Vector3 clampedPush = UpdatePush(desiredPush);
            Vector3 frameTarget = target.position + clampedPush;

            Transform camTransform = targetCamera.transform;
            Vector3 lookDir = TopDownRotation * Vector3.forward;
            camTransform.position = frameTarget - lookDir * distance;
            camTransform.rotation = TopDownRotation;
        }

        private void ReadConfig()
        {
            IConfig activeConfig = Config;
            zoom = activeConfig?.GetFloat(ConfigKey.CameraZoom, defaultZoom) ?? defaultZoom;
            pushExtent = Mathf.Max(0f, activeConfig?.GetFloat(ConfigKey.CameraPushExtent, defaultPushExtent) ?? defaultPushExtent);
            safeViewportInset = Mathf.Clamp(activeConfig?.GetFloat(ConfigKey.CameraSafeViewportInset, defaultSafeViewportInset) ?? defaultSafeViewportInset, 0f, 0.45f);
        }

        private Vector3 ComputeAimPush(ILocalPlayerView view, Vector3 bodyPosition)
        {
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
            float distanceAlongAim = Mathf.Min(aimDistance, pushExtent);
            return worldDirection * distanceAlongAim;
        }

        private Vector3 ClampPushToSafeViewport(Vector3 desiredPush)
        {
            if (desiredPush.sqrMagnitude <= 0.0001f)
                return Vector3.zero;

            float halfHeight = Mathf.Max(0f, zoom);
            float halfWidth = halfHeight * Mathf.Max(0.01f, targetCamera.aspect);
            float safeScale = 1f - safeViewportInset * 2f;
            float maxX = halfWidth * safeScale;
            float maxY = halfHeight * safeScale;

            Vector3 localBodyOffset = Quaternion.Inverse(TopDownRotation) * -desiredPush;
            float clampedX = Mathf.Clamp(localBodyOffset.x, -maxX, maxX);
            float clampedY = Mathf.Clamp(localBodyOffset.y, -maxY, maxY);

            if (Mathf.Approximately(localBodyOffset.x, clampedX) && Mathf.Approximately(localBodyOffset.y, clampedY))
                return desiredPush;

            return GroundPushForScreenOffset(-clampedX, -clampedY);
        }

        private Vector3 UpdatePush(Vector3 desiredPush)
        {
            currentPush = ClampPushToSafeViewport(desiredPush);
            return currentPush;
        }

        private static Vector3 GroundPushForScreenOffset(float screenX, float screenY)
        {
            Vector3 screenRight = TopDownRotation * Vector3.right;
            Vector3 screenUp = TopDownRotation * Vector3.up;
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
