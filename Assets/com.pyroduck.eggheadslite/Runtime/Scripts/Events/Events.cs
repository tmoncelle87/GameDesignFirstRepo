using com.pyroduck.eggheadslite.Runtime.Scripts.Audio;
using com.pyroduck.eggheadslite.Runtime.Scripts.Enums;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Events
{
    /// <summary>
    /// UI fire button was pressed. Published by a UI button's OnClick → WeaponController.OnFireButtonPressed;
    /// consumed by WeaponController.
    /// </summary>
    public struct FireButtonPressedEvent
    {
    }

    /// <summary>
    /// UI fire button was released. Published by a UI button's OnClick → WeaponController.OnFireButtonReleased;
    /// consumed by WeaponController.
    /// </summary>
    public struct FireButtonReleaseEvent
    {
    }
 
    /// <summary>
    /// Animation preview button is held down by UI; consumed by AnimationsController.
    /// </summary>
    public struct AnimationButtonPressedEvent
    {
        public AnimationType AnimationType;
    }

    /// <summary>
    /// Animation preview button was released by UI; consumed by AnimationsController.
    /// </summary>
    public struct AnimationButtonReleasedEvent
    {
        public AnimationType AnimationType;
    }


    /// <summary>
    /// Broadcasts the current movement state for animator synchronization.
    /// Published by character controllers; consumed by animation controllers.
    /// </summary>
    public struct SetMovementEvent
    {
        public float Direction;
        public bool IsWalking;
        public bool IsCrouching;
        public bool IsRunning;
    }

    /// <summary>
    /// Health changed because damage was applied. Published by HealthComponent;
    /// consumed by animation, VFX, UI, or gameplay listeners.
    /// </summary>
    public struct TakeDamage
    {
        public float DamageAmount;
        public float CurrentHealth;
        public float MaxHealth;
        public GameObject Source;
        public GameObject Target;
        public Vector2 HitPoint;
    }

    /// <summary>
    /// Character reached zero health. Source identifies the character GameObject.
    /// </summary>
    public struct CharacterDiedEvent
    {
        public GameObject Source;
    }

    /// <summary>
    /// Character returned to a living health state. Source identifies the character GameObject.
    /// </summary>
    public struct CharacterRevivedEvent
    {
        public GameObject Source;
    }

    /// <summary>
    /// Jump animation should begin. Published by BaseCharacterController when input
    /// requests a grounded jump; consumed by AnimationsController.
    /// </summary>
    public struct TriggerJumpStartEvent
    {
    }

    /// <summary>
    /// Jump animation should end. Published when the controller detects landing.
    /// </summary>
    public struct TriggerJumpEndEvent
    {
    }

    /// <summary>
    /// Physical jump impulse should be applied.
    /// </summary>
    public struct TriggerJumpEvent
    {
    }

    /// <summary>
    /// A dropped weapon pickup was touched. Published by WeaponPickup; consumed by
    /// WeaponLoadoutController.
    /// </summary>
    public struct WeaponPickupEvent
    {
        public Data.VisualDataSO WeaponData;
    }

    /// <summary>
    /// Fire-and-forget 2D sound playback (no spatialisation).
    /// </summary>
    public struct PlaySoundEvent
    {
        public SoundId Id;
        /// <summary>Multiplier on top of the SoundEntry volume. 0 (default) is treated as 1.</summary>
        public float VolumeScale;
    }

    /// <summary>
    /// Fire-and-forget 3D sound playback at a world position.
    /// </summary>
    public struct PlaySoundAtEvent
    {
        public SoundId Id;
        public Vector3 Position;
        /// <summary>Multiplier on top of the SoundEntry volume. 0 (default) is treated as 1.</summary>
        public float VolumeScale;
    }

    /// <summary>
    /// Sets the global multiplier the AudioManager applies to every playback.
    /// </summary>
    public struct SetAudioMasterVolumeEvent
    {
        /// <summary>Clamped to [0, 1] by the AudioManager.</summary>
        public float Volume;
    }
}
