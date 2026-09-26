using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Alternates between spikes out (dangerous from every side) and spikes in (stompable). Time your jump!</summary>
    public class Turtle : Enemy
    {
        bool m_Spiky = true;
        float m_Timer;
        bool m_Transition;

        public static Turtle Create(Transform parent, Vector2 feet)
        {
            var go = EntityKit.Root("Turtle", parent, feet);
            var t = go.AddComponent<Turtle>();
            t.SetupVisual("Turtle", 13f / 16f, new Vector2(2.1f, 1.1f), new Vector2(0f, 0.55f), "Idle 1");
            t.m_Timer = Random.Range(1.5f, 2.8f);
            t.Face(-1f);
            return t;
        }

        protected override bool CanBeStomped => !m_Spiky && !m_Transition;

        protected override void Update()
        {
            base.Update();
            if (Dead || m_Transition) return;
            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f) return;
            m_Transition = true;
            if (m_Spiky)
                anim.Play(F("Spikes in"), 20f, false, () => { m_Spiky = false; m_Transition = false; m_Timer = 2f; anim.Play(F("Idle 2")); });
            else
            {
                m_Spiky = true; // dangerous as soon as the spikes start coming out
                anim.Play(F("Spikes out"), 20f, false, () => { m_Transition = false; m_Timer = 2.4f; anim.Play(F("Idle 1")); });
            }
        }
    }
}
