using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Damages the player while overlapping (spikes, saws, fire, spiked balls...).</summary>
    public class Hazard : MonoBehaviour
    {
        public bool active = true;
        public Transform source;

        void OnTriggerStay2D(Collider2D other)
        {
            if (!active) return;
            var p = EntityKit.PlayerFrom(other);
            if (p != null) p.Damage((source != null ? source : transform).position);
        }

        public static GameObject Spikes(Transform parent, Vector2 cellCenter, int dir)
        {
            // dir: 0 up (floor), 1 down (ceiling), 2 left, 3 right
            var go = EntityKit.Root("Spikes", parent, cellCenter, Layers.Hazard);
            float rot = dir == 0 ? 0f : dir == 1 ? 180f : dir == 2 ? 90f : -90f;
            go.transform.rotation = Quaternion.Euler(0, 0, rot);
            EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Spikes/Idle"), Sort.Traps);
            EntityKit.Box(go, new Vector2(0.8f, 0.34f), new Vector2(0f, -0.3f), true);
            go.AddComponent<Hazard>();
            return go;
        }
    }
}
