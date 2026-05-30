using System;

namespace Garrison.Shared.Player
{
    // The handoff point between the runtime-spawned local body and persistent
    // local-presentation services (camera, later aim/spectator). Followers read
    // Current and re-read it on CurrentChanged; they never look up the body
    // themselves (no Find/Instance/GetComponent).
    public interface ILocalPlayerRegistry
    {
        ILocalPlayerView Current { get; }
        event Action CurrentChanged;
    }
}
