namespace Garrison.Shared.Config
{
    // Network-synced by value (it's a SyncDictionary key), so declaration order is the
    // wire identity: append new keys, don't reorder.
    public enum ConfigKey
    {
        PlayerCount,
        MoveSpeed,
        CameraZoom,
        CameraPushExtent,
        CameraSafeViewportInset,
        SprintSpeed,
        DefenderSlot,
        BodyTurnSpeed,
        AimLineWidth,
        AimLineLength,
        MaxHearts,
        BleedOutSec,
        AccuracyIdleSpread,
        AccuracyMovingSpread,
        AccuracySprintSpread,
        WeaponDamageHearts,
        WeaponBaseSpread,
        WeaponRange,
        WeaponFalloff,
        RecoilPerShot,
        RecoilMax,
        RecoilSettleTime,
        DefenderMaxHearts,
        SyretteReachRadius,
        ViewDistance,
        LosTickRate,
        NpcConeArc,
        NpcConeRange
    }
}
