using System;
using UnityEngine;

namespace MiniPlatformer
{
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int unlocked = 1;              // number of unlocked levels (1-based)
        public int[] stars = new int[LevelCatalog.LevelCount];       // bitmask: 1 finish, 2 all fruits, 4 no damage
        public float[] bestTime = new float[LevelCatalog.LevelCount]; // 0 = none
        public int character;
        public float music = 0.7f;
        public float sfx = 0.9f;
        public int touchControls;             // 0 auto, 1 on, 2 off
        public bool screenShake = true;
        public int lastWorld;
        public int heroAdUnlocks;             // bitmask of heroes unlocked early with a rewarded ad
    }

    public static class SaveSystem
    {
        const string Key = "fruitquest_save_v1";
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
            // keep arrays sized to the level count if levels were added
            if (s_Data.stars == null || s_Data.stars.Length != LevelCatalog.LevelCount)
                Array.Resize(ref s_Data.stars, LevelCatalog.LevelCount);
            if (s_Data.bestTime == null || s_Data.bestTime.Length != LevelCatalog.LevelCount)
                Array.Resize(ref s_Data.bestTime, LevelCatalog.LevelCount);
            s_Data.unlocked = Mathf.Clamp(s_Data.unlocked, 1, LevelCatalog.LevelCount);
        }

        public static void Save()
        {
            try { Platform.SaveString(Key, JsonUtility.ToJson(Data)); }
            catch (Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        public static void ResetProgress()
        {
            var keep = Data;
            s_Data = new SaveData { heroAdUnlocks = keep.heroAdUnlocks, music = keep.music, sfx = keep.sfx, touchControls = keep.touchControls, screenShake = keep.screenShake };
            Save();
        }

        public static int StarCount(int level) => BitCount(Data.stars[level]);

        public static int TotalStars()
        {
            int n = 0;
            for (int i = 0; i < Data.stars.Length; i++) n += BitCount(Data.stars[i]);
            return n;
        }

        public static bool HeroUnlocked(int i) =>
            TotalStars() >= LevelCatalog.Characters[i].starsRequired || (Data.heroAdUnlocks & (1 << i)) != 0;

        /// <summary>A brand-new player who hasn't cleared the first level yet.</summary>
        public static bool IsNewPlayer => Data.unlocked <= 1 && TotalStars() == 0;

        static int BitCount(int m) => (m & 1) + ((m >> 1) & 1) + ((m >> 2) & 1);

        /// <summary>Records a finished run. Returns true when this set a new best time.</summary>
        public static bool RecordResult(int level, int starMask, float time)
        {
            var d = Data;
            d.stars[level] |= starMask;
            bool best = d.bestTime[level] <= 0f || time < d.bestTime[level];
            if (best) d.bestTime[level] = time;
            d.unlocked = Mathf.Clamp(Mathf.Max(d.unlocked, level + 2), 1, LevelCatalog.LevelCount);
            Save();
            return best;
        }
    }
}
