using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>One-way platform that glides along a chain and carries the player.</summary>
    public class MovingPlatform : MonoBehaviour
    {
        public static GameObject Create(Transform parent, Vector2 pos, Vector2? pathEnd, string spriteKey)
        {
            var go = EntityKit.Root("MovingPlatform", parent, pos, Layers.OneWay);
            EntityKit.Sprite(go, GameAssets.I.Frames(spriteKey), Sort.OneWay + 2, new Vector2(0f, 0.25f));
            var col = EntityKit.Box(go, new Vector2(2f, 0.35f), new Vector2(0f, 0.32f), false);
            col.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 160f;
            eff.useSideFriction = false;
            EntityKit.Kinematic(go);
            go.AddComponent<MovingPlatform>();
            if (pathEnd.HasValue)
            {
                EntityKit.Chain(parent, pos + Vector2.up * 0.3f, pathEnd.Value + Vector2.up * 0.3f);
                go.AddComponent<PathMover>().Init(pos, pathEnd.Value, 2.8f, 0.5f, true);
            }
            return go;
        }
    }
}
