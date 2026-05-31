using System;

namespace Garrison.Shared.Player
{
    public interface ILifeState
    {
        LifeState State { get; }
        bool CanAct { get; }

        event Action<LifeState> StateChanged;
        event Action BecameDowned;
        event Action GotUp;
        event Action Died;
    }
}
