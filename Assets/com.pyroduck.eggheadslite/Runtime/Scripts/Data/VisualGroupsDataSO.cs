using System.Collections.Generic;
using com.pyroduck.eggheadslite.Runtime.Scripts.Enums;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Data
{
    [CreateAssetMenu(fileName = "BodyPartGroupSO", menuName = "PyroDuck/EggHeadsLite/Visual Group")]
    public class VisualGroupsDataSO : ScriptableObject
    {
        public string Name;
        public bool IsSubGroup;
        public List<VisualDataSO> VisualList;
        public VisualType VisualType;
    }
}

