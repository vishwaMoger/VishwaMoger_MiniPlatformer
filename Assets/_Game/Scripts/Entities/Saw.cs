using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Spinning saw blade; optionally travels back and forth along a chain.</summary>
    public class Saw : MonoBehaviour
    {
        public static GameObject Create(Transform parent, Vector2 pos, Vector2? pathEnd, float speed = 3.2f)
        {
            var go = EntityKit.Root("Saw", parent, pos, Layers.Hazard);
            var anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Saw/On"), Sort.Traps, default, true);
            anim.Speed = 1.5f;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 0.95f;
            c.isTrigger = true;
            go.AddComponent<Hazard>();
            if (pathEnd.HasValue)
            {
                EntityKit.Kinematic(go);
                EntityKit.Chain(parent, pos, pathEnd.Value, "PA1/Traps/Saw/Chain");
                go.AddComponent<PathMover>().Init(pos, pathEnd.Value, speed, 0.15f, false);
            }
            return go;
        }
    }
}
