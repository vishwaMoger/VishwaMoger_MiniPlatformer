using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Trigger child that forwards player contact to its enemy.</summary>
    public class EnemyHitbox : MonoBehaviour
    {
        public Enemy owner;

        void OnTriggerStay2D(Collider2D other)
        {
            if (owner == null) return;
            var p = EntityKit.PlayerFrom(other);
            if (p != null) owner.OnPlayerContact(p);
        }
    }
}
