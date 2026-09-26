using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>
    /// Final boss: a giant skull that ricochets around the arena. Stomp it 6 times.
    /// Phase 2 (half health) turns it red, faster, and trailing embers.
    /// </summary>
    public class SkullBoss : Enemy
    {
        public const int MaxHp = 6;
        public int Hp => hp;
        float m_Speed = 4.2f;
        float m_Invuln;
        bool m_Active;
        bool m_Phase2;
        Light2D m_Light;
        float m_Ember;
        const float Scale = 1.35f;

        public static SkullBoss Create(Transform parent, Vector2 center)
        {
            var go = EntityKit.Root("SkullBoss", parent, center, Layers.Enemy);
            var s = go.AddComponent<SkullBoss>();
            s.hp = MaxHp;
            s.SetupVisual("Skull", 0f, new Vector2(2.6f, 2.6f), Vector2.zero, "Idle 1");
            s.anim.transform.localScale = Vector3.one * Scale;
            s.rb = go.AddComponent<Rigidbody2D>();
            s.rb.gravityScale = 0f;
            s.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            s.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            s.rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = 1.45f;
            c.sharedMaterial = new PhysicsMaterial2D { bounciness = 1f, friction = 0f };
            s.body = c;
            s.m_Light = FxSystem.PointLight(go.transform, new Color(0.7f, 0.6f, 1f), 0.8f, 5f, 0.5f);
            return s;
        }

        public void Activate()
        {
            if (m_Active) return;
            m_Active = true;
            rb.linearVelocity = new Vector2(-1f, 0.8f).normalized * m_Speed;
            AudioManager.I?.PlayMusic("Music/boss", 0.6f);
            UIRoot.I?.ShowBossBar(true);
        }

        void OnCollisionEnter2D(Collision2D c)
        {
            if (Dead || !m_Active) return;
            var n = c.GetContact(0).normal;
            string key = m_Phase2 ? "Hit Wall 2" : "Hit Wall 1";
            anim.Play(F(key), 20f, false, () => anim.Play(F(m_Phase2 ? "Idle 2" : "Idle 1")), true);
            FxSystem.Dust(c.GetContact(0).point, 6, 2f, 1f);
            CameraRig.I?.Shake(0.12f);
            AudioManager.I?.Play("slam", 0.35f, 0.1f);
        }

        protected override bool CanBeStomped => m_Invuln <= 0f;
        protected override bool IsHarmful => m_Invuln <= 0f;

        protected override void Stomped(PlayerController p)
        {
            p.Bounce(15f, 18f);
            FxSystem.HitStop(0.12f);
            CameraRig.I?.Shake(0.4f);
            AudioManager.I?.Play("bosshit", 1f);
            PostFX.I?.GlowPulse();
            m_Invuln = 1.2f;
            TakeHit();
            UIRoot.I?.SetBossHealth(hp / (float)MaxHp);
        }

        protected override void OnHurt()
        {
            anim.Play(F("Hit"), 20f, false, () => anim.Play(F(m_Phase2 ? "Idle 2" : "Idle 1")));
            m_Speed += 0.7f;
            if (!m_Phase2 && hp <= MaxHp / 2)
            {
                m_Phase2 = true;
                m_Speed += 1.2f;
                m_Light.color = new Color(1f, 0.4f, 0.3f);
                UIRoot.I?.Toast("THE SKULL KING IS FURIOUS!");
            }
            // push away from the player so it doesn't immediately re-hit them
            var p = Player;
            if (p != null)
            {
                Vector2 away = ((Vector2)transform.position - (Vector2)p.transform.position).normalized;
                if (away.y > -0.2f) away.y = -0.6f;
                rb.linearVelocity = away.normalized * m_Speed;
            }
        }

        protected override void Die()
        {
            Dead = true;
            hitbox.enabled = false;
            body.enabled = false;
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            UIRoot.I?.ShowBossBar(false);
            AudioManager.I?.Play("bosshit", 1f, 0f, 0.7f);
            CameraRig.I?.Shake(0.8f);
            FxSystem.HitStop(0.3f);
            anim.Play(F("Hit"), 12f, true);
            Tween.To(this, 1.4f, k =>
            {
                anim.transform.localScale = Vector3.one * Scale * (1f + k * 0.4f);
                sr.color = new Color(1f, 1f, 1f, 1f - k);
                if (Random.value < 0.5f) FxSystem.Sparkle((Vector2)transform.position + Random.insideUnitCircle * 1.5f, new Color(1f, 0.6f, 0.3f), 3, 5f, 0.8f, 0.6f);
            }, x => x, false, () =>
            {
                FxSystem.Confetti(transform.position, 120, 14f);
                FxSystem.Debris(transform.position, new Color(0.85f, 0.85f, 0.95f), 30, 8f, 0.35f);
                AudioManager.I?.PlayMusic(LevelSession.Current != null ? LevelSession.Current.world.music : "Music/stage2", 1.2f);
                LevelSession.Current?.end?.Reveal();
                UIRoot.I?.Toast("VICTORY! GRAB THE TROPHY!");
                Destroy(gameObject, 0.1f);
            });
        }

        protected override void Update()
        {
            base.Update();
            if (Dead) return;
            float dt = Time.deltaTime;
            if (!m_Active)
            {
                var p = Player;
                if (p != null && Vector2.Distance(p.transform.position, transform.position) < 13f) Activate();
                anim.transform.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 2f) * 0.2f, 0f);
                return;
            }
            m_Invuln -= dt;
            sr.color = m_Invuln > 0f ? new Color(1f, 1f, 1f, Mathf.Repeat(Time.time * 12f, 1f) > 0.5f ? 1f : 0.4f) : Color.white;
            if (m_Phase2)
            {
                m_Ember -= dt;
                if (m_Ember <= 0f)
                {
                    m_Ember = 0.05f;
                    FxSystem.Sparkle((Vector2)transform.position + Random.insideUnitCircle * 1.2f, new Color(1f, 0.45f, 0.2f), 1, 1f, 0.5f, 0.6f);
                }
            }
        }

        void FixedUpdate()
        {
            if (Dead || !m_Active) return;
            // keep a constant speed and avoid getting stuck in purely horizontal/vertical bounces
            var v = rb.linearVelocity;
            if (v.sqrMagnitude < 0.01f) v = new Vector2(1f, 1f);
            v = v.normalized;
            if (Mathf.Abs(v.x) < 0.35f) v.x = Mathf.Sign(v.x == 0 ? 1 : v.x) * 0.35f;
            if (Mathf.Abs(v.y) < 0.35f) v.y = Mathf.Sign(v.y == 0 ? 1 : v.y) * 0.35f;
            rb.linearVelocity = v.normalized * m_Speed;
            Face(v.x);
        }
    }
}
