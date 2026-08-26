using UnityEngine;
using com.pyroduck.eggheadslite.Runtime.Scripts.Character;
using com.pyroduck.eggheadslite.Runtime.Scripts.Events;
using com.pyroduck.eggheadslite.Runtime.Scripts.Enums;
using UnityEngine.EventSystems;
using com.pyroduck.eggheadslite.Runtime.Scripts.InputSystem;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Combat
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform weaponPlaceTransform;
        private Weapon _weapon;
        private IInputProvider _input;
        private Camera _mainCamera;
        private bool _fireRequestedFromUI;
        private bool _wasFireRequestedFromUI;
        private Quaternion _initialLocalRotation;
        private Vector2 _cachedAimDirection = Vector2.right;
        private Transform _weaponVisualParent;

        /// <summary>Optional; when present and dead, aiming and firing are disabled.</summary>
        private HealthComponent _health;

        private void Awake()
        {
            ResolveWeaponPlaceTransform();

            if (weaponPlaceTransform != null)
                _initialLocalRotation = weaponPlaceTransform.localRotation;
            RefreshMainCamera();
            ResolveWeaponVisualParent();

            _input = InputProviderService.Get();
            _health = GetComponentInParent<HealthComponent>();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || weaponPlaceTransform != null)
                return;

            var resolvedWeaponPlace = FindChildTransform("WeaponPlace");
            if (resolvedWeaponPlace == null)
                return;

            weaponPlaceTransform = resolvedWeaponPlace;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>
        /// Re-resolves <see cref="Camera.main"/>. Call after you swap active cameras
        /// (e.g. cut-scene or cinemachine brain activation) so aim keeps working.
        /// </summary>
        public void RefreshMainCamera()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_health != null && !_health.IsAlive)
                return;

            RotateWeaponToPointer();
            HandleFireInput();
        }

        private void OnEnable()
        {
            EventManager.Subscribe<FireButtonPressedEvent>(OnFireEvent);
            EventManager.Subscribe<FireButtonReleaseEvent>(OnFireReleaseEvent);
        }

        private void OnDisable()
        {
            // Provider is shared via InputProviderService — see BaseCharacterController.
            EventManager.Unsubscribe<FireButtonPressedEvent>(OnFireEvent);
            EventManager.Unsubscribe<FireButtonReleaseEvent>(OnFireReleaseEvent);
        }

        private void OnFireEvent(FireButtonPressedEvent evt) => OnFireButtonPressed();
        private void OnFireReleaseEvent(FireButtonReleaseEvent evt) => OnFireButtonReleased();

        // Bind this method to a UI Button OnClick event.
        public void OnFireButtonPressed()
        {
            _fireRequestedFromUI = true;
        }

        public void OnFireButtonReleased()
        {
            _fireRequestedFromUI = false;
        }

        private void HandleFireInput()
        {
            if (_weapon == null || _weapon.gameObject == null)
                _weapon = GetComponentInChildren<Weapon>();

            if (_weapon == null || _input == null) return;

            // Block fire when pointer is over a UI element
            if (IsPointerOverUI()) return;

            bool uiFireDown = _fireRequestedFromUI && !_wasFireRequestedFromUI;
            bool uiFireHeld = _fireRequestedFromUI;
            _wasFireRequestedFromUI = _fireRequestedFromUI;

            bool pcFireDown = !Application.isMobilePlatform && _input.GetFireDown();
            bool pcFireHeld = !Application.isMobilePlatform && _input.GetFireHeld();

            bool isThrowable = _weapon is IThrowableWeapon;

            bool effectiveFireDown = pcFireDown || uiFireDown;
            bool effectiveFireHeld = isThrowable ? false : (pcFireHeld || uiFireHeld);

            if (!effectiveFireDown && !effectiveFireHeld) return;

            _weapon.TryAttack(_cachedAimDirection, effectiveFireDown, effectiveFireHeld);
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

#if ENABLE_INPUT_SYSTEM
            if (Application.isMobilePlatform)
            {
                var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
                if (touchscreen != null)
                {
                    int touchId = touchscreen.primaryTouch.touchId.ReadValue();
                    return EventSystem.current.IsPointerOverGameObject(touchId);
                }
                return false;
            }
#else
            if (Application.isMobilePlatform)
            {
                if (Input.touchCount > 0)
                    return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
                return false;
            }
#endif
            return EventSystem.current.IsPointerOverGameObject();
        }

        private void RotateWeaponToPointer()
        {
            // Camera.main can become null when the tagged camera is deactivated or destroyed
            // (e.g. scene transitions). Try to recover instead of silently disabling aiming.
            if (_mainCamera == null) RefreshMainCamera();
            if (_mainCamera == null || weaponPlaceTransform == null || _input == null)
            {
                return;
            }

            if (!_input.IsPointerActive())
            {
                return;
            }

            Vector2 pointerScreenPos = _input.GetPointerPosition();

            // Mobile: Avoid aiming at UI buttons
            if (Application.isMobilePlatform && EventSystem.current != null)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Touchscreen.current != null &&
                    EventSystem.current.IsPointerOverGameObject(UnityEngine.InputSystem.Touchscreen.current.primaryTouch
                        .touchId.ReadValue()))
                    return;
