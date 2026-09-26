"""
Level authoring kit for Fruit Quest.

Levels are authored in Python with y-up coordinates (y = 0 is the bottom row) and exported to the
text format read by the game (Assets/_Game/Resources/Levels/level_XX.txt).

Run:  python Tools/levelgen/build_levels.py
"""
import os
from collections import deque

SOLID = set("#BQWf")          # blocks the player
SUPPORT = SOLID | set("=XMR")  # can be stood on
HAZARD = set("^v<>SKO")       # hurts
WALKERS = set("mclpunrtkw")    # ground enemies (need a floor)
FLYERS = set("bdagzx")
ENEMIES = WALKERS | FLYERS


class Level:
    def __init__(self, name, width, height, hints=None, music=None):
        self.name = name
        self.w = width
        self.h = height
        self.hints = hints or []
        self.music = music
        self.g = [["." for _ in range(width)] for _ in range(height)]  # g[y][x], y up

    # ------------------------------------------------------------------ primitives
    def put(self, x, y, ch):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[y][x] = ch
        return self

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.g[y][x]
        return "#" if y < 0 else "."

    def rect(self, x0, x1, y0, y1, ch="#"):
        for y in range(min(y0, y1), max(y0, y1) + 1):
            for x in range(min(x0, x1), max(x0, x1) + 1):
                self.put(x, y, ch)
        return self

    def ground(self, x0, x1, top, ch="#"):
        """Solid column from the bottom up to (and including) row 'top'."""
        return self.rect(x0, x1, 0, top, ch)

    def clear(self, x0, x1, y0, y1):
        return self.rect(x0, x1, y0, y1, ".")

    def hline(self, x0, x1, y, ch):
        return self.rect(x0, x1, y, y, ch)

    def vline(self, x, y0, y1, ch):
        return self.rect(x, x, y0, y1, ch)

    def text(self, x, y, s):
        for i, c in enumerate(s):
            if c != " ":
                self.put(x + i, y, c)
        return self

    def fruits(self, x0, x1, y, step=1):
        for x in range(x0, x1 + 1, step):
            if self.get(x, y) in ".+":
                self.put(x, y, "o")
        return self

    def arc(self, x0, x1, y_base, height=2):
        n = max(1, x1 - x0)
        for i, x in enumerate(range(x0, x1 + 1)):
            t = i / n
            y = y_base + round(height * 4 * t * (1 - t))
            if self.get(x, y) in ".+":
                self.put(x, y, "o")
        return self

    def column_fruits(self, x, y0, y1):
        for y in range(y0, y1 + 1):
            if self.get(x, y) in ".+":
                self.put(x, y, "o")
        return self

    def profile(self, pts, ch="#"):
        """Stepped terrain: pts = [(x_start, top), ...]; each segment runs to the next x (last one to x_end given as (x, None))."""
        for (x0, top), (x1, _) in zip(pts, pts[1:]):
            if top is not None and top >= 0:
                self.ground(x0, x1 - 1, top, ch)
        return self

    def island(self, x0, x1, top, depth=2, ch="#"):
        """Floating chunk of terrain (at least 2 rows so the autotiler gives it a proper underside)."""
        return self.rect(x0, x1, top - depth + 1, top, ch)

    def trail(self, pts, spacing=1.4):
        """Fruit along a polyline (e.g. tracing a jump arc) - breadcrumbs that teach the route."""
        import math
        placed = set()
        for (ax, ay), (bx, by) in zip(pts, pts[1:]):
            n = max(1, int(math.hypot(bx - ax, by - ay) / spacing))
            for i in range(n + 1):
                x = round(ax + (bx - ax) * i / n)
                y = round(ay + (by - ay) * i / n)
                if (x, y) not in placed and self.get(x, y) in ".+":
                    self.put(x, y, "o")
                    placed.add((x, y))
        return self

    def jump_arc(self, x0, y0, x1, y1, peak=3):
        """Fruit tracing a parabola from (x0,y0) to (x1,y1) that peaks 'peak' tiles above the higher end."""
        n = max(2, abs(x1 - x0))
        top = max(y0, y1) + peak
        pts = []
        for i in range(n + 1):
            t = i / n
            x = x0 + (x1 - x0) * t
            y = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * (2 * top - (y0 + y1) / 2) + t * t * y1
            pts.append((x, y))
        for (x, y) in pts[1:-1:2]:
            xi, yi = round(x), round(y)
            if self.get(xi, yi) in ".+":
                self.put(xi, yi, "o")
        return self

    def hint(self, x, y, text):
        self.put(x, y, "?")
        self.hints.append(text)
        return self

    # ------------------------------------------------------------------ export
    def to_text(self):
        out = [f"name: {self.name}"]
        for h in self.hints:
            out.append(f"hint: {h}")
        if self.music:
            out.append(f"music: {self.music}")
        out.append("---")
        for y in range(self.h - 1, -1, -1):
            out.append("".join(self.g[y]).rstrip("."))
        return "\n".join(out) + "\n"

    def save(self, path):
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(self.to_text())

    # ------------------------------------------------------------------ layout polish
    def _footprint(self, x, y, ch):
        """Cells an entity visually occupies (y-up)."""
        if ch in WALKERS:
            return [(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (0, 1)]
        if ch in "bdag":
            return [(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]
        if ch in "zx":
            return [(x + dx, y + dy) for dx in (-1, 0, 1, 2) for dy in (-2, -1, 0, 1)]
        if ch == "C":
            return [(x + dx, y + dy) for dx in (0, 1) for dy in (0, 1, 2)]
        if ch == "E":
            return [(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (0, 1, 2)]
        if ch == "P":
            return [(x + dx, y + dy) for dx in (-2, -1, 0, 1) for dy in (0, 1)]
        if ch == "?":
            return [(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (0, 1)]
        if ch in "TFA":
            return [(x + dx, y) for dx in (-1, 0, 1)] + [(x, y + 1)]
        if ch in "^v<>":
            return [(x, y)]
        if ch == "S":
            return [(x + dx, y + dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]
        if ch == "f":
            return [(x, y + 1), (x, y + 2)]
        if ch in "KR":
            return [(x + dx, y + dy) for dx in (0, 1) for dy in (-1, 0)]
        if ch in "QWH":
            return [(x, y)]
        return []

    def polish(self):
        """Removes fruit that would overlap other things; returns human-readable notes about placement issues."""
        notes = []
        # lift ground-standing objects that ended up inside raised terrain back onto the surface
        lifted = 0
        for y in range(self.h - 1, -1, -1):
            for x in range(self.w):
                ch = self.g[y][x]
                if (ch in WALKERS or ch in "PCE?TFH") and self.get(x - 1, y) == "#" and self.get(x + 1, y) == "#":
                    top = y
                    while self.get(x, top + 1) == "#" or (self.get(x - 1, top + 1) == "#" and self.get(x + 1, top + 1) == "#" and self.get(x, top + 1) == "."):
                        top += 1
                    if self.get(x, top + 1) in ".+":
                        for yy in range(y, top + 1):
                            self.put(x, yy, "#")
                        self.put(x, top + 1, ch)
                        lifted += 1
        if lifted:
            notes.append(f"lifted {lifted} objects out of raised terrain")
        blocked = set()
        for y in range(self.h):
            for x in range(self.w):
                ch = self.g[y][x]
                if ch != "o":
                    blocked.update(self._footprint(x, y, ch))
        removed = 0
        for y in range(self.h):
            for x in range(self.w):
                if self.g[y][x] == "o" and (x, y) in blocked:
                    self.g[y][x] = "."
                    removed += 1
                elif self.g[y][x] == "o" and self.get(x, y - 1) in "^f":
                    self.g[y][x] = "."
                    removed += 1
        if removed:
            notes.append(f"removed {removed} fruit overlapping other objects")
        p = self.find("P")
        for ch in ENEMIES:
            for (x, y) in self.find(ch):
                if ch in WALKERS and self.get(x, y - 1) not in SUPPORT:
                    notes.append(f"enemy '{ch}' at {(x, y)} has no floor")
                if p and abs(x - p[0][0]) < 10 and abs(y - p[0][1]) < 5:
                    notes.append(f"enemy '{ch}' at {(x, y)} is too close to the start")
        for ch in "C?TFH":
            for (x, y) in self.find(ch):
                if ch in "C?TF" and self.get(x, y - 1) not in SUPPORT:
                    notes.append(f"'{ch}' at {(x, y)} is floating")
        for y in range(self.h):
            for x in range(self.w):
                ch = self.g[y][x]
                if ch in WALKERS or ch in "PCE?TFHo":
                    if self.get(x - 1, y) == "#" and self.get(x + 1, y) == "#":
                        notes.append(f"'{ch}' at {(x, y)} is embedded in terrain")
        for (x, y) in self.find("?"):
            for ch in ENEMIES:
                for (ex, ey) in self.find(ch):
                    if abs(ex - x) < 3 and abs(ey - y) < 3:
                        notes.append(f"sign at {(x, y)} crowded by enemy '{ch}'")
        return notes

    # ------------------------------------------------------------------ validation
    def find(self, ch):
        return [(x, y) for y in range(self.h) for x in range(self.w) if self.g[y][x] == ch]

    def check(self, verbose=True):
        """Approximate reachability: can the player get from P to E (and to every fruit)?"""
        problems = []
        p = self.find("P")
        e = self.find("E")
        if len(p) != 1:
            problems.append(f"expected 1 'P', found {len(p)}")
        if len(e) != 1:
            problems.append(f"expected 1 'E', found {len(e)}")
        if problems:
            return problems

        movers = {}
        # moving platforms / rock heads: their whole path counts as support
        for (mx, my) in self.find("M") + self.find("R"):
            end = self._path_end(mx, my)
            if end:
                ex, ey = end
                steps = max(abs(ex - mx), abs(ey - my))
                for i in range(steps + 1):
                    px = mx + (ex - mx) * i // max(1, steps)
                    py = my + (ey - my) * i // max(1, steps)
                    movers[(px, py)] = True

        def solid(x, y):
            c = self.get(x, y)
            return c in SOLID or (c == "R")

        def support(x, y):
            c = self.get(x, y)
            return c in SUPPORT or (x, y) in movers

        def free(x, y):
            c = self.get(x, y)
            return c not in SOLID and c not in HAZARD and c != "R" and y >= 0

        def stand(x, y):
            if x < 0 or x >= self.w or y < 1 or y >= self.h:
                return False
            return support(x, y - 1) and free(x, y) and self.get(x, y + 1) not in SOLID

        def cling(x, y):
            # wall slide spot: empty cell next to a solid wall column
            if not (0 <= x < self.w and 1 <= y < self.h - 1):
                return False
            if not (free(x, y) and self.get(x, y + 1) not in SOLID):
                return False
            return (solid(x - 1, y) and solid(x - 1, y + 1)) or (solid(x + 1, y) and solid(x + 1, y + 1))

        nodes = set()
        for y in range(self.h):
            for x in range(self.w):
                if stand(x, y) or cling(x, y):
                    nodes.add((x, y))
        # trampolines / arrows / fans become launch nodes
        launch = {}
        for (x, y) in self.find("T"):
            launch[(x, y)] = 7
        for (x, y) in self.find("A"):
            launch[(x, y)] = 5
            nodes.add((x, y))
        for (x, y) in self.find("F"):
            top = y + 1
            while top < self.h - 1 and self.get(x, top) not in SOLID and top - y < 9:
                top += 1
            launch[(x, y)] = top - y + 2
        for k in launch:
            nodes.add(k)

        def reach(dx, dy, extra=0):
            if dy > 5 + extra:
                return False
            lim = 9 - max(0, dy - extra) + min(3, max(0, -dy) // 2)
            return abs(dx) <= max(2, lim)

        start = p[0]
        seen = {start}
        q = deque([start])
        node_list = list(nodes)
        while q:
            cx, cy = q.popleft()
            extra = launch.get((cx, cy), 0)
            for (nx, ny) in node_list:
                if (nx, ny) in seen:
                    continue
                dx, dy = nx - cx, ny - cy
                if abs(dx) > 14 or dy > 5 + extra or dy < -30:
                    continue
                if reach(dx, dy, extra) and not self._blocked(cx, cy, nx, ny):
                    seen.add((nx, ny))
                    q.append((nx, ny))
        ex, ey = e[0]
        if not any(abs(sx - ex) <= 1 and abs(sy - ey) <= 1 for (sx, sy) in seen):
            problems.append(f"END at {e[0]} not reachable")
        missing = []
        for (fx, fy) in self.find("o"):
            if not any(abs(sx - fx) <= 1 and 0 <= fy - sy <= 5 for (sx, sy) in seen):
                missing.append((fx, fy))
        if missing:
            problems.append(f"{len(missing)} fruits maybe unreachable, e.g. {missing[:6]}")
        for (cx, cy) in self.find("C"):
            if not any(abs(sx - cx) <= 1 and abs(sy - cy) <= 1 for (sx, sy) in seen):
                problems.append(f"checkpoint at {(cx, cy)} unreachable")
        return problems

    def _blocked(self, x0, y0, x1, y1):
        # sample the straight line between head positions, plus a raised apex; blocked if every path hits solid
        def hits(points):
            for (x, y) in points:
                c = self.get(int(round(x)), int(round(y)))
                if c in SOLID:
                    return True
            return False

        n = max(abs(x1 - x0), abs(y1 - y0)) * 2 + 1
        straight = [(x0 + (x1 - x0) * i / n, y0 + 1 + (y1 - y0) * i / n) for i in range(1, n)]
        apex = max(y0, y1) + 2
        arc = []
        for i in range(1, n):
            t = i / n
            x = x0 + (x1 - x0) * t
            y = (1 - t) * (1 - t) * (y0 + 1) + 2 * (1 - t) * t * apex + t * t * (y1 + 1)
            arc.append((x, y))
        return hits(straight) and hits(arc)

    def _path_end(self, x, y):
        best = None
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            for i in range(1, 40):
                c = self.get(x + dx * i, y + dy * i)
                if c == "~":
                    if best is None or i < best[0]:
                        best = (i, (x + dx * i, y + dy * i))
                    break
                if not (0 <= x + dx * i < self.w and 0 <= y + dy * i < self.h):
                    break
        return best[1] if best else None
