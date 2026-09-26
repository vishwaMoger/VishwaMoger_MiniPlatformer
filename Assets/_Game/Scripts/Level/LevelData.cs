using System.Collections.Generic;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// A level parsed from a tiny text file (Resources/Levels/level_XX.txt).
    /// Header lines "key: value" are followed by a line "---" and the ASCII map (row 0 = top).
    ///
    /// Map legend
    ///   #  ground (autotiled world terrain)     B  solid block           =  one-way platform
    ///   +  background wall (decor)              P  player start          E  end trophy
    ///   C  checkpoint    o  fruit    H  heart     ?  hint sign   L lantern   ~ path marker (for movers)
    ///   ^ v &lt; &gt;  spikes (up/down/left/right)   S  saw   M  moving platform   R  rock head   K  spike head
    ///   O  spiked ball   T  trampoline   F  fan   f  fire trap   X  falling platform   A  arrow boost
    ///   Q  box (1 hit)   W  big box (3 hits)
    ///   enemies: m mushroom  c chicken  l slime  p angry pig  u bunny  n snail  r rino  t plant  k trunk
    ///            b bee  d bluebird  a bat  g ghost  z fat bird  w turtle  x skull boss
    /// </summary>
    public class LevelData
    {
        public string name = "Level";
        public readonly List<string> hints = new List<string>();
        public string music;
        public int width, height;
        char[,] m_Map; // [x, row]

        public char At(int x, int row)
        {
            if (x < 0 || row < 0 || x >= width || row >= height) return '\0';
            return m_Map[x, row];
        }

        /// <summary>World-space y of the bottom of a map row.</summary>
        public int RowToY(int row) => height - 1 - row;

        public static LevelData Parse(string text)
        {
            var d = new LevelData();
            var lines = text.Replace("\r", "").Split('\n');
            int i = 0;
            for (; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Trim() == "---") { i++; break; }
                int c = line.IndexOf(':');
                if (c <= 0) continue;
                var key = line.Substring(0, c).Trim().ToLowerInvariant();
                var val = line.Substring(c + 1).Trim();
                switch (key)
                {
                    case "name": d.name = val; break;
                    case "hint": d.hints.Add(val); break;
                    case "music": d.music = val; break;
                }
            }
            var rows = new List<string>();
            for (; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(";")) continue; // comment
                rows.Add(lines[i].TrimEnd());
            }
            while (rows.Count > 0 && rows[rows.Count - 1].Length == 0) rows.RemoveAt(rows.Count - 1);
            d.height = rows.Count;
            foreach (var r in rows) d.width = Mathf.Max(d.width, r.Length);
            d.m_Map = new char[d.width, d.height];
            for (int row = 0; row < d.height; row++)
                for (int x = 0; x < d.width; x++)
                    d.m_Map[x, row] = x < rows[row].Length ? rows[row][x] : '.';
            return d;
        }
    }
}
