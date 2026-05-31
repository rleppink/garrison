using UnityEngine;

namespace Garrison.Shared.Player
{
    // Shared seam for spawned components that need the local-view provider injected
    // without taking a compile-time dependency on the Player slice's concrete body.
    public interface ILocalPlayerViewConsumer
    {
        void BindLocalView(MonoBehaviour source);
    }
}
