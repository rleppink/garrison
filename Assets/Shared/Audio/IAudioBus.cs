using UnityEngine;

namespace Garrison.Shared.Audio
{
    public interface IAudioBus
    {
        void Play(AudioChannel channel, AudioClip clip, Vector3 worldPosition);
    }
}
