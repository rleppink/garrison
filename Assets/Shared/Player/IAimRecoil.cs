namespace Garrison.Shared.Player
{
    // Cross-slice seam: weapon recoil expressed as a yaw offset (degrees) layered on
    // top of the player's aim. Owned server-side so a client can't suppress its own
    // inaccuracy — the body facing and shot direction read YawOffsetDegrees on the
    // server. LocalYawOffsetDegrees is a client-only cosmetic mirror with the same
    // tuning, used to kick the aim line responsively without a server round-trip;
    // faking it only blinds the cheater, it does not change where the bullet goes.
    public interface IAimRecoil
    {
        // Server-authoritative recoil offset. Zero when settled.
        float YawOffsetDegrees { get; }

        // Local presentation recoil offset for the aim line. Zero when settled.
        float LocalYawOffsetDegrees { get; }
    }
}
