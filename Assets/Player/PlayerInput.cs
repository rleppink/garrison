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
        [SerializeField] private float sendInterval = 0.05f;

        private float nextSendTime;
        private Vector2 lastSentInput;
        private bool lastSentSprint;

        private void Update()
        {
            if (!isClient || !localPlayer.HasValue || capsule.AssignedPlayer != localPlayer.Value)
                return;

            if (Time.unscaledTime < nextSendTime)
                return;

            Vector2 moveInput = ReadMoveInput();
            bool sprint = ReadSprintInput();
            if (moveInput == lastSentInput && sprint == lastSentSprint)
                return;

            nextSendTime = Time.unscaledTime + sendInterval;
            lastSentInput = moveInput;
            lastSentSprint = sprint;
            SendMoveInput(moveInput, sprint);
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

        [ServerRpc(channel: Channel.Unreliable, requireOwnership: false)]
        private void SendMoveInput(Vector2 moveInput, bool sprint, RPCInfo info = default)
        {
            if (info.sender != capsule.AssignedPlayer)
                return;

            movement.SetMoveInput(moveInput, sprint);
        }
    }
}
