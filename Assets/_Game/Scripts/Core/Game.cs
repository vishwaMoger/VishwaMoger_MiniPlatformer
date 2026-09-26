using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MiniPlatformer
{
    /// <summary>
    /// Bootstrap and game flow. The whole game runs in one scene: menus sit over a live animated backdrop,
    /// and levels are built from text data on demand (instant restarts, tiny build size).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        [SerializeField] GameAssets assets;

        public Camera Cam { get; private set; }
        public bool IsPaused { get; private set; }
        public bool InLevel => m_Session != null;
        public int CurrentLevel { get; private set; }
        public LevelSession Session => m_Session;

        Transform m_WorldRoot;
        LevelSession m_Session;
        MenuStage m_Menu;
        Light2D m_Global;
        bool m_Busy;
        bool m_ShowingResults;

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
            // new players land straight in the (tutorial) first level; returning players get the title screen
            if (SaveSystem.IsNewPlayer) PlayLevel(0);
            else ShowMenuInstant(MenuScreen.Title);
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
            if (InLevel && !m_ShowingResults && m_Session.running && GameInput.PauseDown) TogglePause();
            else if (!InLevel && GameInput.PauseDown) UIRoot.I.MenuBack();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && InLevel && !IsPaused && !m_ShowingResults && m_Session.running) TogglePause();
        }

        // ---------------------------------------------------------------- world helpers

        public void ApplyWorld(WorldTheme w)
        {
            m_Global.color = w.lightColor;
            m_Global.intensity = w.lightIntensity;
            Cam.backgroundColor = w.sky;
            PostFX.I?.ApplyWorld(w);
        }

        void ClearWorld()
        {
            if (m_WorldRoot != null) Destroy(m_WorldRoot.gameObject);
            m_WorldRoot = new GameObject("World").transform;
            m_Session = null;
            m_Menu = null;
            GameInput.ClearTouch();
        }

        // ---------------------------------------------------------------- menus

        void ShowMenuInstant(MenuScreen screen)
        {
            ClearWorld();
            BuildMenuStage(SaveSystem.Data.lastWorld);
            UIRoot.I.ShowMenu(screen);
            AudioManager.I.PlayMusic("Music/menu");
            StartCoroutine(UIRoot.I.IrisOpen(new Vector2(0.5f, 0.5f)));
        }

        void BuildMenuStage(int world)
        {
            m_Menu = MenuStage.Create(m_WorldRoot, world, Cam);
            ApplyWorld(LevelCatalog.Worlds[world]);
        }

        /// <summary>Changes the live menu backdrop with a crossfade (title tour, level select browsing).</summary>
        public void SetMenuWorld(int world)
        {
            if (m_Menu == null || m_Menu.World == world) return;
            // freeze the current frame, swap the world underneath, then fade the frozen frame away
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
            yield return UIRoot.I.IrisClose(new Vector2(0.5f, 0.5f));
            SetPaused(false);
            m_ShowingResults = false;
            UIRoot.I.HideInGame();
            ClearWorld();
            int world = screen == MenuScreen.LevelSelect ? LevelCatalog.WorldOf(CurrentLevel) : SaveSystem.Data.lastWorld;
            BuildMenuStage(world);
            UIRoot.I.ShowMenu(screen, world);
            AudioManager.I.PlayMusic("Music/menu");
            yield return UIRoot.I.IrisOpen(new Vector2(0.5f, 0.5f));
            m_Busy = false;
        }

        // ---------------------------------------------------------------- levels

        /// <param name="adBreak">true when coming from a results / game-over screen: a natural break for a midgame ad.</param>
        public void PlayLevel(int index, bool adBreak = false)
        {
            if (m_Busy) return;
            StartCoroutine(LoadLevelRoutine(Mathf.Clamp(index, 0, LevelCatalog.LevelCount - 1), adBreak));
        }

        public void RestartLevel() => PlayLevel(CurrentLevel);
        public void RetryAfterBreak() => PlayLevel(CurrentLevel, true);
        public void NextLevel() => PlayLevel(Mathf.Min(CurrentLevel + 1, LevelCatalog.LevelCount - 1), true);
        public bool HasNextLevel => CurrentLevel + 1 < LevelCatalog.LevelCount;

        IEnumerator LoadLevelRoutine(int index, bool adBreak)
        {
            m_Busy = true;
            Platform.GameplayStop();
            Vector2 center = new Vector2(0.5f, 0.5f);
            if (m_Session != null && m_Session.player != null) center = Cam.WorldToViewportPoint(m_Session.player.transform.position);
            yield return UIRoot.I.IrisClose(center);
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

            CurrentLevel = index;
            UIRoot.I.RememberStars();
            int world = LevelBuilder.WorldFor(index);
            SaveSystem.Data.lastWorld = world;
            var theme = LevelCatalog.Worlds[world];
            ApplyWorld(theme);
            m_Session = LevelBuilder.Build(index, m_WorldRoot, Cam);
            CameraRig.I.SetZoom(1f);
            CameraRig.I.ZoomPunch(1.15f);
            var data = m_Session.data;
            AudioManager.I.PlayMusic(!string.IsNullOrEmpty(data.music) ? "Music/" + data.music : theme.music);
            UIRoot.I.ShowHUD(m_Session);
            m_Session.player.Controllable = false;
            yield return null;
            yield return null; // let composite colliders & lights settle

            Vector2 pc = Cam.WorldToViewportPoint(m_Session.player.transform.position);
            yield return UIRoot.I.IrisOpen(pc);
            UIRoot.I.Banner($"{LevelCatalog.Code(index)}  {LevelCatalog.LevelNames[index].ToUpperInvariant()}", theme.name.ToUpperInvariant());
            m_Session.player.PlayAppear();
            m_Session.running = true;
            Platform.SetLevelContext(index);
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

        public void OnLevelComplete(LevelResult r)
        {
            m_ShowingResults = true;
            Platform.GameplayStop();
            // celebrate real milestones only: the final boss, or a first-time perfect (3-star) clear
            if (r.level == LevelCatalog.LevelCount - 1 || (r.starMask == 7 && r.firstPerfect)) Platform.HappyTime();
            AudioManager.I.Duck(0.25f, 2.5f);
            AudioManager.I.Play("complete", 0.9f, 0f);
            UIRoot.I.ShowResults(r);
        }

        public void OnGameOver()
        {
            m_ShowingResults = true;
            Platform.GameplayStop();
            AudioManager.I.Duck(0.3f, 2.5f);
            AudioManager.I.Play("gameover", 0.9f, 0f);
            UIRoot.I.ShowGameOver();
        }

        /// <summary>Rewarded revive: back to the last checkpoint with full hearts.</summary>
        public void Revive()
        {
            if (!InLevel || !m_ShowingResults || m_Session.player == null) return;
            m_ShowingResults = false;
            SetPaused(false);
            UIRoot.I.CloseGameOver();
            m_Session.Revive();
            AudioManager.I.Play("checkpoint", 0.9f, 0f);
            Platform.GameplayStart();
        }
    }
}
