using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Garrison.Shared.Audio
{
    public sealed class AudioBus : MonoBehaviour, IAudioBus
    {
        [SerializeField] private ChannelRoute[] routes = Array.Empty<ChannelRoute>();
        [SerializeField] private int initialPoolSize = 8;

        [Header("3D falloff")]
        // Full 3D so position drives panning and volume. Lower toward 0 to bleed in a
        // non-positional (2D) component if the spatialization ever feels too aggressive.
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;

        // Logarithmic so loudness drops naturally with distance (roughly halving each time
        // the distance doubles past minDistance), rather than the flat Linear ramp.
        [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

        // World units. The camera frames ~10 units half-height, so a full screen is ~20-35
        // units across: minDistance keeps sounds at the character full volume, maxDistance
        // (~a few screens) is where they fade to the floor.
        [SerializeField, Min(0.1f)] private float minDistance = 5f;
        [SerializeField, Min(0.2f)] private float maxDistance = 80f;

        private readonly Dictionary<AudioChannel, AudioMixerGroup> groups = new();
        private readonly List<AudioSource> sources = new();

        private void Awake()
        {
            groups.Clear();
            for (int i = 0; i < routes.Length; i++)
                groups[routes[i].channel] = routes[i].group;

            for (int i = sources.Count; i < initialPoolSize; i++)
                sources.Add(CreateSource());
        }

        public void Play(AudioChannel channel, AudioClip clip, Vector3 worldPosition)
        {
            Play(channel, clip, worldPosition, 1f, 1f);
        }

        public void Play(AudioChannel channel, AudioClip clip, Vector3 worldPosition, float volume, float pitch)
        {
            if (!clip)
                return;

            AudioSource source = GetSource();
            source.transform.position = worldPosition;
            source.clip = clip;
            source.volume = Mathf.Max(0f, volume);
            source.pitch = Mathf.Max(0.01f, pitch);
            source.outputAudioMixerGroup = groups.TryGetValue(channel, out AudioMixerGroup group) ? group : null;
            source.spatialBlend = spatialBlend;
            source.Play();
        }

        private AudioSource GetSource()
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (!sources[i].isPlaying)
                    return sources[i];
            }

            AudioSource source = CreateSource();
            sources.Add(source);
            return source;
        }

        private AudioSource CreateSource()
        {
            GameObject sourceObject = new("AudioBusSource");
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = spatialBlend;
            source.volume = 1f;
            source.pitch = 1f;
            source.rolloffMode = rolloffMode;
            source.minDistance = minDistance;
            source.maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
            return source;
        }

        [Serializable]
        private struct ChannelRoute
        {
            public AudioChannel channel;
            public AudioMixerGroup group;
        }
    }
}
