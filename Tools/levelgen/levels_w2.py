"""World 2 - Autumn Woods: golden forest. Falling platforms, fire, fans, chargers and shooters."""
from levelkit import Level


def level_4():
    L = Level("Falling Leaves", 200, 22)
    L.profile([(0, 3), (9, 4), (14, 3), (19, None)])
    L.put(3, 4, "P")
    L.jump_arc(6, 4, 12, 5, peak=2)
    L.hint(12, 4, "FALLING PLATFORMS DROP SOON AFTER YOU LAND. KEEP MOVING!")
    for i, x in enumerate(range(21, 34, 3)):
        L.put(x, 4 + (i % 2), "X")
        L.put(x, 7 + (i % 2), "o")
    L.ground(36, 60, 3)
    L.put(43, 4, "n")
    L.hint(46, 4, "HOT! FIRE TRAPS IGNITE A MOMENT AFTER YOU STEP ON THEM.")
    L.hline(50, 52, 3, "f")
    L.jump_arc(48, 4, 54, 4, peak=2)
    # bonus island: reach it from the ledge ahead by jumping back
    L.island(52, 57, 10)
    L.fruits(53, 57, 11)
    L.put(57, 4, "n")
    # step up with an angry pig
    L.ground(61, 75, 6)
    L.hint(62, 7, "ANGRY PIGS TAKE TWO STOMPS!")
    L.put(69, 7, "p")
    L.put(73, 7, "C")
    L.jump_arc(62, 7, 67, 7, peak=2)
    # descending falling-platform chain
    for x, y in ((78, 7), (82, 6), (86, 7), (90, 8)):
        L.put(x, y, "X")
        L.put(x, y + 3, "o")
    L.profile([(94, 5), (104, 6), (111, 5), (121, None)])
    L.put(102, 6, "p")
    L.put(107, 10, "Q")
    L.hline(114, 116, 5, "f")
    L.fruits(113, 117, 9)
    L.put(119, 6, "n")
    # fire floor with a one-way safe route above
    L.ground(121, 140, 3)
    for x in range(124, 138, 2):
        L.put(x, 3, "f")
    L.hline(123, 128, 8, "=")
    L.hline(132, 137, 8, "=")
    L.fruits(123, 128, 10)
    L.fruits(132, 137, 10)
    L.ground(141, 160, 5)
    L.put(143, 6, "C")
    L.put(149, 6, "n")
    L.put(155, 6, "p")
    L.put(152, 9, "H")
    # falling staircase to the goal
    for x, y in ((163, 6), (167, 8), (171, 10)):
        L.put(x, y, "X")
        L.put(x, y + 3, "o")
    L.ground(175, 199, 11)
    L.jump_arc(176, 12, 184, 12, peak=2)
    L.island(179, 186, 17)
    L.fruits(180, 185, 18)
    L.put(187, 12, "n")
    L.put(193, 12, "E")
    return L


