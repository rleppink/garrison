using Garrison.Shared.Audio;
using Garrison.Shared.Vision;
using PurrNet;
using UnityEngine;

namespace Garrison.Vision
{
    // Minimal M3 consumer for the perception seam: play a local positional cue when the
    // server-side perception layer acquires a player. M5 replaces this stub with real use.
    public sealed class NpcAlertCue : MonoBehaviour, IAudioBusSink
    {
        [SerializeField] private MonoBehaviour perceptionSource;
        [SerializeField] private AudioClip alertClip;

        private IAudioBus audioBus;

        private IPerception Perception => perceptionSource as IPerception;

        public void BindAudioBus(IAudioBus source)
        {
            audioBus = source;
        }

        private void OnEnable()
        {
            Subscribe(true);
        }

        private void OnDisable()
        {
            Subscribe(false);
        }

        private void OnTargetAcquired(PlayerID _)
        {
            audioBus?.Play(AudioChannel.Alarms, alertClip, transform.position);
        }

        private void Subscribe(bool subscribe)
        {
            IPerception perception = Perception;
            if (perception == null)
                return;

            if (subscribe)
                perception.TargetAcquired += OnTargetAcquired;
            else
                perception.TargetAcquired -= OnTargetAcquired;
        }
    }
}
