using UnityEngine;

namespace MiniPlatformer
{
    public enum Ambient { Pollen, Leaves, Petals, Fireflies, Motes }

    public struct BgLayer
    {
        public string key;       // texture key (Art/Backgrounds/...)
        public float factor;     // 0 = glued to camera (far), 1 = moves with the world
        public float bottom;     // bottom edge offset from the camera's bottom edge (world units)
        public float scale;      // world units per 16 texels
        public float autoScroll; // extra drift in units/sec (clouds)
        public Color tint;
        public Color fog;        // rgb + amount in alpha
        public float heightFog;

        public BgLayer(string key, float factor, float bottom, float scale, float autoScroll = 0f, Color? tint = null, Color? fog = null, float heightFog = 0f)
        {
            this.key = key; this.factor = factor; this.bottom = bottom; this.scale = scale; this.autoScroll = autoScroll;
            this.tint = tint ?? Color.white; this.fog = fog ?? new Color(1, 1, 1, 0); this.heightFog = heightFog;
        }
    }

    public class WorldTheme
    {
        public string name;
        public Vector2Int terrain;        // top-left of the 3x3 autotile block in the terrain sheet (col,row from top)
        public Vector2Int block;          // 1x1 standalone block tile
        public int oneWayRow;             // row of the 3-tile thin platform strip (cols 17-19)
        public Vector2Int backWall;       // decorative background wall tile
        public Color backWallTint = new Color(0.45f, 0.45f, 0.55f);
        public BgLayer[] background;
        public Color sky;
        public Color lightColor = Color.white;
        public float lightIntensity = 1f;
        public float playerLight;         // 0 = none
        public Ambient ambient;
        public bool water;
        public Color waterTop, waterDeep;
        public Color voidFog = new Color(0.1f, 0.08f, 0.15f, 0.9f);
        public string music;
        public Color colorFilter = Color.white;
        public float saturation = 10f, contrast = 8f, exposure = 0f, vignette = 0.28f, bloom = 0.6f;
        public string platformKey = "PA1/Traps/Platforms/Grey On";
    }

    public class CharacterDef
    {
        public string name, folder; public int starsRequired;
        public CharacterDef(string n, string f, int s) { name = n; folder = f; starsRequired = s; }
    }

    public static class LevelCatalog
    {
        public const int LevelCount = 15;
        public const int LevelsPerWorld = 3;

        public static readonly string[] LevelNames =
        {
            "Seaside Start", "Double Trouble", "Cliffside Climb",
            "Falling Leaves", "Woodland Rush", "Timber Trials",
            "Rosy Ridge", "Sunset Sprint", "Summit Spikes",
            "Overgrown Gate", "Ghostly Grove", "Temple Depths",
            "Castle Walls", "Midnight Keep", "The Skull King",
        };

        public static readonly CharacterDef[] Characters =
        {
            new CharacterDef("Ninja Frog", "PA1/Main Characters/Ninja Frog", 0),
            new CharacterDef("Pink Man", "PA1/Main Characters/Pink Man", 8),
            new CharacterDef("Virtual Guy", "PA1/Main Characters/Virtual Guy", 18),
            new CharacterDef("Mask Dude", "PA1/Main Characters/Mask Dude", 30),
        };

        public static readonly WorldTheme[] Worlds = BuildWorlds();

        public static int WorldOf(int level) => level / LevelsPerWorld;
        public static string Code(int level) => $"{WorldOf(level) + 1}-{level % LevelsPerWorld + 1}";

