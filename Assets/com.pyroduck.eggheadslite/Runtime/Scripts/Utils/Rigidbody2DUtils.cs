using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Utils
{
    /// <summary>
    /// Thin compatibility layer that maps <see cref="Rigidbody2D.linearVelocity"/> (Unity 6+)
    /// onto <c>velocity</c> on older editors (Unity 2021.3 / 2022.3 LTS).
    /// Keeps the runtime package compiling across every version declared in
    /// <c>package.json</c>.
    /// </summary>
    public static class Rigidbody2DUtils
    {
        public static Vector2 GetLinearVelocity(this Rigidbody2D rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetLinearVelocity(this Rigidbody2D rb, Vector2 value)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = value;
#else
            rb.velocity = value;
#endif
        }
    }
}
