using System;
using System.Collections.Generic;
using com.pyroduck.eggheadslite.Runtime.Scripts.Character;
using com.pyroduck.eggheadslite.Runtime.Scripts.Data;
using TMPro;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.UI
{
    /// <summary>
    /// Populates a TMP_Dropdown from EggHeadDatabaseSO and applies the selected
    /// character prefab to EggHeadController.
    /// Attach to a Canvas GameObject that is active at game-start.
    /// </summary>
    public class CharacterSelectUI : MonoBehaviour
    {
        [SerializeField] private EggHeadDatabaseSO database;
        [SerializeField] private TMP_Dropdown characterDropdown;
        [SerializeField] private EggHeadController targetController;

        private readonly List<GameObject> _prefabs = new List<GameObject>();

        public static event Action<GameObject> OnCharacterSelected;

        private void Awake()
        {
            LoadPrefabsFromDatabase();
            PopulateDropdown();

            if (characterDropdown != null)
                characterDropdown.onValueChanged.AddListener(OnSelectionChanged);
        }

        private void Start()
        {
            var controller = ResolveTargetController();
            SyncDropdownWithActiveController();

            if (controller != null && controller.CharacterBasePrefab == null && _prefabs.Count > 0)
                OnSelectionChanged(0);
        }

        private void OnDestroy()
        {
            if (characterDropdown != null)
                characterDropdown.onValueChanged.RemoveListener(OnSelectionChanged);
        }

        private void LoadPrefabsFromDatabase()
        {
            _prefabs.Clear();
            if (database?.characterPrefabs != null)
            {
                foreach (var prefab in database.characterPrefabs)
                {
                    if (prefab != null && !_prefabs.Contains(prefab))
                        _prefabs.Add(prefab);
                }
            }

            if (_prefabs.Count == 0)
                Debug.LogWarning("[CharacterSelectUI] No character prefabs assigned in EggHeadDatabaseSO.");
        }

        private void PopulateDropdown()
        {
            if (characterDropdown == null) return;

            characterDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var p in _prefabs)
                options.Add(p.name);
            characterDropdown.AddOptions(options);
            characterDropdown.value = 0;
            characterDropdown.RefreshShownValue();
        }

        private void SyncDropdownWithActiveController()
        {
            if (characterDropdown == null || _prefabs.Count == 0) return;

            var controller = ResolveTargetController();
            if (controller == null || controller.CharacterBasePrefab == null) return;

            int index = FindPrefabIndex(controller.CharacterBasePrefab);
            if (index < 0) return;

            characterDropdown.SetValueWithoutNotify(index);
            characterDropdown.RefreshShownValue();
        }

        private int FindPrefabIndex(GameObject prefab)
        {
            int index = _prefabs.IndexOf(prefab);
            if (index >= 0) return index;

            for (int i = 0; i < _prefabs.Count; i++)
            {
                if (_prefabs[i] != null && _prefabs[i].name == prefab.name)
                    return i;
            }

            return -1;
        }

        private void OnSelectionChanged(int index)
        {
            if (_prefabs.Count == 0) return;

            index = Mathf.Clamp(index, 0, _prefabs.Count - 1);
            var selectedPrefab = _prefabs[index];
            if (selectedPrefab == null) return;

            var controller = ResolveTargetController();
            if (controller != null)
            {
                controller.SetCharacterPrefab(selectedPrefab);
            }
            else
            {
                Debug.LogWarning("[CharacterSelectUI] No EggHeadController found to apply the selected character.");
            }

            OnCharacterSelected?.Invoke(selectedPrefab);
        }

        private EggHeadController ResolveTargetController()
        {
            if (targetController != null)
                return targetController;

            targetController = FindFirstObjectByType<EggHeadController>();
            return targetController;
        }
    }
}
