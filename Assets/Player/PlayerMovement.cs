using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Player
{
    public sealed class PlayerMovement : NetworkBehaviour, IConfigConsumer
    {
        [SerializeField] private PlayerBody body;
        [SerializeField] private MonoBehaviour lifeStateSource;
        [SerializeField] private MonoBehaviour recoilSource;
        [SerializeField] private LayerMask collisionMask = 1 << 8;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.5f;
        [SerializeField, Min(0.01f)] private float collisionHeight = 2f;
        [SerializeField, Min(0f)] private float collisionSkin = 0.02f;

        // Server simulation rate. Deliberately NOT Unity's physics FixedUpdate: this is
        // kinematic (transform writes, no Rigidbody), so it runs on its own fixed-step
        // accumulator decoupled from Time.fixedDeltaTime. Fixed-step keeps movement
        // framerate-independent and gives a stable tick to migrate onto PurrDiction later.
        [SerializeField] private float tickRate = 60f;

        // Hard cap on catch-up steps per frame so a frame hitch can't spiral the loop.
        private const float MaxAccumulated = 0.25f;

        private Vector2 moveInput;
        private Vector2 aimDirectionInput;
        private bool sprintInput;
        private IConfig config;
        private float tickAccumulator;

        private ILifeState LifeState => lifeStateSource as ILifeState;
        private IAimRecoil Recoil => recoilSource as IAimRecoil;

        public void Configure(IConfig source)
        {
            config = source;
        }

        public void SetInputSnapshot(Vector2 moveInput, bool sprint, Vector2 aimDirection)
        {
            if (!isServer)
                return;

            this.moveInput = moveInput.sqrMagnitude > 1f ? moveInput.normalized : moveInput;
            sprintInput = sprint;
            aimDirectionInput = NormalizeAimDirection(aimDirection);
        }

        private void Update()
        {
            if (!isServer)
                return;

            float tickDelta = 1f / tickRate;
            tickAccumulator = Mathf.Min(tickAccumulator + Time.deltaTime, MaxAccumulated);
            while (tickAccumulator >= tickDelta)
            {
                tickAccumulator -= tickDelta;
                Step(tickDelta);
            }
        }

        private void Step(float delta)
        {
            if (!(LifeState?.CanAct ?? true))
            {
                moveInput = Vector2.zero;
                sprintInput = false;
                body.SetMovementState(MovementState.Idle);
                return;
            }

            ApplyFacing(delta);

            bool isMoving = moveInput != Vector2.zero;
            bool isSprinting = isMoving && sprintInput;
            body.SetMovementState(isMoving
                ? isSprinting ? MovementState.Sprinting : MovementState.Running
                : MovementState.Idle);

            if (!isMoving)
                return;

            float speed = isSprinting
                ? config?.GetFloat(ConfigKey.SprintSpeed, 5.8f) ?? 5.8f
                : config?.GetFloat(ConfigKey.MoveSpeed, 4.5f) ?? 4.5f;
            Vector3 delta3 = new Vector3(moveInput.x, 0f, moveInput.y) * (speed * delta);
            MoveWithCollision(delta3);
        }

        // Collide-and-slide. On hitting a surface we advance up to it, then deflect the
        // leftover motion onto the surface plane and try again, so pressing into a wall at
        // an angle slides along it instead of dead-stopping. Iterating lets the leftover
        // resolve against a second surface (inside corners) and naturally stops the player
        // when wedged. Pressing straight into a wall projects to zero — also correct.
        private const int MaxSlideIterations = 4;

        private void MoveWithCollision(Vector3 delta)
        {
            Vector3 remaining = delta;

            for (int i = 0; i < MaxSlideIterations; i++)
            {
                float distance = remaining.magnitude;
                if (distance <= Mathf.Epsilon)
                    return;

                Vector3 direction = remaining / distance;
                GetCapsulePoints(transform.position, out Vector3 bottom, out Vector3 top);

                if (!Physics.CapsuleCast(bottom, top, collisionRadius, direction, out RaycastHit hit, distance + collisionSkin, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    transform.position += remaining;
                    return;
                }

                float travel = Mathf.Min(Mathf.Max(0f, hit.distance - collisionSkin), distance);
                transform.position += direction * travel;

                // Deflect the unused motion along the surface and keep sliding.
                Vector3 leftover = remaining - direction * travel;
                remaining = Vector3.ProjectOnPlane(leftover, hit.normal);
            }
        }

        private void GetCapsulePoints(Vector3 position, out Vector3 bottom, out Vector3 top)
        {
            float radius = Mathf.Max(0.01f, collisionRadius);
            float height = Mathf.Max(radius * 2f, collisionHeight);
            float halfSegment = (height * 0.5f) - radius;
            Vector3 center = position + Vector3.up * (height * 0.5f);

            bottom = center + Vector3.down * halfSegment;
            top = center + Vector3.up * halfSegment;
        }

        private void ApplyFacing(float delta)
        {
            if (aimDirectionInput == Vector2.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(aimDirectionInput.x, 0f, aimDirectionInput.y), Vector3.up);

            // Layer server-authoritative recoil on top of the aimed facing, so the body
            // visibly kicks (synced to everyone) and the shot — which fires along this
            // facing — follows it. Zero when settled.
            float recoilYaw = Recoil?.YawOffsetDegrees ?? 0f;
            if (recoilYaw != 0f)
                targetRotation = Quaternion.AngleAxis(recoilYaw, Vector3.up) * targetRotation;

            float bodyTurnSpeed = config?.GetFloat(ConfigKey.BodyTurnSpeed, 0f) ?? 0f;

            transform.rotation = bodyTurnSpeed > 0f
                ? Quaternion.RotateTowards(transform.rotation, targetRotation, bodyTurnSpeed * delta)
                : targetRotation;
        }

        private static Vector2 NormalizeAimDirection(Vector2 direction)
        {
            return direction.sqrMagnitude > Mathf.Epsilon
                ? direction.normalized
                : Vector2.zero;
        }
    }
}
