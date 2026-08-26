using System.Collections.Generic;
using com.pyroduck.eggheadslite.Runtime.Scripts.Data;
using com.pyroduck.eggheadslite.Runtime.Scripts.Enums;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Animations;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Character
{
    public class CharacterVisualController : MonoBehaviour
    {
        public Transform BodyRoot;
        public Transform BodyVisual;
        public GameObject BodyParent;
        public GameObject RotatingBodyParent;
        public GameObject DeathEyesParent;
        public GameObject EyesParent;
        public List<VisualMapping> VisualMappings;

        public void SetAnimatedBody(Transform animatedBodyParent)
        {
            BodyParent.GetComponent<RotationConstraint>().SetSource(0,
                new ConstraintSource { sourceTransform = animatedBodyParent, weight = 1 });
            BodyParent.GetComponent<ScaleConstraint>()
                .SetSource(0, new ConstraintSource { sourceTransform = animatedBodyParent, weight = 1 });
        }

        public void SetDeathEyes(bool isDead)
        {
            DeathEyesParent.SetActive(isDead);
            EyesParent.SetActive(!isDead);
        }

        /// <summary>
        /// Preferred API — attaches a <see cref="VisualInstanceTag"/> on the spawned
        /// instance so <c>CharacterSerializer</c> can round-trip the exact source asset.
        /// </summary>
        public void CreateItem([CanBeNull] VisualDataSO data, VisualType visualType)
        {
            if (data == null)
            {
                CreateItem((GameObject)null, visualType);
                return;
            }

            CreateItem(data.BodyPartPrefab, visualType);
            var spawned = GetCurrentVisual(visualType);
            if (spawned != null)
            {
                var tag = spawned.GetComponent<VisualInstanceTag>();
                if (tag == null) tag = spawned.AddComponent<VisualInstanceTag>();
                tag.Bind(data);
            }
        }

        public void CreateItem(GameObject visualPrefab, VisualType visualType)
        {
            foreach (var visualMapping in VisualMappings)
            {
                if (visualMapping.Type == visualType)
                {
                    if (visualMapping.Parent == null)
                    {
                        Debug.LogWarning($"Parent transform for visual type {visualType} is not assigned.");
                    }
                    else
                    {
                        //Clear child in visualMapping.Parent
                        if (visualMapping.CurrentVisual != null)
                        {
                            DestroyVisual(visualMapping.CurrentVisual);
                            visualMapping.CurrentVisual = null;
                        }
                        else
                        {
                            if (visualMapping.Parent.childCount > 0)
                            {
                                foreach (Transform child in visualMapping.Parent)
                                {
                                    DestroyVisual(child.gameObject);
                                }
                            }
                        }

                        if (visualPrefab != null)
                        {
#if UNITY_EDITOR
                            visualMapping.CurrentVisual = !Application.isPlaying
                                ? (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, visualMapping.Parent)
                                : Instantiate(visualPrefab, visualMapping.Parent);
#else
                            visualMapping.CurrentVisual = Instantiate(visualPrefab, visualMapping.Parent);
#endif
                            visualMapping.CurrentVisual.transform.localPosition = Vector3.zero;
                        }
                    }

                    return;
                }
            }
        }

        private static void DestroyVisual(GameObject visual)
        {
            if (visual == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(visual);
            else
                Destroy(visual);
#else
            Destroy(visual);
#endif
        }

        public GameObject GetCurrentVisual(VisualType visualType)
        {
            foreach (var visualMapping in VisualMappings)
            {
                if (visualMapping.Type == visualType)
                {
                    return visualMapping.CurrentVisual;
                }
            }

            return null;
        }

        public void SetRotatingBody(Transform rotatingBodyParent)
        {
            RotatingBodyParent.GetComponent<RotationConstraint>().SetSource(0,
                new ConstraintSource { sourceTransform = rotatingBodyParent, weight = 1 });
        }

        public void LookBack()
        {
            BodyRoot.localScale = new Vector3(-1, 1, 1);
            BodyVisual.localScale = new Vector3(-1, 1, 1);
        }

        public void LookForward()
        {
            BodyRoot.localScale = new Vector3(1, 1, 1);
            BodyVisual.localScale = new Vector3(1, 1, 1);
        }
    }

    [System.Serializable]
    public class VisualMapping
    {
        public VisualType Type;
        public Transform Parent;
        public GameObject CurrentVisual;
    }
}
