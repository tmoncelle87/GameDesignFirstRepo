using System.Collections.Generic;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Audio
{
    /// <summary>
    /// Asset that maps every <see cref="SoundId"/> to a <see cref="SoundEntry"/>.
    /// Create one via Assets > Create > PyroDuck > Audio > Audio Library and assign it
    /// to the AudioManager component in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "PyroDuck/EggHeadsLite/Audio Library")]
    public class AudioLibrarySO : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> entries = new List<SoundEntry>();

        private Dictionary<SoundId, SoundEntry> _lookup;

        public SoundEntry Get(SoundId id)
        {
            if (id == SoundId.None) return null;
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(id, out var entry);
            return entry;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<SoundId, SoundEntry>(entries.Count);
            foreach (var e in entries)
            {
                if (e == null || e.Id == SoundId.None) continue;
                _lookup[e.Id] = e; // last one wins on duplicates
            }
        }

        // Force lookup rebuild whenever the asset is edited in the inspector.
        private void OnValidate() => _lookup = null;
    }
}
