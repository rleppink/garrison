using PurrNet;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Garrison.Player
{
    public sealed class PlayerInput : NetworkBehaviour
    {
        [SerializeField] private PlayerBody capsule;
        [SerializeField] private PlayerMovement movement;

        // Stream input at the server's simulation rate (PlayerMovement.tickRate, 60Hz).
        // On Unreliable, every tick is a fresh snapshot so a dropped packet self-corrects
        // on the next one (last wins). Sending faster than the server steps just gets
        // overwritten unconsumed; slower undersamples the sim. So match the server tick.
        private const float SendInterval = 1f / 60f;

        private float nextSendTime;

        private void Update()
        {
            if (!isClient || !localPlayer.HasValue || capsule.AssignedPlayer != localPlayer.Value)
                return;

            if (Time.unscaledTime < nextSendTime)
                return;

            nextSendTime = Time.unscaledTime + SendInterval;
            SendMoveInput(ReadMoveInput(), ReadSprintInput(), ReadAimDirection());
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            Vector2 input = Vector2.zero;

            if (keyboard.aKey.isPressed)
                input.x -= 1f;
            if (keyboard.dKey.isPressed)
                input.x += 1f;
            if (keyboard.sKey.isPressed)
                input.y -= 1f;
            if (keyboard.wKey.isPressed)
                input.y += 1f;

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private static bool ReadSprintInput()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.leftShiftKey.isPressed;
        }

        private Vector2 ReadAimDirection()
        {
            var aimSource = capsule != null ? capsule.Aim : null;
            if (aimSource == null)
                return Vector2.zero;

            Vector2 aimDirection = aimSource.AimDirection;
            return aimDirection.sqrMagnitude > Mathf.Epsilon
                ? aimDirection.normalized
                : Vector2.zero;
        }

        [ServerRpc(channel: Channel.Unreliable, requireOwnership: false)]
        private void SendMoveInput(Vector2 moveInput, bool sprint, Vector2 aimDirection, RPCInfo info = default)
        {
            if (info.sender != capsule.AssignedPlayer)
                return;

            movement.SetInputSnapshot(moveInput, sprint, aimDirection);
        }
    }
}
