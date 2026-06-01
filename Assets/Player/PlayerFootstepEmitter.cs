using Garrison.Shared.Audio;
using Garrison.Shared.Player;
using UnityEngine;

namespace Garrison.Player
{
    public sealed class PlayerFootstepEmitter : MonoBehaviour
    {
        [SerializeField] private PlayerBody movementSource;
        [SerializeField] private AudioClip[] footstepClips = System.Array.Empty<AudioClip>();
        [SerializeField, Min(0.05f)] private float runInterval = 0.55f;
        [SerializeField, Min(0.05f)] private float sprintInterval = 0.36f;
        [SerializeField, Range(0f, 0.5f)] private float volumeVariance = 0.08f;
        [SerializeField, Range(0f, 0.5f)] private float pitchVariance = 0.06f;

        private IAudioBus audioBus;
        private float timeUntilNextStep;
        private int lastClipIndex = -1;

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
            if (state != MovementState.Running && state != MovementState.Sprinting)
            {
                timeUntilNextStep = 0f;
                return;
            }

            timeUntilNextStep -= Time.deltaTime;
            if (timeUntilNextStep > 0f)
                return;

            AudioClip clip = PickClip();
            if (audioBus != null && clip != null)
            {
                float volume = 1f + Random.Range(-volumeVariance, volumeVariance);
                float pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
                audioBus.Play(AudioChannel.Footsteps, clip, transform.position, volume, pitch);
            }

            timeUntilNextStep = state == MovementState.Sprinting ? sprintInterval : runInterval;
        }

        private AudioClip PickClip()
        {
            if (footstepClips == null || footstepClips.Length == 0)
                return null;

            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips.Length > 1 && index == lastClipIndex)
                index = (index + 1) % footstepClips.Length;

            lastClipIndex = index;
            return footstepClips[index];
        }

        private void OnValidate()
        {
            runInterval = Mathf.Max(0.05f, runInterval);
            sprintInterval = Mathf.Max(0.05f, sprintInterval);
            volumeVariance = Mathf.Clamp(volumeVariance, 0f, 0.5f);
            pitchVariance = Mathf.Clamp(pitchVariance, 0f, 0.5f);
        }
    }
}
