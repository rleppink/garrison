using Garrison.Shared.Config;
using Garrison.Shared.Player;
using PurrNet;
using UnityEngine;

namespace Garrison.Player
{
    public sealed class PlayerMovement : NetworkBehaviour
    {
        [SerializeField] private PlayerBody body;

        private Vector2 moveInput;
        private bool sprintInput;
        private IConfig config;

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

        private void FixedUpdate()
        {
            if (!isServer)
                return;

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
            Vector3 delta = new(moveInput.x, 0f, moveInput.y);
            transform.position += delta * (speed * Time.fixedDeltaTime);
        }
    }
}
