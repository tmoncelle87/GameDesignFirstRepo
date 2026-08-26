using System.Collections.Generic;
using com.pyroduck.eggheadslite.Runtime.Scripts.Audio;
using com.pyroduck.eggheadslite.Runtime.Scripts.Character;
using com.pyroduck.eggheadslite.Runtime.Scripts.Utils;
using UnityEngine;
namespace com.pyroduck.eggheadslite.Runtime.Scripts.Combat
{
    /// <summary>
    /// Swing-based melee weapon. Damage scales with the angular speed (deg/s) of the
    /// parent weapon pivot, so faster swings deal more damage.
    /// Each press of the attack input defines a single "swing window" during which
    /// a given target can only be hit once.
    /// </summary>
    public class MeleeWeapon : Weapon, IMeleeWeapon
    {
        [Header("Damage Settings")]
        [SerializeField] private float baseDamage = 10f;

        [Tooltip("Final damage = baseDamage * (1 + angularSpeedDegPerSec * velocityMultiplier). Set velocityMultiplier to 0 to ignore swing speed.")]
        [SerializeField] private float velocityMultiplier = 0.01f;

        [Tooltip("Clamp applied to the derived swing multiplier to avoid one-hit kills.")]
        [SerializeField] private float maxDamageMultiplier = 3f;

        [Tooltip("Optional collision filter. Set to Everything (-1) to hit any layer that carries an IDamageable.")]
        [SerializeField] private LayerMask damageLayers = ~0;

        [Tooltip("How long a swing window lasts (seconds). Defaults to the attack cooldown.")]
        [SerializeField] private float swingWindow = -1f;

        [Header("Impact Sounds")]
        [Tooltip("First entry matching surfaceTag is used. Entries with an empty surfaceTag act as the fallback.")]
        [SerializeField] private List<SoundData> impactSounds = new List<SoundData>();

        [Header("Blood Effect")]
        [Tooltip("Blood effect prefab spawned at the impact point. ParticleSystem prefabs are recommended.")]
        [SerializeField] private GameObject bloodEffectPrefab;

        [Header("Air Swing Sound")]
        [SerializeField] private AudioClip airSwingClip;
        [SerializeField] private AudioSource airSwingAudioSource;
        [Tooltip("Minimum angular speed (deg/s) to play the swing sound.")]
        [SerializeField] private float minSwingSpeed = 150f;
        [Tooltip("Angular speed (deg/s) at which the swing sound reaches max volume/pitch.")]
        [SerializeField] private float maxSwingSpeed = 600f;
        [SerializeField] private float minSwingVolume = 0.15f;
        [SerializeField] private float maxSwingVolume = 1f;
        [SerializeField] private float minSwingPitch = 0.9f;
        [SerializeField] private float maxSwingPitch = 1.35f;

        /// <summary>Current angular speed of the parent pivot in degrees/second.</summary>
        private float _angularSpeedDegPerSec;
        private float _lastParentAngle;
        private bool _hasLastAngle;

        private readonly HashSet<int> _hitThisSwing = new HashSet<int>();
        private readonly List<Collider2D> _overlapBuffer = new List<Collider2D>(16);
        private float _swingEndsAt;
        private Collider2D _weaponCollider;

        protected override bool CanAttack(bool fireDown, bool fireHeld) => fireDown;

        private void Awake()
        {
            if (airSwingAudioSource != null)
            {
                airSwingAudioSource.loop = true;
                airSwingAudioSource.playOnAwake = false;
                airSwingAudioSource.clip = airSwingClip;
            }

            _weaponCollider = GetComponent<Collider2D>();
        }

        protected override void ExecuteAttack(Vector2 attackDirection)
        {
            _hitThisSwing.Clear();
            float window = swingWindow > 0f ? swingWindow : attackCooldown;
            _swingEndsAt = Time.time + window;

            // Targets already overlapping the trigger do not get another OnTriggerEnter; scan overlaps now.
            if (_weaponCollider != null && _weaponCollider.isTrigger)
            {
                var filter = new ContactFilter2D
                {
                    useTriggers = true,
                    useLayerMask = true,
                    layerMask = damageLayers
                };
                _overlapBuffer.Clear();
                int n = Physics2D.OverlapCollider(_weaponCollider, filter, _overlapBuffer);
                for (int i = 0; i < n; i++)
                {
                    var other = _overlapBuffer[i];
                    if (other == null) continue;
                    ApplyMeleeDamage(other.gameObject, _angularSpeedDegPerSec, other.ClosestPoint(transform.position));
                }
            }
        }

        public void ExecuteMeleeAttack(Vector2 attackDirection)
        {
            // Kept for IMeleeWeapon compatibility. Actual damage is handled by the collider.
        }

        private void Update()
        {
            var parent = transform.parent;
            if (parent == null)
            {
                _hasLastAngle = false;
                _angularSpeedDegPerSec = 0f;
                StopAirSwingSound();
                return;
            }

            float current = parent.localRotation.eulerAngles.z;
            if (current > 180f) current -= 360f;

            if (!_hasLastAngle)
            {
                _lastParentAngle = current;
                _hasLastAngle = true;
                _angularSpeedDegPerSec = 0f;
                return;
            }

            float delta = Mathf.DeltaAngle(_lastParentAngle, current);
            _lastParentAngle = current;

            float dt = Time.deltaTime;
            _angularSpeedDegPerSec = dt > 0f ? Mathf.Abs(delta) / dt : 0f;

            UpdateAirSwingSound();
        }

