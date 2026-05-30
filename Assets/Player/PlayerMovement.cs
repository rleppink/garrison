using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Player
{
    public sealed class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private PlayerBody body;

        // Server simulation rate. Deliberately NOT Unity's physics FixedUpdate: this is
        // kinematic (transform writes, no Rigidbody), so it runs on its own fixed-step
        // accumulator decoupled from Time.fixedDeltaTime. Fixed-step keeps movement
        // framerate-independent and gives a stable tick to migrate onto PurrDiction later.
        [SerializeField] private float tickRate = 60f;

        // Hard cap on catch-up steps per frame so a frame hitch can't spiral the loop.
        private const float MaxAccumulated = 0.25f;

        private Vector2 moveInput;
        private bool sprintInput;
        private IConfig config;
        private float tickAccumulator;

        public void Configure(IConfig source)
        {
            config = source;
        }

        public void SetMoveInput(Vector2 input, bool sprint)
        {
            if (!isServer)
                return;

            moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
            sprintInput = sprint;
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
    }
}
