"""World 3 - Sunset Peaks: pink mountains at dusk. Rock heads, bees, birds, crushers."""
from levelkit import Level


def level_7():
    L = Level("Rosy Ridge", 202, 24)
    L.profile([(0, 3), (12, 4), (21, None)])
    L.put(3, 4, "P")
    L.hint(9, 4, "ROCK HEADS ARE HEAVY, BUT YOU CAN RIDE THEM!")
    L.jump_arc(4, 4, 10, 5, peak=2)
    L.put(22, 5, "R").put(33, 5, "~")
    L.fruits(23, 33, 8, 2)
    L.ground(38, 60, 5)
    L.hint(40, 6, "BEES DROP STINGERS. HIDE UNDER BLOCKS!")
    L.put(47, 12, "b")
    L.hline(51, 56, 9, "B")
    L.put(54, 13, "b")
    L.fruits(51, 56, 6)
    L.put(59, 6, "u")
    # gold block staircase
    for x, y in ((62, 7), (65, 9), (68, 11), (71, 13)):
        L.put(x, y, "B")
        L.put(x, y + 2, "o")
    L.ground(74, 95, 12)
    L.put(76, 13, "C")
    L.put(83, 13, "u")
    L.put(90, 13, "u")
    L.put(86, 19, "b")
    L.fruits(80, 84, 13)
    # two rock heads over the chasm
    L.put(97, 11, "R").put(102, 11, "~")
    L.put(106, 9, "R").put(111, 9, "~")
    L.arc(97, 112, 14, 2)
    L.profile([(114, 5), (121, 6), (131, 5), (141, None)])
    L.hline(135, 137, 10, "B")
    L.island(129, 134, 14)
    L.fruits(129, 134, 15)
    L.put(118, 10, "Q")
    L.hline(124, 130, 9, "B")
    L.put(127, 13, "b")
    L.fruits(124, 130, 6)
    L.put(135, 6, "u")
    L.put(138, 8, "H")
    # bunny hops on gold one-ways
    L.ground(141, 160, 5)
    L.put(142, 6, "C")
    L.hline(146, 150, 9, "=")
    L.hline(153, 157, 12, "=")
    L.put(150, 6, "u")
    L.put(156, 6, "u")
    L.fruits(146, 150, 11)
    L.fruits(153, 157, 14)
    # vertical rock-head lift to the summit
    L.ground(161, 166, 5)
    L.put(163, 7, "R").put(163, 14, "~")
    L.ground(169, 201, 13)
    L.column_fruits(166, 9, 14)
    L.put(178, 14, "u")
    L.jump_arc(170, 14, 177, 14, peak=2)
    L.island(180, 187, 19)
    L.fruits(181, 186, 20)
    L.hline(176, 177, 16, "B")
    L.put(185, 20, "b")
    L.put(194, 14, "E")
    return L


def level_8():
    L = Level("Sunset Sprint", 232, 22)
    L.profile([(0, 3), (20, 4), (31, None)])
    L.put(3, 4, "P")
    L.hint(8, 4, "BLUEBIRDS FLY IN PATROLS. BOUNCE ON THEM!")
    L.put(18, 8, "d").put(26, 8, "~")
    L.fruits(10, 16, 4, 2)
    L.put(28, 4, "r")
    # falling platforms over a canyon
    for i, x in enumerate(range(33, 52, 4)):
        L.put(x, 5 + (i % 2), "X")
        L.put(x, 8 + (i % 2), "o")
    L.put(42, 12, "d").put(50, 12, "~")
    L.profile([(54, 3), (68, 4), (74, 3), (81, None)])
    L.put(58, 4, "S").put(66, 4, "~")
    L.put(72, 4, "r")
    L.put(62, 8, "Q")
    L.fruits(68, 71, 4)
    L.ground(81, 88, 6)
    L.put(84, 7, "C")
    # high road with birds
    L.ground(89, 118, 3)
    L.hline(92, 116, 9, "=")
    L.put(96, 13, "d").put(110, 13, "~")
    L.fruits(92, 116, 11, 3)
    L.put(100, 4, "r")
    L.put(114, 4, "r")
    L.fruits(104, 110, 4, 2)
    # saws on chains across the gap
    L.put(123, 9, "S").put(123, 3, "~")
    L.put(129, 3, "S").put(129, 9, "~")
    L.hline(121, 125, 5, "=")
    L.hline(127, 131, 6, "=")
    L.fruits(121, 131, 9, 2)
    L.ground(134, 160, 4)
    L.put(136, 5, "C")
    L.put(145, 5, "r")
    L.put(150, 10, "d").put(158, 10, "~")
    L.put(154, 7, "H")
    # falling-platform zig zag
    for x, y in ((164, 6), (168, 8), (172, 6), (176, 8), (180, 6)):
        L.put(x, y, "X")
        L.put(x, y + 3, "o")
    L.profile([(184, 4), (200, 5), (208, 4), (220, 6), (232, None)])
    L.island(192, 198, 13)
    L.fruits(193, 197, 14)
    L.put(190, 5, "r")
    L.put(197, 9, "W")
    L.put(204, 10, "d").put(214, 10, "~")
    L.put(210, 5, "r")
    L.jump_arc(215, 5, 222, 7, peak=2)
    L.put(226, 7, "E")
    return L


def level_9():
    # vertical climb to the summit
    L = Level("Summit Spikes", 56, 72)
    L.ground(0, 55, 2)
    L.rect(0, 1, 0, 71)
    L.rect(54, 55, 0, 71)
    L.put(4, 3, "P")
    L.hint(8, 3, "SPIKE HEADS SLAM DOWN WHEN YOU PASS BELOW!")
    # crusher corridor
    L.rect(14, 40, 14, 15)
    L.put(18, 12, "K")
    L.put(27, 12, "K")
    L.put(36, 12, "K")
    L.fruits(16, 40, 3, 3)
    # narrow wall-jump shaft on the right
    L.rect(48, 49, 7, 34)
    for y in range(8, 32, 4):
        L.put(51 if (y // 4) % 2 == 0 else 52, y, "o")
    L.rect(26, 49, 33, 34)
    L.put(44, 35, "C")
    L.put(36, 35, "u")
    # bat cave
    L.hline(28, 32, 38, "=")
    L.hline(20, 24, 41, "=")
    L.rect(4, 22, 44, 45)
    L.put(30, 42, "a")
    L.put(12, 42, "a")
    L.fruits(28, 32, 40)
    L.fruits(20, 24, 43)
    L.put(8, 46, "u")
    L.put(18, 46, "H")
    # the upper climb
    L.hline(26, 30, 49, "=")
    L.hline(34, 38, 52, "=")
    L.hline(42, 50, 55, "=")
    L.put(40, 47, "S").put(40, 54, "~")
    L.fruits(26, 30, 51)
    L.fruits(34, 38, 54)
    L.fruits(43, 49, 57, 2)
    L.put(46, 62, "a")
    L.hline(33, 37, 59, "=")
    L.put(39, 57, "S").put(39, 63, "~")
    L.fruits(33, 37, 61)
    # summit with spikes and safe spots
    L.rect(4, 28, 60, 61)
    L.hline(5, 27, 62, "^")
    L.clear(8, 12, 62, 62)
    L.clear(17, 20, 62, 62)
    L.clear(24, 27, 62, 62)
    L.fruits(17, 20, 62)
    L.put(10, 62, "E")
    return L


LEVELS = [level_7, level_8, level_9]
