using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Live title-screen backdrop: drifting parallax, a scrolling strip of terrain and the chosen hero running along it.</summary>
    public class MenuStage : MonoBehaviour
    {
        public int World { get; private set; }
        ParallaxBackground m_Bg;
        Transform m_Strip;
        SpriteAnim m_Hero;
        float m_X;
        int m_Character = -1;
        Camera m_Cam;
        const float Speed = 3.2f;
        const float GroundY = -5.5f;

        public static MenuStage Create(Transform parent, int world, Camera cam)
        {
            var go = new GameObject("MenuStage");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<MenuStage>();
            m.World = world;
            m.m_Cam = cam;
            m.Build();
            return m;
        }

        void Build()
        {
            var w = LevelCatalog.Worlds[World];
            var rig = CameraRig.I;
            rig.SetTarget(null, false);
            rig.clampToBounds = false;
            rig.SetZoom(1f);
            rig.SnapTo(Vector2.zero);

            m_Bg = ParallaxBackground.Create(transform, w.background, m_Cam, 0f);
            AmbientFX.Create(transform, w.ambient, m_Cam.transform);

            // ground strip made of the world's terrain tiles
            m_Strip = new GameObject("Strip").transform;
            m_Strip.SetParent(transform, false);
            var frames = GameAssets.I.Frames("PA1/Terrain/Terrain 1");
            Sprite T(int c, int r) => frames[(w.terrain.y + r) * 22 + w.terrain.x + c];
            var top = MakeTiled(T(1, 0), new Vector2(60f, 1f), new Vector2(0f, GroundY - 0.5f));
            var fill = MakeTiled(T(1, 1), new Vector2(60f, 4f), new Vector2(0f, GroundY - 3f));
            top.transform.SetParent(m_Strip, false);
            fill.transform.SetParent(m_Strip, false);

            var hero = new GameObject("Hero");
            hero.transform.SetParent(transform, false);
            hero.transform.position = new Vector3(-7.5f, GroundY + 1f, 0f);
            var sr = hero.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = GameAssets.I.spriteLit;
            sr.sortingOrder = Sort.Player;
            m_Hero = hero.AddComponent<SpriteAnim>();
            if (w.playerLight > 0f) FxSystem.PointLight(hero.transform, new Color(1f, 0.92f, 0.8f), w.playerLight, 7f);
            RefreshHero();
        }

        SpriteRenderer MakeTiled(Sprite s, Vector2 size, Vector2 pos)
        {
            var go = new GameObject("Tiles");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            sr.sharedMaterial = GameAssets.I.spriteLit;
            sr.sortingOrder = Sort.Terrain;
            go.transform.position = pos;
            return sr;
        }

        void RefreshHero()
        {
            m_Character = SaveSystem.Data.character;
            var c = LevelCatalog.Characters[Mathf.Clamp(m_Character, 0, LevelCatalog.Characters.Length - 1)];
            m_Hero.Play(GameAssets.I.Frames(c.folder + "/Run"), 20f, true, null, true);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            m_X += Speed * dt;
            m_Bg.virtualX = m_X;
            m_Strip.localPosition = new Vector3(-Mathf.Repeat(m_X, 1f), 0f, 0f);
            if (m_Character != SaveSystem.Data.character) RefreshHero();
            // occasional hop for life
            float hop = Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * 1.7f) - 0.85f) * 6f;
            m_Hero.transform.position = new Vector3(-7.5f, GroundY + 1f + hop, 0f);
            if (Random.value < dt * 3f) FxSystem.Dust(new Vector2(-7.8f, GroundY), 1, 0.6f, 0.3f);
        }
    }
}
