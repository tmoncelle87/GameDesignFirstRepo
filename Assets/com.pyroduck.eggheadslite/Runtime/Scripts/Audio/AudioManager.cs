using System.Collections.Generic;
using UnityEngine;
using com.pyroduck.eggheadslite.Runtime.Scripts.Events;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Audio
{
    /// <summary>
    /// Pooled, event-driven audio playback.
    /// Drop one of these in the scene, assign an <see cref="AudioLibrarySO"/>, and any
    /// system can fire <see cref="PlaySoundEvent"/> or <see cref="PlaySoundAtEvent"/>
    /// without holding a direct reference.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class AudioManager : MonoBehaviour
    {
        // Tracked only to enforce a single active instance — never exposed publicly.
        // All interaction with this manager goes through PlaySoundEvent / PlaySoundAtEvent.
        private static AudioManager _instance;

        /// <summary>
        /// Resets the static singleton reference when the runtime starts.
        /// Required when "Enter Play Mode Options → Reload Domain" is disabled,
        /// otherwise the stale reference from the previous play session would
        /// survive and every new AudioManager would self-destruct on Awake.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null;
        }

        [Header("Library")]
        [SerializeField] private AudioLibrarySO library;

        [Header("Pool")]
        [Tooltip("Number of AudioSource components created at startup.")]
        [SerializeField] private int initialPoolSize = 8;

        [Tooltip("Hard cap of simultaneously playing sources. Extra requests are dropped.")]
        [SerializeField] private int maxPoolSize = 32;

        [Header("Behaviour")]
        [Tooltip("If true, this manager survives scene loads.")]
        [SerializeField] private bool dontDestroyOnLoad = false;

        [Tooltip("Skip identical SoundIds requested within this window (seconds). 0 disables.")]
        [SerializeField] private float duplicateGuardWindow = 0.04f;

        [Tooltip("Master multiplier applied on top of every sound's volume.")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;

        // ── Runtime ───────────────────────────────────────────────────────────

        private readonly Queue<AudioSource> _free            = new Queue<AudioSource>();
        private readonly List<AudioSource>  _busy            = new List<AudioSource>();
        private readonly Dictionary<SoundId, float> _lastPlayedAt = new Dictionary<SoundId, float>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            for (int i = 0; i < initialPoolSize; i++)
                _free.Enqueue(CreateSource());
        }

        private void OnEnable()
        {
            EventManager.Subscribe<PlaySoundEvent>(OnPlaySound);
            EventManager.Subscribe<PlaySoundAtEvent>(OnPlaySoundAt);
            EventManager.Subscribe<SetAudioMasterVolumeEvent>(OnSetMasterVolume);
        }

        private void OnDisable()
        {
            EventManager.Unsubscribe<PlaySoundEvent>(OnPlaySound);
            EventManager.Unsubscribe<PlaySoundAtEvent>(OnPlaySoundAt);
            EventManager.Unsubscribe<SetAudioMasterVolumeEvent>(OnSetMasterVolume);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            // Recycle finished sources back to the free queue.
            for (int i = _busy.Count - 1; i >= 0; i--)
            {
                var src = _busy[i];
                if (src == null)
                {
                    _busy.RemoveAt(i);
                    continue;
                }

                if (!src.isPlaying)
                {
                    ReturnToPool(src);
                    _busy.RemoveAt(i);
                }
            }
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnPlaySound(PlaySoundEvent evt)
        {
            PlayInternal(evt.Id, null, false, ResolveVolumeScale(evt.VolumeScale));
        }

        private void OnPlaySoundAt(PlaySoundAtEvent evt)
        {
            PlayInternal(evt.Id, evt.Position, true, ResolveVolumeScale(evt.VolumeScale));
        }

        private void OnSetMasterVolume(SetAudioMasterVolumeEvent evt)
        {
            masterVolume = Mathf.Clamp01(evt.Volume);
        }

        // ── Core ──────────────────────────────────────────────────────────────

        private void PlayInternal(SoundId id, Vector3? position, bool positional, float volumeScale)
        {
            if (id == SoundId.None || library == null) return;

            // Prevent amplitude doubling when many systems request the same id same frame.
            if (duplicateGuardWindow > 0f
                && _lastPlayedAt.TryGetValue(id, out float last)
                && Time.unscaledTime - last < duplicateGuardWindow)
                return;

            var entry = library.Get(id);
            if (entry == null)
            {
                Debug.LogWarning($"[AudioManager] No SoundEntry registered for '{id}'.");
                return;
            }

            var clip = entry.GetRandomClip();
            if (clip == null) return;

            var src = AcquireSource();
            if (src == null) return;

            ConfigureSource(src, entry, clip, positional, volumeScale);

            if (positional && position.HasValue)
            {
                src.transform.SetParent(null, true);
                src.transform.position = position.Value;
            }
            else
            {
                src.transform.SetParent(transform, false);
                src.transform.localPosition = Vector3.zero;
            }

            src.Play();
            _busy.Add(src);
            _lastPlayedAt[id] = Time.unscaledTime;
        }

        private void ConfigureSource(AudioSource src, SoundEntry entry, AudioClip clip,
                                     bool positional, float volumeScale)
        {
            src.clip                  = clip;
            src.volume                = Mathf.Clamp01(entry.GetRandomVolume() * volumeScale * masterVolume);
            src.pitch                 = entry.GetRandomPitch();
            src.outputAudioMixerGroup = entry.MixerGroup;
            src.spatialBlend          = positional ? entry.SpatialBlend : 0f;
            src.minDistance           = entry.MinDistance;
            src.maxDistance           = entry.MaxDistance;
            src.rolloffMode           = AudioRolloffMode.Linear;
            src.loop                  = false;
            src.dopplerLevel          = 0f;
        }

        private static float ResolveVolumeScale(float requested)
            => requested > 0f ? requested : 1f;

        // ── Pool ──────────────────────────────────────────────────────────────

        private AudioSource AcquireSource()
        {
            if (_free.Count > 0) return _free.Dequeue();

            int total = _busy.Count + _free.Count;
            if (total < maxPoolSize) return CreateSource();

            // Hard cap reached: optionally we could steal the oldest, but for now drop.
            return null;
        }

        private void ReturnToPool(AudioSource src)
        {
            src.Stop();
            src.clip = null;
            src.transform.SetParent(transform, false);
            src.transform.localPosition = Vector3.zero;
            _free.Enqueue(src);
        }

        private AudioSource CreateSource()
        {
            int index = _busy.Count + _free.Count + 1;
            var go = new GameObject($"AudioSource_{index:00}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop        = false;
            return src;
        }
    }
}
