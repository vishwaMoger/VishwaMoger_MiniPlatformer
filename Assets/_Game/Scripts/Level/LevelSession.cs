using System;
using UnityEngine;

namespace MiniPlatformer
{
    public struct LevelResult
    {
        public int level, starMask, fruits, fruitsTotal;
        public float time;
        public bool newBest, tookDamage, firstPerfect;
    }

    /// <summary>Runtime state of the level being played: hearts, fruits, timer, checkpoint and win/lose flow.</summary>
    public class LevelSession : MonoBehaviour
    {
        public static LevelSession Current { get; private set; }

        public int levelIndex;
        public WorldTheme world;
        public LevelData data;
        public PlayerController player;
        public int maxHearts = 3;
        public int hearts = 3;
        public int fruitsTotal, fruitsCollected;
        public float time;
        public bool tookDamage;
        public bool running;
        public bool finished;
        public bool revived;          // a rewarded revive was used this attempt
        public Vector2 checkpoint;
        public float killY = -3f;
        public SkullBoss boss;
        public EndTrophy end;

        public event Action OnHeartsChanged, OnFruitsChanged;

        void Awake() => Current = this;
        void OnDestroy() { if (Current == this) Current = null; }

        void Update()
        {
            if (!running || player == null) return;
            time += Time.deltaTime;
            if (!player.IsDead && player.Controllable && player.transform.position.y < killY) Fell();
        }

        void Fell()
        {
            AudioManager.I?.Play("hurt", 0.9f);
            CameraRig.I?.Shake(0.35f);
            PostFX.I?.HitPulse();
            if (LoseHeart())
            {
                player.Die();
                return;
            }
            player.RespawnAt(checkpoint);
        }

        /// <summary>Removes a heart. Returns true if the player is out of hearts.</summary>
        public bool LoseHeart()
        {
            tookDamage = true;
            hearts = Mathf.Max(0, hearts - 1);
            OnHeartsChanged?.Invoke();
            return hearts <= 0;
        }

        public bool AddHeart()
        {
            if (hearts >= maxHearts) return false;
            hearts++;
            OnHeartsChanged?.Invoke();
            return true;
        }

        public void CollectFruit()
        {
            fruitsCollected++;
            OnFruitsChanged?.Invoke();
        }

        public void SetCheckpoint(Vector2 feetPos) => checkpoint = feetPos;

        public void Complete()
        {
            if (finished) return;
            finished = true;
            running = false;
            int mask = 1;
            if (fruitsCollected >= fruitsTotal) mask |= 2;
            if (!tookDamage) mask |= 4;
            bool firstPerfect = mask == 7 && SaveSystem.Data.stars[levelIndex] != 7;
            bool best = SaveSystem.RecordResult(levelIndex, mask, time);
            var r = new LevelResult
            {
                level = levelIndex, starMask = mask, fruits = fruitsCollected, fruitsTotal = fruitsTotal,
                time = time, newBest = best, tookDamage = tookDamage, firstPerfect = firstPerfect,
            };
            Game.I.OnLevelComplete(r);
        }

        public void Revive()
        {
            revived = true;
            finished = false;
            hearts = maxHearts;
            OnHeartsChanged?.Invoke();
            player.Revive(checkpoint);
            running = true;
        }

        public void OnPlayerDied()
        {
            if (finished) return;
            finished = true;
            running = false;
            Tween.Delay(1.3f, () => { if (this != null) Game.I.OnGameOver(); });
        }
    }
}
