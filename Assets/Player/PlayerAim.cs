using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Player
{
    // Computes the local player's mouse-aim each frame and exposes it via the Shared
    // IAimSource seam. Runs ONLY for the local/owner body (gated on PlayerBody.IsLocalView);
    // on remote bodies it stays idle and reports zero — aim is per-client local
    // presentation and is never networked in M1.
    //
    // The cursor is projected onto a flat ground math-plane at y = body.y via a plain
    // ray-plane intersection (no physics raycast — the greybox has no geometry to hit).
    // The gameplay camera is not looked up here: it is injected by the local-player
    // registry through PlayerBody.BindCamera when this body becomes the current view.
    [RequireComponent(typeof(PlayerBody))]
    public sealed class PlayerAim : MonoBehaviour, IAimSource
    {
        [SerializeField] private PlayerBody body;

        private Camera aimCamera;
        private Vector3 aimPoint;
        private Vector2 aimDirection;
        private float aimDistance;

        public Vector3 AimPoint => aimPoint;
        public Vector2 AimDirection => aimDirection;
        public float AimDistance => aimDistance;

        // Called from PlayerBody.BindCamera (registry hands the persistent camera over).
        public void SetCamera(Camera camera)
        {
            aimCamera = camera;
        }

        private void Update()
        {
            // Only the owner's own body computes aim; remote bodies hold their last
            // (zero) value. Keep AimPoint at the body when we can't compute, so a stale
            // point never lies about where the cursor is.
            if (body == null || !body.IsLocalView || aimCamera == null)
            {
                aimPoint = transform.position;
                aimDirection = Vector2.zero;
                aimDistance = 0f;
                return;
            }

            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null)
                return; // keep last stable value when there is no mouse device

            Vector3 bodyPos = transform.position;
            Ray ray = aimCamera.ScreenPointToRay(mouse.position.ReadValue());

            // Ground math-plane at the body's height; normal up.
            Plane ground = new(Vector3.up, bodyPos);
            if (!ground.Raycast(ray, out float enter))
                return; // ray parallel to / facing away from the plane — keep last value

            aimPoint = ray.GetPoint(enter);

            Vector3 planar = aimPoint - bodyPos;
            planar.y = 0f;
            aimDistance = planar.magnitude;
            aimDirection = aimDistance > Mathf.Epsilon
                ? new Vector2(planar.x, planar.z) / aimDistance
                : Vector2.zero;
        }
    }
}
