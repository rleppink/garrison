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
                ? isSprinting ? MovementState.Sprinting : MovementState.Walking
                : MovementState.Idle);

            if (!isMoving)
                return;

            float speed = isSprinting
                ? config?.GetFloat(ConfigKey.SprintSpeed, 5.8f) ?? 5.8f
                : config?.GetFloat(ConfigKey.MoveSpeed, 4.5f) ?? 4.5f;
            Vector3 delta3 = new(moveInput.x, 0f, moveInput.y);
            transform.position += delta3 * (speed * delta);
        }

        private void ApplyFacing(float delta)
        {
            if (aimDirectionInput == Vector2.zero)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(aimDirectionInput.x, 0f, aimDirectionInput.y), Vector3.up);
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