def level_5():
    L = Level("Woodland Rush", 212, 24)
    L.ground(0, 25, 3)
    L.put(3, 4, "P")
    L.put(14, 4, "u")
    L.hint(17, 4, "STAND IN THE FAN'S WIND TO FLOAT UP!")
    L.put(23, 4, "F")
    L.column_fruits(22, 7, 12)
    L.ground(26, 40, 12)
    L.hint(28, 13, "RINOS CHARGE WHEN THEY SEE YOU. JUMP OVER THEM!")
    L.put(37, 13, "r")
    L.jump_arc(28, 13, 35, 13, peak=2)
    # moving saws along the forest floor
    L.profile([(41, 3), (50, 4), (53, 3), (61, None)])
    L.put(45, 4, "S").put(55, 4, "~")
    L.arc(44, 56, 7, 2)
    # sunken fan pit
    L.ground(61, 72, 0)
    L.put(63, 1, "F")
    L.put(69, 1, "F")
    L.column_fruits(66, 4, 9)
    L.ground(73, 90, 8)
    L.put(75, 9, "C")
    L.put(80, 9, "u")
    L.put(88, 9, "r")
    # one-way hops between vertical saws
    L.hline(93, 96, 10, "=")
    L.hline(100, 103, 12, "=")
    L.hline(107, 110, 10, "=")
    L.put(98, 8, "S").put(98, 15, "~")
    L.put(105, 8, "S").put(105, 15, "~")
    L.fruits(93, 96, 12)
    L.fruits(100, 103, 14)
    L.fruits(107, 110, 12)
    # rino run
    L.profile([(112, 4), (120, 5), (128, 4), (141, None)])
    L.island(114, 119, 13)
    L.fruits(114, 119, 14)
    L.put(120, 9, "W")
    L.put(125, 7, "H")
    L.put(131, 5, "r")
    L.put(139, 5, "r")
    L.fruits(114, 118, 5)
    # fan lift to the high ledge
    L.ground(141, 151, 4)
    L.put(145, 5, "F")
    L.column_fruits(144, 8, 13)
    L.ground(152, 185, 12)
    L.put(154, 13, "C")
    L.put(160, 13, "S").put(168, 13, "~")
    L.put(174, 13, "u")
    L.put(180, 13, "u")
    L.fruits(170, 178, 15, 2)
    L.ground(186, 211, 3)
    L.jump_arc(186, 4, 196, 4, peak=3)
    L.put(204, 4, "E")
    return L


def level_6():
    L = Level("Timber Trials", 212, 26)
    L.ground(0, 24, 3)
    L.put(3, 4, "P")
    L.hint(8, 4, "PLANTS SPIT SEEDS. DODGE THEM OR STOMP THE PLANT!")
    L.put(20, 4, "t")
    L.jump_arc(9, 4, 17, 4, peak=2)
    # spiked balls under a canopy, with small gaps to jump
    L.ground(25, 45, 3)
    L.clear(32, 34, 0, 3)
    L.clear(39, 41, 0, 3)
    L.rect(27, 44, 14, 15)
    for x in (30, 36, 42):
        L.put(x, 13, "O")
    L.fruits(27, 30, 4)
    L.fruits(43, 45, 4)
    # trunks and an arrow to the upper route
    L.profile([(46, 3), (52, 4), (58, 3), (71, None)])
    L.put(55, 4, "k")
    L.put(66, 4, "k")
    L.put(60, 8, "A")
    L.hline(57, 65, 12, "=")
    L.fruits(57, 65, 14)
    # big box and a plant on a ledge
    L.ground(71, 81, 3)
    L.hint(72, 4, "BIG BOXES NEED THREE HITS!")
    L.put(76, 8, "W")
    L.ground(82, 92, 7)
    L.put(86, 8, "t")
    L.put(90, 8, "C")
    # arrow chain over a chasm
    L.put(95, 8, "A")
    L.put(100, 10, "A")
    L.put(104, 12, "A")
    L.arc(95, 105, 13, 2)
    L.ground(108, 130, 9)
    L.put(118, 10, "k")
    L.rect(120, 126, 19, 20)
    L.put(123, 18, "O")
    L.fruits(110, 114, 10)
    # stairs down
    for i, x in enumerate(range(131, 147, 3)):
        L.ground(x, x + 2, 8 - i)
    L.put(145, 4, "t")
    L.profile([(146, 3), (160, 4), (170, 3), (181, None)])
    L.hline(150, 152, 8, "=")
    L.island(155, 161, 12)
    L.fruits(156, 160, 13)
    L.put(152, 4, "C")
    L.put(158, 4, "k")
    L.put(166, 4, "t")
    L.put(162, 8, "H")
    L.put(172, 8, "W")
    L.fruits(154, 157, 4)
    L.put(176, 4, "k")
    # swinging finale over one-way platforms
    L.hline(184, 187, 6, "=")
    L.hline(191, 194, 6, "=")
    L.hline(198, 201, 6, "=")
    L.put(189, 14, "B").put(189, 13, "O")
    L.put(196, 14, "B").put(196, 13, "O")
    L.fruits(184, 187, 8)
    L.fruits(191, 194, 8)
    L.fruits(198, 201, 8)
    L.ground(203, 211, 3)
    L.put(207, 4, "E")
    return L


LEVELS = [level_4, level_5, level_6]
