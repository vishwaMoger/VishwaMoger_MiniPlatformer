"""
Builds all Fruit Quest levels into Assets/_Game/Resources/Levels and validates reachability.

    python Tools/levelgen/build_levels.py

Coordinates are y-up: y = 0 is the bottom row. Something standing on ground whose top row is t sits at y = t + 1.
"""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
from levelkit import Level  # noqa: E402
import levels_w1, levels_w2, levels_w3, levels_w4, levels_w5  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "_Game", "Resources", "Levels")


def main():
    os.makedirs(OUT, exist_ok=True)
    builders = levels_w1.LEVELS + levels_w2.LEVELS + levels_w3.LEVELS + levels_w4.LEVELS + levels_w5.LEVELS
    ok = True
    for i, build in enumerate(builders):
        lvl = build()
        notes = lvl.polish()
        problems = lvl.check()
        fruits = len(lvl.find("o")) + 3 * len(lvl.find("Q")) + 5 * len(lvl.find("W"))
        status = "OK " if not problems else "!! "
        print(f"{status}level_{i + 1:02d}  {lvl.name:<18} {lvl.w}x{lvl.h}  fruits={fruits}")
        for n in notes:
            print("      *", n)
        for p in problems:
            print("      -", p)
            if "END" in p:
                ok = False
        lvl.save(os.path.join(OUT, f"level_{i + 1:02d}.txt"))
    print("all levels completable" if ok else "SOME LEVELS ARE NOT COMPLETABLE")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
