using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>
    /// Bootstrap and game flow. The whole game runs in one scene: menus sit over a live animated backdrop and
    /// puzzles are generated on demand from their level number (instant restarts, endless levels, tiny build).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        /// <summary>Wallet coins per coin picked up in a puzzle.</summary>
        public const int CoinValue = 2;
        /// <summary>Coins for solving a puzzle (plus per star).</summary>
        public const int SolveReward = 10, StarReward = 5;
        /// <summary>Coin price of a revive (Basic Launch has no rewarded videos).</summary>
        public const int RevivePrice = 40;

        [SerializeField] GameAssets assets;

        public Camera Cam { get; private set; }
        public bool IsPaused { get; private set; }
        public bool InLevel => m_Board != null;
        public int CurrentLevel { get; private set; } = 1;
        public bool InDaily { get; private set; }
        public TrekBoard Board => m_Board;
        public bool Busy => m_Busy;

        Transform m_WorldRoot;
        TrekBoard m_Board;
        MenuStage m_Menu;
        Light2D m_Global;
        bool m_Busy;
        bool m_ShowingResults;

        public static int WorldForLevel(int level) => ((Mathf.Max(1, level) - 1) / 6) % LevelCatalog.Worlds.Length;

        void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            m_Busy = true;
            StartCoroutine(Boot());
        }

        /// <summary>Waits for the CrazyGames SDK (so saves come from the cloud), then builds everything.</summary>
        IEnumerator Boot()
        {
            yield return Platform.Init();
            Setup();
            yield return null;
            m_Busy = false;
            // new players land straight in the first puzzle; returning players get the title screen
            if (SaveSystem.IsNewPlayer) PlayLevel(1);
            else
            {
                ShowMenuInstant(MenuScreen.Title);
                UIRoot.I.OfferDailyReward(1.1f);
            }
            StartCoroutine(MenuWorldCycle());
        }

        /// <summary>The title screen tours all five worlds, crossfading to the next one every few seconds.</summary>
        IEnumerator MenuWorldCycle()
        {
            const float hold = 6.5f;
            float t = 0f;
            while (true)
            {
                yield return null;
                bool idle = !InLevel && !m_Busy && m_Menu != null && UIRoot.I.OnTitleIdle;
                t = idle ? t + Time.unscaledDeltaTime : 0f;
                if (t < hold) continue;
                t = 0f;
                SetMenuWorld((m_Menu.World + 1) % LevelCatalog.Worlds.Length);
            }
        }

        void Setup()
        {
            Application.targetFrameRate = Application.isMobilePlatform ? 60 : -1;
            QualitySettings.vSyncCount = Application.isMobilePlatform ? 0 : 1;
            Time.fixedDeltaTime = 1f / 60f;
            Layers.ConfigureCollisionMatrix();
            SaveSystem.Load();

            Cam = Camera.main;
            if (Cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                Cam = camGo.AddComponent<Camera>();
            }
            Cam.orthographic = true;
            Cam.orthographicSize = K.CameraSize;
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color32(40, 30, 50, 255);
            Cam.transform.position = new Vector3(0, 0, -10);
            var urp = Cam.GetUniversalAdditionalCameraData();
            urp.renderPostProcessing = true;
            urp.antialiasing = AntialiasingMode.None;
            if (Cam.GetComponent<CameraRig>() == null) Cam.gameObject.AddComponent<CameraRig>();

            var lightGo = new GameObject("GlobalLight");
            lightGo.transform.SetParent(transform, false);
            m_Global = lightGo.AddComponent<Light2D>();
            m_Global.lightType = Light2D.LightType.Global;
            m_Global.intensity = 1f;

            AudioManager.Create();
            FxSystem.Create();
            PostFX.Create();
            UIRoot.Create();
#if UNITY_EDITOR
            Application.runInBackground = true; // keep play mode ticking while the Editor is unfocused
            gameObject.AddComponent<DevConsole>();
#endif
        }

        void Update()
        {
            if (m_Busy) return;
            if (InLevel && !m_ShowingResults && GameInput.PauseDown) TogglePause();
            else if (!InLevel && GameInput.PauseDown) UIRoot.I.MenuBack();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && InLevel && !IsPaused && !m_ShowingResults) TogglePause();
        }

        // ---------------------------------------------------------------- world helpers

        public void ApplyWorld(WorldTheme w, float minLight = 0f)
        {
            m_Global.color = w.lightColor;
            m_Global.intensity = Mathf.Max(w.lightIntensity, minLight);
            Cam.backgroundColor = w.sky;
            PostFX.I?.ApplyWorld(w);
        }

        Coroutine m_Flash;

        /// <summary>Storm lightning: a double flash of the global light, thunder a moment later.</summary>
        public void LightningFlash()
        {
            if (m_Flash != null) StopCoroutine(m_Flash);
            m_Flash = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            float baseI = m_Global.intensity;
            var baseC = m_Global.color;
            var white = new Color(0.85f, 0.9f, 1f);
            for (int k = 0; k < 2; k++)
            {
                m_Global.color = white;
                for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
                {
                    m_Global.intensity = Mathf.Lerp(2.2f, baseI, t / 0.18f);
                    yield return null;
                }
                yield return new WaitForSecondsRealtime(0.07f);
            }
            m_Global.intensity = baseI;
            m_Global.color = baseC;
            yield return new WaitForSecondsRealtime(Random.Range(0.3f, 0.9f));
            AudioManager.I?.Play("thunder", 0.75f, 0.1f);
            CameraRig.I?.Shake(0.08f);
            m_Flash = null;
        }

        void ClearWorld()
        {
            if (m_Flash != null) { StopCoroutine(m_Flash); m_Flash = null; }
            if (m_WorldRoot != null) Destroy(m_WorldRoot.gameObject);
            m_WorldRoot = new GameObject("World").transform;
            m_Board = null;
            m_Menu = null;
        }

        // ---------------------------------------------------------------- menus

        void ShowMenuInstant(MenuScreen screen)
        {
            ClearWorld();
            BuildMenuStage(WorldForLevel(SaveSystem.Data.level));
            UIRoot.I.ShowMenu(screen);
            AudioManager.I.PlayMusic("Music/menu");
            StartCoroutine(UIRoot.I.IrisOpen(new Vector2(0.5f, 0.5f)));
        }

        void BuildMenuStage(int world)
        {
            m_Menu = MenuStage.Create(m_WorldRoot, world, Cam);
            ApplyWorld(LevelCatalog.Worlds[world]);
        }

        /// <summary>Changes the live menu backdrop with a crossfade (title tour).</summary>
        public void SetMenuWorld(int world)
        {
            if (m_Menu == null || m_Menu.World == world) return;
            var rt = RenderTexture.GetTemporary(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height), 24);
            var prev = Cam.targetTexture;
            Cam.targetTexture = rt;
            Cam.Render();
            Cam.targetTexture = prev;
            ClearWorld();
            BuildMenuStage(world);
            UIRoot.I.FadeBackdropSnapshot(rt, 1.3f);
        }

        public void GoToMenu(MenuScreen screen)
        {
            if (m_Busy) return;
            StartCoroutine(GoToMenuRoutine(screen));
        }

        IEnumerator GoToMenuRoutine(MenuScreen screen)
        {
            m_Busy = true;
            Platform.GameplayStop();
            Platform.ClearLevelContext();
            yield return UIRoot.I.IrisClose(new Vector2(0.5f, 0.5f));
            SetPaused(false);
            m_ShowingResults = false;
            UIRoot.I.HideInGame();
            ClearWorld();
            SaveSystem.Save();
            BuildMenuStage(WorldForLevel(InDaily ? SaveSystem.Data.level : CurrentLevel));
            UIRoot.I.ShowMenu(screen);
            AudioManager.I.PlayMusic("Music/menu");
            yield return UIRoot.I.IrisOpen(new Vector2(0.5f, 0.5f));
            m_Busy = false;
        }

        // ---------------------------------------------------------------- puzzles

        /// <param name="adBreak">true when coming from a result screen: a natural break for a midgame ad.</param>
        public void PlayLevel(int level, bool adBreak = false)
        {
            if (m_Busy) return;
            StartCoroutine(LoadRoutine(Mathf.Clamp(level, 1, SaveSystem.MaxLevels), false, adBreak));
        }

        /// <summary>Today's puzzle: the same for everyone, new every day.</summary>
        public void PlayDaily(bool adBreak = false)
        {
            if (m_Busy) return;
            StartCoroutine(LoadRoutine(CurrentLevel, true, adBreak));
        }

        public void RestartLevel() { if (InDaily) PlayDaily(); else PlayLevel(CurrentLevel); }
        public void RetryAfterBreak() { if (InDaily) PlayDaily(true); else PlayLevel(CurrentLevel, true); }
        public void NextLevel() => PlayLevel(CurrentLevel + 1, true);

        /// <summary>Phones (and any upright screen) play portrait-shaped boards.</summary>
        public static bool PortraitLevels => ForcePortrait ?? (Platform.IsMobileDevice || UIRoot.Portrait);
        /// <summary>Editor capture (trailer / covers) picks the board shape regardless of the window.</summary>
        public static bool? ForcePortrait;

        public static TrekLevel BuildLevel(int level, bool daily) =>
            daily ? TrekLevel.Generate(20, Economy.Today * 7331 + 11, true, PortraitLevels) : TrekLevel.Campaign(level, PortraitLevels);

        /// <summary>What PLAY starts: the next new level, or once all are beaten the first one still missing stars.</summary>
        public static int NextToPlay()
        {
            var d = SaveSystem.Data;
            if (d.stars[SaveSystem.MaxLevels] == 0) return Mathf.Clamp(d.level, 1, SaveSystem.MaxLevels);
            for (int n = 1; n <= SaveSystem.MaxLevels; n++) if (d.stars[n] != 7) return n;
            return SaveSystem.MaxLevels;
        }

        IEnumerator LoadRoutine(int level, bool daily, bool adBreak)
        {
            m_Busy = true;
            Platform.GameplayStop();
            Platform.ClearLevelContext();
            yield return UIRoot.I.IrisClose(new Vector2(0.5f, 0.5f));
            if (adBreak)
            {
                // the SDK paces midgame ads itself (and ignores early requests); the game waits behind the closed iris
                bool adDone = false;
                Platform.Midgame(() => adDone = true);
                while (!adDone) yield return null;
            }
            SetPaused(false);
            m_ShowingResults = false;
            UIRoot.I.HideMenus();
            UIRoot.I.HideInGame();
            ClearWorld();

            CurrentLevel = level;
            InDaily = daily;
            TrekHero.SpeedMul = Mathf.Clamp(SaveSystem.Data.speed, 1, 3);
            var lv = BuildLevel(level, daily);
            var theme = LevelCatalog.Worlds[lv.world];
            ApplyWorld(theme);
            m_Global.intensity = theme.puzzleLight; // night worlds stay dark and let the room lamps do the work
            m_Board = TrekBoard.Create(m_WorldRoot, lv, Cam);
            AudioManager.I.PlayMusic(theme.music);
            UIRoot.I.ShowTrekHUD(m_Board);
            yield return null;
            yield return UIRoot.I.IrisOpen(new Vector2(0.5f, 0.5f));
            Platform.SetLevelContext(daily ? "daily" : level.ToString());
            Platform.GameplayStart();
            m_Busy = false;
        }

        public void TogglePause()
        {
            if (!InLevel) return;
            SetPaused(!IsPaused);
            if (IsPaused) { Platform.GameplayStop(); UIRoot.I.ShowPause(); }
            else { UIRoot.I.HidePause(); Platform.GameplayStart(); }
        }

        void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioManager.I?.SetMuffled(paused);
        }

        public void OnTrekWon(TrekResult r)
        {
            m_ShowingResults = true;
            Platform.GameplayStop();
            int fresh;
            if (r.daily)
            {
                var d = SaveSystem.Data;
                bool first = d.puzzleDay != Economy.Today;
                d.puzzleStreak = d.puzzleDay == Economy.Today - 1 ? d.puzzleStreak + 1 : first ? 1 : d.puzzleStreak;
                fresh = r.stars & ~(first ? 0 : d.puzzleStars);
                d.puzzleStars = first ? r.stars : d.puzzleStars | r.stars;
                d.puzzleDay = Economy.Today;
                if (first) Missions.Report(MissionKind.Daily, 1);
                SaveSystem.Save();
            }
            else fresh = SaveSystem.RecordResult(r.level, r.stars, r.slides);
            r.newRecord = fresh != 0;

            int starCount = (r.stars & 1) + ((r.stars >> 1) & 1) + ((r.stars >> 2) & 1);
            r.coins += SolveReward + StarReward * starCount + (r.daily ? 50 : 0);
            r.spinToken = Economy.AddTokenProgress();
            Economy.AddCoins(r.coins);
            Missions.Report(MissionKind.ClearLevels, 1);
            Missions.Report(MissionKind.Coins, r.coins);
            if (r.stars == 7) Missions.Report(MissionKind.ThreeStars, 1);
            if ((r.stars & 2) != 0) Missions.Report(MissionKind.FastClear, 1);
            if ((r.stars & 4) != 0) Missions.Report(MissionKind.FewSlides, 1);
            // celebrate sparingly: a first perfect solve, or finishing a season world for the first time
            bool worldDone = !r.daily && r.level % 6 == 0 && SaveSystem.Data.stars[r.level] == fresh && fresh != 0;
            if ((r.stars == 7 && (fresh & 7) != 0) || worldDone) Platform.HappyTime();

            AudioManager.I.Duck(0.25f, 2.5f);
            AudioManager.I.Play("complete", 0.9f, 0f);
            UIRoot.I.ShowTrekResults(r);
        }

        public void OnTrekFailed()
        {
            m_ShowingResults = true;
            Platform.GameplayStop();
            SaveSystem.Save();
            AudioManager.I.Duck(0.3f, 2.5f);
            AudioManager.I.Play("gameover", 0.9f, 0f);
            UIRoot.I.ShowTrekFail();
        }

        /// <summary>Rewarded revive: the hero reappears in the start room, the board stays as the player left it.</summary>
        public void Revive()
        {
            if (!InLevel || !m_ShowingResults) return;
            m_ShowingResults = false;
            SetPaused(false);
            UIRoot.I.CloseTrekFail();
            m_Board.Revive();
            AudioManager.I.Play("checkpoint", 0.9f, 0f);
            UIRoot.I.Toast("REVIVED! FIX THE PATH!"); // confirm the rewarded video paid out
            Platform.GameplayStart();
        }

    }
}
