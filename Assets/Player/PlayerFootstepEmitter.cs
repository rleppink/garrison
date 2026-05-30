using Garrison.Shared.Audio;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Player
{
    public sealed class PlayerFootstepEmitter : MonoBehaviour
    {
        [SerializeField] private PlayerBody movementSource;
        [SerializeField] private AudioClip footstepClip;
        [SerializeField, Min(0.05f)] private float walkInterval = 0.55f;
        [SerializeField, Min(0.05f)] private float sprintInterval = 0.36f;

        private IAudioBus audioBus;
        private float timeUntilNextStep;

        public void BindAudioBus(IAudioBus bus)
        {
            audioBus = bus;
        }

        private void Update()
        {
            if (movementSource == null)
                return;

            IMovementState movement = movementSource.Movement;
            MovementState state = movement.State;
            if (state != MovementState.Walking && state != MovementState.Sprinting)
            {
                timeUntilNextStep = 0f;
                return;
            }

            timeUntilNextStep -= Time.deltaTime;
            if (timeUntilNextStep > 0f)
                return;

            if (audioBus != null && footstepClip != null)
                audioBus.Play(AudioChannel.Footsteps, footstepClip, transform.position);

            timeUntilNextStep = state == MovementState.Sprinting ? sprintInterval : walkInterval;
        }

        private void OnValidate()
        {
            walkInterval = Mathf.Max(0.05f, walkInterval);
            sprintInterval = Mathf.Max(0.05f, sprintInterval);
        }
    }
}