        private void OnDisable()
        {
            StopAirSwingSound();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            ApplyMeleeDamage(other.gameObject, _angularSpeedDegPerSec, other.ClosestPoint(transform.position));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other == null) return;
            var damageable = ResolveTargetDamageable(other.gameObject);
            if (damageable != null)
                _hitThisSwing.Remove(DedupKeyFor(damageable));
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null) return;
            ApplyMeleeDamage(other.gameObject, _angularSpeedDegPerSec, other.ClosestPoint(transform.position));
        }

        private void ApplyMeleeDamage(GameObject target, float angularSpeedDegPerSec, Vector2 point)
        {
            if (target == null) return;

            var myDamageable = GetComponentInParent<IDamageable>();
            var targetDamageable = ResolveTargetDamageable(target);

            if (myDamageable != null && targetDamageable != null && ReferenceEquals(myDamageable, targetDamageable)) return;

            if (((1 << target.layer) & damageLayers.value) == 0) return;

            if (targetDamageable == null) return;

            int key = DedupKeyFor(targetDamageable);
            if (!_hitThisSwing.Add(key)) return;

            float swingFactor = 1f + Mathf.Max(0f, angularSpeedDegPerSec) * velocityMultiplier;
            swingFactor = Mathf.Min(swingFactor, maxDamageMultiplier);
            float finalDamage = baseDamage * swingFactor;

            targetDamageable.TakeDamage(finalDamage, gameObject, point);
            PlayImpactSound(target, point);
            SpawnBloodEffect(point);
        }

        private IDamageable ResolveTargetDamageable(GameObject target)
        {
            var damageable = target.GetComponent<IDamageable>() ?? target.GetComponentInParent<IDamageable>();
            if (damageable != null) return damageable;
 
            var puppetChecker = target.GetComponent<PuppetActionChecker>()
                                ?? target.GetComponentInParent<PuppetActionChecker>();
            if (puppetChecker != null) return puppetChecker;

            return null;
        }

        private static int DedupKeyFor(IDamageable damageable)
        { 
            if (damageable is UnityEngine.Object unityObj && unityObj != null)
            {
                if (unityObj is Component component)
                {
                    var health = component.GetComponentInParent<HealthComponent>();
                    if (health != null)
                        return health.GetInstanceID();
                }

                return unityObj.GetInstanceID();
            }

            return damageable.GetHashCode();
        }
 
        private void PlayImpactSound(GameObject target, Vector2 point)
        {
            if (impactSounds == null || impactSounds.Count == 0) return;

            SoundData chosen = default;
            SoundData fallback = default;
            bool found = false;
            bool hasFallback = false;

            foreach (var data in impactSounds)
            {
                if (string.IsNullOrEmpty(data.surfaceTag))
                {
                    if (!hasFallback)
                    {
                        fallback = data;
                        hasFallback = true;
                    }
                    continue;
                }

                if (target.CompareTag(data.surfaceTag))
                {
                    chosen = data;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                if (!hasFallback) return;
                chosen = fallback;
            }

            if (chosen.clip == null) return;

            if (audioSource != null)
                audioSource.PlayOneShot(chosen.clip, chosen.volume > 0f ? chosen.volume : 1f);
            else
                AudioSource.PlayClipAtPoint(chosen.clip, point, chosen.volume > 0f ? chosen.volume : 1f);
        }

        private void UpdateAirSwingSound()
        {
            if (airSwingClip == null || airSwingAudioSource == null)
            {
                StopAirSwingSound();
                return;
            }

            float speed = _angularSpeedDegPerSec;
            
            // Scale up legacy values if they are still using the old linear velocity thresholds
            float minDeg = minSwingSpeed < 50f ? minSwingSpeed * 40f : minSwingSpeed; 
            float maxDeg = maxSwingSpeed < 50f ? maxSwingSpeed * 40f : maxSwingSpeed;

            if (speed < minDeg)
            {
                StopAirSwingSound();
                return;
            }

            if (!airSwingAudioSource.isPlaying)
                airSwingAudioSource.Play();

            float t = Mathf.InverseLerp(minDeg, maxDeg, speed);
            airSwingAudioSource.volume = Mathf.Lerp(minSwingVolume, maxSwingVolume, t);
            airSwingAudioSource.pitch = Mathf.Lerp(minSwingPitch, maxSwingPitch, t);
        }

        private void SpawnBloodEffect(Vector2 point)
        {
            if (bloodEffectPrefab == null) return;

            var go = Instantiate(bloodEffectPrefab, point, Quaternion.identity,
                SceneOrganizer.Get(SceneOrganizer.Buckets.Effects));
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
                Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax);
            else
                Destroy(go, 3f);
        }

        private void StopAirSwingSound()
        {
            if (airSwingAudioSource != null && airSwingAudioSource.isPlaying)
                airSwingAudioSource.Stop();
        }
    }
}
