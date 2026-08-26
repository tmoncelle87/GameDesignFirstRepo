using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Character
{
    public class ShadowGroundSnapper : MonoBehaviour
    {
        [SerializeField] private Transform _shadow;
        [SerializeField] private Transform _shadowVisual;
        [SerializeField] private Vector2 _offset;

        [SerializeField] private float disappearDistance = 5f;
        [SerializeField] private float maxSize = 0.6f;

        private int _groundMask;
        private bool _ready;

        private void Awake()
        {
            _groundMask = LayerMask.GetMask("Ground");
            if (_groundMask == 0)
                Debug.LogWarning($"[ShadowGroundSnapper] No layer named 'Ground' found on '{name}'. " +
                                 "Create it in Project Settings → Tags & Layers and assign ground colliders to it.");

            if (_shadow == null || _shadowVisual == null)
                Debug.LogWarning($"[ShadowGroundSnapper] '_shadow' or '_shadowVisual' is not assigned on '{name}'. " +
                                 "The component will be disabled.");

            _ready = _shadow != null && _shadowVisual != null;
        }

        private void Update()
        {
            if (!_ready || _groundMask == 0) return;

            var hit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Infinity, _groundMask);

            if (hit.collider != null)
            {
                float distanceToGround   = hit.distance;
                float normalizedDistance = 1f - Mathf.Clamp01(distanceToGround / disappearDistance);
                _shadowVisual.localScale = Vector3.one * Mathf.Lerp(0f, maxSize, normalizedDistance);
                _shadow.position         = hit.point + _offset;
            }
            else
            {
                _shadowVisual.localScale = Vector3.zero;
            }
        }
    }
}