        static WorldTheme[] BuildWorlds()
        {
            const string C = "BG/Cliffs/", F = "BG/Forest/", D = "BG/Dusk/", N = "BG/Night/";
            var w = new WorldTheme[5];

            w[0] = new WorldTheme
            {
                name = "Sunny Shores",
                terrain = new Vector2Int(6, 0), block = new Vector2Int(12, 1), oneWayRow = 1, backWall = new Vector2Int(18, 5),
                sky = new Color32(152, 232, 232, 255),
                background = new[]
                {
                    new BgLayer(C + "sky", 0f, -2.2f, 1f),
                    new BgLayer(C + "clouds", 0.04f, 1.2f, 1f, 0.12f),
                    new BgLayer(C + "sea", 0.08f, -1.6f, 1f),
                    new BgLayer(C + "far-grounds", 0.16f, -0.6f, 1f),
                },
                lightColor = new Color(1f, 0.98f, 0.92f), lightIntensity = 1.0f,
                ambient = Ambient.Pollen, water = true,
                waterTop = new Color(0.45f, 0.86f, 0.92f, 0.85f), waterDeep = new Color(0.12f, 0.42f, 0.62f, 0.95f),
                music = "Music/stage1", saturation = 12f, bloom = 0.45f, vignette = 0.22f,
            };

            w[1] = new WorldTheme
            {
                name = "Autumn Woods",
                terrain = new Vector2Int(6, 4), block = new Vector2Int(12, 9), oneWayRow = 1, backWall = new Vector2Int(18, 5),
                sky = new Color32(249, 205, 128, 255),
                background = new[]
                {
                    new BgLayer(F + "parallax-forest-back-trees", 0.03f, -0.4f, 1.75f),
                    new BgLayer(F + "parallax-forest-lights", 0.07f, -0.4f, 1.75f, 0f, new Color(1, 1, 1, 0.9f)),
                    new BgLayer(F + "parallax-forest-middle-trees", 0.14f, -0.4f, 1.75f),
                    new BgLayer(F + "parallax-forest-front-trees", 0.26f, -0.8f, 1.75f),
                },
                lightColor = new Color(1f, 0.9f, 0.75f), lightIntensity = 0.95f,
                ambient = Ambient.Leaves,
                voidFog = new Color(0.16f, 0.1f, 0.08f, 0.95f),
                music = "Music/stage2", colorFilter = new Color(1f, 0.97f, 0.92f), bloom = 0.7f,
            };

            w[2] = new WorldTheme
            {
                name = "Sunset Peaks",
                terrain = new Vector2Int(6, 8), block = new Vector2Int(17, 9), oneWayRow = 0, backWall = new Vector2Int(18, 5),
                sky = new Color32(173, 112, 148, 255),
                background = new[]
                {
                    new BgLayer(D + "parallax-mountain-bg", 0f, -0.4f, 1.75f),
                    new BgLayer(D + "parallax-mountain-montain-far", 0.04f, -0.4f, 1.75f),
                    new BgLayer(D + "parallax-mountain-mountains", 0.09f, -0.6f, 1.75f),
                    new BgLayer(D + "parallax-mountain-trees", 0.18f, -0.8f, 1.75f),
                    new BgLayer(D + "parallax-mountain-foreground-trees", 0.3f, -1.4f, 1.75f),
                },
                lightColor = new Color(1f, 0.86f, 0.9f), lightIntensity = 0.92f,
                ambient = Ambient.Petals,
                voidFog = new Color(0.14f, 0.07f, 0.12f, 0.95f),
                music = "Music/select", colorFilter = new Color(1f, 0.95f, 0.97f), bloom = 0.8f,
                platformKey = "PA1/Traps/Platforms/Brown On",
            };

            var jungleTint = new Color(0.5f, 0.78f, 0.68f);
            var jungleFog = new Color(0.08f, 0.2f, 0.2f, 0.25f);
            w[3] = new WorldTheme
            {
                name = "Jungle Ruins",
                terrain = new Vector2Int(0, 8), block = new Vector2Int(12, 5), oneWayRow = 1, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.3f, 0.48f, 0.42f),
                sky = new Color32(38, 70, 64, 255),
                background = new[]
                {
                    new BgLayer(F + "parallax-forest-back-trees", 0.03f, -0.4f, 1.75f, 0f, jungleTint * 0.8f, new Color(0.06f, 0.16f, 0.17f, 0.45f)),
                    new BgLayer(F + "parallax-forest-lights", 0.07f, -0.4f, 1.75f, 0f, new Color(0.6f, 1f, 0.8f, 0.55f)),
                    new BgLayer(F + "parallax-forest-middle-trees", 0.14f, -0.4f, 1.75f, 0f, jungleTint * 0.7f, jungleFog, 0.25f),
                    new BgLayer(F + "parallax-forest-front-trees", 0.26f, -0.8f, 1.75f, 0f, jungleTint * 0.55f, new Color(0.05f, 0.12f, 0.12f, 0.2f)),
                },
                lightColor = new Color(0.62f, 0.9f, 0.8f), lightIntensity = 0.55f, playerLight = 0.9f,
                ambient = Ambient.Fireflies, water = true,
                waterTop = new Color(0.3f, 0.62f, 0.38f, 0.8f), waterDeep = new Color(0.07f, 0.2f, 0.14f, 0.97f),
                music = "Music/stage1", colorFilter = new Color(0.92f, 1f, 0.97f), exposure = 0.1f, bloom = 1.0f, vignette = 0.36f,
            };

            w[4] = new WorldTheme
            {
                name = "Moonlit Castle",
                terrain = new Vector2Int(0, 0), block = new Vector2Int(12, 5), oneWayRow = 2, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.32f, 0.3f, 0.48f),
                sky = new Color32(48, 38, 72, 255),
                background = new[]
                {
                    new BgLayer(N + "sky", 0f, -0.6f, 1.2f),
                    new BgLayer(N + "far-clouds", 0.02f, -0.6f, 1.2f, 0.08f),
                    new BgLayer(N + "near-clouds", 0.05f, -0.6f, 1.2f, 0.22f),
                    new BgLayer(N + "far-mountains", 0.08f, -0.6f, 1.2f),
                    new BgLayer(N + "mountains", 0.15f, -0.8f, 1.2f),
                    new BgLayer(N + "trees", 0.26f, -1.2f, 1.2f),
                },
                lightColor = new Color(0.6f, 0.62f, 1f), lightIntensity = 0.42f, playerLight = 1.1f,
                ambient = Ambient.Motes,
                voidFog = new Color(0.07f, 0.05f, 0.12f, 0.97f),
                music = "Music/stage2", colorFilter = new Color(0.93f, 0.93f, 1f), exposure = 0.15f, bloom = 1.1f, vignette = 0.4f,
            };
            return w;
        }
    }
}
