using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Data
{
    [CreateAssetMenu(fileName = "BodyPartSO", menuName = "PyroDuck/EggHeadsLite/Visual Data")]
    public class VisualDataSO : ScriptableObject
    {
        public string Name;
        public GameObject BodyPartPrefab;
        public Sprite IconSprite;
        public bool isColorable;
    }
}

