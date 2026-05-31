using Garrison.Shared.Net;
using UnityEngine;

namespace Garrison.Shared.Audio
{
    // Persistent scene service that hands the inspector-wired audio bus to spawned
    // network identities that need local presentation audio. Runtime-spawned prefabs
    // cannot safely serialize references to Bootstrap scene services.
    public sealed class AudioBusBinder : SpawnedIdentityBinder<IAudioBusSink>
    {
        [SerializeField] private MonoBehaviour audioBusSource;

        private IAudioBus AudioBus => audioBusSource as IAudioBus;

        protected override void ValidateConfiguration()
        {
            if (audioBusSource == null)
                Debug.LogError("AudioBusBinder has no AudioBus source wired; spawned audio sinks will be silent.");
            else if (AudioBus == null)
                Debug.LogError("AudioBusBinder audioBusSource must implement IAudioBus.");
        }

        protected override void Bind(IAudioBusSink sink, bool bound)
        {
            sink.BindAudioBus(bound ? AudioBus : null);
        }
    }
}
