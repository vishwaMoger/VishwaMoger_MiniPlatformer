using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    public interface IMovingSurface { Vector2 Velocity { get; } }
    public interface IStandable { void OnStood(PlayerController p); }

    /// <summary>
    /// Tight, forgiving platformer controller: acceleration curves, coyote time, jump buffering, variable jump height,
    /// apex hang, double jump, wall slide / wall jump, air dash, moving-platform carry, one-way drop-through,
    /// knockback + invulnerability, and squash & stretch.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController I { get; private set; }

        // ---- tuning (units: tiles & seconds)
        const float MaxSpeed = 8.5f;
        const float GroundAccel = 95f, GroundDecel = 120f, AirAccel = 72f, AirDecel = 38f, TurnBoost = 1.7f;
        const float JumpVel = 15.2f, DoubleJumpVel = 13.2f, WallJumpX = 9.5f, WallJumpY = 14.2f;
        const float GravRise = 3.2f, GravFall = 5.0f, GravCut = 7.5f, GravApex = 2.1f, ApexBand = 1.6f;
        const float MaxFall = 20f, FastFall = 25f, WallSlideMax = 2.8f;
        const float CoyoteTime = 0.1f, BufferTime = 0.13f, WallCoyoteTime = 0.12f, WallJumpLockTime = 0.15f;
        const float DashSpeed = 20f, DashTime = 0.14f, DashCooldown = 0.3f;
        public const float ColliderWidth = 0.8f, ColliderHeight = 1.5f;

        public Rigidbody2D Body { get; private set; }
        public BoxCollider2D Col { get; private set; }
        public SpriteRenderer Sprite { get; private set; }
        public Vector2 Velocity => Body.linearVelocity;
        public float Facing { get; private set; } = 1f;
        public bool IsGrounded { get; private set; }
        public bool IsWallSliding { get; private set; }
        public bool IsDashing => m_DashTimer > 0f;
        public bool IsDead { get; private set; }
        public bool Invulnerable => m_Invuln > 0f || IsDead;
        public bool Controllable { get; set; } = true;
        public float FeetY => Col.bounds.min.y;
        public Vector2 PrevVelocity { get; private set; }

        Transform m_Visual;
        SpriteAnim m_Anim;
        Light2D m_Light;
        MaterialPropertyBlock m_Mpb;
        Sprite[] m_Idle, m_Run, m_Jump, m_Fall, m_Double, m_Wall, m_Hit;

        // input (captured in Update, consumed in FixedUpdate)
        float m_MoveX;
        bool m_JumpHeld, m_DownHeld;
        float m_JumpBuffer, m_DashBuffer;

        // state
        float m_Coyote, m_WallCoyote;
        int m_LastWallDir, m_WallDir;
        int m_AirJumps;
        bool m_CanDash = true;
        float m_DashTimer, m_DashCooldown, m_GhostTimer;
        int m_DashDir;
        float m_WallJumpLock;
        int m_WallJumpDir;
        float m_Stun, m_Invuln;
        float m_DoubleAnim;
        bool m_JumpCut;
        float m_JumpTimer;
        bool m_JustJumped;
        float m_WindTimer, m_WindTarget;
        float m_DropTimer;
        Collider2D m_DropCollider;
        float m_AirTime, m_RunDust;
        Vector2 m_PlatformVel;
        float m_LastPlatformVx;
        Collider2D m_GroundCol;
        Vector2 m_Squash = Vector2.one, m_SquashVel;
        readonly List<Collider2D> m_Hits = new List<Collider2D>(8);
        readonly Dictionary<Collider2D, IMovingSurface> m_SurfaceCache = new Dictionary<Collider2D, IMovingSurface>();
        ContactFilter2D m_GroundFilter;

        public static PlayerController Spawn(Transform parent, Vector2 feetPos, CharacterDef character, float lightIntensity)
        {
            var go = new GameObject("Player");
            go.layer = Layers.Player;
            go.transform.SetParent(parent, false);
            go.transform.position = feetPos + Vector2.up * (ColliderHeight * 0.5f);
            var p = go.AddComponent<PlayerController>();
            p.Build(character, lightIntensity);
            return p;
        }

        void Build(CharacterDef character, float lightIntensity)
        {
            I = this;
            Body = gameObject.AddComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.gravityScale = GravFall;
            Body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            Col = gameObject.AddComponent<BoxCollider2D>();
            Col.size = new Vector2(ColliderWidth, ColliderHeight);
            Col.edgeRadius = 0.02f;
            Col.sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };

            // visual root sits at the feet so squash & stretch pivots from the ground
            m_Visual = new GameObject("Visual").transform;
            m_Visual.SetParent(transform, false);
            m_Visual.localPosition = new Vector3(0f, -ColliderHeight * 0.5f, 0f);
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(m_Visual, false);
            spriteGo.transform.localPosition = new Vector3(0f, 1f, 0f); // 32px frame, feet on the bottom row
            Sprite = spriteGo.AddComponent<SpriteRenderer>();
            Sprite.sharedMaterial = GameAssets.I.spriteLit;
            Sprite.sortingOrder = Sort.Player;
            m_Anim = spriteGo.AddComponent<SpriteAnim>();
            m_Mpb = new MaterialPropertyBlock();

            var a = GameAssets.I;
            string f = character.folder + "/";
            m_Idle = a.Frames(f + "Idle"); m_Run = a.Frames(f + "Run"); m_Jump = a.Frames(f + "Jump");
            m_Fall = a.Frames(f + "Fall"); m_Double = a.Frames(f + "Double Jump"); m_Wall = a.Frames(f + "Wall Jump");
            m_Hit = a.Frames(f + "Hit");
            m_Anim.Play(m_Idle);

            if (lightIntensity > 0f)
                m_Light = FxSystem.PointLight(transform, new Color(1f, 0.92f, 0.8f), lightIntensity, 7.5f, 0.55f);

            m_GroundFilter = new ContactFilter2D { useLayerMask = true, layerMask = Layers.GroundMask, useTriggers = false };
            m_AirJumps = 1;
        }

        void OnDestroy() { if (I == this) I = null; }

        // ------------------------------------------------------------------ Update: input + visuals

        void Update()
        {
            float dt = Time.deltaTime;
            if (Controllable && !IsDead && Time.timeScale > 0f)
            {
                m_MoveX = GameInput.Horizontal;
                m_JumpHeld = GameInput.JumpHeld;
                m_DownHeld = GameInput.Down;
                if (GameInput.JumpDown) m_JumpBuffer = BufferTime;
                if (GameInput.DashDown) m_DashBuffer = 0.1f;
            }
            else
            {
                m_MoveX = 0f; m_JumpHeld = false; m_DownHeld = false;
            }
            m_JumpBuffer -= dt;
            m_DashBuffer -= dt;

            UpdateAnimation(dt);
        }

        void UpdateAnimation(float dt)
        {
            if (IsDead)
            {
                m_Anim.Play(m_Hit, 20f, true);
                m_Visual.Rotate(0, 0, 360f * dt);
                return;
            }

            if (Mathf.Abs(m_MoveX) > 0.01f && m_Stun <= 0f && m_WallJumpLock <= 0f && !IsDashing) Facing = Mathf.Sign(m_MoveX);
            if (IsWallSliding) Facing = -m_WallDir; // face away from the wall like Pixel Adventure's wall pose
            Sprite.flipX = Facing < 0f;

            if (m_Stun > 0f) m_Anim.Play(m_Hit, 20f, true);
            else if (IsWallSliding) m_Anim.Play(m_Wall);
            else if (IsGrounded)
            {
                float relVx = Body.linearVelocity.x - m_PlatformVel.x;
                m_Anim.Play(Mathf.Abs(relVx) > 0.6f || Mathf.Abs(m_MoveX) > 0.01f ? m_Run : m_Idle);
                m_Anim.Speed = m_Anim.Current == m_Run ? Mathf.Lerp(0.7f, 1.25f, Mathf.Abs(relVx) / MaxSpeed) : 1f;
            }
            else if (m_DoubleAnim > 0f) m_Anim.Play(m_Double, 22f, true);
            else m_Anim.Play(Body.linearVelocity.y > 0.1f ? m_Jump : m_Fall);
            if (!IsGrounded) m_Anim.Speed = 1f;

            // squash & stretch spring back to 1
            m_Squash = Vector2.SmoothDamp(m_Squash, Vector2.one, ref m_SquashVel, 0.08f, Mathf.Infinity, dt);
            float stretch = 1f;
            if (!IsGrounded && !IsWallSliding) stretch = 1f + Mathf.Clamp(Mathf.Abs(Body.linearVelocity.y) / 60f, 0f, 0.1f);
            m_Visual.localScale = new Vector3(m_Squash.x / Mathf.Sqrt(stretch), m_Squash.y * stretch, 1f);
            m_Visual.localRotation = Quaternion.identity;

            // invulnerability blink + hit flash
            if (m_Invuln > 0f)
            {
                bool on = Mathf.Repeat(Time.time * 14f, 1f) > 0.5f;
                Sprite.color = new Color(1f, 1f, 1f, on ? 1f : 0.35f);
            }
            else Sprite.color = Color.white;

            float flash = m_Stun > 0.2f ? 0.7f : (IsDashing ? 0.25f : 0f);
            if (flash > 0f)
            {
                m_Mpb.SetFloat("_FlashAmount", flash);
                m_Mpb.SetColor("_FlashColor", IsDashing ? new Color(0.7f, 0.95f, 1f) : Color.white);
                Sprite.SetPropertyBlock(m_Mpb);
            }
            else Sprite.SetPropertyBlock(null);
        }

        public void SetSquash(float x, float y) { m_Squash = new Vector2(x, y); m_SquashVel = Vector2.zero; }

        // ------------------------------------------------------------------ FixedUpdate: physics

        void FixedUpdate()
        {
            if (IsDead) return;
            float dt = Time.fixedDeltaTime;
            PrevVelocity = Body.linearVelocity;
            bool wasGrounded = IsGrounded;
            CheckGround();
            CheckWalls();

            m_JumpTimer += dt;
            m_Stun -= dt; m_Invuln -= dt; m_WallJumpLock -= dt; m_DoubleAnim -= dt;
            m_DashCooldown -= dt; m_WindTimer -= dt; m_DropTimer -= dt;
            if (m_DropTimer <= 0f && m_DropCollider != null)
            {
                Physics2D.IgnoreCollision(Col, m_DropCollider, false);
                m_DropCollider = null;
            }

            var v = Body.linearVelocity;

            if (IsGrounded)
            {
                m_Coyote = CoyoteTime;
                m_AirJumps = 1;
                m_CanDash = true;
                if (!wasGrounded) OnLand(PrevVelocity.y);
                m_AirTime = 0f;
            }
            else
            {
                m_Coyote -= dt;
                m_AirTime += dt;
            }
            if (m_WallDir != 0 && !IsGrounded) { m_WallCoyote = WallCoyoteTime; m_LastWallDir = m_WallDir; }
            else m_WallCoyote -= dt;

            // ---- dash
            if (m_DashBuffer > 0f && m_CanDash && m_DashCooldown <= 0f && m_Stun <= 0f && Controllable)
            {
                m_DashBuffer = 0f;
                StartDash();
            }
            if (m_DashTimer > 0f)
            {
                m_DashTimer -= dt;
                Body.gravityScale = 0f;
                Body.linearVelocity = new Vector2(m_DashDir * DashSpeed, 0f);
                m_GhostTimer -= dt;
                if (m_GhostTimer <= 0f) { m_GhostTimer = 0.025f; FxSystem.Ghost(Sprite, new Color(0.55f, 0.9f, 1f, 0.55f)); }
                if (m_DashTimer <= 0f) Body.linearVelocity = new Vector2(m_DashDir * MaxSpeed, 0f);
                return;
            }

            // ---- horizontal movement relative to whatever we're standing on
            float target = m_MoveX * MaxSpeed;
            if (m_WallJumpLock > 0f) target = m_WallJumpDir * MaxSpeed;
            float rel = v.x - m_LastPlatformVx;
            if (m_Stun <= 0f)
            {
                float accel;
                if (Mathf.Abs(target) > 0.01f)
                {
                    accel = IsGrounded ? GroundAccel : AirAccel;
                    if (Mathf.Sign(target) != Mathf.Sign(rel) && Mathf.Abs(rel) > 0.1f) accel *= TurnBoost;
                }
                else accel = IsGrounded ? GroundDecel : AirDecel;
                rel = Mathf.MoveTowards(rel, target, accel * dt);
            }
            v.x = rel + m_PlatformVel.x;
            m_LastPlatformVx = m_PlatformVel.x;

            // ---- jumping
            m_JustJumped = false;
            if (m_JumpBuffer > 0f && m_Stun <= 0f)
            {
                if (m_Coyote > 0f)
                {
                    if (m_DownHeld && m_GroundCol != null && m_GroundCol.gameObject.layer == Layers.OneWay) DropThrough();
                    else { v = DoJump(v, JumpVel); m_Coyote = 0f; }
                }
                else if (m_WallCoyote > 0f)
                {
                    v = new Vector2(-m_LastWallDir * WallJumpX, WallJumpY);
                    m_WallJumpLock = WallJumpLockTime;
                    m_WallJumpDir = -m_LastWallDir;
                    Facing = m_WallJumpDir;
                    m_WallCoyote = 0f;
                    m_AirJumps = 1;
                    m_CanDash = true;
                    m_JumpCut = true;
                    m_JumpTimer = 0f;
                    m_JumpBuffer = 0f;
                    SetSquash(0.75f, 1.3f);
                    FxSystem.Dust(new Vector2(transform.position.x + m_LastWallDir * 0.45f, transform.position.y), 4, 0.8f, 1.2f);
                    AudioManager.I?.Play("jump", 0.8f, 0.08f, 1.08f);
                }
                else if (m_AirJumps > 0)
                {
                    m_AirJumps--;
                    v = DoJump(v, DoubleJumpVel);
                    m_DoubleAnim = 0.3f;
                    FxSystem.DustRing(new Vector2(transform.position.x, FeetY), 10, 3.2f);
                    AudioManager.I?.Play("doublejump", 0.8f, 0.05f);
                }
            }

            // ---- variable jump height
            if (!m_JumpHeld && m_JumpCut && v.y > 0f && m_JumpTimer > 0.08f)
            {
                v.y *= 0.55f;
                m_JumpCut = false;
            }
            if (v.y <= 0f) m_JumpCut = false;

            // ---- gravity shaping (snappy fall, floaty apex while holding jump)
            float g;
            if (m_WindTimer > 0f) g = 1.2f;
            else if (Mathf.Abs(v.y) < ApexBand && m_JumpHeld && !IsGrounded) g = GravApex;
            else if (v.y > 0f) g = m_JumpHeld ? GravRise : GravCut;
            else g = GravFall;
            Body.gravityScale = g;

            // ---- wall slide / wind / terminal velocity
            if (IsWallSliding && v.y < -WallSlideMax)
            {
                v.y = Mathf.MoveTowards(v.y, -WallSlideMax, 80f * dt);
                if (Random.value < 0.25f) FxSystem.Dust(new Vector2(transform.position.x + m_WallDir * 0.45f, transform.position.y + 0.3f), 1, 0.3f, 0.2f);
            }
            if (m_WindTimer > 0f) v.y = Mathf.MoveTowards(v.y, m_WindTarget, 75f * dt);
            float maxFall = m_DownHeld ? FastFall : MaxFall;
            if (v.y < -maxFall) v.y = -maxFall;

            // ---- ride moving platforms vertically
            if (IsGrounded && !m_JustJumped && m_GroundCol != null && m_SurfaceCache.TryGetValue(m_GroundCol, out var surf) && surf != null)
                v.y = Mathf.Min(v.y, surf.Velocity.y);

            Body.linearVelocity = v;

            // ---- run dust
            if (IsGrounded && Mathf.Abs(rel) > 5f)
            {
                m_RunDust -= dt;
                if (m_RunDust <= 0f) { m_RunDust = 0.14f; FxSystem.Dust(new Vector2(transform.position.x - Facing * 0.3f, FeetY), 1, 0.6f, 0.4f); }
            }
        }

        Vector2 DoJump(Vector2 v, float vel)
        {
            v.y = vel + Mathf.Max(0f, m_PlatformVel.y);
            m_JumpBuffer = 0f;
            m_JumpCut = true;
            m_JumpTimer = 0f;
            m_JustJumped = true;
            IsGrounded = false;
            SetSquash(0.72f, 1.32f);
            if (m_Coyote > 0f)
            {
                FxSystem.Dust(new Vector2(transform.position.x, FeetY), 5, 1.4f, 0.5f);
                AudioManager.I?.Play("jump", 0.75f, 0.06f);
            }
            return v;
        }

        void StartDash()
        {
            m_DashDir = Mathf.Abs(m_MoveX) > 0.01f ? (int)Mathf.Sign(m_MoveX) : (int)Facing;
            Facing = m_DashDir;
            m_DashTimer = DashTime;
            m_DashCooldown = DashCooldown;
            m_CanDash = IsGrounded; // one dash per air-time; grounded dashes refresh instantly
            m_GhostTimer = 0f;
            SetSquash(1.35f, 0.75f);
            FxSystem.DustRing(transform.position, 8, 2.5f);
            CameraRig.I?.Shake(0.08f);
            AudioManager.I?.Play("dash", 0.7f, 0.05f);
        }

        void DropThrough()
        {
            m_JumpBuffer = 0f;
            m_DropCollider = m_GroundCol;
            m_DropTimer = 0.3f;
            Physics2D.IgnoreCollision(Col, m_DropCollider, true);
            Body.linearVelocity = new Vector2(Body.linearVelocity.x, -3f);
            IsGrounded = false;
        }

        void OnLand(float impactVy)
        {
            float k = Mathf.Clamp01(-impactVy / 22f);
            if (k > 0.25f)
            {
                SetSquash(1f + 0.35f * k, 1f - 0.3f * k);
                FxSystem.Dust(new Vector2(transform.position.x, FeetY), 3 + (int)(k * 5), 1.2f + k * 1.5f, 0.35f);
                AudioManager.I?.Play("land", 0.5f + k * 0.5f, 0.1f);
            }
            if (impactVy < -18f) CameraRig.I?.Shake(0.12f);
        }

        void CheckGround()
        {
            var b = Col.bounds;
            var center = new Vector2(b.center.x, b.min.y - 0.03f);
            var size = new Vector2(b.size.x * 0.9f, 0.08f);
            m_Hits.Clear();
            Physics2D.OverlapBox(center, size, 0f, m_GroundFilter, m_Hits);
            Collider2D best = null;
            foreach (var h in m_Hits)
            {
                if (h == m_DropCollider || h.isTrigger) continue;
                // one-way platforms only count when our feet are above their top
                if (h.gameObject.layer == Layers.OneWay && b.min.y < h.bounds.max.y - 0.2f && !(h is CompositeCollider2D)) continue;
                best = h;
                break;
            }
            IMovingSurface surf = null;
            if (best != null && !m_SurfaceCache.TryGetValue(best, out surf))
            {
                surf = best.GetComponentInParent<IMovingSurface>();
                m_SurfaceCache[best] = surf;
            }
            float relVy = Body.linearVelocity.y - (surf != null ? surf.Velocity.y : 0f);
            bool grounded = best != null && relVy <= 0.5f;
            IsGrounded = grounded;
            m_GroundCol = grounded ? best : null;
            m_PlatformVel = grounded && surf != null ? surf.Velocity : Vector2.zero;
            if (grounded) best.GetComponentInParent<IStandable>()?.OnStood(this);
        }

        void CheckWalls()
        {
            var b = Col.bounds;
            var size = new Vector2(0.08f, b.size.y * 0.55f);
            float y = b.center.y + 0.05f;
            bool right = Physics2D.OverlapBox(new Vector2(b.max.x + 0.05f, y), size, 0f, Layers.SolidMask);
            bool left = Physics2D.OverlapBox(new Vector2(b.min.x - 0.05f, y), size, 0f, Layers.SolidMask);
            m_WallDir = right ? 1 : left ? -1 : 0;
            IsWallSliding = !IsGrounded && m_WallDir != 0 && Mathf.Abs(m_MoveX) > 0.01f && Mathf.Sign(m_MoveX) == m_WallDir
                            && Body.linearVelocity.y <= 0.5f && m_Stun <= 0f;
            if (IsWallSliding) { m_AirJumps = 1; m_CanDash = true; }
        }

        // ------------------------------------------------------------------ interactions

        /// <summary>Stomp rebound. Holding jump gives a higher bounce.</summary>
        public void Bounce(float low = 12.5f, float high = 17f)
        {
            var v = Body.linearVelocity;
            v.y = m_JumpHeld ? high : low;
            Body.linearVelocity = v;
            m_AirJumps = 1;
            m_CanDash = true;
            m_JumpCut = false;
            m_DashTimer = 0f;
            m_Coyote = 0f;
            SetSquash(0.75f, 1.3f);
        }

        /// <summary>Launch from springs, arrows, etc.</summary>
        public void Launch(Vector2 velocity, bool keepX = true)
        {
            var v = Body.linearVelocity;
            if (keepX) v.y = velocity.y; else v = velocity;
            Body.linearVelocity = v;
            m_AirJumps = 1;
            m_CanDash = true;
            m_JumpCut = false;
            m_DashTimer = 0f;
            m_Coyote = 0f;
            IsGrounded = false;
            SetSquash(0.7f, 1.35f);
        }

        public void ApplyWind(float targetVy)
        {
            m_WindTimer = 0.08f;
            m_WindTarget = targetVy;
            m_AirJumps = 1;
        }

        public void Damage(Vector2 from)
        {
            if (Invulnerable || !Controllable) return;
            var session = LevelSession.Current;
            bool dead = session != null && session.LoseHeart();
            FxSystem.HitStop(0.09f);
            CameraRig.I?.Shake(0.45f);
            PostFX.I?.HitPulse();
            AudioManager.I?.Play("hurt", 0.9f, 0.05f);
            FxSystem.Sparkle(transform.position, new Color(1f, 0.4f, 0.4f), 10, 5f);
            if (dead) { Die(); return; }
            m_Invuln = 1.5f;
            m_Stun = 0.3f;
            m_DashTimer = 0f;
            float dir = Mathf.Sign(transform.position.x - from.x);
            if (Mathf.Abs(transform.position.x - from.x) < 0.05f) dir = -Facing;
            Body.linearVelocity = new Vector2(dir * 8f, 11f);
            SetSquash(1.3f, 0.75f);
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            Controllable = false;
            Col.enabled = false;
            Body.gravityScale = 4f;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            Body.linearVelocity = new Vector2(0f, 16f);
            Sprite.sortingOrder = Sort.WorldUI;
            AudioManager.I?.Play("die", 1f, 0f);
            LevelSession.Current?.OnPlayerDied();
        }

        /// <summary>Brings the player back after a death (rewarded revive).</summary>
        public void Revive(Vector2 feetPos)
        {
            IsDead = false;
            Col.enabled = true;
            Sprite.sortingOrder = Sort.Player;
            m_Visual.localRotation = Quaternion.identity;
            RespawnAt(feetPos);
            m_Invuln = 2.5f;
        }

        /// <summary>Teleports to a checkpoint with the Pixel Adventure "appearing" effect.</summary>
        public void RespawnAt(Vector2 feetPos)
        {
            Body.linearVelocity = Vector2.zero;
            Body.position = feetPos + Vector2.up * (ColliderHeight * 0.5f);
            transform.position = Body.position;
            m_DashTimer = 0f; m_Stun = 0f; m_WindTimer = 0f;
            m_Invuln = 1.6f;
            PlayAppear();
        }

        public void PlayAppear()
        {
            Controllable = false;
            Sprite.enabled = false;
            Body.simulated = false;
            AudioManager.I?.Play("appear", 0.7f, 0f);
            var fx = new GameObject("Appear");
            fx.transform.position = transform.position + Vector3.up * 0.2f;
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = GameAssets.I.spriteGlow;
            sr.sortingOrder = Sort.Player + 1;
            var anim = fx.AddComponent<SpriteAnim>();
            anim.Play(GameAssets.I.Frames("PA1/Main Characters/Appearing"), 20f, false, () =>
            {
                Destroy(fx);
                if (this == null) return;
                Sprite.enabled = true;
                Body.simulated = true;
                Controllable = true;
                SetSquash(0.8f, 1.2f);
            });
        }

        /// <summary>Victory: freeze and play the disappearing effect.</summary>
        public void PlayDisappear()
        {
            Controllable = false;
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
            var fx = new GameObject("Disappear");
            fx.transform.position = transform.position + Vector3.up * 0.2f;
            var sr = fx.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = GameAssets.I.spriteGlow;
            sr.sortingOrder = Sort.Player + 1;
            Sprite.enabled = false;
            fx.AddComponent<SpriteAnim>().Play(GameAssets.I.Frames("PA1/Main Characters/Desappearing"), 20f, false, () => Destroy(fx));
        }

        public void Freeze()
        {
            Controllable = false;
            Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
        }
    }
}
