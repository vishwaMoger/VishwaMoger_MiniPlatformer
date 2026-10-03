using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    public enum RoomKind { Start, Exit, Path, Plain, Spikes, Saw, Critter, Fire }

    /// <summary>One sliding room. Openings: bit 0 left, 1 right, 2 up (ceiling hole), 3 down (floor hole).</summary>
    public class RoomDef
    {
        public const int L = 1, R = 2, U = 4, D = 8;
        public int id;
        public int open;
        public RoomKind kind;
        public int coins;
        public int variant;        // cosmetic / hazard flavour

        public bool Has(int side) => (open & side) != 0;
        public bool Deadly => kind == RoomKind.Spikes || kind == RoomKind.Saw || kind == RoomKind.Critter || kind == RoomKind.Fire;

        public static int Opposite(int side) => side == L ? R : side == R ? L : side == U ? D : U;
    }

    /// <summary>
    /// A Tile Trek puzzle: a grid of rooms with one empty slot. The generator lays out a solved route from the start
    /// room to the trophy (rooms only lead sideways or down, since the hero can't climb), fills the other cells with
    /// decoys and hazard rooms, then scrambles the board with real slides, so every level is solvable.
    /// </summary>
    public class TrekLevel
    {
        public int number, seed, world;
        public int w, h;
        public RoomDef[] rooms;     // by room id
        public int[] solved;        // cell -> room id, -1 empty (the solution)
        public int[] layout;        // cell -> room id, -1 empty (what the player starts with)
        public int startRoom, exitRoom;
        public int pathLength;
        public int par;             // slides for the third star
        public float targetTime;    // seconds for the "fast" star
        public bool daily;

        public int Cell(int x, int y) => y * w + x;
        public int CellX(int c) => c % w;
        public int CellY(int c) => c / w;

        // ------------------------------------------------------------------ generation

        /// <param name="portrait">Phones: boards are taller than wide (columns and rows swapped) to fill an upright screen.</param>
        public static TrekLevel Generate(int number, int seed, bool daily = false, bool portrait = false)
        {
            // a few attempts in case a random walk gets boxed in
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var lv = TryGenerate(number, seed + attempt * 7919, daily, portrait);
                if (lv != null) return lv;
            }
            return TryGenerate(1, seed, daily, portrait); // tiny fallback, always works
        }

        static void SizeFor(int n, bool daily, out int w, out int h)
        {
            if (daily) { w = 4; h = 3; return; }
            if (n <= 2) { w = 3; h = 2; }
            else if (n <= 8) { w = 3; h = 3; }
            else if (n <= 24) { w = 4; h = 3; }
            else if (n <= 45) { w = 4; h = 4; }
            else { w = 5; h = 4; }
        }

        static TrekLevel TryGenerate(int number, int seed, bool daily, bool portrait)
        {
            var rng = new System.Random(seed);
            SizeFor(number, daily, out int w, out int h);
            if (portrait && w > h) { int t = w; w = h; h = t; }
            int cells = w * h;
            var lv = new TrekLevel { number = number, seed = seed, w = w, h = h, daily = daily };
            lv.world = daily ? seed % LevelCatalog.Worlds.Length : ((number - 1) / 6) % LevelCatalog.Worlds.Length;
            float diff = daily ? 0.6f : Mathf.Clamp01((number - 1) / 50f);

            // ---- route: a self-avoiding walk that only goes sideways or down
            int targetLen = Mathf.Clamp(2 + number / 2, 2, cells - 2);
            if (daily) targetLen = cells - 3;
            var path = new List<int>();
            int cx = rng.Next(w), cy = h - 1; // y up: top row = h-1
            path.Add(lv.Cell(cx, cy));
            var visited = new HashSet<int> { path[0] };
            int lastDir = 0;
            while (path.Count < targetLen)
            {
                var options = new List<int>(); // 0 left, 1 right, 2 down
                if (cx > 0 && !visited.Contains(lv.Cell(cx - 1, cy)) && lastDir != 1) options.Add(0);
                if (cx < w - 1 && !visited.Contains(lv.Cell(cx + 1, cy)) && lastDir != 0) options.Add(1);
                if (cy > 0) options.Add(2);
                if (options.Count == 0) break;
                // prefer moving sideways a bit before dropping, so routes wind across the board
                int pick = options[rng.Next(options.Count)];
                if (pick == 2 && options.Count > 1 && rng.NextDouble() < 0.45) pick = options[rng.Next(options.Count)];
                if (pick == 0) cx--; else if (pick == 1) cx++; else cy--;
                lastDir = pick;
                int c = lv.Cell(cx, cy);
                if (visited.Contains(c)) break;
                visited.Add(c);
                path.Add(c);
            }
            if (path.Count < 2) return null;
            if (path.Count >= cells) return null; // keep room for the empty slot

            // ---- rooms along the route
            var rooms = new List<RoomDef>();
            lv.solved = new int[cells];
            for (int i = 0; i < cells; i++) lv.solved[i] = -1;
            for (int i = 0; i < path.Count; i++)
            {
                var r = new RoomDef { id = rooms.Count, kind = i == 0 ? RoomKind.Start : i == path.Count - 1 ? RoomKind.Exit : RoomKind.Path };
                if (i > 0) r.open |= SideTowards(lv, path[i], path[i - 1]);
                if (i < path.Count - 1) r.open |= SideTowards(lv, path[i], path[i + 1]);
                r.coins = r.kind == RoomKind.Path ? 2 : 0;
                r.variant = rng.Next(100);
                rooms.Add(r);
                lv.solved[path[i]] = r.id;
            }
            lv.startRoom = 0;
            lv.exitRoom = path.Count - 1;
            lv.pathLength = path.Count;

            // ---- empty slot + fillers (decoys get scarier as levels go on)
            var free = new List<int>();
            for (int i = 0; i < cells; i++) if (lv.solved[i] < 0) free.Add(i);
            int empty = free[rng.Next(free.Count)];
            float hazardChance = Mathf.Lerp(0.15f, 0.6f, diff);
            foreach (int c in free)
            {
                if (c == empty) continue;
                var r = new RoomDef { id = rooms.Count, variant = rng.Next(100) };
                if (number >= 2 && rng.NextDouble() < hazardChance)
                {
                    var kinds = number < 6 ? new[] { RoomKind.Spikes } :
                                number < 12 ? new[] { RoomKind.Spikes, RoomKind.Critter } :
                                new[] { RoomKind.Spikes, RoomKind.Critter, RoomKind.Saw, RoomKind.Fire };
                    r.kind = kinds[rng.Next(kinds.Length)];
                }
                else
                {
                    r.kind = RoomKind.Plain;
                    r.coins = rng.NextDouble() < 0.35 ? 1 : 0;
                }
                // random doorways: decoys look like they could belong to the route
                int[] sides = { RoomDef.L, RoomDef.R, RoomDef.U, RoomDef.D };
                int n = 1 + rng.Next(2);
                for (int k = 0; k < n; k++) r.open |= sides[rng.Next(4)];
                rooms.Add(r);
                lv.solved[c] = r.id;
            }
            lv.rooms = rooms.ToArray();

            // ---- scramble with real slides (so the solution exists), then sanity-check the start
            int depth = daily ? 24 : Mathf.Clamp(2 + Mathf.RoundToInt(number * 0.55f), 2, 30);
            lv.layout = (int[])lv.solved.Clone();
            int hole = empty, prevHole = -1, moves = 0;
            for (int guard = 0; guard < 400 && (moves < depth || lv.Connected(lv.layout) || lv.StartIsDeadly(lv.layout)); guard++)
            {
                var nbrs = lv.Neighbours(hole);
                nbrs.Remove(prevHole);
                int pickCell = nbrs[rng.Next(nbrs.Count)];
                lv.layout[hole] = lv.layout[pickCell];
                lv.layout[pickCell] = -1;
                prevHole = hole;
                hole = pickCell;
                moves++;
            }
            if (lv.Connected(lv.layout) || lv.StartIsDeadly(lv.layout)) return null;

            lv.par = Mathf.Max(2, Mathf.Min(moves, depth) + 1);
            lv.targetTime = 4f + lv.pathLength * 3.2f + lv.par * 1.1f;
            return lv;
        }

        static int SideTowards(TrekLevel lv, int from, int to)
        {
            int dx = lv.CellX(to) - lv.CellX(from), dy = lv.CellY(to) - lv.CellY(from);
            if (dx < 0) return RoomDef.L;
            if (dx > 0) return RoomDef.R;
            return dy < 0 ? RoomDef.D : RoomDef.U;
        }

        public List<int> Neighbours(int c)
        {
            var list = new List<int>(4);
            int x = CellX(c), y = CellY(c);
            if (x > 0) list.Add(c - 1);
            if (x < w - 1) list.Add(c + 1);
            if (y > 0) list.Add(c - w);
            if (y < h - 1) list.Add(c + w);
            return list;
        }

        // ------------------------------------------------------------------ the hero's walk, simulated

        /// <summary>Where the hero's walk leads on a given arrangement: the route points (board space) and how it ends.</summary>
        public class WalkResult
        {
            public bool reached, died;
            public readonly List<Vector2> points = new List<Vector2>();
            public readonly List<int> cells = new List<int>();
            public int RouteRooms(TrekLevel lv, int[] lay)
            {
                int n = 0;
                foreach (int c in cells) if (lay[c] >= 0 && lay[c] < lv.pathLength) n++;
                return n;
            }
        }

        const float WalkY = 1.6f; // line height above a room's floor tile (local)

        /// <summary>Hero's spawn in the start room (local x and facing), same rule the board uses.</summary>
        public void StartSpawn(out float x, out float dir)
        {
            var sd = rooms[startRoom];
            x = sd.Has(RoomDef.R) ? 1.5f : sd.Has(RoomDef.L) ? 5.5f : 1.1f;
            dir = sd.Has(RoomDef.L) && !sd.Has(RoomDef.R) ? -1f : 1f;
        }

        public WalkResult WalkFromStart(int[] lay)
        {
            StartSpawn(out float x, out float dir);
            return Walk(lay, System.Array.IndexOf(lay, startRoom), x, dir, false);
        }

        /// <summary>
        /// Follows the hero room by room exactly like the physics does: walk ahead, drop through a floor hole when
        /// it is ahead (unless the room below has a solid ceiling, which makes the hole a harmless dip), leave
        /// through an open doorway into a room whose matching doorway is open, otherwise turn around at the wall.
        /// Hazard rooms, the void and the bottom edge are fatal; an endless back-and-forth is a safe dead end.
        /// </summary>
        public WalkResult Walk(int[] lay, int cell, float x, float dir, bool fallingIn)
        {
            var res = new WalkResult();
            var seen = new HashSet<int>();
            for (int guard = 0; guard < 80; guard++)
            {
                if (cell < 0 || lay[cell] < 0) { res.died = true; return res; }
                var r = rooms[lay[cell]];
                var o = new Vector2(CellX(cell) * TrekRoom.Size, CellY(cell) * TrekRoom.Size);
                if (!res.cells.Contains(cell)) res.cells.Add(cell);
                if (fallingIn)
                {
                    if (!r.Has(RoomDef.U)) { res.died = true; return res; } // landed on a roof in the void
                    res.points.Add(o + new Vector2(x, TrekRoom.Size));
                }
                else res.points.Add(o + new Vector2(x, WalkY));
                if (r.Deadly) { res.points.Add(o + new Vector2(3.5f, WalkY)); res.died = true; return res; }
                if (r.id == exitRoom) { res.points.Add(o + new Vector2(3.5f, WalkY)); res.reached = true; return res; }

                // loop guard on (cell, direction, how we came in)
                int key = cell * 8 + (dir > 0f ? 4 : 0) + (fallingIn ? 2 : 0);
                if (!seen.Add(key)) return res;

                int below = Step(cell, RoomDef.D);
                bool holeOpen = r.Has(RoomDef.D) && (below < 0 || lay[below] < 0 || rooms[lay[below]].Has(RoomDef.U));
                if (fallingIn)
                {
                    x = Mathf.Clamp(x, 2.4f, 4.6f);
                    if (holeOpen) { res.points.Add(o + new Vector2(x, WalkY)); cell = below; continue; } // straight through
                    res.points.Add(o + new Vector2(x, WalkY));
                }
                fallingIn = false;

                bool moved = false;
                for (int pass = 0; pass < 2 && !moved; pass++)
                {
                    if (holeOpen && ((dir > 0f && x < 4.5f) || (dir < 0f && x > 2.5f)))
                    {
                        x = 3.5f;
                        res.points.Add(o + new Vector2(x, WalkY));
                        cell = below;
                        fallingIn = true;
                        moved = true;
                        break;
                    }
                    int side = dir > 0f ? RoomDef.R : RoomDef.L;
                    if (r.Has(side))
                    {
                        int n = Step(cell, side);
                        if (n >= 0 && (lay[n] < 0 || rooms[lay[n]].Has(RoomDef.Opposite(side))))
                        {
                            res.points.Add(o + new Vector2(dir > 0f ? TrekRoom.Size : 0f, WalkY));
                            cell = n;
                            x = dir > 0f ? 0f : TrekRoom.Size;
                            moved = true;
                            break;
                        }
                    }
                    // a wall (or a closed neighbour): turn around
                    float wx = dir > 0f ? (r.Has(side) ? 6.8f : 5.7f) : (r.Has(side) ? 0.2f : 1.3f);
                    res.points.Add(o + new Vector2(wx, WalkY));
                    x = wx;
                    dir = -dir;
                }
                if (!moved) return res; // shut in on both sides: walks back and forth safely
            }
            return res;
        }

        /// <summary>The hero's walk from the start already reaches the trophy (the puzzle is solved).</summary>
        public bool Connected(int[] lay) => WalkFromStart(lay).reached;

        /// <summary>The hero would die almost immediately: give the player a fair start.</summary>
        public bool StartIsDeadly(int[] lay)
        {
            var w = WalkFromStart(lay);
            return w.died && w.cells.Count <= 2;
        }

        public int Step(int c, int side)
        {
            int x = CellX(c), y = CellY(c);
            if (side == RoomDef.L) x--; else if (side == RoomDef.R) x++; else if (side == RoomDef.U) y++; else y--;
            if (x < 0 || x >= w || y < 0 || y >= h) return -1;
            return Cell(x, y);
        }

        // ------------------------------------------------------------------ hint solver

        /// <summary>
        /// Next slide towards the solution as (cell that moves into the hole), or -1. Iterative deepening with a
        /// distance heuristic on the route rooms; gives up after a node budget on big boards.
        /// </summary>
        int m_GoalRoom = -1;
        float m_GoalX, m_GoalDir;

        /// <summary>Goal of the hint search: the hero's walk (from the given room/position, or the start) reaches the trophy.</summary>
        bool GoalReached(int[] s)
        {
            if (m_GoalRoom < 0) return Connected(s);
            int c = System.Array.IndexOf(s, m_GoalRoom);
            return c >= 0 && Walk(s, c, m_GoalX, m_GoalDir, false).reached;
        }

        /// <summary>Hint towards a route that works for the hero where they are now (heroRoom = room id they're in).</summary>
        public int HintMove(int[] current, int heroRoom, float heroX, float heroDir, int budget = 120000)
        {
            m_GoalRoom = heroRoom; m_GoalX = heroX; m_GoalDir = heroDir;
            try { return HintMove(current, budget); }
            finally { m_GoalRoom = -1; }
        }

        public int HintMove(int[] current, int budget = 120000)
        {
            var state = (int[])current.Clone();
            var target = new int[rooms.Length];
            for (int c = 0; c < solved.Length; c++) if (solved[c] >= 0) target[solved[c]] = c;
            int bound = Heuristic(state, target);
            int nodes = 0;
            var pathMoves = new List<int>();
            for (int iter = 0; iter < 40; iter++)
            {
                int t = Search(state, target, 0, bound, -1, pathMoves, ref nodes, budget);
                if (t == -1) return pathMoves.Count > 0 ? pathMoves[0] : -1;
                if (t == int.MaxValue || nodes > budget) break;
                bound = t;
            }
            return -1;
        }

        int Heuristic(int[] s, int[] target)
        {
            int sum = 0;
            for (int c = 0; c < s.Length; c++)
            {
                int id = s[c];
                if (id < 0 || id >= pathLength) continue; // only the route rooms matter
                int t = target[id];
                sum += Mathf.Abs(CellX(c) - CellX(t)) + Mathf.Abs(CellY(c) - CellY(t));
            }
            return sum;
        }

        int Search(int[] s, int[] target, int g, int bound, int prevHole, List<int> moves, ref int nodes, int budget)
        {
            int hcost = Heuristic(s, target);
            int f = g + hcost;
            if (f > bound) return f;
            if (GoalReached(s)) return -1;
            if (++nodes > budget) return int.MaxValue;
            int hole = System.Array.IndexOf(s, -1);
            int min = int.MaxValue;
            foreach (int n in Neighbours(hole))
            {
                if (n == prevHole) continue;
                s[hole] = s[n]; s[n] = -1;
                moves.Add(n);
                int t = Search(s, target, g + 1, bound, hole, moves, ref nodes, budget);
                if (t == -1) { s[n] = s[hole]; s[hole] = -1; return -1; }
                moves.RemoveAt(moves.Count - 1);
                s[n] = s[hole]; s[hole] = -1;
                if (t < min) min = t;
            }
            return min;
        }
    }
}