#else
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return;
#endif
            }

            Vector3 pointerWorldPos = _mainCamera.ScreenToWorldPoint(
                new Vector3(pointerScreenPos.x, pointerScreenPos.y,
                    Mathf.Abs(_mainCamera.transform.position.z - weaponPlaceTransform.position.z))
            );

            Vector2 direction = (pointerWorldPos - weaponPlaceTransform.position);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            _cachedAimDirection = direction.normalized;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bool isLookingLeft = direction.x < 0f;
            float xRotation = isLookingLeft ? 180f : 0f;
            float zRotation = isLookingLeft ? -angle : angle;
            weaponPlaceTransform.rotation = Quaternion.Euler(xRotation, 0f, zRotation);
            SyncWeaponVisualParent();
        }

        private void ResolveWeaponVisualParent()
        {
            var visualController = GetComponent<CharacterVisualController>()
                                   ?? GetComponentInChildren<CharacterVisualController>(true);
            if (visualController == null || visualController.VisualMappings == null) return;

            foreach (var mapping in visualController.VisualMappings)
            {
                if (mapping.Type == VisualType.Weapon)
                {
                    _weaponVisualParent = mapping.Parent;
                    return;
                }
            }
        }

        public void SetWeaponPlaceTransform(Transform weaponPlace)
        {
            if (weaponPlace == null)
                return;

            weaponPlaceTransform = weaponPlace;
            _initialLocalRotation = weaponPlaceTransform.localRotation;
            ResolveWeaponVisualParent();
            SyncWeaponVisualParent();
        }

        private void ResolveWeaponPlaceTransform()
        {
            if (weaponPlaceTransform != null)
                return;

            weaponPlaceTransform = FindChildTransform("WeaponPlace");
            if (weaponPlaceTransform == null)
                Debug.LogWarning($"{name}: WeaponPlace transform is not assigned and could not be found.");
        }

        private Transform FindChildTransform(string childName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private void SyncWeaponVisualParent()
        {
            if (_weaponVisualParent == null)
                ResolveWeaponVisualParent();

            if (_weaponVisualParent == null || weaponPlaceTransform == null || _weaponVisualParent == weaponPlaceTransform)
                return;

            _weaponVisualParent.SetPositionAndRotation(weaponPlaceTransform.position, weaponPlaceTransform.rotation);
        }

        public void ResetWeaponRotation()
        {
            if (weaponPlaceTransform != null)
            {
                weaponPlaceTransform.localRotation = _initialLocalRotation;
            }
        }
    }
}
