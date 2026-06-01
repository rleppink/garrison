using Garrison.Shared.Net;
using UnityEngine;

namespace Garrison.Vision
{
    // Runtime-spawned NPC prefabs cannot serialize Bootstrap scene references, so this
    // binder hands the persistent ServerVisibility service to sinks as they spawn.
    public sealed class ServerVisibilityBinder : SpawnedIdentityBinder<IServerVisibilitySink>
    {
        [SerializeField] private ServerVisibility serverVisibilitySource;

        protected override void ValidateConfiguration()
        {
            if (serverVisibilitySource == null)
                Debug.LogError("ServerVisibilityBinder has no ServerVisibility source wired; spawned perception sinks will be blind.");
        }

        protected override void Bind(IServerVisibilitySink sink, bool bound)
        {
            sink.BindServerVisibility(bound ? serverVisibilitySource : null);
        }
    }
}
