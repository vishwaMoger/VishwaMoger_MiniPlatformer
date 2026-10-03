using System;
using UnityEngine;

namespace MiniPlatformer
{
    [Serializable]
    public class SaveData
    {
        public int version = 2;
        public int level = 1;                 // next puzzle to play (= highest unlocked)
        public int[] stars = new int[SaveSystem.MaxLevels + 1]; // per level bitmask: 1 finish, 2 fast, 4 smart (par slides)
        public int character;
        public float music = 0.7f;
        public float sfx = 0.9f;
        public bool screenShake = true;
        public int speed = 1;                 // hero walking speed chosen in the HUD (1x-3x)
        public int heroAdUnlocks;             // bitmask of heroes unlocked early with a rewarded ad
        public int heroesBought;              // bitmask of heroes bought with coins

        public int coins;
        public int[] boosters = new int[Boosters.Count];

        // ---- daily loops (day index from Economy.Today)
        public int dailyDay = -1;             // day the login reward was last claimed
        public int dailyStreak;
        public int spinDay = -1;
        public bool freeSpinUsed;
        public int adSpins;
        public int missionDay = -1;
        public int[] missions = new int[Missions.PerDay];
        public int[] missionProgress = new int[Missions.PerDay];
        public int missionsClaimed;
        public int missionRerolls;
        public int coinAdDay = -1;
        public int coinAds;
        public int spinTokens;                // extra lucky spins, earned by solving puzzles
        public int tokenProgress;             // puzzles solved towards the next spin token
        public int giftDay = -1;              // day the shop's free gift was last opened

        // ---- daily puzzle
        public int puzzleDay = -1;            // day the daily puzzle was last solved
        public int puzzleStars;
        public int puzzleStreak;

        // ---- stats
        public int solved;
        public int totalSlides;
    }

    public static class SaveSystem
    {
        public const int MaxLevels = 999;
        const string Key = "tiletrek_save_v1";
        static SaveData s_Data;

        public static SaveData Data
        {
            get
            {
                if (s_Data == null) Load();
                return s_Data;
            }
        }

        public static void Load()
        {
            try
            {
                var json = Platform.LoadString(Key); // CrazyGames cloud data when available, else PlayerPrefs
                s_Data = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Save data corrupt, starting fresh: " + e.Message);
                s_Data = new SaveData();
            }
            if (s_Data.stars == null || s_Data.stars.Length != MaxLevels + 1) Array.Resize(ref s_Data.stars, MaxLevels + 1);
            if (s_Data.boosters == null || s_Data.boosters.Length != Boosters.Count) Array.Resize(ref s_Data.boosters, Boosters.Count);
            if (s_Data.missions == null || s_Data.missions.Length != Missions.PerDay) Array.Resize(ref s_Data.missions, Missions.PerDay);
            if (s_Data.missionProgress == null || s_Data.missionProgress.Length != Missions.PerDay) Array.Resize(ref s_Data.missionProgress, Missions.PerDay);
            s_Data.level = Mathf.Clamp(s_Data.level, 1, MaxLevels);
        }

        public static void Save()
        {
            try { Platform.SaveString(Key, JsonUtility.ToJson(Data)); }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        /// <summary>Puzzle progress only: coins, purchases and daily streaks are kept.</summary>
        public static void ResetProgress()
        {
            var d = Data;
            d.level = 1;
            d.stars = new int[MaxLevels + 1];
            if (!HeroUnlocked(d.character)) d.character = 0;
            Save();
        }

        public static int StarCount(int level) => level >= 1 && level <= MaxLevels ? BitCount(Data.stars[level]) : 0;

        public static int TotalStars()
        {
            int n = 0;
            for (int i = 1; i < Data.stars.Length; i++) n += BitCount(Data.stars[i]);
            return n;
        }

        public static bool HeroUnlocked(int i) =>
            TotalStars() >= LevelCatalog.Characters[i].starsRequired || (Data.heroAdUnlocks & (1 << i)) != 0 || (Data.heroesBought & (1 << i)) != 0;

        /// <summary>Brand-new player: lands straight in the first puzzle.</summary>
        public static bool IsNewPlayer => Data.level <= 1 && TotalStars() == 0;

        static int BitCount(int m) => (m & 1) + ((m >> 1) & 1) + ((m >> 2) & 1);

        /// <summary>Records a solved puzzle and unlocks the next. Returns the stars that are new.</summary>
        public static int RecordResult(int level, int starMask, int slides)
        {
            var d = Data;
            if (level < 1 || level > MaxLevels) return 0;
            int fresh = starMask & ~d.stars[level];
            d.stars[level] |= starMask;
            d.level = Mathf.Clamp(Mathf.Max(d.level, level + 1), 1, MaxLevels);
            d.solved++;
            d.totalSlides += slides;
            Save();
            return fresh;
        }
    }
}
