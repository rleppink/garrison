using System;
using System.Collections.Generic;
using PurrNet;

namespace Garrison.Shared.Vision
{
    // Shared perception result seam so M5 Defenses can react without referencing
    // the Vision slice implementation.
    public interface IPerception
    {
        event Action<PlayerID> TargetAcquired;

        event Action<PlayerID> TargetLost;

        IReadOnlyCollection<PlayerID> CurrentTargets { get; }
    }
}
