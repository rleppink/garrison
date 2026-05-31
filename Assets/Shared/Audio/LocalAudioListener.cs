using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Shared.Audio
{
    // Local presentation only — NOT networked. Keeps the single scene AudioListener glued
    // to the local player's CHARACTER position, so the player always hears from their
    // body's perspective rather than the camera's. This matters because CameraRig pushes
    // the camera toward the aim point (camera-push), so the camera is the wrong anchor for
    // audio: a listener riding it would drift forward with the cursor.
    //
    // Reads the body only through ILocalPlayerRegistry — the same seam CameraRig uses — so
    // there is no Find/Camera.main/Instance lookup and the Audio code stays ignorant of the
    // Player slice. Orientation is held world-fixed (identity) so the stereo image stays
    // screen-aligned (world +x = screen right, world +z = screen up) and never spins as the
    // body turns or aims; only position follows the character.
    [RequireComponent(typeof(AudioListener))]
    public sealed class LocalAudioListener : MonoBehaviour
    {
        // Wired from the persistent Bootstrap systems object, cast to ILocalPlayerRegistry
        // (same pattern as CameraRig.localPlayerRegistrySource).
        [SerializeField] private MonoBehaviour localPlayerRegistrySource;

        private ILocalPlayerRegistry registry;

        private ILocalPlayerRegistry Registry => registry ??= localPlayerRegistrySource as ILocalPlayerRegistry;

        private void OnEnable()
        {
            if (localPlayerRegistrySource == null)
                Debug.LogError("LocalAudioListener has no local-player registry wired; audio will not follow the character.");
            else if (Registry == null)
                Debug.LogError("LocalAudioListener localPlayerRegistrySource must implement ILocalPlayerRegistry.");
        }

        // LateUpdate so the body has finished moving for the frame, matching CameraRig.
        private void LateUpdate()
        {
            ILocalPlayerView view = Registry?.Current;
            if (view == null)
                return;

            Transform target = view.ViewTarget;
            if (target == null)
                return;

            // Position tracks the character; rotation stays world-fixed so panning is
            // screen-aligned and independent of body facing or camera push.
            transform.SetPositionAndRotation(target.position, Quaternion.identity);
        }
    }
}
