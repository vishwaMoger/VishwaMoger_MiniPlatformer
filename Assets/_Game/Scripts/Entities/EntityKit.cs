using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Small helpers shared by entity factories.</summary>
    public static class EntityKit
    {
        public static GameObject Root(string name, Transform parent, Vector2 pos, int layer = Layers.Default)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go;
        }

        public static SpriteAnim Sprite(GameObject owner, Sprite[] frames, int order, Vector2 localPos = default, bool glow = false, string name = "Sprite")
        {
            var go = new GameObject(name);
            go.layer = owner.layer;
            go.transform.SetParent(owner.transform, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = glow ? GameAssets.I.spriteGlow : GameAssets.I.spriteLit;
            sr.sortingOrder = order;
            var anim = go.AddComponent<SpriteAnim>();
            if (frames != null && frames.Length > 0) anim.Play(frames);
            return anim;
        }

        public static SpriteRenderer StaticSprite(Transform parent, Sprite sprite, int order, Vector2 worldPos, Color? color = null)
        {
            var go = new GameObject("Deco");
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = GameAssets.I.spriteLit;
            sr.sortingOrder = order;
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        public static BoxCollider2D Box(GameObject go, Vector2 size, Vector2 offset, bool trigger)
        {
            var c = go.AddComponent<BoxCollider2D>();
            c.size = size;
            c.offset = offset;
            c.isTrigger = trigger;
            return c;
        }

        public static Rigidbody2D Kinematic(GameObject go)
        {
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.useFullKinematicContacts = true;
            return rb;
        }

        /// <summary>Chain dots along a path, drawn behind movers so their route is readable.</summary>
        public static void Chain(Transform parent, Vector2 a, Vector2 b, string chainKey = "PA1/Traps/Platforms/Chain")
        {
            var s = GameAssets.I.Sprite(chainKey);
            if (s == null) return;
            float len = Vector2.Distance(a, b);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.5f));
            for (int i = 0; i <= n; i++)
                StaticSprite(parent, s, Sort.Chains, Vector2.Lerp(a, b, i / (float)n), new Color(1, 1, 1, 0.75f));
        }

    }
}
