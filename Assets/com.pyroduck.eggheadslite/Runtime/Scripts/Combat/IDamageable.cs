using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount, GameObject source,Vector2 hitPoint);
    }
}
