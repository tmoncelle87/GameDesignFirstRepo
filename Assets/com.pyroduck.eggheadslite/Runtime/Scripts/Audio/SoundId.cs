namespace com.pyroduck.eggheadslite.Runtime.Scripts.Audio
{
    /// <summary>
    /// Central identifier for every sound the audio system can play.
    /// Add new entries at the end to keep serialized values stable.
    /// </summary>
    public enum SoundId
    {
        None = 0,

        // Ranged weapons
        ProjectileFire      = 100,
        ProjectileImpact    = 101,
        ProjectileExplosion = 102,

        // Throwable weapons
        ThrowableThrow      = 200,
        ThrowableImpact     = 201,
        ThrowableExplosion  = 202,

        // Melee weapons
        MeleeSwing          = 300,
        MeleeImpact         = 301,
    }
}
