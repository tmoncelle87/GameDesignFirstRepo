using System.Collections.Generic;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Data
{
    [CreateAssetMenu(fileName = "EggHeadDatabase", menuName = "PyroDuck/EggHeadsLite/EggHead Database")]
    public class EggHeadDatabaseSO : ScriptableObject
    {
        [Tooltip("Contains all the major visual categories, such as Eyes, Hair, Hats, etc.")]
        public List<VisualGroupsDataSO> visualGroups = new List<VisualGroupsDataSO>();
        
        [Tooltip("All body parts available for generation/validation.")]
        public List<VisualDataSO> allVisuals = new List<VisualDataSO>();

        [Tooltip("Playable character prefabs available to the runtime character selector.")]
        public List<GameObject> characterPrefabs = new List<GameObject>();

        public VisualGroupsDataSO GetGroup(Enums.VisualType type)
        {
            return visualGroups.Find(g => g != null && g.VisualType == type);
        }
    }
}

