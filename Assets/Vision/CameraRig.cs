using Garrison.Shared.Config;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Vision
{
    // Local presentation only — NOT networked. Drives the persistent Bootstrap
    // Main Camera to frame the local player's body from a fixed top-down/iso angle,
    // orthographic, with zoom driven by config (ConfigKey.CameraZoom). It reads the
    // body only through the Shared ILocalPlayerRegistry seam, so Vision never touches
    // the Player slice. No aim-push yet (that is C4): this is the baseline of the
    // "character never leaves the screen" invariant — the body stays centred.
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        // Wired from the persistent Bootstrap systems object (cast to ILocalPlayerRegistry).
        [SerializeField] private MonoBehaviour localPlayerRegistrySource;

        // Wired from ConfigService (cast to IConfig), same pattern as RoundController.
        [SerializeField] private MonoBehaviour configSource;

        // Feel-pass dials (not config keys yet — C2 only adds CameraZoom). The camera
        // sits back along this direction at this distance; the angle gives the iso look.
        [SerializeField] private Vector3 viewDirection = new(0f, -1f, -0.5f);
        [SerializeField] private float distance = 20f;

        // Fallback used until config delivers a value (e.g. before the client connects).
        [SerializeField] private float defaultZoom = 10f;

        private ILocalPlayerRegistry registry;
        private IConfig config;
        private float zoom;

        private ILocalPlayerRegistry Registry => registry ??= localPlayerRegistrySource as ILocalPlayerRegistry;
        private IConfig Config => config ??= configSource as IConfig;

        private void OnEnable()
        {
            zoom = Config?.GetFloat(ConfigKey.CameraZoom, defaultZoom) ?? defaultZoom;

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
            zoom = Config?.GetFloat(ConfigKey.CameraZoom, defaultZoom) ?? defaultZoom;
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

            // Frame the target from a fixed angle; the target stays centred (no push).
            Vector3 backwards = viewDirection.sqrMagnitude > 0f ? -viewDirection.normalized : Vector3.up;
            Transform camTransform = targetCamera.transform;
            camTransform.position = target.position + backwards * distance;
            camTransform.rotation = Quaternion.LookRotation(-backwards, Vector3.up);
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
