using Garrison.Shared.Config;
using PurrNet;
using UnityEngine;

namespace Garrison.Shared.Player
{
    public sealed class PlayerMovement : NetworkBehaviour
    {
        private Vector2 moveInput;
        private IConfig config;

        public void Configure(IConfig source)
        {
            config = source;
        }

        public void SetMoveInput(Vector2 input)
        {
            if (!isServer)
                return;

            moveInput = input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private void FixedUpdate()
        {
            if (!isServer || moveInput == Vector2.zero)
                return;

            float speed = config?.GetFloat(ConfigKey.MoveSpeed, 4.5f) ?? 4.5f;
            Vector3 delta = new(moveInput.x, 0f, moveInput.y);
            transform.position += delta * (speed * Time.fixedDeltaTime);
        }
    }
}
