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
        [SerializeField] private float spatialBlend = 1f;

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
            if (!clip)
                return;

            AudioSource source = GetSource();
            source.transform.position = worldPosition;
            source.clip = clip;
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
            source.rolloffMode = AudioRolloffMode.Linear;
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
