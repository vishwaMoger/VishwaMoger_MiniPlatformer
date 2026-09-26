using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Base for all enemies. The root sits at the enemy's feet (ground enemies) or center (flyers).
    /// Player contact goes through a child trigger (EnemyHitbox) so bodies never shove the player around.
    /// </summary>
    public abstract class Enemy : MonoBehaviour
    {
        protected string folder;
        protected SpriteAnim anim;
        protected SpriteRenderer sr;
        protected Rigidbody2D rb;
        protected Collider2D body;
        protected Collider2D hitbox;
        protected int hp = 1;
        protected float facing = -1f;
        protected bool stompable = true;
        public bool Dead { get; protected set; }

        float m_Flash;
        float m_IgnoreUntil;
        MaterialPropertyBlock m_Mpb;

        protected Sprite[] F(string anim) => GameAssets.I.Frames(folder + "/" + anim);

        /// <summary>Creates sprite + hitbox. spriteY is the sprite center relative to the root.</summary>
        protected void SetupVisual(string enemyFolder, float spriteY, Vector2 hitSize, Vector2 hitOffset, string startAnim)
        {
            folder = "PA2/Enemies/" + enemyFolder;
            anim = EntityKit.Sprite(gameObject, F(startAnim), Sort.Enemies, new Vector2(0f, spriteY));
            anim.RandomizeStart();
            sr = anim.Renderer;
            var hb = new GameObject("Hitbox") { layer = Layers.EnemyHit };
            hb.transform.SetParent(transform, false);
            var c = hb.AddComponent<BoxCollider2D>();
            c.size = hitSize;
            c.offset = hitOffset;
            c.isTrigger = true;
            hitbox = c;
            hb.AddComponent<EnemyHitbox>().owner = this;
            m_Mpb = new MaterialPropertyBlock();
        }

        protected void SetupWalkerBody(Vector2 size)
        {
            gameObject.layer = Layers.Enemy;
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var b = gameObject.AddComponent<BoxCollider2D>();
            b.size = size;
            b.offset = new Vector2(0f, size.y * 0.5f);
            b.sharedMaterial = new PhysicsMaterial2D { friction = 0f };
            body = b;
        }

        protected void SetupFlyerBody()
        {
            gameObject.layer = Layers.Enemy;
            rb = EntityKit.Kinematic(gameObject);
        }

        protected void Face(float dir)
        {
            if (Mathf.Abs(dir) > 0.01f) facing = Mathf.Sign(dir);
            if (sr != null) sr.flipX = facing > 0f; // Pixel Adventure enemies are drawn facing left
        }

        public virtual void OnPlayerContact(PlayerController p)
        {
            if (Dead || p.IsDead || Time.time < m_IgnoreUntil) return;
            if (stompable && CanBeStomped && IsStomp(p)) Stomped(p);
            else if (IsHarmful) p.Damage(transform.position + Vector3.up * 0.5f);
        }

        protected virtual bool CanBeStomped => true;
        protected virtual bool IsHarmful => true;

        protected bool IsStomp(PlayerController p)
        {
            var b = hitbox.bounds;
            if (p.PrevVelocity.y >= 2f) return false;
            // a fast fall can sink ~half a unit into the hitbox before the trigger reports, so also accept where the
            // feet were a moment ago (no effect when walking in from the side, where vertical speed is ~0)
            float feetBefore = p.FeetY - Mathf.Min(0f, p.PrevVelocity.y) * Time.fixedDeltaTime * 1.5f;
            return Mathf.Max(p.FeetY, feetBefore) > b.center.y + b.extents.y * 0.1f;
        }

        protected virtual void Stomped(PlayerController p)
        {
            p.Bounce();
            m_IgnoreUntil = Time.time + 0.3f;
            FxSystem.HitStop(0.06f);
            CameraRig.I?.Shake(0.16f);
            AudioManager.I?.Play("stomp", 0.85f, 0.08f);
            FxSystem.Dust(new Vector2(transform.position.x, hitbox.bounds.max.y), 6, 2.2f, 0.4f);
            TakeHit();
        }

        public virtual void TakeHit()
        {
            hp--;
            m_Flash = 1f;
            if (hp <= 0) Die();
            else OnHurt();
        }

        protected virtual void OnHurt() { }

        protected virtual void Die()
        {
            Dead = true;
            hitbox.enabled = false;
            Tween.Punch(anim.transform, 0.3f, 0.15f, false);
            anim.Play(F(HitAnim), 20f, false, () =>
            {
                if (this == null) return;
                if (body != null) body.enabled = false;
                if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 3.5f;
                rb.constraints = RigidbodyConstraints2D.None;
                rb.linearVelocity = new Vector2(-facing * 2f, 9f);
                rb.angularVelocity = facing * 400f;
                sr.sortingOrder = Sort.FX;
                FxSystem.Sparkle(transform.position + Vector3.up * 0.5f, Color.white, 6, 3f, 0.5f, 0.4f);
                Destroy(gameObject, 1.6f);
            });
        }

        protected virtual string HitAnim => "Hit";

        protected virtual void Update()
        {
            if (m_Flash > 0f && sr != null)
            {
                m_Flash = Mathf.MoveTowards(m_Flash, 0f, Time.deltaTime * 6f);
                m_Mpb.SetFloat("_FlashAmount", m_Flash);
                m_Mpb.SetColor("_FlashColor", Color.white);
                sr.SetPropertyBlock(m_Mpb);
                if (m_Flash <= 0f) sr.SetPropertyBlock(null);
            }
        }

        protected PlayerController Player => PlayerController.I != null && !PlayerController.I.IsDead ? PlayerController.I : null;
    }
}
