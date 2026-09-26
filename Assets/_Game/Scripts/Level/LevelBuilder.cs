using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniPlatformer
{
    /// <summary>Builds a playable level at runtime from its text data: tilemaps, colliders, backdrop, water and entities.</summary>
    public static class LevelBuilder
    {
        const int SheetCols = 22;
        static readonly Dictionary<Sprite, Tile> s_Tiles = new Dictionary<Sprite, Tile>();

        static Tile TileFor(Sprite s)
        {
            if (s == null) return null;
            if (!s_Tiles.TryGetValue(s, out var t) || t == null)
            {
                t = ScriptableObject.CreateInstance<Tile>();
                t.sprite = s;
                t.colliderType = Tile.ColliderType.Grid;
                s_Tiles[s] = t;
            }
            return t;
        }

        static Sprite Terrain(int col, int row)
        {
            var f = GameAssets.I.Frames("PA1/Terrain/Terrain 1");
            int i = row * SheetCols + col;
            return i >= 0 && i < f.Length ? f[i] : null;
        }

        public static bool IsEntity(char c) => c != '.' && c != ' ' && c != '#' && c != 'B' && c != '=' && c != '+' && c != '~' && c != '\0';

#if UNITY_EDITOR
        // editor-only override used by the trailer capture to build hand-made set pieces
        public static string DebugLevelText;
        public static int DebugWorld = -1;
#endif

        /// <summary>World theme index for a level (respects the editor capture override).</summary>
        public static int WorldFor(int levelIndex)
        {
#if UNITY_EDITOR
            if (DebugLevelText != null && DebugWorld >= 0) return DebugWorld;
#endif
            return LevelCatalog.WorldOf(levelIndex);
        }

        public static LevelSession Build(int levelIndex, Transform root, Camera cam)
        {
            var assets = GameAssets.I;
            string text = assets.levels[levelIndex].text;
#if UNITY_EDITOR
            if (DebugLevelText != null) text = DebugLevelText;
#endif
            var data = LevelData.Parse(text);
            var world = LevelCatalog.Worlds[WorldFor(levelIndex)];
            bool dark = world.playerLight > 0f;

            var session = root.gameObject.AddComponent<LevelSession>();
            session.levelIndex = levelIndex;
            session.world = world;
            session.data = data;

            BuildTerrain(root, data, world);
            Decorate(root, data, WorldFor(levelIndex), levelIndex);

            // ------------------------------------------------ entities
            var ents = new GameObject("Entities").transform;
            ents.SetParent(root, false);
            int fruitTotal = 0, hintIndex = 0;
            Vector2 start = new Vector2(2.5f, data.height);
            bool hasBoss = false;
            for (int row = 0; row < data.height; row++)
                for (int x = 0; x < data.width; x++)
                    if (data.At(x, row) == 'x') hasBoss = true;

            for (int row = 0; row < data.height; row++)
            {
                for (int x = 0; x < data.width; x++)
                {
                    char ch = data.At(x, row);
                    if (!IsEntity(ch)) continue;
                    int y = data.RowToY(row);
                    var center = new Vector2(x + 0.5f, y + 0.5f);
                    var bottom = new Vector2(x + 0.5f, y);
                    var big = new Vector2(x + 1f, y); // 2x2 objects: char marks the top-left cell
                    switch (ch)
                    {
                        case 'P':
                            start = bottom;
                            var flag = EntityKit.Root("StartFlag", ents, bottom + new Vector2(-1.2f, 2f));
                            var fa = EntityKit.Sprite(flag, assets.Frames("PA1/Items/Checkpoints/Start/Start (Moving)"), Sort.Decor);
                            fa.Play(assets.Frames("PA1/Items/Checkpoints/Start/Start (Moving)"), 20f, false,
                                () => fa.Play(assets.Frames("PA1/Items/Checkpoints/Start/Start (Idle)")));
                            break;
                        case 'E': session.end = EndTrophy.Create(ents, bottom, hasBoss); break;
                        case 'C': Checkpoint.Create(ents, bottom); break;
                        case 'o': Fruit.Create(ents, center, x / 3 + levelIndex, dark); fruitTotal++; break;
                        case 'H': HeartPickup.Create(ents, center); break;
                        case '?':
                            HintSign.Create(ents, bottom, hintIndex < data.hints.Count ? data.hints[hintIndex] : "...");
                            hintIndex++;
                            break;
                        case 'L': Lantern.Create(ents, center + Vector2.up * 0.2f); break;
                        case '^': Hazard.Spikes(ents, center, 0); break;
                        case 'v': Hazard.Spikes(ents, center, 1); break;
                        case '<': Hazard.Spikes(ents, center, 2); break;
                        case '>': Hazard.Spikes(ents, center, 3); break;
                        case 'S': Saw.Create(ents, center, Path(data, x, row, center)); break;
                        case 'M': MovingPlatform.Create(ents, center, Path(data, x, row, center), world.platformKey); break;
                        case 'R': RockHead.Create(ents, big, Path(data, x, row, big)); break;
                        case 'K': SpikeHead.Create(ents, big); break;
                        case 'O': SpikedBall.Create(ents, center, 3.5f, x * 0.13f); break;
                        case 'T': Trampoline.Create(ents, bottom); break;
                        case 'F': Fan.Create(ents, bottom, FanHeight(data, x, row)); break;
                        case 'f': FireTrap.Create(ents, center); break;
                        case 'X': FallingPlatform.Create(ents, center); break;
                        case 'A': ArrowBoost.Create(ents, center); break;
                        case 'Q': Box.Create(ents, center, false); fruitTotal += Box.FruitCount(false); break;
                        case 'W': Box.Create(ents, center, true); fruitTotal += Box.FruitCount(true); break;
                        case 'm': Walker.Create(ents, bottom, Walker.Kind.Mushroom); break;
                        case 'c': Walker.Create(ents, bottom, Walker.Kind.Chicken); break;
                        case 'l': Walker.Create(ents, bottom, Walker.Kind.Slime); break;
                        case 'p': Walker.Create(ents, bottom, Walker.Kind.AngryPig); break;
                        case 'u': Walker.Create(ents, bottom, Walker.Kind.Bunny); break;
                        case 'n': Walker.Create(ents, bottom, Walker.Kind.Snail); break;
                        case 'k': Walker.Create(ents, bottom, Walker.Kind.Trunk); break;
                        case 'r': Rino.Create(ents, bottom); break;
                        case 't': Plant.Create(ents, bottom); break;
                        case 'w': Turtle.Create(ents, bottom); break;
                        case 'd': Flyer.Create(ents, center, Flyer.Kind.BlueBird, Path(data, x, row, center)); break;
                        case 'b': Flyer.Create(ents, center, Flyer.Kind.Bee, null); break;
                        case 'a': Flyer.Create(ents, center + Vector2.up * 0.3f, Flyer.Kind.Bat, null); break;
                        case 'g': Flyer.Create(ents, center, Flyer.Kind.Ghost, Path(data, x, row, center)); break;
                        case 'z': FatBird.Create(ents, center); break;
                        case 'x': session.boss = SkullBoss.Create(ents, big); break;
                    }
                }
            }
            session.fruitsTotal = fruitTotal;

            // ------------------------------------------------ world edges, water, backdrop, ambience
            var edges = new GameObject("Edges") { layer = Layers.Ground };
            edges.transform.SetParent(root, false);
            var left = edges.AddComponent<EdgeCollider2D>();
            left.points = new[] { new Vector2(0f, -10f), new Vector2(0f, data.height + 30f) };
            var right = edges.AddComponent<EdgeCollider2D>();
            right.points = new[] { new Vector2(data.width, -10f), new Vector2(data.width, data.height + 30f) };

            if (world.water)
            {
                BuildWater(root, data.width, world.waterTop, world.waterDeep, 2.35f, 0.04f);
                session.killY = 1.1f;
            }
            else
            {
                BuildWater(root, data.width, new Color(world.voidFog.r, world.voidFog.g, world.voidFog.b, 0f), world.voidFog, 2.2f, 0f);
                session.killY = -2.5f;
            }

            ParallaxBackground.Create(root, world.background, cam, start.y + 2f);
            AmbientFX.Create(root, world.ambient, cam.transform);

            var player = PlayerController.Spawn(root, start, LevelCatalog.Characters[Mathf.Clamp(SaveSystem.Data.character, 0, LevelCatalog.Characters.Length - 1)], world.playerLight);
            session.player = player;
            session.checkpoint = start;

            var rig = CameraRig.I;
            rig.clampToBounds = true;
            rig.bounds = new Rect(0f, 0f, data.width, Mathf.Max(data.height + 2f, K.CameraSize * 2f + 1f));
            rig.SetTarget(player.transform, true);
            return session;
        }

        /// <summary>Scatters scenery on walkable surfaces and hall ceilings, keeping clear of gameplay objects.</summary>
        static void Decorate(Transform root, LevelData d, int world, int levelIndex)
        {
            var parent = new GameObject("Decor").transform;
            parent.SetParent(root, false);
            var set = DecorArt.GroundSet(world);
            float total = 0f;
            foreach (var e in set) total += e.weight;
            var rng = new System.Random(levelIndex * 7919 + 13);

            bool Free(int x, int row) { char c = d.At(x, row); return c == '.' || c == '+' || c == ' '; }
            bool NearEntity(int x, int row)
            {
                for (int dx = -2; dx <= 2; dx++)
                    for (int dr = -2; dr <= 0; dr++)
                    {
                        char c = d.At(x + dx, row + dr);
                        if (c == 'o' || c == 'L' || c == 'H') continue;
                        if (Mathf.Abs(dx) == 2 && c != '?' && c != 'C' && c != 'E' && c != 'P') continue;
                        if (IsEntity(c) || c == '~') return true;
                    }
                return false;
            }

            for (int row = 1; row < d.height; row++)
            {
                for (int x = 1; x < d.width - 1; x++)
                {
                    if (d.At(x, row) != '#') continue;
                    // ground scenery on top of the surface
                    if (Free(x, row - 1) && !NearEntity(x, row - 1) && rng.NextDouble() < 0.5)
                    {
                        double r = rng.NextDouble() * total;
                        var pick = set[0];
                        foreach (var e in set) { if (r < e.weight) { pick = e; break; } r -= e.weight; }
                        float jitter = Mathf.Round(((float)rng.NextDouble() - 0.5f) * 6f) / K.PPU;
                        var pos = new Vector2(x + 0.5f + jitter, d.RowToY(row) + 1f - 1f / K.PPU);
                        var sr = EntityKit.StaticSprite(parent, DecorArt.Get(pick.kind, world, pick.variant), Sort.Terrain + 2, pos);
                        sr.flipX = rng.NextDouble() < 0.5;
                        if (DecorArt.Glows(pick.kind))
                        {
                            sr.sharedMaterial = GameAssets.I.spriteGlow;
                            FxSystem.Halo(sr.transform, world == 3 ? new Color(0.2f, 0.9f, 1f, 0.35f) : new Color(1f, 0.5f, 0.9f, 0.3f), 0.8f, Sort.Terrain + 1)
                                .transform.localPosition = new Vector3(0f, 0.3f, 0f);
                        }
                    }
                    // hanging scenery under ceilings
                    if (row + 1 < d.height && Free(x, row + 1) && !NearEntity(x, row + 3))
                    {
                        bool hall = d.At(x, row + 1) == '+';
                        DecorArt.Kind? hang = null;
                        if (world == 3 && rng.NextDouble() < (hall ? 0.3 : 0.18)) hang = DecorArt.Kind.Vine;
                        else if (world == 4 && hall && rng.NextDouble() < 0.1) hang = DecorArt.Kind.Banner;
                        else if (world == 1 && !hall && rng.NextDouble() < 0.08) hang = DecorArt.Kind.Vine;
                        if (hang.HasValue)
                        {
                            var pos = new Vector2(x + 0.5f, d.RowToY(row));
                            var sr = EntityKit.StaticSprite(parent, DecorArt.Get(hang.Value, world, rng.Next(2)), Sort.Decor + 2, pos);
                            if (hang == DecorArt.Kind.Vine) sr.flipX = rng.NextDouble() < 0.5;
                        }
                    }
                }
            }
        }

        static void BuildTerrain(Transform root, LevelData d, WorldTheme w)
        {
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(root, false);
            gridGo.AddComponent<Grid>();
            var ground = MakeTilemap(gridGo.transform, "Ground", Layers.Ground, Sort.Terrain, true, false);
            var oneWay = MakeTilemap(gridGo.transform, "OneWay", Layers.OneWay, Sort.OneWay, true, true);
            var back = MakeTilemap(gridGo.transform, "BackWall", Layers.Default, Sort.BackWall, false, false);
            back.color = w.backWallTint;

            var gPos = new List<Vector3Int>(); var gTiles = new List<TileBase>();
            var oPos = new List<Vector3Int>(); var oTiles = new List<TileBase>();
            var bPos = new List<Vector3Int>(); var bTiles = new List<TileBase>();
            var backTile = TileFor(Terrain(w.backWall.x, w.backWall.y));

            for (int row = 0; row < d.height; row++)
            {
                for (int x = 0; x < d.width; x++)
                {
                    char c = d.At(x, row);
                    var cell = new Vector3Int(x, d.RowToY(row), 0);
                    switch (c)
                    {
                        case '#':
                            gPos.Add(cell); gTiles.Add(TileFor(AutoTile(d, w, x, row)));
                            break;
                        case 'B':
                            gPos.Add(cell); gTiles.Add(TileFor(Terrain(w.block.x, w.block.y)));
                            break;
                        case '=':
                        {
                            bool l = d.At(x - 1, row) == '=', r = d.At(x + 1, row) == '=';
                            int col = !l ? 17 : !r ? 19 : 18;
                            if (!l && !r) col = 18;
                            oPos.Add(cell); oTiles.Add(TileFor(Terrain(col, w.oneWayRow)));
                            break;
                        }
                    }
                    if (c == '+' || (IsBackdropCell(c) && TouchesBackWall(d, x, row)))
                    {
                        bPos.Add(cell); bTiles.Add(backTile);
                    }
                }
            }
            ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
            oneWay.SetTiles(oPos.ToArray(), oTiles.ToArray());
            back.SetTiles(bPos.ToArray(), bTiles.ToArray());
        }

        static bool IsBackdropCell(char c) => c != '#' && c != 'B' && c != '+' && c != '\0' && c != ' ' && c != '.';

        static bool TouchesBackWall(LevelData d, int x, int row) =>
            d.At(x - 1, row) == '+' || d.At(x + 1, row) == '+' || d.At(x, row - 1) == '+' || d.At(x, row + 1) == '+';

        static Sprite AutoTile(LevelData d, WorldTheme w, int x, int row)
        {
            bool top = !Solid(d, x, row - 1), bottom = !Solid(d, x, row + 1);
            bool left = !Solid(d, x - 1, row), right = !Solid(d, x + 1, row);
            int r = top ? 0 : bottom ? 2 : 1;
            int c = left && right ? 1 : left ? 0 : right ? 2 : 1;
            return Terrain(w.terrain.x + c, w.terrain.y + r);
        }

        /// <summary>Out of bounds counts as solid to the sides and below (so borders don't appear at the level edges).</summary>
        static bool Solid(LevelData d, int x, int row)
        {
            if (row < 0) return false;
            if (row >= d.height) row = d.height - 1;
            x = Mathf.Clamp(x, 0, d.width - 1);
            return d.At(x, row) == '#';
        }

        static Tilemap MakeTilemap(Transform grid, string name, int layer, int order, bool collide, bool oneWay)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(grid, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;
            tr.sharedMaterial = GameAssets.I.spriteLit;
            tr.mode = TilemapRenderer.Mode.Chunk;
            if (collide)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                var tc = go.AddComponent<TilemapCollider2D>();
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
                var cc = go.AddComponent<CompositeCollider2D>();
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
                if (oneWay)
                {
                    cc.usedByEffector = true;
                    var eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 160f;
                    eff.useSideFriction = false;
                }
            }
            return tm;
        }

        /// <summary>Nearest '~' marker in the same row or column. Returns the matching end position for a mover.</summary>
        static Vector2? Path(LevelData d, int x, int row, Vector2 startPos)
        {
            int best = int.MaxValue;
            Vector2Int bestOff = default;
            var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            foreach (var dir in dirs)
            {
                for (int i = 1; i < 40; i++)
                {
                    char c = d.At(x + dir.x * i, row + dir.y * i);
                    if (c == '\0') break;
                    if (c == '~')
                    {
                        if (i < best) { best = i; bestOff = dir * i; }
                        break;
                    }
                }
            }
            if (best == int.MaxValue) return null;
            return startPos + new Vector2(bestOff.x, -bestOff.y);
        }

        static float FanHeight(LevelData d, int x, int row)
        {
            int h = 0;
            for (int r = row - 1; r >= 0 && h < 9; r--, h++)
            {
                char c = d.At(x, r);
                if (c == '#' || c == 'B') break;
            }
            return Mathf.Max(2, h);
        }

        static void BuildWater(Transform root, int width, Color top, Color deep, float surfaceY, float wave)
        {
            var go = new GameObject("Water");
            go.transform.SetParent(root, false);
            float bottomY = -8f;
            float h = surfaceY - bottomY;
            go.transform.position = new Vector3(-20f, bottomY, 0f);
            var mf = go.AddComponent<MeshFilter>();
            var mesh = new Mesh();
            float w = width + 40f;
            mesh.vertices = new[] { new Vector3(0, 0), new Vector3(w, 0), new Vector3(0, h), new Vector3(w, h) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(GameAssets.I.water);
            mat.SetColor("_TopColor", top);
            mat.SetColor("_DeepColor", deep);
            mat.SetFloat("_WaveHeight", wave);
            mat.SetVector("_Size", new Vector4(w, h, 0, 0));
            mr.sharedMaterial = mat;
            mr.sortingOrder = Sort.Terrain - 3;
        }
    }
}
