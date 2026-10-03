using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// The hero you never control: walks forward on their own, hops up one-tile ledges, turns around at walls,
    /// falls through holes and rides along when you slide the room they're in. You keep them alive by moving
    /// the world around them.
    /// </summary>
    public class TrekHero : MonoBehaviour
    {
        public const float Speed = 2.6f;
        /// <summary>Player-chosen walking speed (1x, 2x, 3x) from the HUD.</summary>
        public static float SpeedMul = 1f;
        /// <summary>The route to the trophy is complete: the hero breaks into a run for the home stretch.</summary>
        public bool Rushing { get; set; }
        float Pace => SpeedMul * (Rushing ? 1.6f : 1f);

        /// <summary>Turn to face a direction (the board steers the hero along a finished route).</summary>
        public void FaceTowards(float dir)
        {
            if (Mathf.Approximately(Mathf.Sign(dir), Facing)) return;
            Facing = Mathf.Sign(dir);
            m_TurnCooldown = 0.15f;
            SetSquash(1.12f, 0.92f);
        }
        const float Width = 0.62f, Height = 1.4f;

        public Rigidbody2D Body { get; private set; }
        public float Facing { get; private set; } = 1f;
        public bool Dead { get; private set; }
        public bool Won { get; private set; }
        /// <summary>Walking (false during the countdown, the freeze booster and while carried by a sliding room).</summary>
        public bool Moving { get; set; }
        public bool Carried { get; private set; }

        CapsuleCollider2D m_Col;
        SpriteAnim m_Anim;
        Sprite[] m_Idle, m_Run, m_Jump, m_Fall, m_Hit;
        Transform m_Visual;
        Vector2 m_Squash = Vector2.one, m_SquashVel;
        bool m_Grounded, m_WasGrounded;
        float m_TurnCooldown, m_StepCooldown;
        float m_KillY;
        ContactFilter2D m_Solid;
        readonly Collider2D[] m_Hits = new Collider2D[4];

        public Bounds Bounds => m_Col.bounds;
        public float FeetY => transform.position.y - Height * 0.5f;

        public static TrekHero Spawn(Transform parent, Vector2 feet, float facing, float killY, float light)
        {
            var go = new GameObject("Hero") { layer = Layers.Player };
            go.transform.SetParent(parent, false);
            go.transform.position = feet + Vector2.up * (Height * 0.5f);
            var h = go.AddComponent<TrekHero>();
            h.Facing = facing;
            h.m_KillY = killY;
            h.Build(light);
            return h;
        }

        void Build(float light)
        {
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 3.2f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            m_Col = gameObject.AddComponent<CapsuleCollider2D>();
            m_Col.size = new Vector2(Width, Height);
            m_Col.direction = CapsuleDirection2D.Vertical;
            m_Col.sharedMaterial = new PhysicsMaterial2D("Hero") { friction = 0f, bounciness = 0f };

            m_Visual = new GameObject("Visual").transform;
            m_Visual.SetParent(transform, false);
            m_Visual.localPosition = new Vector3(0f, -Height * 0.5f, 0f);
            var sgo = new GameObject("Sprite");
            sgo.transform.SetParent(m_Visual, false);
            sgo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var sr = sgo.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = GameAssets.I.spriteLit;
            sr.sortingOrder = Sort.Player;
            m_Anim = sgo.AddComponent<SpriteAnim>();
            var c = LevelCatalog.Characters[Mathf.Clamp(SaveSystem.Data.character, 0, LevelCatalog.Characters.Length - 1)];
            var a = GameAssets.I;
            m_Idle = a.Frames(c.folder + "/Idle"); m_Run = a.Frames(c.folder + "/Run"); m_Jump = a.Frames(c.folder + "/Jump");
            m_Fall = a.Frames(c.folder + "/Fall"); m_Hit = a.Frames(c.folder + "/Hit");
            m_Anim.Play(m_Idle);
            if (light > 0f) FxSystem.PointLight(transform, new Color(1f, 0.92f, 0.8f), light, 6f, 0.55f);
            m_Solid = new ContactFilter2D { useLayerMask = true, layerMask = Layers.SolidMask, useTriggers = false };
        }

        // ------------------------------------------------------------------ movement

        void FixedUpdate()
        {
            if (Dead || Won || Carried) return;
            float dt = Time.fixedDeltaTime;
            m_TurnCooldown -= dt;
            m_StepCooldown -= dt;
            var b = m_Col.bounds;
            var v = Body.linearVelocity;
            m_WasGrounded = m_Grounded;
            m_Grounded = v.y <= 0.5f && Overlap(new Vector2(b.center.x, b.min.y - 0.04f), new Vector2(Width * 0.7f, 0.06f));

            if (!Moving)
            {
                Body.linearVelocity = new Vector2(0f, Mathf.Min(v.y, 0f));
                return;
            }

            if (m_Grounded)
            {
                if (!m_WasGrounded) Land(v.y);
                float fx = b.center.x + Facing * (Width * 0.5f + 0.08f);
                bool low = Overlap(new Vector2(fx, b.min.y + 0.45f), new Vector2(0.1f, 0.5f));
                bool high = Overlap(new Vector2(fx, b.min.y + 1.5f), new Vector2(0.1f, 0.6f)); // starts above a one-tile ledge
                if (low && !high && m_StepCooldown <= 0f && !Overlap(new Vector2(b.center.x, b.max.y + 0.55f), new Vector2(Width * 0.8f, 0.9f)))
                {
                    // hop up a one-tile ledge
                    Body.position += new Vector2(Facing * 0.12f, 1.02f);
                    m_StepCooldown = 0.2f;
                    SetSquash(0.85f, 1.15f);
                }
                else if ((low || high) && m_TurnCooldown <= 0f)
                {
                    Facing = -Facing;
                    m_TurnCooldown = 0.15f;
                    SetSquash(1.15f, 0.9f);
                }
                v.x = Facing * Speed * Pace;
            }
            else v.x = Facing * Speed * Mathf.Min(Pace, 2f); // keeps drifting through holes so it lands clear of the edge
            if (v.y < -18f) v.y = -18f;
            Body.linearVelocity = v;

            if (transform.position.y < m_KillY) Die(true);
        }

        bool Overlap(Vector2 center, Vector2 size) => Physics2D.OverlapBox(center, size, 0f, m_Solid, m_Hits) > 0;

        void Land(float vy)
        {
            float k = Mathf.Clamp01(-vy / 16f);
            if (k > 0.2f)
            {
                SetSquash(1f + 0.3f * k, 1f - 0.25f * k);
                FxSystem.Dust(new Vector2(transform.position.x, FeetY), 3 + (int)(k * 4), 1.2f, 0.35f);
                AudioManager.I?.Play("land", 0.5f + k * 0.4f, 0.1f);
            }
        }

        void SetSquash(float x, float y) { m_Squash = new Vector2(x, y); m_SquashVel = Vector2.zero; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Dead) { m_Visual.Rotate(0f, 0f, 400f * dt); return; }
            m_Anim.Renderer.flipX = Facing < 0f;
            if (Won) return;
            m_Anim.Speed = 1f;
            if (!Moving || Carried) m_Anim.Play(m_Idle);
            else if (m_Grounded) { m_Anim.Play(m_Run); m_Anim.Speed = Mathf.Lerp(1f, Pace, 0.7f); }
            else m_Anim.Play(Body.linearVelocity.y > 0.1f ? m_Jump : m_Fall);
            m_Squash = Vector2.SmoothDamp(m_Squash, Vector2.one, ref m_SquashVel, 0.08f, Mathf.Infinity, dt);
            m_Visual.localScale = new Vector3(m_Squash.x, m_Squash.y, 1f);
        }

        // ------------------------------------------------------------------ contacts

        void OnTriggerEnter2D(Collider2D other) => Touch(other);
        void OnTriggerStay2D(Collider2D other) => Touch(other);

        void Touch(Collider2D other)
        {
            if (Dead || Won || Carried) return;
            var critter = other.GetComponent<TrekCritter>();
            if (critter != null)
            {
                if (critter.Dead) return;
                // squash it when landing from above, otherwise it gets you
                if (Body.linearVelocity.y < -1f && FeetY > other.bounds.center.y)
                {
                    critter.Squash();
                    Body.linearVelocity = new Vector2(Body.linearVelocity.x, 11f);
                    SetSquash(0.8f, 1.2f);
                    TrekBoard.I?.OnStomp();
                    return;
                }
                Die(false);
                return;
            }
            if (other.GetComponent<TrekDanger>() != null) { Die(false); return; }
            var coin = other.GetComponent<TrekCoin>();
            if (coin != null) { if (coin.Take()) TrekBoard.I?.OnCoin(other.transform.position); return; }
            var goal = other.GetComponent<TrekGoal>();
            if (goal != null) Win(goal);
        }

        void Win(TrekGoal goal)
        {
            Won = true;
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
            goal.Reach();
            m_Anim.Play(m_Idle);
            Tween.Delay(0.35f, () =>
            {
                if (this == null) return;
                var fx = new GameObject("Disappear");
                fx.transform.position = transform.position + Vector3.up * 0.2f;
                var sr = fx.AddComponent<SpriteRenderer>();
                sr.sharedMaterial = GameAssets.I.spriteGlow;
                sr.sortingOrder = Sort.Player + 1;
                m_Anim.Renderer.enabled = false;
                fx.AddComponent<SpriteAnim>().Play(GameAssets.I.Frames("PA1/Main Characters/Desappearing"), 20f, false, () => Destroy(fx));
            });
            TrekBoard.I?.OnHeroWon();
        }

        public void Die(bool fell)
        {
            if (Dead || Won) return;
            Dead = true;
            Moving = false;
            m_Col.enabled = false;
            m_Anim.Play(m_Hit, 20f, true);
            m_Anim.Renderer.sortingOrder = Sort.WorldUI;
            if (!fell)
            {
                Body.linearVelocity = new Vector2(-Facing * 2f, 12f);
                Body.gravityScale = 3.5f;
            }
            AudioManager.I?.Play("die", 1f, 0f);
            CameraRig.I?.Shake(0.35f);
            PostFX.I?.HitPulse();
            TrekBoard.I?.OnHeroDied();
        }

        // ------------------------------------------------------------------ riding a sliding room

        public void SetCarried(bool on, Transform room)
        {
            Carried = on;
            Body.simulated = !on;
            transform.SetParent(room, true);
            if (!on) Body.linearVelocity = Vector2.zero;
        }

        /// <summary>Back to a spot after a revive.</summary>
        public void Respawn(Vector2 feet, float facing)
        {
            Dead = false;
            m_Col.enabled = true;
            Body.gravityScale = 3.2f;
            Body.simulated = true;
            Body.linearVelocity = Vector2.zero;
            transform.position = feet + Vector2.up * (Height * 0.5f);
            Body.position = transform.position;
            m_Visual.localRotation = Quaternion.identity;
            m_Anim.Renderer.sortingOrder = Sort.Player;
            Facing = facing;
            AudioManager.I?.Play("appear", 0.7f, 0f);
        }
    }
}
