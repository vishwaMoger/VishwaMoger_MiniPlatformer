using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>Flag that raises when touched and becomes the respawn point.</summary>
    public class Checkpoint : MonoBehaviour
    {
        SpriteAnim m_Anim;
        Light2D m_Light;
        bool m_Active;
        Vector2 m_Feet;

        public static Checkpoint Create(Transform parent, Vector2 feetPos)
        {
            var go = EntityKit.Root("Checkpoint", parent, feetPos, Layers.Pickup);
            var c = go.AddComponent<Checkpoint>();
            c.m_Feet = feetPos;
            c.m_Anim = EntityKit.Sprite(go, GameAssets.I.Frames("PA1/Items/Checkpoints/Checkpoint/Checkpoint (No Flag)"), Sort.Items - 2, new Vector2(0.2f, 2f));
            EntityKit.Box(go, new Vector2(1.2f, 3f), new Vector2(0f, 1.5f), true);
            c.m_Light = FxSystem.PointLight(go.transform, new Color(1f, 0.85f, 0.5f), 0f, 4f, 0.5f, new Vector2(0.5f, 2.4f));
            return c;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (m_Active) return;
            var p = EntityKit.PlayerFrom(other);
            if (p == null || p.IsDead) return;
            m_Active = true;
            LevelSession.Current?.SetCheckpoint(m_Feet);
            AudioManager.I?.Play("checkpoint", 0.8f, 0f);
            FxSystem.Sparkle((Vector2)transform.position + new Vector2(0.6f, 2.5f), new Color(1f, 0.9f, 0.5f), 16, 4f, 0.6f, 0.7f);
            var a = GameAssets.I;
            m_Anim.Play(a.Frames("PA1/Items/Checkpoints/Checkpoint/Checkpoint (Flag Out)"), 20f, false,
                () => m_Anim.Play(a.Frames("PA1/Items/Checkpoints/Checkpoint/Checkpoint (Flag Idle)")));
            Tween.To(this, 0.6f, k => m_Light.intensity = k * 0.9f, null, false);
            UIRoot.I?.Toast("CHECKPOINT!");
        }
    }
}
