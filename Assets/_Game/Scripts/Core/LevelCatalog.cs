using UnityEngine;

namespace MiniPlatformer
{
    public enum Ambient { Pollen, Leaves, Petals, Fireflies, Motes, Snow, Rain }

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
        // puzzle-world mood
        public Color terrainTint = Color.white;   // e.g. frosty for the snow world
        public float puzzleLight = 0.9f;          // global light in puzzles (night worlds go darker and light the rooms)
        public bool roomLamps;                    // warm lamps hanging in the rooms
        public bool lightning;                    // storm flashes
    }

    public class CharacterDef
    {
        public string name, folder; public int starsRequired;
        public CharacterDef(string n, string f, int s) { name = n; folder = f; starsRequired = s; }
    }

    public static class LevelCatalog
    {
        public static readonly CharacterDef[] Characters =
        {
            new CharacterDef("Ninja Frog", "PA1/Main Characters/Ninja Frog", 0),
            new CharacterDef("Pink Man", "PA1/Main Characters/Pink Man", 8),
            new CharacterDef("Virtual Guy", "PA1/Main Characters/Virtual Guy", 18),
            new CharacterDef("Mask Dude", "PA1/Main Characters/Mask Dude", 30),
        };

        public static readonly WorldTheme[] Worlds = BuildWorlds();

        static WorldTheme[] BuildWorlds()
        {
            const string C = "BG/Cliffs/", F = "BG/Forest/", D = "BG/Dusk/", N = "BG/Night/";
            var w = new WorldTheme[5];

            // the five puzzle worlds are seasons, each with its own weather and lighting
            w[0] = new WorldTheme
            {
                name = "Spring Meadow",
                terrain = new Vector2Int(6, 0), block = new Vector2Int(12, 1), oneWayRow = 1, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.5f, 0.46f, 0.52f),
                sky = new Color32(170, 232, 236, 255),
                background = new[]
                {
                    new BgLayer(C + "sky", 0f, -2.2f, 1f),
                    new BgLayer(C + "clouds", 0.04f, 1.2f, 1f, 0.12f),
                    new BgLayer(C + "sea", 0.08f, -1.6f, 1f),
                    new BgLayer(C + "far-grounds", 0.16f, -0.6f, 1f),
                },
                lightColor = new Color(1f, 0.97f, 0.9f), lightIntensity = 1.0f, puzzleLight = 1f,
                ambient = Ambient.Petals,
                music = "Music/stage1", colorFilter = new Color(1f, 0.98f, 0.97f), saturation = 14f, bloom = 0.5f, vignette = 0.2f,
            };

            w[1] = new WorldTheme
            {
                name = "Autumn Woods",
                terrain = new Vector2Int(6, 4), block = new Vector2Int(12, 9), oneWayRow = 1, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.5f, 0.42f, 0.4f),
                sky = new Color32(249, 205, 128, 255),
                background = new[]
                {
                    new BgLayer(F + "parallax-forest-back-trees", 0.03f, -0.4f, 1.75f),
                    new BgLayer(F + "parallax-forest-lights", 0.07f, -0.4f, 1.75f, 0f, new Color(1, 1, 1, 0.9f)),
                    new BgLayer(F + "parallax-forest-middle-trees", 0.14f, -0.4f, 1.75f),
                    new BgLayer(F + "parallax-forest-front-trees", 0.26f, -0.8f, 1.75f),
                },
                lightColor = new Color(1f, 0.88f, 0.72f), lightIntensity = 0.95f, puzzleLight = 0.92f,
                ambient = Ambient.Leaves,
                music = "Music/stage2", colorFilter = new Color(1f, 0.96f, 0.9f), bloom = 0.7f,
            };

            var storm = new Color(0.52f, 0.6f, 0.74f);
            w[2] = new WorldTheme
            {
                name = "Rainy Peaks",
                terrain = new Vector2Int(6, 0), block = new Vector2Int(17, 9), oneWayRow = 0, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.36f, 0.38f, 0.48f), terrainTint = new Color(0.78f, 0.85f, 0.9f),
                sky = new Color32(64, 76, 100, 255),
                background = new[]
                {
                    new BgLayer(D + "parallax-mountain-bg", 0f, -0.4f, 1.75f, 0f, storm * 0.8f),
                    new BgLayer(D + "parallax-mountain-montain-far", 0.04f, -0.4f, 1.75f, 0f, storm * 0.85f, new Color(0.3f, 0.36f, 0.46f, 0.4f)),
                    new BgLayer(D + "parallax-mountain-mountains", 0.09f, -0.6f, 1.75f, 0f, storm * 0.8f, new Color(0.25f, 0.3f, 0.4f, 0.3f)),
                    new BgLayer(D + "parallax-mountain-trees", 0.18f, -0.8f, 1.75f, 0f, storm * 0.7f),
                    new BgLayer(D + "parallax-mountain-foreground-trees", 0.3f, -1.4f, 1.75f, 0f, storm * 0.6f),
                },
                lightColor = new Color(0.75f, 0.83f, 1f), lightIntensity = 0.7f, puzzleLight = 0.62f, playerLight = 0.8f,
                ambient = Ambient.Rain, roomLamps = true, lightning = true,
                music = "Music/select", colorFilter = new Color(0.92f, 0.96f, 1.04f), saturation = -8f, contrast = 12f, bloom = 0.95f, vignette = 0.4f,
            };

            var nightTint = new Color(0.42f, 0.6f, 0.62f);
            w[3] = new WorldTheme
            {
                name = "Firefly Night",
                terrain = new Vector2Int(0, 8), block = new Vector2Int(12, 5), oneWayRow = 1, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.28f, 0.36f, 0.42f),
                sky = new Color32(22, 34, 52, 255),
                background = new[]
                {
                    new BgLayer(F + "parallax-forest-back-trees", 0.03f, -0.4f, 1.75f, 0f, nightTint * 0.6f, new Color(0.04f, 0.08f, 0.16f, 0.5f)),
                    new BgLayer(F + "parallax-forest-lights", 0.07f, -0.4f, 1.75f, 0f, new Color(0.7f, 1f, 0.6f, 0.6f)),
                    new BgLayer(F + "parallax-forest-middle-trees", 0.14f, -0.4f, 1.75f, 0f, nightTint * 0.5f, new Color(0.03f, 0.07f, 0.14f, 0.35f), 0.25f),
                    new BgLayer(F + "parallax-forest-front-trees", 0.26f, -0.8f, 1.75f, 0f, nightTint * 0.4f, new Color(0.02f, 0.05f, 0.1f, 0.25f)),
                },
                lightColor = new Color(0.55f, 0.7f, 1f), lightIntensity = 0.5f, puzzleLight = 0.42f, playerLight = 1.1f,
                ambient = Ambient.Fireflies, roomLamps = true,
                music = "Music/stage1", colorFilter = new Color(0.92f, 0.98f, 1.04f), exposure = 0.15f, bloom = 1.2f, vignette = 0.42f,
            };

            w[4] = new WorldTheme
            {
                name = "Snowy Castle",
                terrain = new Vector2Int(0, 0), block = new Vector2Int(12, 5), oneWayRow = 2, backWall = new Vector2Int(18, 5),
                backWallTint = new Color(0.33f, 0.4f, 0.6f), terrainTint = new Color(0.86f, 0.93f, 1.05f),
                sky = new Color32(78, 96, 140, 255),
                background = new[]
                {
                    new BgLayer(N + "sky", 0f, -0.6f, 1.2f, 0f, new Color(0.85f, 0.95f, 1.15f)),
                    new BgLayer(N + "far-clouds", 0.02f, -0.6f, 1.2f, 0.08f, new Color(1f, 1f, 1.1f)),
                    new BgLayer(N + "near-clouds", 0.05f, -0.6f, 1.2f, 0.22f, new Color(1f, 1f, 1.1f)),
                    new BgLayer(N + "far-mountains", 0.08f, -0.6f, 1.2f, 0f, new Color(0.9f, 0.97f, 1.2f), new Color(0.75f, 0.85f, 1f, 0.35f)),
                    new BgLayer(N + "mountains", 0.15f, -0.8f, 1.2f, 0f, new Color(0.85f, 0.93f, 1.15f), new Color(0.7f, 0.8f, 1f, 0.25f)),
                    new BgLayer(N + "trees", 0.26f, -1.2f, 1.2f, 0f, new Color(0.8f, 0.9f, 1.1f)),
                },
                lightColor = new Color(0.8f, 0.88f, 1f), lightIntensity = 0.8f, puzzleLight = 0.72f, playerLight = 0.9f,
                ambient = Ambient.Snow, roomLamps = true,
                music = "Music/stage2", colorFilter = new Color(0.95f, 0.98f, 1.05f), exposure = 0.1f, bloom = 1.0f, vignette = 0.32f,
            };
            return w;
        }
    }
}
