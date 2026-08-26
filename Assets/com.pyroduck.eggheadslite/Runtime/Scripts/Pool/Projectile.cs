using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using com.pyroduck.eggheadslite.Runtime.Scripts.Events;
using com.pyroduck.eggheadslite.Runtime.Scripts.Audio;
using com.pyroduck.eggheadslite.Runtime.Scripts.Combat;
using com.pyroduck.eggheadslite.Runtime.Scripts.Utils;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Pool
{
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Motion")]
        [FormerlySerializedAs("speed")]
        [SerializeField] private float _speed = 15f;

        [FormerlySerializedAs("lifetime")]
        [SerializeField] private float _lifetime = 2f;

        [Header("Explosion")]
        [FormerlySerializedAs("isExplosive")]
        [SerializeField] private bool _isExplosive = false;

        [FormerlySerializedAs("explosionRadius")]
        [SerializeField] private float _explosionRadius = 3f;

        [SerializeField] private ExplosionEffect _explosionEffectPrefab;

        [Header("Effects")]
        [SerializeField] private ParticleSystem _bloodParticlePrefab;

        [Header("Impact Sounds")]
        [Tooltip("Random impact clips played on any surface. surfaceTag values on entries are ignored here.")]
        [SerializeField] private List<SoundData> impactSounds = new List<SoundData>();
        [SerializeField] private AudioSource audioSource;

        public float Damage { get; private set; }

        private float _lifeTimer;
        private Vector2 _moveDirection;
        private GameObject _owner;
        private bool _detonated;

        private Collider2D _myCollider;
        private List<Collider2D> _ignoredColliders = new List<Collider2D>();

        private void Awake()
        {
            _myCollider = GetComponent<Collider2D>();
            ProjectileCollisionRules.EnsureBulletThrowableIgnored();
        }

        // ── Pool ─────────────────────────────────────────

        public void OnSpawn()
        {
            _lifeTimer     = _lifetime;
            _detonated     = false;
            _moveDirection = Vector2.right;
            _owner         = null;
            Damage         = 0f;
        }

        public void OnDespawn()
        {
            ResetIgnoredCollisions();
        }

        // ── Init ─────────────────────────────────────────

        public void Initialize(Vector2 direction, float damage = 0f, GameObject owner = null)
        {
            _moveDirection = direction.normalized;
            Damage         = damage;
            _owner         = owner;
            _lifeTimer     = _lifetime;
            _detonated     = false;

            float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            SetupOwnerCollisionIgnore();
        }

        private void SetupOwnerCollisionIgnore()
        {
            if (_owner == null || _myCollider == null) return;

            var ownerColliders = _owner.GetComponentsInChildren<Collider2D>();

            foreach (var col in ownerColliders)
            {
                if (col == null) continue;

                Physics2D.IgnoreCollision(_myCollider, col, true);
                _ignoredColliders.Add(col);
            }
        }

        private void ResetIgnoredCollisions()
        {
            if (_myCollider == null) return;

            foreach (var col in _ignoredColliders)
            {
                if (col == null) continue;

                Physics2D.IgnoreCollision(_myCollider, col, false);
            }

            _ignoredColliders.Clear();
        }

        // ── Update ───────────────────────────────────────

        private void Update()
        {
            transform.Translate(_moveDirection * (_speed * Time.deltaTime), Space.World);

            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0) ReturnToPool();
        }

        // ── Collision ────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (_detonated) return;
            if (other.collider.isTrigger) return;
            if (TryIgnoreProjectileCollision(other.collider)) return;

            Vector2 hitPoint = other.contactCount > 0
                ? other.contacts[0].point
                : other.collider.bounds.center;
            HandleHit(other.collider, hitPoint);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_detonated) return;
            if (other.isTrigger) return;
            if (TryIgnoreProjectileCollision(other)) return;

            HandleHit(other, other.bounds.center);
        }

        private void HandleHit(Collider2D other, Vector2 hitPoint)
        {
            if (IsOwnerHierarchy(other.transform)) return;

            if (_isExplosive)
            {
                Explode(hitPoint);
            }
            else
            {
                var damageable = other.GetComponent<IDamageable>()
                                 ?? other.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    damageable.TakeDamage(Damage, _owner, hitPoint);
                    PlayBloodParticle(hitPoint);
                }

                PlayImpactSound(hitPoint);
            }

            ReturnToPool();
        }

        private bool TryIgnoreProjectileCollision(Collider2D other)
        {
            if (!ProjectileCollisionRules.IsProjectileLike(other)) return false;

            ProjectileCollisionRules.IgnoreCollision(_myCollider, other, _ignoredColliders);
            return true;
        }

        private void PlayBloodParticle(Vector2 hitPoint)
        {
            if (_bloodParticlePrefab == null) return;
            var blood = Instantiate(_bloodParticlePrefab, hitPoint, Quaternion.identity,
                SceneOrganizer.Get(SceneOrganizer.Buckets.Effects));
            var main = blood.main;
            if (main.stopAction == ParticleSystemStopAction.None)
                main.stopAction = ParticleSystemStopAction.Destroy;
            blood.Play();
        }

        private void PlayImpactSound(Vector2 point)
        {
            ProjectileImpactAudio.PlayAtPoint(impactSounds, audioSource, point);
        }

        // ── Explosion ────────────────────────────────────

        private void Explode(Vector2 origin)
        {
            _detonated = true;

            var hits = Physics2D.OverlapCircleAll(origin, _explosionRadius);

            foreach (var hit in hits)
            {
                if (IsOwnerHierarchy(hit.transform)) continue;

                var damageable = hit.GetComponent<IDamageable>()
                                 ?? hit.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    float dist     = Vector2.Distance(origin, hit.bounds.center);
                    float falloff  = Mathf.Clamp01(1f - dist / _explosionRadius);
                    float finalDmg = Damage * falloff;

                    damageable.TakeDamage(finalDmg, _owner, origin);
                }
            }

            if (ProjectilePool.Instance != null && _explosionEffectPrefab != null)
            {
                var fx = ProjectilePool.Instance.GetExplosionFromPool(_explosionEffectPrefab, origin);
                if (fx != null) fx.Play(origin);
            }

            EventManager.Publish(new PlaySoundAtEvent
            {
                Id       = SoundId.ProjectileExplosion,
                Position = origin
            });
        }

        private bool IsOwnerHierarchy(Transform candidate)
        {
            if (_owner == null || candidate == null) return false;
            return candidate.IsChildOf(_owner.transform) || _owner.transform.IsChildOf(candidate);
        }

        // ── Return ───────────────────────────────────────

        private void ReturnToPool()
        {
            if (ProjectilePool.Instance != null)
                ProjectilePool.Instance.ReturnProjectile(this);
            else
                Destroy(gameObject);
        }
    }
}
