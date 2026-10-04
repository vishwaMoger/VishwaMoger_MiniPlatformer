using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MiniPlatformer
{
    public struct TrekResult
    {
        public int level, stars, coins, slides, par;
        public float time, targetTime;
        public bool daily, newRecord, spinToken;
        public int streak, streakBonus;           // wins in a row, and the coin bonus it gave (percent)
        public int chestBefore, chestAfter;       // star chest progress before / after this solve
        public bool chestOpened;
        public int chestBooster;
    }

    /// <summary>
    /// A live Tile Trek board: builds the rooms, takes taps / swipes / arrow keys to slide a room into the empty
    /// slot (carrying the hero along if they're inside), and reports coins, the win and the hero's demise.
    /// </summary>
    public class TrekBoard : MonoBehaviour
    {
        public static TrekBoard I { get; private set; }

        const float SlideTime = 0.2f;
        const float SwipeMin = 0.6f;
        public const float Countdown = 3f;

        public TrekLevel Level { get; private set; }
        public TrekHero Hero { get; private set; }
        public int Slides { get; private set; }
        public int CoinsTaken { get; private set; }
        public int CoinsTotal { get; private set; }
        public float Elapsed { get; private set; }
        public bool Started { get; private set; }
        public bool Finished { get; private set; }
        public bool Revived { get; set; }
        public float CountdownLeft { get; private set; }
        /// <summary>The player's slides have made a complete, safe route from the hero to the trophy.</summary>
        public bool PathComplete { get; private set; }
        public bool AutoSolved { get; private set; }
        public bool AutoSolving { get; private set; }
        /// <summary>0..1: how much of the route the player has linked up so far.</summary>
        public float Progress { get; private set; }

        public event Action OnChanged;

        TrekRoom[] m_ByCell;
        int m_Empty;
        // the empty slot's path since the solved board (scramble + every slide, back-and-forths cancelled):
        // walking it backwards always solves the puzzle, so a hint can never come up empty
        readonly List<int> m_HolePath = new List<int>();
        bool m_Sliding;
        SpriteRenderer m_Slot;
        Camera m_Cam;
        bool m_Pressed;
        Vector2 m_PressWorld;
        int m_PressCell = -1;
        float m_FreezeLeft;
        TrekRoom m_HintRoom;
        float m_HintLeft;
        TrekPath m_Path;
        float m_PathTimer;
        int m_HeroCell = -1;
        float m_BestProgress;
        readonly System.Collections.Generic.HashSet<int> m_Visited = new System.Collections.Generic.HashSet<int>();
        bool m_SaidHalf, m_SaidAlmost;

        float BoardW => Level.w * TrekRoom.Size;
        float BoardH => Level.h * TrekRoom.Size;
        Vector2 CellPos(int c) => new Vector2(Level.CellX(c) * TrekRoom.Size, Level.CellY(c) * TrekRoom.Size);
        Rect CellRect(int c) { var p = CellPos(c); return new Rect(p.x, p.y, TrekRoom.Size, TrekRoom.Size); }

        public static TrekBoard Create(Transform root, TrekLevel level, Camera cam)
        {
            var go = new GameObject("Board");
            go.transform.SetParent(root, false);
            var b = go.AddComponent<TrekBoard>();
            I = b;
            b.Level = level;
            b.m_Cam = cam;
            b.Build();
            return b;
        }

        void OnDestroy() { if (I == this) I = null; }

        // ------------------------------------------------------------------ build

        void Build()
        {
            var theme = LevelCatalog.Worlds[Level.world];
            int cells = Level.w * Level.h;
            m_ByCell = new TrekRoom[cells];
            for (int c = 0; c < cells; c++)
            {
                int id = Level.layout[c];
                if (id < 0) { m_Empty = c; continue; }
                var room = TrekRoom.Create(transform, Level.rooms[id], theme, Level.world, CellPos(c));
                room.cell = c;
                m_ByCell[c] = room;
                CoinsTotal += room.Contents.GetComponentsInChildren<TrekCoin>().Length;
            }

            // tray behind the board + the empty slot
            TrekArt.SlicedRenderer(transform, TrekArt.Slot, new Vector2(BoardW, BoardH) * 0.5f, new Vector2(BoardW + 0.8f, BoardH + 0.8f), Sort.BackWall - 6, new Color(1f, 1f, 1f, 0.75f), "Tray");
            m_Slot = TrekArt.SlicedRenderer(transform, TrekArt.Slot, CellPos(m_Empty) + Vector2.one * (TrekRoom.Size * 0.5f), new Vector2(TrekRoom.Size - 0.2f, TrekRoom.Size - 0.2f), Sort.BackWall - 5, Color.white, "EmptySlot");

            // invisible walls on the sides and top: only falling out of the bottom is fatal
            var edges = new GameObject("Edges") { layer = Layers.Ground };
            edges.transform.SetParent(transform, false);
            void Edge(Vector2 a, Vector2 b) { var e = edges.AddComponent<EdgeCollider2D>(); e.points = new[] { a, b }; }
            Edge(new Vector2(0f, -1f), new Vector2(0f, BoardH + 2f));
            Edge(new Vector2(BoardW, -1f), new Vector2(BoardW, BoardH + 2f));
            Edge(new Vector2(0f, BoardH), new Vector2(BoardW, BoardH));

            // hero in the start room, facing its way out
            var start = RoomOf(Level.startRoom);
            Level.StartSpawn(out float sx, out float facing);
            Hero = TrekHero.Spawn(transform, start.Origin + new Vector2(sx, 1f), facing, -3f, theme.playerLight);
            Hero.Moving = false;
            m_Path = TrekPath.Create(transform);

            FitCamera();
            ParallaxBackground.Create(transform, theme.background, m_Cam, BoardH * 0.5f - 4f);
            AmbientFX.Create(transform, theme.ambient, m_Cam.transform);
            if (theme.lightning) gameObject.AddComponent<TrekStorm>();
            RefreshMovable();
            CountdownLeft = Countdown;
            m_HolePath.Clear();
            m_HolePath.AddRange(Level.scramblePath);
            if (m_HolePath.Count == 0 || m_HolePath[m_HolePath.Count - 1] != m_Empty) { m_HolePath.Clear(); m_HolePath.Add(m_Empty); }
            UpdatePath(true);
        }

        void FitCamera()
        {
            var rig = CameraRig.I;
            rig.SetTarget(null, false);
            rig.clampToBounds = false;
            rig.lookUp = 0f;
            float aspect = m_Cam.aspect > 0.1f ? m_Cam.aspect : 16f / 9f;
            m_Aspect = m_Cam.aspect;
            bool portrait = aspect < 1f;
            // portrait phones: the HUD sits above and the boosters below, so leave more room vertically
            float half = Mathf.Max(BoardH * 0.5f + (portrait ? 4.5f : 2.1f), (BoardW * 0.5f + (portrait ? 0.4f : 1.2f)) / aspect);
            float zoom = K.CameraSize / half;
            rig.SetZoom(zoom);
            rig.ZoomPunch(zoom);
            rig.SnapTo(new Vector2(BoardW * 0.5f, BoardH * 0.5f + 0.9f));
        }

        TrekRoom RoomOf(int id)
        {
            foreach (var r in m_ByCell) if (r != null && r.def.id == id) return r;
            return null;
        }

        /// <summary>Current arrangement as room ids (-1 = empty), for the hint solver.</summary>
        public int[] CurrentLayout()
        {
            var lay = new int[m_ByCell.Length];
            for (int c = 0; c < lay.Length; c++) lay[c] = m_ByCell[c] != null ? m_ByCell[c].def.id : -1;
            return lay;
        }

        // ------------------------------------------------------------------ loop

        void Update()
        {
            if (Finished) return;
            float dt = Time.deltaTime;
            if (!Started)
            {
                CountdownLeft -= dt;
                if (CountdownLeft <= 0f) { Started = true; Hero.Moving = m_FreezeLeft <= 0f; AudioManager.I?.Play("confirm", 0.7f, 0f); OnChanged?.Invoke(); }
            }
            else if (Time.timeScale > 0f) Elapsed += dt;
            if (m_FreezeLeft > 0f && Time.timeScale > 0f)
            {
                m_FreezeLeft -= dt;
                if (m_FreezeLeft <= 0f && Started && !Hero.Dead && !Hero.Won) Hero.Moving = true;
            }
            if (m_HintLeft > 0f)
            {
                m_HintLeft -= Time.unscaledDeltaTime;
                if (m_HintLeft <= 0f) { if (m_HintRoom != null) m_HintRoom.SetHint(false); m_HintRoom = null; }
            }
            if (Time.timeScale > 0f && !AutoSolving) HandleInput();
            CheckVoid(dt);
            m_PathTimer -= Time.unscaledDeltaTime;
            if (m_PathTimer <= 0f) { m_PathTimer = 0.2f; UpdatePath(false); }
            if (Mathf.Abs(m_Cam.aspect - m_Aspect) > 0.01f) FitCamera();
        }

        float m_InVoid;

        /// <summary>
        /// A hero who drops into the empty slot has fallen into the void. Physics could let them land on the roof of the
        /// room below and walk around inside the slot, where nothing can ever slide in again (a dead end the route
        /// simulation already treats as a fall), so they fall for real. Skipped mid-slide: a carried hero briefly sits
        /// over the cell their room just left.
        /// </summary>
        void CheckVoid(float dt)
        {
            if (Hero == null || Hero.Dead || Hero.Won || m_Sliding || AutoSolving || Hero.Carried) { m_InVoid = 0f; return; }
            int c = CellAt(Hero.transform.position);
            bool inVoid = c >= 0 && c == m_Empty;
            m_InVoid = inVoid ? m_InVoid + dt : 0f;
            if (m_InVoid > 0.12f) Hero.Die(true);
        }

        // ------------------------------------------------------------------ route, progress, completion

        int HeroCell()
        {
            if (Hero == null) return -1;
            return CellAt(Hero.transform.position);
        }

        /// <summary>Re-simulates the hero's walk from where they are now and redraws the glowing route.</summary>
        void UpdatePath(bool force)
        {
            if (Hero == null || Hero.Dead || Hero.Won || AutoSolving) return;
            int cell = HeroCell();
            if (!force && cell == m_HeroCell && !m_Dirty) return;
            m_HeroCell = cell;
            m_Dirty = false;
            var lay = CurrentLayout();
            TrekLevel.WalkResult walk;
            if (cell >= 0 && lay[cell] >= 0)
            {
                var o = CellPos(cell);
                float lx = Mathf.Clamp(Hero.transform.position.x - o.x, 0.3f, TrekRoom.Size - 0.3f);
                walk = Level.Walk(lay, cell, lx, Hero.Facing, false);
                // once a safe route to the trophy exists, head straight for it: turn now if the other way is shorter
                var back = Level.Walk(lay, cell, lx, -Hero.Facing, false);
                if (back.reached && (!walk.reached || Length(back.points) + 0.5f < Length(walk.points)))
                {
                    Hero.FaceTowards(-Hero.Facing);
                    walk = back;
                }
                if (walk.reached) Hero.Rushing = true;
                walk.points.Insert(0, (Vector2)Hero.transform.position);
            }
            else walk = Level.WalkFromStart(lay);
            m_Path.Set(walk.points, walk.reached, walk.died);

            // progress: route rooms the hero has walked plus the route rooms their current walk will reach
            if (cell >= 0 && lay[cell] >= 0 && lay[cell] < Level.pathLength) m_Visited.Add(lay[cell]);
            var linked = new System.Collections.Generic.HashSet<int>(m_Visited);
            foreach (int c in walk.cells) if (lay[c] >= 0 && lay[c] < Level.pathLength) linked.Add(lay[c]);
            float progress = walk.reached ? 1f : Mathf.Clamp01((linked.Count - 1f) / Mathf.Max(1f, Level.pathLength - 1f));
            Progress = progress;
            if (walk.reached && !PathComplete) CompletePath(walk);
            else if (Started || Slides > 0) Motivate(progress);
        }

        bool m_Dirty;
        float m_Aspect;

        static float Length(System.Collections.Generic.List<Vector2> pts)
        {
            float l = 0f;
            for (int i = 1; i < pts.Count; i++) l += Vector2.Distance(pts[i - 1], pts[i]);
            return l;
        }

        void Motivate(float progress)
        {
            if (progress <= m_BestProgress + 0.001f || Finished) return;
            m_BestProgress = progress;
            int missing = Mathf.RoundToInt((1f - progress) * (Level.pathLength - 1));
            if (!m_SaidAlmost && Level.pathLength > 2 && (missing == 1 || progress >= 0.8f))
            {
                m_SaidAlmost = m_SaidHalf = true;
                UIRoot.I?.Cheer("OHH, ALMOST THERE! YOU CAN DO IT!", new Color32(255, 214, 74, 255));
            }
            else if (!m_SaidHalf && Level.pathLength > 3 && progress >= 0.5f)
            {
                m_SaidHalf = true;
                UIRoot.I?.Cheer("HALFWAY THERE, KEEP GOING!", new Color32(140, 230, 255, 255));
            }
        }

        /// <summary>The player linked a safe route to the trophy: the route rooms melt into one path, the rest steps back.</summary>
        void CompletePath(TrekLevel.WalkResult walk)
        {
            PathComplete = true;
            Progress = 1f;
            var onRoute = new System.Collections.Generic.HashSet<int>(walk.cells);
            int order = 0;
            foreach (int c in walk.cells)
            {
                var r = m_ByCell[c];
                if (r == null) continue;
                r.SetFocus(1);
                var t = r.transform;
                Tween.Delay(0.08f * order++, () => { if (t != null) Tween.Punch(t, 0.03f, 0.3f); }, true);
            }
            // the walls between consecutive route rooms crumble away: one seamless hall
            for (int i = 1; i < walk.cells.Count; i++)
            {
                int a = walk.cells[i - 1], b = walk.cells[i];
                var ra = m_ByCell[a]; var rb = m_ByCell[b];
                if (ra == null || rb == null) continue;
                float delay = 0.15f + 0.1f * i;
                if (Level.Step(a, RoomDef.R) == b) { ra.MeltWalls(RoomDef.R, delay); rb.MeltWalls(RoomDef.L, delay); }
                else if (Level.Step(a, RoomDef.L) == b) { ra.MeltWalls(RoomDef.L, delay); rb.MeltWalls(RoomDef.R, delay); }
                else if (Level.Step(a, RoomDef.D) == b) rb.MeltWalls(RoomDef.U, delay);
            }
            for (int c = 0; c < m_ByCell.Length; c++)
                if (m_ByCell[c] != null && !onRoute.Contains(c)) m_ByCell[c].SetFocus(2);
            m_Slot.color = new Color(1f, 1f, 1f, 0.35f);
            AudioManager.I?.Play("bank", 0.8f, 0f);
            CameraRig.I?.Shake(0.08f);
            if (!AutoSolving) UIRoot.I?.Cheer("PATH COMPLETE! GO, GO, GO!", new Color32(255, 214, 74, 255));
            OnChanged?.Invoke();
        }

        void HandleInput()
        {
            // keyboard: arrows push the neighbouring room into the hole from that side
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) TrySlideFrom(Level.Step(m_Empty, RoomDef.R));
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) TrySlideFrom(Level.Step(m_Empty, RoomDef.L));
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) TrySlideFrom(Level.Step(m_Empty, RoomDef.D));
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) TrySlideFrom(Level.Step(m_Empty, RoomDef.U));

            // pointer: tap a room next to the hole, or swipe it towards the hole
            bool down = Input.GetMouseButtonDown(0), up = Input.GetMouseButtonUp(0);
            Vector2 screen = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                screen = t.position;
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended;
            }
            if (down)
            {
                if (EventSystem.current != null && (EventSystem.current.IsPointerOverGameObject() || (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)))) return;
                m_Pressed = true;
                m_PressWorld = m_Cam.ScreenToWorldPoint(screen);
                m_PressCell = CellAt(m_PressWorld);
            }
            else if (up && m_Pressed)
            {
                m_Pressed = false;
                Vector2 w = m_Cam.ScreenToWorldPoint(screen);
                var delta = w - m_PressWorld;
                if (m_PressCell < 0) return;
                if (delta.magnitude >= SwipeMin)
                {
                    int side = Mathf.Abs(delta.x) > Mathf.Abs(delta.y) ? (delta.x > 0 ? RoomDef.R : RoomDef.L) : (delta.y > 0 ? RoomDef.U : RoomDef.D);
                    if (Level.Step(m_PressCell, side) == m_Empty) TrySlideFrom(m_PressCell);
                    else Nudge(m_PressCell);
                }
                else if (Level.Neighbours(m_Empty).Contains(m_PressCell)) TrySlideFrom(m_PressCell);
                else Nudge(m_PressCell);
            }
        }

        int CellAt(Vector2 w)
        {
            if (w.x < 0f || w.y < 0f || w.x >= BoardW || w.y >= BoardH) return -1;
            return Level.Cell((int)(w.x / TrekRoom.Size), (int)(w.y / TrekRoom.Size));
        }

        void Nudge(int cell)
        {
            if (cell < 0 || m_ByCell[cell] == null || m_Sliding) return;
            AudioManager.I?.Play("error", 0.35f);
            Tween.Punch(m_ByCell[cell].transform, 0.03f, 0.15f);
        }

        // ------------------------------------------------------------------ sliding

        public bool TrySlideFrom(int cell)
        {
            if (Finished || m_Sliding || cell < 0 || cell >= m_ByCell.Length) return false;
            if (PathComplete && !AutoSolving) { Nudge(cell); return false; } // the route is done: let the hero walk it
            var room = m_ByCell[cell];
            if (room == null || !Level.Neighbours(m_Empty).Contains(cell)) return false;

            bool carry = false;
            if (Hero != null && !Hero.Dead && !Hero.Won && !AutoSolving)
            {
                var hb = Hero.Bounds;
                var heroRect = new Rect(hb.min.x, hb.min.y, hb.size.x, hb.size.y);
                var from = CellRect(cell);
                carry = from.Contains(heroRect.center);
                var inner = new Rect(from.x + 0.1f, from.y + 0.1f, from.width - 0.2f, from.height - 0.2f);
                var dest = CellRect(m_Empty);
                var destPadded = new Rect(dest.x - 0.7f, dest.y - 0.7f, dest.width + 1.4f, dest.height + 1.4f);
                if (!carry && (heroRect.Overlaps(inner) || heroRect.Overlaps(destPadded)))
                {
                    // the hero is in a doorway (or about to step into the hole's space): can't move this one now
                    AudioManager.I?.Play("error", 0.5f);
                    Tween.Punch(room.transform, 0.04f, 0.2f);
                    UIRoot.I?.WorldPopup(heroRect.center + Vector2.up * 1.6f, "WAIT!", new Color32(255, 220, 120, 255), 30f);
                    return false;
                }
            }
            StartCoroutine(SlideRoutine(room, cell, m_Empty, carry));
            return true;
        }

        IEnumerator SlideRoutine(TrekRoom room, int from, int to, bool carry)
        {
            m_Sliding = true;
            if (m_HintRoom != null) { m_HintRoom.SetHint(false); m_HintRoom = null; m_HintLeft = 0f; }
            Vector2 a = CellPos(from), b = CellPos(to);
            m_ByCell[to] = room;
            m_ByCell[from] = null;
            room.cell = to;
            m_Empty = from;
            if (m_HolePath.Count >= 2 && m_HolePath[m_HolePath.Count - 2] == from) m_HolePath.RemoveAt(m_HolePath.Count - 1);
            else m_HolePath.Add(from);
            Slides++;
            if (carry) Hero.SetCarried(true, room.transform);
            foreach (var r in m_ByCell) if (r != null) r.SetMovable(false, 0);
            m_Slot.transform.localPosition = a + Vector2.one * (TrekRoom.Size * 0.5f);
            AudioManager.I?.Play("switch", 0.55f, 0.06f, 0.9f);
            for (float t = 0f; t < SlideTime; t += Time.deltaTime)
            {
                float k = Ease.OutBack(t / SlideTime);
                room.transform.position = Vector2.LerpUnclamped(a, b, k);
                yield return null;
            }
            room.transform.position = b;
            if (carry && Hero != null) Hero.SetCarried(false, transform);
            AudioManager.I?.Play("boxhit", 0.6f, 0.1f);
            Vector2 dir = (b - a).normalized;
            FxSystem.Dust(b + Vector2.one * (TrekRoom.Size * 0.5f) + dir * (TrekRoom.Size * 0.5f), 6, 2f, 0.6f);
            CameraRig.I?.Shake(0.05f);
            m_Sliding = false;
            RefreshMovable();
            m_Dirty = true;
            UpdatePath(true);
            OnChanged?.Invoke();
        }

        void RefreshMovable()
        {
            foreach (var r in m_ByCell) if (r != null) r.SetMovable(false, 0);
            if (Finished || PathComplete) return;
            foreach (int n in Level.Neighbours(m_Empty))
            {
                var r = m_ByCell[n];
                if (r == null) continue;
                int dx = Level.CellX(m_Empty) - Level.CellX(n), dy = Level.CellY(m_Empty) - Level.CellY(n);
                int dir = dx > 0 ? 0 : dx < 0 ? 2 : dy > 0 ? 1 : 3;
                r.SetMovable(true, dir);
            }
        }

        // ------------------------------------------------------------------ boosters

        /// <summary>Freeze booster: the hero stands still for a few seconds.</summary>
        public void Freeze(float seconds)
        {
            if (Finished || Hero == null) return;
            m_FreezeLeft = seconds;
            Hero.Moving = false;
        }

        public bool Frozen => m_FreezeLeft > 0f;

        /// <summary>Editor capture: keep the start countdown running so the hero waits, or release them now.</summary>
        public void HoldStart(bool hold)
        {
            if (!Started) CountdownLeft = hold ? 999f : 0.01f;
        }

        /// <summary>The hero's walk already reaches the trophy: nothing left to slide.</summary>
        public bool RouteReady
        {
            get
            {
                if (PathComplete) return true;
                var lay = CurrentLayout();
                int hc = HeroCell();
                if (!Started || Hero == null || hc < 0 || lay[hc] < 0) return Level.Connected(lay);
                float lx = Mathf.Clamp(Hero.transform.position.x - CellPos(hc).x, 0.3f, TrekRoom.Size - 0.3f);
                return Level.Walk(lay, hc, lx, Hero.Facing, false).reached;
            }
        }

        /// <summary>
        /// Next slide for a route that works from where the hero is right now. Always answers: the best short
        /// solution if one is found quickly, otherwise the next step of undoing the scramble (the planned route works
        /// from any of its rooms). If the hero is off the route in a room that can't be reached that way, 'rescue'
        /// asks for the hero to be sent back to the start first.
        /// </summary>
        public int HeroHint(out bool rescue)
        {
            rescue = false;
            var lay = CurrentLayout();
            int hc = HeroCell();
            bool atStart = !Started || Hero == null || hc < 0 || lay[hc] < 0;
            int cell = atStart
                ? Level.HintMove(lay, 60000, 150)
                : Level.HintMove(lay, lay[hc], Mathf.Clamp(Hero.transform.position.x - CellPos(hc).x, 0.3f, TrekRoom.Size - 0.3f), Hero.Facing, 60000, 150);
            if (cell >= 0) return cell;
            int undo = m_HolePath.Count >= 2 ? m_HolePath[m_HolePath.Count - 2] : -1;
            bool onRoute = atStart || lay[hc] < Level.pathLength;
            if (!onRoute)
            {
                // off the planned route: undoing the scramble won't help here, so look harder before giving up
                cell = Level.HintMove(lay, lay[hc], Mathf.Clamp(Hero.transform.position.x - CellPos(hc).x, 0.3f, TrekRoom.Size - 0.3f), Hero.Facing, 250000, 400);
                if (cell >= 0) return cell;
            }
            if (!onRoute || undo < 0) rescue = true;
            return undo;
        }

        public int HeroHint() => HeroHint(out _);

        /// <summary>The hero is somewhere no route can reach: they hop back to the start room (board unchanged).</summary>
        void RescueHero()
        {
            var start = RoomOf(Level.startRoom);
            Level.StartSpawn(out float sx, out float facing);
            FxSystem.Sparkle(Hero.transform.position, Color.white, 12, 4f);
            Hero.transform.SetParent(transform, true);
            Hero.Respawn(start.Origin + new Vector2(sx, 1f), facing);
            if (Started && m_FreezeLeft <= 0f) Hero.Moving = true;
            FxSystem.Sparkle(Hero.transform.position, new Color(1f, 0.9f, 0.5f), 16, 4f);
            UIRoot.I?.Toast("THE HERO HOPPED BACK TO THE START!");
            m_Dirty = true;
            UpdatePath(true);
        }

        /// <summary>Hint booster: flashes the room to slide next. False only when there is nothing left to slide.</summary>
        public bool ShowHint()
        {
            if (Finished || RouteReady) return false;
            int cell = HeroHint(out bool rescue);
            if (rescue && Hero != null && !Hero.Dead && !Hero.Won)
            {
                RescueHero();
                cell = HeroHint(out _);
            }
            if (cell < 0 || m_ByCell[cell] == null) return false;
            if (m_HintRoom != null) m_HintRoom.SetHint(false);
            m_HintRoom = m_ByCell[cell];
            m_HintRoom.SetHint(true);
            m_HintLeft = 3.5f;
            Tween.Punch(m_HintRoom.transform, 0.06f, 0.4f);
            UIRoot.I?.WorldPopup(m_HintRoom.Origin + new Vector2(TrekRoom.Size * 0.5f, TrekRoom.Size * 0.5f), "SLIDE THIS!", new Color32(255, 230, 120, 255), 34f);
            return true;
        }

        // ------------------------------------------------------------------ events from the hero

        public void OnCoin(Vector2 at)
        {
            CoinsTaken++;
            AudioManager.I?.Play("coin", 0.6f, 0.08f);
            UIRoot.I?.WorldPopup(at + Vector2.up * 0.6f, "+" + Game.CoinValue, UIKit.Gold, 30f);
            OnChanged?.Invoke();
        }

        public void OnStomp()
        {
            Missions.Report(MissionKind.Stomp, 1);
            CameraRig.I?.Shake(0.12f);
        }

        public void OnHeroWon()
        {
            if (Finished) return;
            Finished = true;
            RefreshMovable();
            AudioManager.I?.Play("win", 0.9f, 0f);
            CameraRig.I?.Shake(0.2f);
            int stars = 1;
            if (!AutoSolved && Elapsed <= Level.targetTime) stars |= 2;
            if (!AutoSolved && Slides <= Level.par) stars |= 4;
            var r = new TrekResult
            {
                level = Level.number, stars = stars, coins = CoinsTaken * Game.CoinValue, slides = Slides, par = Level.par,
                time = Elapsed, targetTime = Level.targetTime, daily = Level.daily,
            };
            Tween.Delay(1.2f, () => { if (this != null) Game.I.OnTrekWon(r); });
            OnChanged?.Invoke();
        }

        public void OnHeroDied()
        {
            if (Finished) return;
            Finished = true;
            RefreshMovable();
            Tween.Delay(1.2f, () => { if (this != null) Game.I.OnTrekFailed(); });
            OnChanged?.Invoke();
        }

        /// <summary>Rewarded revive: the hero reappears in the start room (wherever it is now) and the board stays as it is.</summary>
        public void Revive()
        {
            var start = RoomOf(Level.startRoom);
            Level.StartSpawn(out float sx, out float facing);
            Hero.transform.SetParent(transform, true);
            Hero.Respawn(start.Origin + new Vector2(sx, 1f), facing);
            Finished = false;
            Revived = true;
            Started = false;
            CountdownLeft = 2f; // a breather to fix the board
            RefreshMovable();
            m_Dirty = true;
            UpdatePath(true);
            OnChanged?.Invoke();
        }

        // ------------------------------------------------------------------ auto solve (booster)

        /// <summary>
        /// Solves the puzzle for the player: the hero steps out of the way, the rooms slide into place one by one
        /// (the hint solver's moves; on huge boards the rooms glide straight home), and the hero reappears at the start.
        /// Only the "solved" star can be earned this way.
        /// </summary>
        public void AutoSolve()
        {
            if (Finished || AutoSolving || PathComplete) return;
            StartCoroutine(AutoSolveRoutine());
        }

        IEnumerator AutoSolveRoutine()
        {
            AutoSolving = true;
            AutoSolved = true;
            while (m_Sliding) yield return null;
            Hero.Moving = false;
            Hero.gameObject.SetActive(false);
            FxSystem.Sparkle(Hero.transform.position, Color.white, 12, 4f);
            for (int i = 0; i < 40 && !Level.Connected(CurrentLayout()); i++)
            {
                int c = Level.HintMove(CurrentLayout(), 60000);
                if (c < 0) { yield return GlideHome(); break; }
                TrySlideFrom(c);
                while (m_Sliding) yield return null;
                yield return new WaitForSeconds(0.08f);
            }
            if (!Level.Connected(CurrentLayout())) yield return GlideHome();
            var start = RoomOf(Level.startRoom);
            Level.StartSpawn(out float sx, out float facing);
            Hero.gameObject.SetActive(true);
            Hero.transform.SetParent(transform, true);
            Hero.Respawn(start.Origin + new Vector2(sx, 1f), facing);
            AutoSolving = false;
            m_Dirty = true;
            UpdatePath(true);
            if (!PathComplete) { var w = Level.WalkFromStart(CurrentLayout()); if (w.reached) CompletePath(w); }
            if (Started) Hero.Moving = true;
            OnChanged?.Invoke();
        }

        /// <summary>Fallback: every room glides straight to its solved cell.</summary>
        IEnumerator GlideHome()
        {
            var from = new Vector2[m_ByCell.Length];
            var rooms = new TrekRoom[m_ByCell.Length];
            for (int c = 0; c < m_ByCell.Length; c++) { rooms[c] = m_ByCell[c]; if (rooms[c] != null) from[c] = rooms[c].transform.position; }
            var target = new TrekRoom[m_ByCell.Length];
            for (int c = 0; c < m_ByCell.Length; c++)
            {
                var r = rooms[c];
                if (r == null) continue;
                int dest = System.Array.IndexOf(Level.solved, r.def.id);
                target[dest] = r;
            }
            AudioManager.I?.Play("switch", 0.6f);
            for (float t = 0f; t < 0.6f; t += Time.deltaTime)
            {
                float k = Ease.InOutCubic(t / 0.6f);
                for (int c = 0; c < rooms.Length; c++)
                {
                    var r = rooms[c];
                    if (r == null) continue;
                    r.transform.position = Vector2.Lerp(from[c], CellPos(System.Array.IndexOf(Level.solved, r.def.id)), k);
                }
                yield return null;
            }
            for (int c = 0; c < target.Length; c++)
            {
                m_ByCell[c] = target[c];
                if (target[c] != null) { target[c].cell = c; target[c].transform.position = CellPos(c); }
                else m_Empty = c;
            }
            m_Slot.transform.localPosition = CellPos(m_Empty) + Vector2.one * (TrekRoom.Size * 0.5f);
            m_HolePath.Clear();
            m_HolePath.Add(m_Empty); // the board is back in its solved arrangement
            RefreshMovable();
        }
    }

    /// <summary>Rain world: lightning every few seconds.</summary>
    public class TrekStorm : MonoBehaviour
    {
        float m_Next = 4f;

        void Update()
        {
            m_Next -= Time.deltaTime;
            if (m_Next > 0f) return;
            m_Next = UnityEngine.Random.Range(6f, 13f);
            Game.I?.LightningFlash();
        }
    }
}
