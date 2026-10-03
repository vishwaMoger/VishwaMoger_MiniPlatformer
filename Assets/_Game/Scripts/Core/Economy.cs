using System;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Coins and the daily loops that bring players back: a 7-day login streak, a lucky spin
    /// (one free spin a day, more with videos). All state lives in SaveData.
    /// </summary>
    public static class Economy
    {
        static readonly DateTime Epoch = new DateTime(2024, 1, 1);

        /// <summary>Local calendar day number (changes at the player's midnight).</summary>
        public static int Today => (int)(DateTime.Now.Date - Epoch).TotalDays;

        public static int Coins => SaveSystem.Data.coins;
        public static event Action OnCoinsChanged;

        public static void AddCoins(int amount, bool save = true)
        {
            if (amount <= 0) return;
            SaveSystem.Data.coins += amount;
            if (save) SaveSystem.Save();
            OnCoinsChanged?.Invoke();
        }

        public static bool Spend(int amount)
        {
            if (amount < 0 || SaveSystem.Data.coins < amount) return false;
            SaveSystem.Data.coins -= amount;
            SaveSystem.Save();
            OnCoinsChanged?.Invoke();
            return true;
        }

        // ---------------------------------------------------------------- daily login streak

        public static readonly int[] DailyRewards = { 50, 80, 120, 160, 220, 300, 500 };

        public static bool DailyReady => SaveSystem.Data.dailyDay != Today;

        /// <summary>Which of the 7 days is claimable now (or was claimed today).</summary>
        public static int DailyIndex
        {
            get
            {
                var d = SaveSystem.Data;
                if (d.dailyDay == Today) return Mathf.Max(0, d.dailyStreak - 1) % DailyRewards.Length;
                bool continues = d.dailyDay == Today - 1;
                return (continues ? d.dailyStreak : 0) % DailyRewards.Length;
            }
        }

        /// <summary>Claims today's login reward (mult 2 after a video). Returns the coins given.</summary>
        public static int ClaimDaily(int mult)
        {
            if (!DailyReady) return 0;
            var d = SaveSystem.Data;
            int index = DailyIndex;
            d.dailyStreak = d.dailyDay == Today - 1 ? d.dailyStreak + 1 : 1;
            d.dailyDay = Today;
            int coins = DailyRewards[index] * Mathf.Max(1, mult);
            AddCoins(coins, false);
            if (index == DailyRewards.Length - 1) Boosters.Give(-1, 1, false); // day 7: one of every booster
            SaveSystem.Save();
            return coins;
        }

        // ---------------------------------------------------------------- lucky spin

        public struct WheelSlot
        {
            public string label; public int coins; public int booster; public int weight; public Color color;
            public WheelSlot(string l, int c, int b, int w, Color col) { label = l; coins = c; booster = b; weight = w; color = col; }
        }

        public static readonly WheelSlot[] Wheel =
        {
            new WheelSlot("40", 40, -1, 22, new Color32(255, 196, 64, 255)),
            new WheelSlot("FREEZE", 0, Boosters.Freeze, 12, new Color32(96, 176, 255, 255)),
            new WheelSlot("80", 80, -1, 18, new Color32(255, 128, 96, 255)),
            new WheelSlot("HINT", 0, Boosters.Hint, 12, new Color32(140, 220, 90, 255)),
            new WheelSlot("150", 150, -1, 10, new Color32(255, 196, 64, 255)),
            new WheelSlot("SOLVE", 0, Boosters.Solve, 6, new Color32(190, 140, 255, 255)),
            new WheelSlot("60", 60, -1, 14, new Color32(255, 128, 96, 255)),
            new WheelSlot("500", 500, -1, 2, new Color32(255, 90, 160, 255)),
        };

        public const int MaxAdSpins = 3;

        static void RollSpinDay()
        {
            var d = SaveSystem.Data;
            if (d.spinDay == Today) return;
            d.spinDay = Today;
            d.freeSpinUsed = false;
            d.adSpins = 0;
        }

        public static bool FreeSpinReady { get { RollSpinDay(); return !SaveSystem.Data.freeSpinUsed; } }

        /// <summary>Extra spins earned by playing: one for every few puzzles solved.</summary>
        public const int PuzzlesPerToken = 3;
        public static int SpinTokens => SaveSystem.Data.spinTokens;
        public static bool SpinReady => FreeSpinReady || SpinTokens > 0;

        /// <summary>A puzzle was solved: progress towards the next spin token. True when a token was earned.</summary>
        public static bool AddTokenProgress()
        {
            var d = SaveSystem.Data;
            d.tokenProgress++;
            if (d.tokenProgress < PuzzlesPerToken) return false;
            d.tokenProgress = 0;
            d.spinTokens++;
            return true;
        }

        /// <summary>The shop's free gift (one booster a day).</summary>
        public static bool GiftReady => SaveSystem.Data.giftDay != Today;

        public static int OpenGift()
        {
            var d = SaveSystem.Data;
            d.giftDay = Today;
            int b = UnityEngine.Random.Range(0, Boosters.Count - 1); // freeze or hint (solve stays special)
            Boosters.Give(b, 1, false);
            SaveSystem.Save();
            return b;
        }
        public static int AdSpinsLeft { get { RollSpinDay(); return MaxAdSpins - SaveSystem.Data.adSpins; } }

        /// <summary>Picks a slot, gives the prize and records the spin. Returns the slot index.</summary>
        public static int Spin(bool free)
        {
            RollSpinDay();
            var d = SaveSystem.Data;
            if (free) d.freeSpinUsed = true;
            else if (d.spinTokens > 0) d.spinTokens--;
            else d.adSpins++;
            int total = 0;
            foreach (var s in Wheel) total += s.weight;
            int r = UnityEngine.Random.Range(0, total), pick = 0;
            for (int i = 0; i < Wheel.Length; i++) { if (r < Wheel[i].weight) { pick = i; break; } r -= Wheel[i].weight; }
            var slot = Wheel[pick];
            if (slot.coins > 0) AddCoins(slot.coins, false);
            if (slot.booster >= 0) Boosters.Give(slot.booster, 1, false);
            SaveSystem.Save();
            return pick;
        }

        /// <summary>Something on the title screen wants attention (daily reward, free spin, finished mission).</summary>
        public static bool AnythingReady => DailyReady || SpinReady || Missions.AnyClaimable || GiftReady;
    }

    /// <summary>Puzzle helpers bought with coins, won on the wheel or unlocked with a video. Used from the in-game buttons.</summary>
    public static class Boosters
    {
        public const int Count = 3;
        public const int Freeze = 0, Hint = 1, Solve = 2;

        public static readonly string[] Names = { "FREEZE", "HINT", "SOLVE" };
        public static readonly string[] Descriptions = { "STOPS THE HERO FOR 6 SECONDS", "SHOWS THE NEXT SLIDE", "SOLVES THE PUZZLE FOR YOU" };
        public static readonly int[] Prices = { 80, 120, 300 };

        public static int Owned(int b) => SaveSystem.Data.boosters[b];

        public static void Give(int b, int amount, bool save = true)
        {
            var d = SaveSystem.Data;
            if (b < 0) { for (int i = 0; i < Count; i++) d.boosters[i] += amount; }
            else d.boosters[b] += amount;
            if (save) SaveSystem.Save();
        }

        public static bool Buy(int b)
        {
            if (!Economy.Spend(Prices[b])) return false;
            Give(b, 1);
            return true;
        }

        /// <summary>Uses one owned booster. False if none left.</summary>
        public static bool Use(int b)
        {
            var d = SaveSystem.Data;
            if (d.boosters[b] <= 0) return false;
            d.boosters[b]--;
            SaveSystem.Save();
            return true;
        }
    }

    public enum MissionKind { ClearLevels, ThreeStars, Coins, Stomp, FastClear, Daily, FewSlides }

    /// <summary>Three daily quests that pay coins. A video swaps a quest the player doesn't like.</summary>
    public static class Missions
    {
        public const int PerDay = 3;
        public const int MaxRerolls = 3;

        public struct Def
        {
            public MissionKind kind; public int target, reward; public string text;
            public Def(MissionKind k, int t, int r, string s) { kind = k; target = t; reward = r; text = s; }
            public string Text => string.Format(text, target);
        }

        public static readonly Def[] Pool =
        {
            new Def(MissionKind.ClearLevels, 3, 60, "SOLVE {0} PUZZLES"),
            new Def(MissionKind.ClearLevels, 6, 120, "SOLVE {0} PUZZLES"),
            new Def(MissionKind.ThreeStars, 2, 90, "GET 3 STARS ON {0} PUZZLES"),
            new Def(MissionKind.ThreeStars, 4, 160, "GET 3 STARS ON {0} PUZZLES"),
            new Def(MissionKind.Coins, 20, 60, "COLLECT {0} COINS ON THE WAY"),
            new Def(MissionKind.Coins, 50, 120, "COLLECT {0} COINS ON THE WAY"),
            new Def(MissionKind.Stomp, 2, 80, "DROP THE HERO ON {0} ENEMIES"),
            new Def(MissionKind.FastClear, 3, 90, "SOLVE {0} PUZZLES FAST"),
            new Def(MissionKind.FewSlides, 3, 100, "SOLVE {0} PUZZLES AT OR UNDER PAR"),
            new Def(MissionKind.Daily, 1, 120, "SOLVE THE DAILY PUZZLE"),
        };

        public static event Action OnChanged;

        /// <summary>Hands out a fresh set of missions when the day changes.</summary>
        public static void EnsureToday()
        {
            var d = SaveSystem.Data;
            if (d.missionDay == Economy.Today) return;
            d.missionDay = Economy.Today;
            d.missionsClaimed = 0;
            d.missionRerolls = 0;
            var rng = new System.Random(Economy.Today * 7919 + 17);
            for (int i = 0; i < PerDay; i++)
            {
                d.missions[i] = PickNew(rng, i, false);
                d.missionProgress[i] = 0;
            }
            SaveSystem.Save();
        }

        /// <summary>A pool index whose kind isn't used by the other slots (only slots before 'slot' when filling a new day).</summary>
        static int PickNew(System.Random rng, int slot, bool othersFilled)
        {
            var d = SaveSystem.Data;
            for (int tries = 0; tries < 50; tries++)
            {
                int idx = rng.Next(Pool.Length);
                bool clash = false;
                for (int j = 0; j < PerDay; j++)
                {
                    if (j == slot || (!othersFilled && j > slot)) continue;
                    if (Pool[d.missions[j]].kind == Pool[idx].kind) clash = true;
                }
                if (!clash) return idx;
            }
            return rng.Next(Pool.Length);
        }

        public static Def Get(int slot) { EnsureToday(); return Pool[Mathf.Clamp(SaveSystem.Data.missions[slot], 0, Pool.Length - 1)]; }
        public static int Progress(int slot) { EnsureToday(); return SaveSystem.Data.missionProgress[slot]; }
        public static bool Done(int slot) => Progress(slot) >= Get(slot).target;
        public static bool Claimed(int slot) { EnsureToday(); return (SaveSystem.Data.missionsClaimed & (1 << slot)) != 0; }

        public static bool AnyClaimable
        {
            get
            {
                for (int i = 0; i < PerDay; i++) if (Done(i) && !Claimed(i)) return true;
                return false;
            }
        }

        /// <summary>Progress event from gameplay (adds up).</summary>
        public static void Report(MissionKind kind, int amount)
        {
            if (amount <= 0) return;
            EnsureToday();
            var d = SaveSystem.Data;
            bool changed = false, completed = false;
            for (int i = 0; i < PerDay; i++)
            {
                var def = Pool[d.missions[i]];
                if (def.kind != kind || Claimed(i)) continue;
                int before = d.missionProgress[i];
                int now = before + amount;
                now = Mathf.Min(now, def.target);
                if (now == before) continue;
                d.missionProgress[i] = now;
                changed = true;
                if (before < def.target && now >= def.target)
                {
                    completed = true;
                    UIRoot.I?.Toast("MISSION COMPLETE!");
                    AudioManager.I?.Play("jingle_small", 0.7f, 0f);
                }
            }
            // small steps ride along with the next save (banks, level ends); finished missions save right away
            if (completed) SaveSystem.Save();
            if (changed) OnChanged?.Invoke();
        }

        public static int Claim(int slot)
        {
            if (!Done(slot) || Claimed(slot)) return 0;
            SaveSystem.Data.missionsClaimed |= 1 << slot;
            int reward = Get(slot).reward;
            Economy.AddCoins(reward);
            OnChanged?.Invoke();
            return reward;
        }

        public const int RerollPrice = 25;
        public static bool CanReroll(int slot) => !Claimed(slot) && SaveSystem.Data.missionRerolls < MaxRerolls;

        public static void Reroll(int slot)
        {
            EnsureToday();
            var d = SaveSystem.Data;
            var rng = new System.Random(Environment.TickCount);
            int old = d.missions[slot];
            int pick = old;
            for (int i = 0; i < 20 && pick == old; i++) pick = PickNew(rng, slot, true);
            d.missions[slot] = pick;
            d.missionProgress[slot] = 0;
            d.missionRerolls++;
            SaveSystem.Save();
            OnChanged?.Invoke();
        }
    }
}
