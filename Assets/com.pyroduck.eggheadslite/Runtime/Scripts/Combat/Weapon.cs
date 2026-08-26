using com.pyroduck.eggheadslite.Runtime.Scripts.Character;
using UnityEngine; 
using com.pyroduck.eggheadslite.Runtime.Scripts.Pool;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Combat
{
    public enum ThrowableAttackType
    {
        ThrowOnly,       // Throw, stick 2s, drop — must be retrieved
        ThrowAndReturn,  // Boomerang — returns to owner
        Explosive,       // Explodes on impact — respawns in hand
        Shuriken         // Unlimited ammo — sticks 5s then destroys
    }

    /// <summary>
    /// Determines the firing pattern for a RangedWeapon.
    /// Gun, Rifle, and Sniper all use single-shot mechanics in code;
    /// differentiate them via Inspector fields (damage, projectile speed,
    /// attack cooldown) rather than expecting distinct code paths.
    /// Shotgun is the only value that spawns multiple pellets per shot.
    /// </summary>
    public enum RangedAttackType
    {
        Gun,      // Single shot — tune via attackCooldown / projectile stats
        Rifle,    // Single shot — tune via attackCooldown / projectile stats
        Shotgun,  // Multi-pellet spread (uses shotgunPelletCount)
        Sniper,   // Single shot — tune via attackCooldown / projectile stats
    }

    public interface IWeaponAttack
    {
        bool TryAttack(Vector2 attackDirection, bool fireDown, bool fireHeld);
    }

    public interface IMeleeWeapon
    {
        void ExecuteMeleeAttack(Vector2 attackDirection);
    }

    public interface IRangedWeapon
    {
        void ExecuteRangedAttack(Vector2 attackDirection);
    }

    public interface IThrowableWeapon
    {
        void ExecuteThrowableAttack(Vector2 attackDirection);
    }

    public abstract class Weapon : MonoBehaviour, IWeaponAttack
    {
        [Header("Audio")] 
        [SerializeField] protected AudioClip equipSound;
        [SerializeField] protected AudioClip dropSound;
        [SerializeField] protected AudioSource audioSource;

        private GameObject _owner;
        public GameObject Owner => _owner;

        public void SetOwner(GameObject owner)
        {
            _owner = owner;
        }

        [Header("Common Settings")] [SerializeField]
        protected float attackCooldown = 0.2f;

        protected float _nextAttackTime;

        public virtual bool TryAttack(Vector2 attackDirection, bool fireDown, bool fireHeld)
        {
            if (Time.time < _nextAttackTime) return false;
            if (!CanAttack(fireDown, fireHeld)) return false;

            EnsureOwnerBound();
            _nextAttackTime = Time.time + attackCooldown;
            ExecuteAttack(attackDirection); 
            return true;
        }

        protected void EnsureOwnerBound()
        {
            if (_owner != null) return;

            var character = GetComponentInParent<BaseCharacterController>();
            if (character != null)
            {
                _owner = character.transform.root != null
                    ? character.transform.root.gameObject
                    : character.gameObject;
            }
        }
         
        public virtual void PlayEquipSound()
        {
            if (equipSound != null)
            {
                if (audioSource != null)
                    audioSource.PlayOneShot(equipSound);
                else
                    AudioSource.PlayClipAtPoint(equipSound, transform.position);
            }
        }

        public virtual void PlayDropSound()
        {
            if (dropSound != null)
            {
                AudioSource.PlayClipAtPoint(dropSound, transform.position);
            }
        }

        protected abstract bool CanAttack(bool fireDown, bool fireHeld);
        protected abstract void ExecuteAttack(Vector2 attackDirection);
    }
}
