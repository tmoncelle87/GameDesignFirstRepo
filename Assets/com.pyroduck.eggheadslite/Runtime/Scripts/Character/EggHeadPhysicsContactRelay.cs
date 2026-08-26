using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Character
{
    /// <summary>
    /// Forwards 2D physics callbacks to <see cref="EggHeadController"/> because Unity only
    /// invokes collision/trigger messages on the GameObject that owns the <see cref="Collider2D"/>,
    /// not on parent objects (where the controller typically lives).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class EggHeadPhysicsContactRelay : MonoBehaviour
    {
        private EggHeadController _eggHead;

        private void Awake()
        {
            _eggHead = GetComponentInParent<EggHeadController>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            _eggHead?.TryApplyHazardDamage(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            _eggHead?.TryApplyHazardDamage(other);
        }
    }
}
