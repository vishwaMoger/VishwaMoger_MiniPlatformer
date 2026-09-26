using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Launches the player high into the air.</summary>
    public class Trampoline : MonoBehaviour
    {
        SpriteAnim m_Anim;
        float m_Cooldown;
        public const float LaunchVelocity = 23f;

        public static Trampoline Create(Transform parent, Vector2 cellBottom)
        {
            var go = EntityKit.Root("Trampoline", parent, cellBottom, Layers.Pickup);
            var t = go.AddComponent<Trampoline>();
            // 28x28 frame with the spring on its bottom rows
            t.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Traps/Trampoline/Idle"), Sort.Traps, new Vector2(0f, 0.875f));
            EntityKit.Box(go, new Vector2(1.4f, 0.5f), new Vector2(0f, 0.45f), true);
            return t;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (m_Cooldown > Time.time) return;
            var p = EntityKit.PlayerFrom(other);
            if (p == null || p.IsDead || p.Velocity.y > 1f || p.FeetY < transform.position.y + 0.2f) return;
            m_Cooldown = Time.time + 0.2f;
            p.Launch(new Vector2(0f, LaunchVelocity));
            var a = GameAssets.I;
            m_Anim.Play(a.Frames("PA1/Traps/Trampoline/Jump"), 24f, false, () => m_Anim.Play(a.Frames("PA1/Traps/Trampoline/Idle")), true);
            AudioManager.I?.Play("bounce", 0.8f, 0.05f);
            FxSystem.Dust((Vector2)transform.position + Vector2.up * 0.3f, 5, 1.5f, 0.4f);
        }
    }
}
