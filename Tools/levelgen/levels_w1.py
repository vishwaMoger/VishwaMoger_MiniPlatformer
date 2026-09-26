"""World 1 - Sunny Shores: grassy seaside cliffs. Teaches the basics."""
from levelkit import Level


def level_1():
    L = Level("Seaside Start", 164, 22)
    L.profile([(0, 2), (17, 4), (25, 6), (31, None), (36, 3), (62, None), (66, 3), (88, 8), (97, None),
               (113, 6), (128, None), (137, 6), (164, None)])
    # A - learn to move and hop
    L.put(3, 3, "P")
    L.hint(7, 3, "USE A/D OR ARROWS TO MOVE|HOLD THE ARROW PADS TO MOVE")
    L.jump_arc(9, 3, 15, 3, peak=2)
    # B - steps up
    L.hint(14, 3, "SPACE OR W TO JUMP. HOLD IT TO JUMP HIGHER!|TAP JUMP. HOLD IT TO JUMP HIGHER!")
    L.fruits(19, 23, 6, 2)
    L.fruits(26, 29, 8, 3)
    # C - first bay, with a hidden island above it
    L.jump_arc(30, 7, 37, 4, peak=2)
    L.island(40, 46, 9)
    L.fruits(41, 45, 10)
    L.put(43, 12, "o")
    # D - spikes
    L.hint(38, 4, "WATCH OUT FOR SPIKES!")
    L.hline(48, 50, 4, "^")
    L.jump_arc(46, 4, 52, 4, peak=2)
    # E - first enemy
    L.hint(54, 4, "JUMP ON ENEMIES TO STOMP THEM!")
    L.put(59, 4, "m")
    L.jump_arc(61, 4, 66, 4, peak=2)
    # F - checkpoint, box, a little knoll
    L.put(68, 4, "C")
    L.hint(71, 4, "HIT BOXES FROM BELOW FOR FRUIT!")
    L.put(75, 8, "Q")
    L.ground(79, 82, 4)
    L.fruits(79, 82, 5, 3)
    # G - tall wall: double jump
    L.hint(84, 4, "PRESS JUMP AGAIN IN THE AIR TO DOUBLE JUMP!")
    L.column_fruits(87, 6, 10)
    L.fruits(90, 95, 9, 2)
    # H - one-way platforms over the sea
    L.hline(100, 102, 9, "=")
    L.hline(105, 107, 10, "=")
    L.trail([(98, 11), (101, 12), (106, 13), (110, 10)])
    L.put(118, 7, "m")
    # I - wide gap: dash
    L.hint(122, 7, "SHIFT OR X TO DASH THROUGH THE AIR!|TAP DASH TO BURST THROUGH THE AIR!")
    L.trail([(127, 9), (132, 11), (137, 9)])
    # J - goal, with a bonus island
    L.island(141, 146, 11)
    L.fruits(141, 146, 12)
    L.fruits(149, 152, 7)
    L.put(157, 7, "E")
    return L


def level_2():
    L = Level("Double Trouble", 204, 24)
    L.profile([(0, 2), (10, 3), (21, None), (40, 3), (48, 5), (56, 3), (64, 4), (73, 11), (87, 2), (101, None),
               (108, 9), (122, None), (143, 5), (167, 2), (204, None)])
    L.put(3, 3, "P")
    L.jump_arc(5, 3, 10, 4, peak=2)
    L.hint(14, 4, "MOVING PLATFORMS CARRY YOU. HOP ON!")
    # sea crossing on a moving platform
    L.put(23, 5, "M").put(36, 5, "~")
    L.trail([(24, 8), (30, 9), (36, 8)])
    L.put(47, 10, "Q")
    L.fruits(49, 54, 6, 2)
    L.put(59, 4, "c")
    L.hint(65, 5, "TRAMPOLINES SEND YOU FLYING!")
    L.put(70, 5, "T")
    L.column_fruits(70, 9, 14)
    L.fruits(75, 80, 12, 2)
    L.put(83, 12, "c")
    # spike valley with a one-way bridge
    L.hline(89, 97, 3, "^")
    L.hline(88, 98, 6, "=")
    L.fruits(89, 97, 8, 2)
    # vertical lift over the sea
    L.hint(99, 3, "RIDE THE LIFT UP!")
    L.put(104, 4, "M").put(104, 10, "~")
    L.column_fruits(106, 6, 10)
    L.put(110, 10, "C")
    L.fruits(113, 118, 10, 2)
    L.island(114, 119, 15)
    L.fruits(114, 119, 16, 2)
    # two platforms in a row
    L.put(126, 8, "M").put(131, 8, "~")
    L.put(135, 9, "M").put(140, 9, "~")
    L.trail([(125, 11), (132, 13), (141, 11)])
    L.put(150, 6, "c")
    L.put(147, 9, "H")
    L.put(156, 10, "W")
    L.island(151, 158, 14)
    L.fruits(152, 157, 15)
    L.put(162, 6, "c")
    # final drop and a bonus trampoline
    L.put(173, 3, "T")
    L.trail([(170, 8), (173, 12), (177, 8)])
    L.fruits(179, 183, 3, 2)
    L.put(194, 3, "E")
    return L


def level_3():
    # vertical climb up the sea cliff
    L = Level("Cliffside Climb", 60, 64)
    L.ground(0, 59, 2)
    L.rect(0, 1, 0, 63)
    L.rect(58, 59, 0, 63)
    L.put(4, 3, "P")
    L.fruits(7, 11, 3)
    L.put(18, 3, "l")
    L.put(30, 3, "l")
    L.fruits(34, 44, 3, 2)
    # optional bonus zig-zag on the left
    L.hline(8, 12, 6, "=")
    L.hline(14, 18, 9, "=")
    L.hline(8, 12, 12, "=")
    L.fruits(8, 12, 14)
    L.put(16, 11, "H")
    # wall-jump shaft against the right wall (walk in under the inner wall)
    L.rect(52, 53, 7, 30)
    L.hint(48, 3, "HOLD TOWARD A WALL TO SLIDE, THEN JUMP OFF IT!")
    for y in range(9, 29, 3):
        L.put(55 if (y // 3) % 2 == 0 else 56, y, "o")
    # ledge on top of the inner wall
    L.rect(30, 53, 29, 30)
    L.put(50, 31, "C")
    L.put(44, 31, "S").put(36, 31, "~")
    L.fruits(38, 42, 33)
    # arrow boosts upward
    L.hint(32, 31, "GRAB ARROWS TO SOAR!")
    L.put(27, 35, "A")
    L.put(24, 40, "A")
    L.column_fruits(25, 43, 45)
    # upper left platform
    L.rect(4, 22, 44, 45)
    L.put(18, 46, "l")
    L.put(12, 50, "Q")
    L.fruits(6, 10, 46)
    # stepped finish to the top right
    L.hline(26, 30, 49, "=")
    L.hline(33, 37, 52, "=")
    L.hline(40, 44, 55, "=")
    L.put(31, 48, "S").put(31, 54, "~")
    L.fruits(26, 30, 51)
    L.fruits(33, 37, 54)
    L.fruits(40, 44, 57)
    L.rect(46, 57, 57, 58)
    L.put(49, 59, "l")
    L.put(54, 59, "E")
    return L


LEVELS = [level_1, level_2, level_3]
