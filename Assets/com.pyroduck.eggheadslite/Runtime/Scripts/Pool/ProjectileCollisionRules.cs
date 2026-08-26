using System.Collections.Generic;
using UnityEngine;

namespace com.pyroduck.eggheadslite.Runtime.Scripts.Pool
{
    internal static class ProjectileCollisionRules
    {
        private const string BulletLayerName = "Bullet";
        private const string ThrowableLayerName = "Throwable";

        private static bool _layerCollisionConfigured;

        public static void EnsureBulletThrowableIgnored()
        {
            if (_layerCollisionConfigured) return;
            _layerCollisionConfigured = true;

            int bulletLayer = LayerMask.NameToLayer(BulletLayerName);
            int throwableLayer = LayerMask.NameToLayer(ThrowableLayerName);
            if (bulletLayer < 0 || throwableLayer < 0) return;

            Physics2D.IgnoreLayerCollision(bulletLayer, throwableLayer, true);
        }

        public static bool IsProjectileLike(Collider2D collider)
        {
            if (collider == null) return false;

            return collider.GetComponentInParent<Projectile>() != null
                   || collider.GetComponentInParent<ThrowableProjectile>() != null;
        }

        public static void IgnoreCollision(
            Collider2D ownCollider,
            Collider2D otherCollider,
            List<Collider2D> ignoredColliders)
        {
            if (ownCollider == null || otherCollider == null) return;

            Physics2D.IgnoreCollision(ownCollider, otherCollider, true);
            ignoredColliders?.Add(otherCollider);
        }

        public static void IgnoreCollisions(
            Collider2D[] ownColliders,
            Collider2D otherCollider,
            List<Collider2D> ignoredColliders)
        {
            if (ownColliders == null || otherCollider == null) return;

            foreach (var ownCollider in ownColliders)
            {
                IgnoreCollision(ownCollider, otherCollider, ignoredColliders);
            }
        }
    }
}
