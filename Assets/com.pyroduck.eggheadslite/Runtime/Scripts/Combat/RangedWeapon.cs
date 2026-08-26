using UnityEngine;
using com.pyroduck.eggheadslite.Runtime.Scripts.Pool;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Combat
{
    public class RangedWeapon : Weapon, IRangedWeapon
    {
        [Header("Audio")]
        [SerializeField] protected AudioClip fireSound;
        [SerializeField] protected AudioClip recoilSound;
        [SerializeField] private AudioSource recoilAudioSource;
        [SerializeField] private float recoilSoundDelay = 0.06f;
        
        [Header("Ranged Settings")] [SerializeField]
        private RangedAttackType rangedType = RangedAttackType.Gun;

        [SerializeField] private bool rangedAutomaticFire = true;
        [SerializeField] private int shotgunPelletCount = 6;

        [Header("Damage Settings")]
        [SerializeField] private float projectileDamage = 10f;

        [SerializeField] private GameObject projectilePrefab;

        private Projectile _cachedProjectilePrefab;
        private Projectile _projectilePrefab
        {
            get
            {
                if (_cachedProjectilePrefab != null) return _cachedProjectilePrefab;
                if (projectilePrefab == null) return null;
                _cachedProjectilePrefab = projectilePrefab.GetComponent<Projectile>();
                return _cachedProjectilePrefab;
            }
        }

        [Header("Muzzle Settings")] [SerializeField]
        private Transform muzzlePlace;

        public Transform MuzzlePlace => muzzlePlace;

        [SerializeField] private GameObject muzzleParticle;
        private ParticleSystem muzzleFlash;
        private GameObject _muzzleFlashInstance;

        [Header("Recoil Settings")]
        [SerializeField] private float recoilDuration = 0.2f;
        [SerializeField] private float maxRecoilDistance = 0.15f;

        // Recoil state (Update-driven, no Coroutine)
        private bool _isRecoiling;
        private float _recoilTimer;
        private float _recoilPeakDistance; // how far back we actually travelled when half-time hit
         
        public void CreateMuzzle()
        {
            if (muzzleFlash != null) return;

            if (muzzleParticle != null && muzzlePlace != null)
            {
                _muzzleFlashInstance = Instantiate(muzzleParticle, muzzlePlace);
                _muzzleFlashInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                muzzleFlash = _muzzleFlashInstance.GetComponent<ParticleSystem>();
                ResetMuzzleFlash();
            }
        }

        private void ResetMuzzleFlash()
        {
            if (muzzleFlash == null) return;

            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var childParticles = muzzleFlash.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            foreach (var particle in childParticles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        protected override bool CanAttack(bool fireDown, bool fireHeld)
        {
            if (!rangedAutomaticFire && !fireDown)
            {
                return false;
            }

            return true;
        }

        protected override void ExecuteAttack(Vector2 attackDirection)
        {
            ExecuteRangedAttack(attackDirection);
        }

        public void ExecuteRangedAttack(Vector2 attackDirection)
        {
            Vector2 normalizedDirection = attackDirection.sqrMagnitude > 0.0001f
                ? attackDirection.normalized
                : Vector2.right;

            PlayMuzzleFlash(normalizedDirection);

            Vector3 spawnPos = muzzlePlace != null ? muzzlePlace.position : transform.position;

            switch (rangedType)
            {
                case RangedAttackType.Shotgun:
                {
                    // Fire exactly shotgunPelletCount pellets spread evenly at 5° intervals.
                    // E.g. pelletCount=6 → angles: -12.5°, -7.5°, -2.5°, +2.5°, +7.5°, +12.5°
                    float halfSpread = (shotgunPelletCount - 1) * 0.5f;
                    for (int i = 0; i < shotgunPelletCount; i++)
                    {
                        float angle = (-halfSpread + i) * 5f;
                        FireProjectile(spawnPos, Quaternion.Euler(0, 0, angle) * normalizedDirection);
                    }
                    break;
                }
                default:
                    // Gun, Rifle, Sniper — all single shot; differentiate via Inspector stats.
                    FireProjectile(spawnPos, normalizedDirection);
                    break;
            }
            
            TriggerRecoil();
            PlayFireSound();
        }

        private void FireProjectile(Vector3 pos, Vector2 direction)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning($"{name}: projectilePrefab is not assigned.");
                return;
            }

            if (Owner == null)
            {
                Debug.LogError($"{name}: OWNER NULL! Weapon.SetOwner was not called.");
                return;
            }

            Projectile result = null;

            if (ProjectilePool.Instance != null)
            {
                result = ProjectilePool.Instance.GetProjectile(_projectilePrefab, pos);
            }

            if (result == null)
            {
                result = Instantiate(_projectilePrefab, pos, Quaternion.identity);
            }

            result.Initialize(direction, projectileDamage, Owner);
        }

        public void TriggerRecoil()
        {
            // Restart recoil from current position clamped to maxRecoilDistance
            _isRecoiling = true;
            _recoilTimer = 0f;
            _recoilPeakDistance = 0f;
        }

        private void PlayMuzzleFlash(Vector2 direction)
        {
            if (muzzleFlash == null) return;

            muzzleFlash.transform.rotation = GetMuzzleRotation(direction);
            muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleFlash.Play(true);
        }

        private static Quaternion GetMuzzleRotation(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                direction = Vector2.right;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }
        
        protected virtual void PlayFireSound()
        {
            if (fireSound != null)
            {
                if (audioSource != null)
                    audioSource.PlayOneShot(fireSound);
                else
                    AudioSource.PlayClipAtPoint(fireSound, transform.position);

                QueueRecoilSoundAfter(recoilSoundDelay);
            }
            else if (recoilSound != null)
            {
                QueueRecoilSoundAfter(recoilSoundDelay);
            }
        }
        
        protected virtual void PlayRecoilSound()
        {
            if (recoilSound == null) return;

            AudioSource source = recoilAudioSource != null ? recoilAudioSource : audioSource;
            if (source != null)
                source.PlayOneShot(recoilSound);
            else
                AudioSource.PlayClipAtPoint(recoilSound, transform.position);
        }

        private void QueueRecoilSoundAfter(float delay)
        {
            if (recoilSound == null) return;
            StartCoroutine(PlayRecoilAfterDelay(Mathf.Max(0f, delay)));
        }

        private System.Collections.IEnumerator PlayRecoilAfterDelay(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            PlayRecoilSound();
        }
          
        private void OnEnable()
        {
            if (ProjectilePool.Instance != null && _projectilePrefab != null)
                ProjectilePool.Instance.PrewarmProjectile(_projectilePrefab);

            if (recoilAudioSource == null)
            {
                var allSources = GetComponentsInChildren<AudioSource>(true);
                foreach (var src in allSources)
                {
                    if (src != null && src != audioSource)
                    {
                        recoilAudioSource = src;
                        break;
                    }
                }
            }

            if (muzzleFlash == null) CreateMuzzle();
            else ResetMuzzleFlash();
        }

        private void OnDestroy()
        {
            if (_muzzleFlashInstance == null) return;

            if (Application.isPlaying)
                Destroy(_muzzleFlashInstance);
            else
                DestroyImmediate(_muzzleFlashInstance);
        }
        
        private void Update()
        {
            if (!_isRecoiling) return;

            _recoilTimer += Time.deltaTime;
            float halfDuration = recoilDuration * 0.5f;

            if (_recoilTimer <= halfDuration)
            {
                // Push back phase: move towards -localX up to maxRecoilDistance
                float t = _recoilTimer / halfDuration;
                float targetX = -Mathf.Lerp(0f, maxRecoilDistance, t);
                transform.localPosition = new Vector3(targetX, 0f, 0f);

                // Capture the furthest point reached when we flip
                _recoilPeakDistance = Mathf.Abs(transform.localPosition.x);
            }
            else
            {
                // Return phase: Lerp from peak back to zero in the remaining halfDuration
                float returnElapsed = _recoilTimer - halfDuration;
                float t = Mathf.Clamp01(returnElapsed / halfDuration);
                float startX = -_recoilPeakDistance;
                transform.localPosition = new Vector3(Mathf.Lerp(startX, 0f, t), 0f, 0f);

                if (t >= 1f)
                {
                    transform.localPosition = Vector3.zero;
                    _isRecoiling = false;
                }
            }
        }
        
        
    }
}
