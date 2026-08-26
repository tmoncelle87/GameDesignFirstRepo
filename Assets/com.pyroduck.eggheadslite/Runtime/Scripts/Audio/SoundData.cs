using System;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Audio
{
    
    [Serializable]
    public struct SoundData
    {
        [Tooltip("Tag of the surface (e.g. Metal, Wood, Stone). Leave empty for default fallback.")]
        public string surfaceTag;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }
}