"""Hand-made set pieces for the store trailer (not shipped in the game).

    python Tools/levelgen/trailer_scenes.py   -> Assets/_Game/Editor/Trailer/*.txt

Header keys read by DevCapture.Scene:
  world:   world theme index (0..4)
  script:  input choreography, triggered by the hero's x position
           J@x:hold  jump (waits until grounded)   D@x:hold  air (double) jump
           X@x       dash                          W@x:secs  stand still
           S@x:scale:secs  slow motion             A@x       auto-run bot from here
  trail:   x0-x1 ranges where fruit is laid along the hero's actual path
  seconds: clip length
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Editor", "Trailer")


class Grid:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.g = [["." for _ in range(w)] for _ in range(h)]

    def put(self, x, y, ch):  # y = 0 is the bottom row
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[self.h - 1 - y][x] = ch

    def rect(self, x0, x1, y0, y1, ch="#"):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.put(x, y, ch)

    def text(self):
        return "\n".join("".join(r) for r in self.g)


def save(name, grid, world, script, trail, seconds):
    os.makedirs(OUT, exist_ok=True)
    stomp = trail.endswith(" stomp")
    trail = trail.replace(" stomp", "")
    head = f"name: {name}\nworld: {world}\nscript: {script}\ntrail: {trail}\nseconds: {seconds}\n"
    if stomp:
        head += "stomp: yes\n"
    head += "---\n"
    with open(os.path.join(OUT, name + ".txt"), "w", encoding="utf-8") as f:
        f.write(head + grid.text() + "\n")


# 1. Sunny Shores: sprint, hop the spikes, double-jump a wide gap, fruit arcs along the way
g = Grid(80, 18)
g.rect(0, 29, 0, 3)
g.rect(38, 79, 0, 3)
g.rect(50, 56, 4, 5)
for x in range(15, 19):
    g.put(x, 4, "^")
g.put(4, 4, "P")
save("s1_shores", g, 0, "J@10.5:0.6 J@25:0.6 D@30:0.5 J@46:0.5", "9-60", 4.2)

# 2. Autumn Woods: stomp a mushroom (slow-mo), then double-jump up onto a ledge
#    (stomp: yes -> DevCapture places a mushroom exactly where the first jump lands)
g = Grid(80, 20)
g.rect(0, 79, 0, 3)
g.rect(36, 79, 4, 7)
g.put(4, 4, "P")
save("s2_woods", g, 1, "J@12:0.45 S@16:0.3:0.5 J@25:0.6 D@29.5:0.5", "10-52 stomp", 4.2)

# 3. Sunset Peaks: jump onto a trampoline (jump held so the launch isn't cut), land on a ledge, launch again
g = Grid(64, 30)
g.rect(0, 63, 0, 3)
for x in (17, 18, 19):
    g.put(x, 4, "T")
g.rect(22, 38, 10, 10, "=")
for x in (33, 34, 35):
    g.put(x, 11, "T")
g.rect(40, 63, 16, 17)
g.put(4, 4, "P")
save("s3_peaks", g, 2, "J@10:1.3 J@26.5:1.3", "12-58", 4.4)

# 4. Jungle Ruins: double jump + mid-air dash across a wide gap over spinning saws
g = Grid(70, 22)
g.rect(0, 24, 0, 3)
g.rect(39, 69, 0, 3)
for sx in (29, 34):
    g.put(sx, 1, "S")
    for y in range(2, 5):
        g.put(sx, y, "~")
g.put(4, 4, "P")
save("s4_ruins", g, 3, "J@21.5:0.6 D@26.5:0.5 X@29.5", "12-50", 4.0)

# 5. Moonlit Castle: lanterns, a fire-trap hop and the finish trophy
g = Grid(60, 20)
g.rect(0, 59, 0, 3)
g.rect(0, 59, 4, 13, "+")
for x in range(16, 19):
    g.put(x, 3, "f")
for x in range(8, 56, 7):
    g.put(x, 10, "L")
g.put(4, 4, "P")
g.put(34, 4, "E")
save("s5_castle", g, 4, "J@12.5:0.6", "11-33", 4.0)

print("trailer scenes written to", os.path.abspath(OUT))
