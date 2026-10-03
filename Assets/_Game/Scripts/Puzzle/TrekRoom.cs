using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace MiniPlatformer
{
    /// <summary>Procedural world-space sprites for the puzzle board (room frames, the empty slot, slide arrows).</summary>
    public static class TrekArt
    {
        static Sprite s_Frame, s_Slot, s_Glow;

        static Sprite Sliced(Texture2D t, float border, float ppu) =>
            Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border), false);

        /// <summary>Thin rounded outline with a soft outer shadow: makes each room read as a sliding tile.</summary>
        public static Sprite Frame
        {
            get
            {
                if (s_Frame != null) return s_Frame;
                const int S = 64;
                var c = new Vector2(S / 2f, S / 2f);
                var half = new Vector2(S / 2f - 3f, S / 2f - 3f);
                var tex = UIArt.Render(S, S,
                    new UIArt.Layer(p => Mathf.Abs(UIArt.RoundBox(p, c, half, 10f)) - 1.6f, new Color(0.08f, 0.04f, 0.14f, 0.55f)).Soft(3f),
                    new UIArt.Layer(p => Mathf.Abs(UIArt.RoundBox(p, c, half - new Vector2(1.5f, 1.5f), 9f)) - 0.9f, new Color(1f, 1f, 1f, 0.55f)));
                s_Frame = Sliced(tex, 20f, 64f / 1f * 0.5f); // 2 world units per texture => 0.6-unit corners
                return s_Frame;
            }
        }

        /// <summary>Dark recessed hole for the empty slot.</summary>
        public static Sprite Slot
        {
            get
            {
                if (s_Slot != null) return s_Slot;
                const int S = 64;
                var c = new Vector2(S / 2f, S / 2f);
                var half = new Vector2(S / 2f - 3f, S / 2f - 3f);
                var tex = UIArt.Render(S, S,
                    new UIArt.Layer(p => UIArt.RoundBox(p, c, half, 10f), new Color(0.05f, 0.03f, 0.1f, 0.55f)),
                    new UIArt.Layer(p => Mathf.Abs(UIArt.RoundBox(p, c, half - new Vector2(2f, 2f), 8f)) - 1f, new Color(1f, 1f, 1f, 0.22f)));
                s_Slot = Sliced(tex, 20f, 32f);
                return s_Slot;
            }
        }

        /// <summary>Soft filled rounded square (highlight for rooms that can slide).</summary>
        public static Sprite Glow
        {
            get
            {
                if (s_Glow != null) return s_Glow;
                const int S = 64;
                var c = new Vector2(S / 2f, S / 2f);
                var half = new Vector2(S / 2f - 4f, S / 2f - 4f);
                var tex = UIArt.Render(S, S,
                    new UIArt.Layer(p => Mathf.Abs(UIArt.RoundBox(p, c, half, 10f)) - 2.5f, Color.white).Soft(4f));
                s_Glow = Sliced(tex, 20f, 32f);
                return s_Glow;
            }
        }

        public static SpriteRenderer SlicedRenderer(Transform parent, Sprite s, Vector2 localCenter, Vector2 size, int order, Color color, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }
    }

    /// <summary>
    /// One 7x7-tile room of the board: autotiled walls with doorways (sides at floor level, 3-wide holes in the
    /// ceiling/floor), a back wall, its hazards/coins/trophy, and a frame. Everything inside moves with it.
    /// </summary>
    public class TrekRoom : MonoBehaviour
    {
        public const int Size = 7;
        const int SheetCols = 22;
        static readonly Dictionary<Sprite, Tile> s_Tiles = new Dictionary<Sprite, Tile>();

        public RoomDef def;
        public int cell;
        Tilemap m_Ground, m_Back;
        WorldTheme m_Theme;
        int m_Merged; // sides melted into a neighbouring route room (same bits as RoomDef openings)
        public Transform Contents { get; private set; }
        SpriteRenderer m_Highlight, m_Frame, m_Shade;
        Transform m_Arrow;
        float m_Pulse;
        bool m_Hint;
        float m_FrameAlpha = 1f, m_ShadeAlpha;
        /// <summary>0 normal, 1 part of the finished route (frame melts away), 2 off the route (dimmed).</summary>
        int m_Focus;
        // trophy room markers: a pulsing gold frame, a bobbing GOAL label and arrows at the doorway to enter by
        SpriteRenderer m_GoalGlow;
        Transform m_GoalLabel;
        readonly List<SpriteRenderer> m_GoalDoors = new List<SpriteRenderer>();

        public Vector2 Origin => transform.position;
        public Rect WorldRect => new Rect(transform.position.x, transform.position.y, Size, Size);

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

        /// <summary>Solid wall tiles of a room (local x,y, y up). 'merged' sides are walls that melted into the route.</summary>
        public static bool Solid(RoomDef def, int x, int y, int merged = 0)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size) return false;
            if ((merged & RoomDef.L) != 0 && x == 0 && y >= 1 && y <= Size - 2) return false;
            if ((merged & RoomDef.R) != 0 && x == Size - 1 && y >= 1 && y <= Size - 2) return false;
            if ((merged & RoomDef.U) != 0 && y == Size - 1 && x >= 1 && x <= Size - 2) return false;
            bool border = x == 0 || y == 0 || x == Size - 1 || y == Size - 1;
            if (!border) return false;
            if (x == 0 && (y == 1 || y == 2) && def.Has(RoomDef.L)) return false;
            if (x == Size - 1 && (y == 1 || y == 2) && def.Has(RoomDef.R)) return false;
            if (y == Size - 1 && x >= 2 && x <= 4 && def.Has(RoomDef.U)) return false;
            if (y == 0 && x >= 2 && x <= 4 && def.Has(RoomDef.D)) return false;
            return true;
        }

        public static TrekRoom Create(Transform parent, RoomDef def, WorldTheme theme, int worldIndex, Vector2 origin)
        {
            var go = new GameObject("Room" + def.id + "_" + def.kind);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            go.AddComponent<Grid>();
            var room = go.AddComponent<TrekRoom>();
            room.def = def;
            room.Build(theme, worldIndex);
            return room;
        }

        void Build(WorldTheme theme, int worldIndex)
        {
            // ---- tiles
            m_Theme = theme;
            m_Ground = MakeTilemap("Ground", Layers.Ground, Sort.Terrain, true);
            m_Back = MakeTilemap("BackWall", Layers.Default, Sort.BackWall, false);
            m_Back.color = theme.backWallTint * 0.85f;
            m_Ground.color = theme.terrainTint;
            BuildTiles();
            BuildLights(theme);

            Contents = new GameObject("Contents").transform;
            Contents.SetParent(transform, false);

            // ---- frame + slide highlight
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            m_Frame = TrekArt.SlicedRenderer(transform, TrekArt.Frame, center, new Vector2(Size + 0.1f, Size + 0.1f), Sort.FX - 2, Color.white, "Frame");
            m_Shade = TrekArt.SlicedRenderer(transform, TrekArt.Slot, center, new Vector2(Size, Size), Sort.FX - 3, new Color(1f, 1f, 1f, 0f), "Shade");
            m_Highlight = TrekArt.SlicedRenderer(transform, TrekArt.Glow, center, new Vector2(Size - 0.1f, Size - 0.1f), Sort.FX - 1, new Color(1f, 0.85f, 0.3f, 0f), "Highlight");
            var arrow = new GameObject("Arrow");
            arrow.transform.SetParent(transform, false);
            var asr = arrow.AddComponent<SpriteRenderer>();
            asr.sprite = UIArt.Arrow(0);
            asr.sortingOrder = Sort.WorldUI;
            asr.color = new Color(1f, 0.9f, 0.45f, 0.9f);
            arrow.transform.localScale = Vector3.one * 0.9f;
            m_Arrow = arrow.transform;
            m_Arrow.gameObject.SetActive(false);

            BuildContents(worldIndex);
        }

        void BuildTiles()
        {
            m_Ground.ClearAllTiles();
            m_Back.ClearAllTiles();
            var backTile = TileFor(Terrain(m_Theme.backWall.x, m_Theme.backWall.y));
            var gPos = new List<Vector3Int>(); var gTiles = new List<TileBase>();
            var bPos = new List<Vector3Int>(); var bTiles = new List<TileBase>();
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var cellPos = new Vector3Int(x, y, 0);
                    if (Solid(def, x, y, m_Merged)) { gPos.Add(cellPos); gTiles.Add(TileFor(AutoTile(m_Theme, x, y))); }
                    else { bPos.Add(cellPos); bTiles.Add(backTile); }
                }
            m_Ground.SetTiles(gPos.ToArray(), gTiles.ToArray());
            m_Back.SetTiles(bPos.ToArray(), bTiles.ToArray());
        }

        /// <summary>
        /// Melts the walls on the given sides into the neighbouring route room (side walls open floor-to-ceiling,
        /// a ceiling opens into the floor of the room above), with crumbling dust, so the route reads as one hall.
        /// Floors are never removed, so the hero's walk is unchanged.
        /// </summary>
        public void MeltWalls(int sides, float delay)
        {
            sides &= ~m_Merged;
            if (sides == 0) return;
            Tween.Delay(delay, () =>
            {
                if (this == null) return;
                m_Merged |= sides;
                BuildTiles();
                if (m_Caps != null) BuildSnowCaps();
                Vector2 o = Origin;
                var dust = new Color(0.75f, 0.6f, 0.45f);
                if ((sides & RoomDef.L) != 0) for (int y = 1; y < Size - 1; y++) FxSystem.Debris(o + new Vector2(0.5f, y + 0.5f), dust, 2, 3f, 0.2f);
                if ((sides & RoomDef.R) != 0) for (int y = 1; y < Size - 1; y++) FxSystem.Debris(o + new Vector2(Size - 0.5f, y + 0.5f), dust, 2, 3f, 0.2f);
                if ((sides & RoomDef.U) != 0) for (int x = 1; x < Size - 1; x++) FxSystem.Debris(o + new Vector2(x + 0.5f, Size - 0.5f), dust, 2, 3f, 0.2f);
                AudioManager.I?.Play("break", 0.45f, 0.1f);
            }, true);
        }

        Transform m_Caps;
        Light2D m_Lamp;
        float m_LampBase, m_LampSeed;

        /// <summary>
        /// Mood lighting: hanging lamps (night, rain and snow worlds), a red warning glow in trap rooms,
        /// a golden glow around the trophy, and snow caps on every exposed ledge in the snow world.
        /// </summary>
        void BuildLights(WorldTheme theme)
        {
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            if (def.Deadly)
                FxSystem.PointLight(transform, new Color(1f, 0.25f, 0.25f), 0.7f, 5.5f, 0.6f, new Vector2(3.5f, 2.2f));
            else if (def.kind == RoomKind.Exit)
                FxSystem.PointLight(transform, new Color(1f, 0.75f, 0.35f), 0.45f, 6f, 0.55f, new Vector2(3.5f, 2.6f));
            else if (theme.roomLamps && (def.variant % 3 != 0 || def.kind == RoomKind.Start))
            {
                // a little lamp hanging from the ceiling, flickering softly
                var lamp = new GameObject("Lamp");
                lamp.transform.SetParent(transform, false);
                lamp.transform.localPosition = new Vector2(def.variant % 2 == 0 ? 1.6f : 5.4f, 5.3f);
                var cord = EntityKit.StaticSprite(lamp.transform, PixelArt.Pixel, Sort.Decor, Vector2.zero, new Color(0.2f, 0.16f, 0.2f, 1f));
                cord.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                cord.transform.localScale = new Vector3(0.32f, 3.2f, 1f); // 0.08 x 0.8 units (the pixel sprite is 0.25 units)
                var bulb = lamp.AddComponent<SpriteRenderer>();
                bulb.sprite = PixelArt.Glow;
                bulb.sharedMaterial = GameAssets.I.additive;
                bulb.color = new Color(1f, 0.62f, 0.25f, 0.45f);
                bulb.sortingOrder = Sort.Decor + 1;
                lamp.transform.localScale = Vector3.one * 0.55f;
                m_Lamp = FxSystem.PointLight(lamp.transform, new Color(1f, 0.72f, 0.4f), 0.85f, 6.5f, 0.5f);
                m_LampBase = 1f;
                m_LampSeed = def.variant * 0.37f;
            }
            if (theme.ambient == Ambient.Snow) BuildSnowCaps();
        }

        void BuildSnowCaps()
        {
            if (m_Caps != null) Destroy(m_Caps.gameObject);
            m_Caps = new GameObject("SnowCaps").transform;
            m_Caps.SetParent(transform, false);
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    if (!Solid(def, x, y, m_Merged) || Solid(def, x, y + 1, m_Merged)) continue;
                    var cap = EntityKit.StaticSprite(m_Caps, PixelArt.Pixel, Sort.Terrain + 3, Vector2.zero, new Color(0.96f, 0.98f, 1f, 1f));
                    float lump = ((x * 7 + y * 13 + def.variant) % 3) * 0.04f;
                    cap.transform.localPosition = new Vector3(x + 0.5f, y + 0.95f + lump * 0.5f, 0f);
                    cap.transform.localScale = new Vector3(4.08f, (0.2f + lump) * 4f, 1f); // ~1 x 0.2 units
                }
        }

        Tilemap MakeTilemap(string name, int layer, int order, bool collide)
        {
            var go = new GameObject(name) { layer = layer };
            go.transform.SetParent(transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;
            tr.sharedMaterial = GameAssets.I.spriteLit;
            tr.mode = TilemapRenderer.Mode.Chunk;
            if (collide)
            {
                var rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                var tc = go.AddComponent<TilemapCollider2D>();
                tc.compositeOperation = Collider2D.CompositeOperation.Merge;
                var cc = go.AddComponent<CompositeCollider2D>();
                cc.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }
            return tm;
        }

        Sprite AutoTile(WorldTheme w, int x, int y)
        {
            bool top = !Solid(def, x, y + 1, m_Merged), bottom = !Solid(def, x, y - 1, m_Merged);
            bool left = !Solid(def, x - 1, y, m_Merged), right = !Solid(def, x + 1, y, m_Merged);
            int r = top ? 0 : bottom ? 2 : 1;
            int c = left && right ? 1 : left ? 0 : right ? 2 : 1;
            return Terrain(w.terrain.x + c, w.terrain.y + r);
        }

        // ------------------------------------------------------------------ contents

        void BuildContents(int worldIndex)
        {
            bool glow = LevelCatalog.Worlds[worldIndex].playerLight > 0f;
            switch (def.kind)
            {
                case RoomKind.Start:
                    // a plain label: checkered flags looked like a second finish line next to the trophy
                    WorldLabel("START", new Vector2(3.5f, 4.7f), 5.5f, new Color(0.62f, 1f, 0.62f), new Color32(16, 52, 24, 255));
                    break;
                case RoomKind.Exit:
                    TrekGoal.Create(Contents, new Vector2(3.5f, 1f));
                    BuildGoalMarkers();
                    break;
                case RoomKind.Spikes:
                    for (int x = 1; x <= 5; x++) TrekHazards.Spike(Contents, new Vector2(x + 0.5f, 1.5f));
                    break;
                case RoomKind.Saw:
                    TrekHazards.Saw(Contents, new Vector2(1.6f, 1.1f), new Vector2(5.4f, 1.1f), 1.6f + (def.variant % 3) * 0.3f);
                    break;
                case RoomKind.Fire:
                    TrekHazards.Fire(Contents, new Vector2(2.5f, 1f), 0f);
                    TrekHazards.Fire(Contents, new Vector2(4.5f, 1f), 0.7f);
                    break;
                case RoomKind.Critter:
                    TrekCritter.Create(Contents, def.variant, 1.4f, 5.6f);
                    break;
            }
            // coins sit where the hero is sure to pass: beside each side doorway, or mid-shaft for straight drops
            if (def.coins <= 0) return;
            var spots = new List<Vector2>();
            if (def.Has(RoomDef.L)) spots.Add(new Vector2(1.5f, 1.8f));
            if (def.Has(RoomDef.R)) spots.Add(new Vector2(5.5f, 1.8f));
            if (spots.Count == 0) spots.Add(def.Has(RoomDef.U) && def.Has(RoomDef.D) ? new Vector2(3.5f, 3.6f) : new Vector2(3.5f, 1.8f));
            for (int i = 0; i < spots.Count && i < def.coins; i++) TrekCoin.Create(Contents, spots[i], glow);
        }

        /// <summary>Makes the trophy room impossible to miss and shows which doorway leads in.</summary>
        void BuildGoalMarkers()
        {
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            m_GoalGlow = TrekArt.SlicedRenderer(transform, TrekArt.Glow, center, new Vector2(Size + 0.3f, Size + 0.3f), Sort.FX - 1, new Color(1f, 0.82f, 0.25f, 0.6f), "GoalGlow");

            m_GoalLabel = WorldLabel("GOAL", new Vector2(3.5f, 4.7f), 7f, new Color(1f, 0.86f, 0.3f), new Color32(70, 34, 10, 255)).transform;

            // the way in glows: the exit room only opens towards the route, so every opening is an entrance
            void Door(int side, Vector2 c, Vector2 size)
            {
                if (!def.Has(side)) return;
                m_GoalDoors.Add(TrekArt.SlicedRenderer(transform, TrekArt.Glow, c, size, Sort.WorldUI - 1, new Color(1f, 0.85f, 0.3f, 0.8f), "GoalDoor"));
            }
            Door(RoomDef.L, new Vector2(0.45f, 2f), new Vector2(1.1f, 2.3f));
            Door(RoomDef.R, new Vector2(6.55f, 2f), new Vector2(1.1f, 2.3f));
            Door(RoomDef.U, new Vector2(3.5f, 6.55f), new Vector2(3.3f, 1.1f));
        }

        TextMeshPro WorldLabel(string text, Vector2 at, float size, Color color, Color32 outline)
        {
            var go = new GameObject(text + "Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = at;
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.font = GameAssets.I.font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.outlineColor = outline;
            tmp.outlineWidth = 0.3f;
            tmp.rectTransform.sizeDelta = new Vector2(6f, 2f);
            tmp.GetComponent<MeshRenderer>().sortingOrder = Sort.WorldUI;
            return tmp;
        }

        void AnimateGoal()
        {
            float t = Time.time;
            float k = 0.5f + 0.5f * Mathf.Sin(t * 3f);
            if (m_GoalGlow != null) m_GoalGlow.color = new Color(1f, 0.82f, 0.25f, m_Focus == 2 ? 0f : 0.35f + 0.4f * k);
            if (m_GoalLabel != null) m_GoalLabel.localPosition = new Vector2(3.5f, 4.7f + 0.15f * Mathf.Sin(t * 2.4f));
            bool doors = m_Focus != 1; // once the route is built the hall melts: the doorway hint is done
            foreach (var d in m_GoalDoors) d.color = new Color(1f, 0.85f, 0.3f, doors ? 0.45f + 0.5f * k : 0f);
        }

        // ------------------------------------------------------------------ slide highlight

        /// <summary>Shows that this room can slide, with an arrow pointing to the hole (dir: 0 right, 1 up, 2 left, 3 down).</summary>
        public void SetMovable(bool on, int dir)
        {
            m_Arrow.gameObject.SetActive(on);
            if (!on) { m_Highlight.color = new Color(1f, 0.85f, 0.3f, 0f); return; }
            m_Arrow.GetComponent<SpriteRenderer>().sprite = UIArt.Arrow(dir);
            var c = new Vector2(Size * 0.5f, Size * 0.5f);
            Vector2 d = dir == 0 ? Vector2.right : dir == 1 ? Vector2.up : dir == 2 ? Vector2.left : Vector2.down;
            m_Arrow.localPosition = c + d * (Size * 0.5f - 0.75f);
        }

        /// <summary>Hint booster: this is the room to slide next.</summary>
        public void SetHint(bool on)
        {
            m_Hint = on;
            if (!on && !m_Arrow.gameObject.activeSelf) m_Highlight.color = new Color(1f, 0.85f, 0.3f, 0f);
        }

        /// <summary>When the route is complete its rooms melt into one path and the others step back.</summary>
        public void SetFocus(int focus)
        {
            m_Focus = focus;
            if (focus != 0) { m_Arrow.gameObject.SetActive(false); m_Hint = false; m_Highlight.color = new Color(1f, 0.85f, 0.3f, 0f); }
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (m_Lamp != null)
            {
                float t = Time.time * 7f + m_LampSeed;
                m_Lamp.intensity = m_LampBase * (0.9f + 0.06f * Mathf.Sin(t) + 0.04f * Mathf.Sin(t * 2.7f + 1.3f));
            }
            m_FrameAlpha = Mathf.MoveTowards(m_FrameAlpha, m_Focus == 1 ? 0f : m_Focus == 2 ? 0.35f : 1f, dt * 2.5f);
            m_ShadeAlpha = Mathf.MoveTowards(m_ShadeAlpha, m_Focus == 2 ? 1f : 0f, dt * 2.5f);
            if (m_Frame != null) m_Frame.color = new Color(1f, 1f, 1f, m_FrameAlpha);
            if (m_Shade != null) m_Shade.color = new Color(1f, 1f, 1f, m_ShadeAlpha);
        }

        void Update()
        {
            if (m_GoalLabel != null) AnimateGoal();
            bool movable = m_Arrow != null && m_Arrow.gameObject.activeSelf;
            if (!movable && !m_Hint) return;
            m_Pulse += Time.unscaledDeltaTime;
            float k = 0.5f + 0.5f * Mathf.Sin(m_Pulse * (m_Hint ? 10f : 5f));
            m_Highlight.color = m_Hint ? new Color(0.45f, 1f, 0.6f, 0.45f + 0.4f * k) : new Color(1f, 0.85f, 0.3f, 0.18f + 0.22f * k);
            if (movable) m_Arrow.localScale = Vector3.one * (0.8f + 0.15f * k);
        }
    }
}
