using UnityEngine;
using com.pyroduck.eggheadslite.Runtime.Scripts.Combat;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Pool
{
    /// <summary>
    /// Scene-level singleton that manages Projectile, ThrowableProjectile, and
    /// ExplosionEffect instances through PoolManager / ObjectPool&lt;T&gt;.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        public static ProjectilePool Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[ProjectilePool] More than one ProjectilePool exists. " +
                                 $"Destroying '{gameObject.name}'.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            RegisterPools();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Inspector ─────────────────────────────────────────────────────────

       private Projectile          _projectilePrefab;
       private ThrowableProjectile _throwablePrefab;
       private ExplosionEffect     _explosionPrefab;

        // ── Internal ──────────────────────────────────────────────────────────

        private PoolManager _poolManager;

        private void RegisterPools()
        {
            _poolManager = new PoolManager(transform);

            if (_projectilePrefab != null)
                _poolManager.RegisterPool(_projectilePrefab);

            if (_throwablePrefab != null)
                _poolManager.RegisterPool(_throwablePrefab);

            if (_explosionPrefab != null)
                _poolManager.RegisterPool(_explosionPrefab);
        }

        // ── Get / Return API ──────────────────────────────────────────────────

        /// <summary>Gets from a specific prefab pool, registering it if needed.</summary>
        public Projectile GetProjectile(Projectile prefab, Vector3 position)
        {
            if (prefab == null) return null;
            if (prefab != _projectilePrefab)
            {
                _projectilePrefab = prefab;
                _poolManager.RegisterPool(_projectilePrefab, force: true);
            }
            return _poolManager.Get(prefab,position, Quaternion.identity);
        }

       

        public void ReturnProjectile(Projectile proj)
        {
            if (proj == null) return;
            _poolManager.Return(proj);
        }

        public ThrowableProjectile GetThrowable(ThrowableProjectile prefab,Vector3 position)
        {
            if (prefab == null)
            {
                Debug.LogError("[ProjectilePool] GetThrowable was called with a null prefab.");
                return null;
            }
            if (prefab != _throwablePrefab)
            {
                _throwablePrefab = prefab;
                _poolManager.RegisterPool(_throwablePrefab, force: true);
            }
            return _poolManager.Get<ThrowableProjectile>(prefab,position, Quaternion.identity);
        }

        public void ReturnThrowable(ThrowableProjectile proj)
        {
            if (proj == null) return;
            _poolManager.Return(proj);
        }

        /// <summary>Gets from a specific explosion effect prefab pool, registering it if needed.</summary>
        public ExplosionEffect GetExplosionFromPool(ExplosionEffect prefab, Vector3 position)
        {
            if (prefab == null) return null;
            if (prefab != _explosionPrefab)
            {
                _explosionPrefab = prefab;
                _poolManager.RegisterPool(_explosionPrefab, force: true);
            }
            return _poolManager.Get(prefab,position, Quaternion.identity);
        }

        public void ReturnExplosion(ExplosionEffect fx)
        {
            if (fx == null) return;
            _poolManager.Return(fx);
        }

        /// <summary>
        /// Clears pooled instances and rebuilds pools for the currently assigned prefabs.
        /// </summary>
        public void ClearPool()
        {
            _poolManager?.Clear();
            RegisterPools();
        }

        /// <summary>Called by RangedWeapon on enable to register the projectile prefab early.</summary>
        public void PrewarmProjectile(Projectile prefab)
        {
            if (prefab == null) return;
            if (prefab != _projectilePrefab)
            {
                _projectilePrefab = prefab;
                _poolManager.RegisterPool(_projectilePrefab, force: true);
            }
        }
        public void PrewarmThrowable(ThrowableProjectile throwablePrefab)
        {
            if (throwablePrefab == null) return;
            if (throwablePrefab != _throwablePrefab)
            {
                _throwablePrefab = throwablePrefab;
                _poolManager.RegisterPool(_throwablePrefab, force: true);
            }
        }

        public void PrewarmExplosion(ExplosionEffect explosionEffectPrefab)
        {
            if (explosionEffectPrefab == null) return;
            if (explosionEffectPrefab != _explosionPrefab)
            {
                _explosionPrefab = explosionEffectPrefab;
                _poolManager.RegisterPool(_explosionPrefab, force: true);
            }
        }
    }
}
