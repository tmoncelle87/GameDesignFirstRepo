using UnityEngine;
using com.pyroduck.eggheadslite.Runtime.Scripts.Events;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Pool
{
    /// <summary>
    /// Wraps an explosion ParticleSystem. When the particle finishes playing,
    /// it returns itself to the ProjectilePool singleton automatically.
    /// Attach to an explosion effect prefab that has a ParticleSystem component.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class ExplosionEffect : MonoBehaviour, IPoolable
    {
        [Header("Audio")]
        [SerializeField] private AudioClip explosionSound;
        [SerializeField] private AudioSource audioSource;

        private ParticleSystem _ps;

        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();

            // Stop & clear so pooled instances don't auto-play on Awake
            var main = _ps.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        // ── IPoolable ─────────────────────────────────────────────────────────

        public void OnSpawn()
        {
            // Activated by ObjectPool; Play() must be called explicitly afterward.
        }

        public void OnDespawn()
        {
            if (_ps != null)
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDisable()
        {
            if (_ps != null)
                _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Plays the explosion effect at the requested position. Return to the
        /// ProjectilePool singleton happens automatically when playback stops.
        /// </summary>
        public void Play(Vector3 position)
        {
            transform.position = position;
            gameObject.SetActive(true);
            _ps.Play();

            if (explosionSound != null)
            {
                if (audioSource != null)
                    audioSource.PlayOneShot(explosionSound);
                else
                    AudioSource.PlayClipAtPoint(explosionSound, position);
            }
        }

        private void OnParticleSystemStopped()
        {
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            if (ProjectilePool.Instance != null)
                ProjectilePool.Instance.ReturnExplosion(this);
            else
                gameObject.SetActive(false);
        }
    }
}
