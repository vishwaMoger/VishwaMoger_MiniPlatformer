using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPlatformer
{
    public partial class UIRoot
    {
        LevelSession m_Session;
        Image[] m_Hearts;
        TextMeshProUGUI m_FruitText, m_TimerText;
        RectTransform m_FruitIcon;
        GameObject m_Pause, m_Results, m_Touch, m_BossBar;
        Image m_BossFill;
        int m_ShownHearts;

        // ---------------------------------------------------------------- HUD

        public void ShowHUD(LevelSession s)
        {
            HideInGame();
            m_Session = s;
            UIKit.Group(m_HudLayer.gameObject).alpha = 1f;
            var root = UIKit.Stretch(m_HudLayer, "HUD");

            // hearts
            var heartsRt = UIKit.Rect(root, "Hearts", new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(360f, 100f), new Vector2(0f, 1f));
            m_Hearts = new Image[s.maxHearts];
            for (int i = 0; i < s.maxHearts; i++)
            {
                m_Hearts[i] = UIKit.Image(heartsRt, UIArt.Heart, new Vector2(0f, 0.5f), new Vector2(50f + i * 92f, 0f), new Vector2(78f, 72f), "Heart" + i);
            }
            m_ShownHearts = s.hearts;

            // fruit counter
            var fruitRt = UIKit.Rect(root, "Fruits", new Vector2(0f, 1f), new Vector2(40f, -140f), new Vector2(340f, 90f), new Vector2(0f, 1f));
            UIKit.Panel(fruitRt, "glass", new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(300f, 84f));
            var icon = UIKit.Image(fruitRt, GameAssets.I.Sprite("PA1/Items/Fruits/Apple"), new Vector2(0f, 0.5f), new Vector2(46f, 2f), new Vector2(112f, 112f), "FruitIcon");
            icon.gameObject.AddComponent<UIImageAnim>().SetFrames(GameAssets.I.Frames("PA1/Items/Fruits/Apple"));
            m_FruitIcon = icon.rectTransform;
            m_FruitText = UIKit.Text(fruitRt, "", 30f, new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(200f, 60f), TextStyle.Heading, UIKit.Cream, TextAlignmentOptions.Left);

            // level + timer
            var top = UIKit.Rect(root, "Top", new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(420f, 90f));
            UIKit.Panel(top, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 84f));
            UIKit.Text(top, LevelCatalog.Code(s.levelIndex), 26f, new Vector2(0.5f, 0.5f), new Vector2(-120f, 0f), new Vector2(120f, 50f), TextStyle.Heading, UIKit.Gold);
            UIKit.Image(top, UIArt.Clock, new Vector2(0.5f, 0.5f), new Vector2(-30f, 0f), new Vector2(40f, 40f));
            m_TimerText = UIKit.Text(top, "00:00.0", 26f, new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(200f, 50f), TextStyle.Heading);

            // pause
            UIKit.IconButton(root, UIArt.Pause, "blue", new Vector2(1f, 1f), new Vector2(-90f, -80f), 110f, () => Game.I.TogglePause());

            // boss bar
            var boss = UIKit.Rect(root, "BossBar", new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(760f, 70f));
            UIKit.Panel(boss, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 70f));
            var fillBg = UIKit.Panel(boss, "track", new Vector2(0.5f, 0.5f), new Vector2(90f, 0f), new Vector2(540f, 36f));
            m_BossFill = UIKit.Panel(fillBg.transform, "red", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(540f, 36f), "Fill");
            m_BossFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            UIKit.Text(boss, "SKULL KING", 20f, new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Vector2(200f, 40f), TextStyle.Heading, UIKit.Gold);
            m_BossBar = boss.gameObject;
            m_BossBar.SetActive(false);

            s.OnHeartsChanged += RefreshHearts;
            s.OnFruitsChanged += RefreshFruits;
            RefreshFruits(false);
            UIKit.SlideIn(heartsRt, new Vector2(-300f, 0f), 0.3f);
            UIKit.SlideIn(fruitRt, new Vector2(-300f, 0f), 0.4f);
            UIKit.SlideIn(top, new Vector2(0f, 150f), 0.35f);
            RefreshTouchControls();
            if (!TouchUI && s.levelIndex <= 2) ControlsStrip(root);
        }

        // ---------------------------------------------------------------- keyboard controls overlay

        static readonly (string[] keys, string label)[] s_Controls =
        {
            (new[] { "<", ">" }, "MOVE"),
            (new[] { "SPACE" }, "JUMP"),
            (new[] { "SHIFT" }, "DASH"),
            (new[] { "P" }, "PAUSE"),
        };

        static float KeyWidth(string k) => k.Length <= 1 ? 64f : 40f + k.Length * 22f;

        static void KeyCap(Transform parent, string key, Vector2 anchor, Vector2 pos)
        {
            float w = KeyWidth(key);
            var cap = UIKit.Panel(parent, "grey", anchor, pos, new Vector2(w, 64f), "Key");
            if (key == "<" || key == ">")
            {
                var ic = UIKit.Image(cap.transform, UIArt.Arrow(key == "<" ? 2 : 0), new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(34f, 34f));
                ic.preserveAspect = true;
            }
            else UIKit.Text(cap.transform, key, key.Length <= 1 ? 28f : 22f, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(w, 50f), TextStyle.Heading);
        }

        /// <summary>Key legend along the bottom of the first levels (desktop). Fades away once the player has had time to read it.</summary>
        void ControlsStrip(Transform root)
        {
            const float gap = 10f, labelW = 110f, groupGap = 36f;
            float total = -groupGap;
            foreach (var c in s_Controls)
            {
                foreach (var k in c.keys) total += KeyWidth(k) + gap;
                total += labelW + groupGap;
            }
            var strip = UIKit.Rect(root, "Controls", new Vector2(0.5f, 0f), new Vector2(0f, 64f), new Vector2(total + 60f, 90f));
            UIKit.Panel(strip, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(total + 60f, 90f));
            float x = -total / 2f;
            foreach (var c in s_Controls)
            {
                foreach (var k in c.keys)
                {
                    float w = KeyWidth(k);
                    KeyCap(strip, k, new Vector2(0.5f, 0.5f), new Vector2(x + w / 2f, 0f));
                    x += w + gap;
                }
                UIKit.Text(strip, c.label, 24f, new Vector2(0.5f, 0.5f), new Vector2(x + labelW / 2f - 4f, 0f), new Vector2(labelW, 50f), TextStyle.Heading, UIKit.Cream, TextAlignmentOptions.Left);
                x += labelW + groupGap;
            }
            var g = UIKit.Group(strip.gameObject);
            g.blocksRaycasts = false;
            UIKit.SlideIn(strip, new Vector2(0f, -160f), 0.6f);
            StartCoroutine(FadeOutLater(g, 16f));
        }

        static IEnumerator FadeOutLater(CanvasGroup g, float delay)
        {
            yield return new WaitForSeconds(delay); // game time: waits while paused
            for (float t = 0f; t < 1f && g != null; t += Time.unscaledDeltaTime)
            {
                g.alpha = 1f - t;
                yield return null;
            }
            if (g != null) Destroy(g.gameObject);
        }

        /// <summary>Vertical key legend shown next to the pause menu on desktop.</summary>
        static void ControlsCard(Transform root)
        {
            var card = UIKit.Rect(root, "Controls", new Vector2(0.5f, 0.5f), new Vector2(-600f, -10f), new Vector2(400f, 560f));
            UIKit.Panel(card, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 560f));
            var rib = UIKit.Panel(card, "blue", new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(320f, 90f));
            UIKit.Text(rib.transform, "CONTROLS", 32f, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(300f, 70f), TextStyle.Heading);
            float y = -140f;
            foreach (var c in s_Controls)
            {
                float x = -150f;
                foreach (var k in c.keys)
                {
                    float w = KeyWidth(k);
                    KeyCap(card, k, new Vector2(0.5f, 1f), new Vector2(x + w / 2f, y));
                    x += w + 10f;
                }
                UIKit.Text(card, c.label, 26f, new Vector2(0.5f, 1f), new Vector2(110f, y), new Vector2(160f, 50f), TextStyle.Dark, null, TextAlignmentOptions.Left);
                y -= 92f;
            }
            UIKit.Text(card, "ALSO: WASD, Z / X, GAMEPAD", 17f, new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(380f, 30f), TextStyle.Dark, new Color32(120, 90, 110, 255));
            UIKit.PopIn(card, 0.05f, 0.3f);
        }

        public void HideInGame()
        {
            if (m_Session != null)
            {
                m_Session.OnHeartsChanged -= RefreshHearts;
                m_Session.OnFruitsChanged -= RefreshFruits;
            }
            m_Session = null;
            Clear(m_HudLayer);
            HidePause();
            if (m_Results != null) Destroy(m_Results);
            CloseSettings();
            m_Touch = null;
            m_BossBar = null;
            GameInput.ClearTouch();
        }

        void Update()
        {
            if (m_Session != null && m_TimerText != null) m_TimerText.text = UIKit.FormatTime(Mathf.Max(0.01f, m_Session.time));
        }

        void RefreshHearts()
        {
            if (m_Session == null || m_Hearts == null) return;
            int h = m_Session.hearts;
            for (int i = 0; i < m_Hearts.Length; i++)
            {
                bool full = i < h;
                var img = m_Hearts[i];
                img.sprite = full ? UIArt.Heart : UIArt.HeartEmpty;
                if (i == h && h < m_ShownHearts) StartCoroutine(HeartLost(img.rectTransform));
                if (i == h - 1 && h > m_ShownHearts) Tween.Punch(img.transform, 0.5f, 0.35f);
            }
            m_ShownHearts = h;
        }

        IEnumerator HeartLost(RectTransform rt)
        {
            Vector2 p = rt.anchoredPosition;
            for (float t = 0; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                rt.anchoredPosition = p + Random.insideUnitCircle * 8f * (1f - t / 0.4f);
                rt.localScale = Vector3.one * (1f + 0.4f * (1f - t / 0.4f));
                yield return null;
            }
            rt.anchoredPosition = p;
            rt.localScale = Vector3.one;
        }

        void RefreshFruits() => RefreshFruits(true);

        void RefreshFruits(bool punch)
        {
            if (m_Session == null) return;
            m_FruitText.text = $"{m_Session.fruitsCollected}/{m_Session.fruitsTotal}";
            m_FruitText.color = m_Session.fruitsCollected >= m_Session.fruitsTotal ? UIKit.Gold : UIKit.Cream;
            if (punch) Tween.Punch(m_FruitIcon, 0.25f, 0.18f);
        }

        public void ShowBossBar(bool show)
        {
            if (m_BossBar == null) return;
            m_BossBar.SetActive(show);
            if (show) { SetBossHealth(1f); UIKit.PopIn((RectTransform)m_BossBar.transform); }
        }

        public void SetBossHealth(float k)
        {
            if (m_BossFill == null) return;
            var rt = m_BossFill.rectTransform;
            float from = rt.sizeDelta.x, to = 540f * Mathf.Clamp01(k);
            Tween.To(rt, 0.4f, e => rt.sizeDelta = new Vector2(Mathf.Lerp(from, to, e), 36f));
            Tween.Punch(m_BossBar.transform, 0.1f, 0.25f);
        }

        void FadeHud(float to)
        {
            var g = UIKit.Group(m_HudLayer.gameObject);
            float from = g.alpha;
            Tween.To(m_HudLayer, 0.25f, k => g.alpha = Mathf.Lerp(from, to, k));
        }

        // ---------------------------------------------------------------- touch

        void RefreshTouchControls()
        {
            if (m_Touch != null) { Destroy(m_Touch); m_Touch = null; }
            if (m_Session == null || !TouchUI) return;
            var root = UIKit.Stretch(m_HudLayer, "Touch");
            m_Touch = root.gameObject;
            TouchPad(root, UIArt.Arrow(2), new Vector2(0f, 0f), new Vector2(150f, 150f), 190f, TouchButton.Action.Left);
            TouchPad(root, UIArt.Arrow(0), new Vector2(0f, 0f), new Vector2(370f, 150f), 190f, TouchButton.Action.Right);
            TouchPad(root, UIArt.Arrow(1), new Vector2(1f, 0f), new Vector2(-170f, 170f), 220f, TouchButton.Action.Jump);
            TouchPad(root, UIArt.Dash, new Vector2(1f, 0f), new Vector2(-420f, 130f), 160f, TouchButton.Action.Dash);
        }

        void TouchPad(Transform parent, Sprite icon, Vector2 anchor, Vector2 pos, float size, TouchButton.Action action)
        {
            var img = UIKit.Panel(parent, "grey", anchor, pos, new Vector2(size, size), "Touch" + action);
            img.raycastTarget = true;
            img.color = new Color(1f, 1f, 1f, 0.45f);
            var ic = UIKit.Image(img.transform, icon, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(size * 0.42f, size * 0.42f));
            ic.preserveAspect = true;
            ic.color = new Color(1f, 1f, 1f, 0.9f);
            img.gameObject.AddComponent<TouchButton>().action = action;
        }

        // ---------------------------------------------------------------- pause

        public void ShowPause()
        {
            HidePause();
            var root = UIKit.Stretch(m_PopupLayer, "Pause");
            m_Pause = root.gameObject;
            UIKit.Dim(root, 0.55f);
            var panel = UIKit.Rect(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(640f, 900f));
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 900f));
            var ribbon = UIKit.Panel(panel, "yellow", new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(440f, 100f));
            UIKit.Text(ribbon.transform, "PAUSED", 42f, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(420f, 80f), TextStyle.Dark);

            var s = Game.I.Session;
            if (s != null)
            {
                UIKit.Text(panel, $"{LevelCatalog.Code(s.levelIndex)}  {LevelCatalog.LevelNames[s.levelIndex].ToUpperInvariant()}", 20f,
                    new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(600f, 40f), TextStyle.Dark);
                // live star goals
                string[] goals = { "FINISH", "FRUITS", "NO HIT" };
                bool[] ok = { false, s.fruitsCollected >= s.fruitsTotal, !s.tookDamage };
                for (int i = 0; i < 3; i++)
                {
                    UIKit.Image(panel, ok[i] ? UIArt.Star : UIArt.StarEmpty, new Vector2(0.5f, 1f), new Vector2((i - 1) * 180f, -175f), new Vector2(52f, 52f));
                    UIKit.Text(panel, goals[i], 15f, new Vector2(0.5f, 1f), new Vector2((i - 1) * 180f, -224f), new Vector2(170f, 30f), TextStyle.Dark);
                }
            }

            float y = -300f;
            var resume = UIKit.Button(panel, "RESUME", "green", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.TogglePause(), UIArt.Play, 32f, "confirm");
            y -= 110f;
            UIKit.Button(panel, "RESTART", "yellow", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.RestartLevel(), UIArt.Restart, 32f);
            y -= 110f;
            UIKit.Button(panel, "SETTINGS", "blue", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => OpenSettings(true), UIArt.Gear, 32f);
            y -= 110f;
            UIKit.Button(panel, "LEVELS", "purple", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.GoToMenu(MenuScreen.LevelSelect), UIArt.Levels, 32f, "back");
            y -= 110f;
            UIKit.Button(panel, "MAIN MENU", "red", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.GoToMenu(MenuScreen.Title), UIArt.Home, 32f, "back");
            UIKit.PopIn(panel, 0f, 0.3f);
            if (!TouchUI) ControlsCard(root);
            Select(resume);
        }

        public void HidePause()
        {
            if (m_Pause != null) Destroy(m_Pause);
            m_Pause = null;
            CloseSettings();
        }

        // ---------------------------------------------------------------- results

        public void ShowResults(LevelResult r)
        {
            if (m_Results != null) Destroy(m_Results);
            StartCoroutine(ResultsRoutine(r));
        }

        IEnumerator ResultsRoutine(LevelResult r)
        {
            yield return new WaitForSecondsRealtime(0.6f);
            if (m_Touch != null) m_Touch.SetActive(false);
            FadeHud(0f);
            var root = UIKit.Stretch(m_PopupLayer, "Results");
            m_Results = root.gameObject;
            UIKit.Dim(root, 0.5f);
            var panel = UIKit.Rect(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 780f));
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 780f));
            UIKit.Panel(panel, "card", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(940f, 640f));
            bool finalLevel = r.level == LevelCatalog.LevelCount - 1;
            var ribbon = UIKit.Panel(panel, "green", new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(720f, 120f));
            UIKit.Text(ribbon.transform, finalLevel ? "YOU WIN!" : "LEVEL CLEAR!", 46f, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(700f, 90f), TextStyle.Heading, UIKit.Gold);
            UIKit.PopIn(panel, 0f, 0.4f);
            Tween.Punch(ribbon.transform, 0.15f, 0.4f);

            string[] labels = { "FINISH", "ALL FRUITS", "NO DAMAGE" };
            var starImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = UIKit.Image(panel, UIArt.StarEmpty, new Vector2(0.5f, 1f), new Vector2((i - 1) * 250f, -210f - (i == 1 ? 20f : 0f)), new Vector2(150f, 150f), "Star" + i);
                starImgs[i] = slot;
                UIKit.Text(panel, labels[i], 18f, new Vector2(0.5f, 1f), new Vector2((i - 1) * 250f, -318f), new Vector2(240f, 30f), TextStyle.Dark);
            }

            UIKit.Image(panel, GameAssets.I.Sprite("PA1/Items/Fruits/Apple"), new Vector2(0.5f, 0.5f), new Vector2(-290f, -70f), new Vector2(96f, 96f));
            UIKit.Text(panel, $"{r.fruits}/{r.fruitsTotal}", 34f, new Vector2(0.5f, 0.5f), new Vector2(-130f, -70f), new Vector2(260f, 60f), TextStyle.Dark, null, TextAlignmentOptions.Left);
            UIKit.Image(panel, UIArt.Clock, new Vector2(0.5f, 0.5f), new Vector2(90f, -70f), new Vector2(52f, 52f)).color = new Color32(90, 60, 70, 255);
            UIKit.Text(panel, UIKit.FormatTime(r.time), 34f, new Vector2(0.5f, 0.5f), new Vector2(270f, -70f), new Vector2(300f, 60f), TextStyle.Dark, null, TextAlignmentOptions.Left);
            if (r.newBest)
            {
                var best = UIKit.Panel(panel, "red", new Vector2(0.5f, 0.5f), new Vector2(300f, -10f), new Vector2(230f, 56f));
                UIKit.Text(best.transform, "NEW BEST!", 20f, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(220f, 40f), TextStyle.Body);
                best.transform.localRotation = Quaternion.Euler(0, 0, 8f);
                best.gameObject.AddComponent<UIWave>().single = true;
            }

            var levels = UIKit.Button(panel, "", "blue", new Vector2(0.5f, 0f), new Vector2(-290f, 100f), new Vector2(150f, 110f), () => Game.I.GoToMenu(MenuScreen.LevelSelect), UIArt.Levels, 0f, "back");
            var retry = UIKit.Button(panel, "", "yellow", new Vector2(0.5f, 0f), new Vector2(-110f, 100f), new Vector2(150f, 110f), () => Game.I.RetryAfterBreak(), UIArt.Restart);
            Button next;
            if (Game.I.HasNextLevel)
                next = UIKit.Button(panel, "NEXT", "green", new Vector2(0.5f, 0f), new Vector2(170f, 100f), new Vector2(340f, 110f), () => Game.I.NextLevel(), UIArt.Play, 36f, "confirm");
            else
                next = UIKit.Button(panel, "MENU", "green", new Vector2(0.5f, 0f), new Vector2(170f, 100f), new Vector2(340f, 110f), () => Game.I.GoToMenu(MenuScreen.Title), UIArt.Home, 36f, "confirm");
            Select(next);

            // star reveal sequence
            yield return new WaitForSecondsRealtime(0.45f);
            for (int i = 0; i < 3; i++)
            {
                if ((r.starMask & (1 << i)) == 0) { yield return new WaitForSecondsRealtime(0.15f); continue; }
                var img = starImgs[i];
                img.sprite = UIArt.Star;
                var rt = img.rectTransform;
                rt.localScale = Vector3.one * 2.2f;
                Tween.To(rt, 0.35f, k => rt.localScale = Vector3.one * Mathf.LerpUnclamped(2.2f, 1f, Ease.OutBack(k)));
                AudioManager.I?.Play("star", 0.8f, 0f, 1f + i * 0.12f);
                img.gameObject.AddComponent<UIWave>().single = true;
                yield return new WaitForSecondsRealtime(0.35f);
            }

            // hero unlock announcements
            int now = SaveSystem.TotalStars();
            foreach (var c in LevelCatalog.Characters)
                if (c.starsRequired > 0 && m_StarsBeforeLevel < c.starsRequired && now >= c.starsRequired)
                {
                    AudioManager.I?.Play("unlock", 0.9f);
                    Toast($"NEW HERO: {c.name.ToUpperInvariant()}!");
                    Platform.HappyTime();
                    yield return new WaitForSecondsRealtime(1.2f);
                }
        }

        int m_StarsBeforeLevel;

        /// <summary>Called before a level starts so the results screen can announce newly unlocked heroes.</summary>
        public void RememberStars() => m_StarsBeforeLevel = SaveSystem.TotalStars();

        public void ShowGameOver()
        {
            if (m_Results != null) Destroy(m_Results);
            if (m_Touch != null) m_Touch.SetActive(false);
            FadeHud(0f);
            var s = Game.I.Session;
            // rewarded revive: only once per attempt and only after a real try, never after every quick death
            bool canRevive = s != null && !s.revived && s.time > 12f && Platform.RewardedAvailable;
            float w = canRevive ? 1000f : 820f;
            var root = UIKit.Stretch(m_PopupLayer, "GameOver");
            m_Results = root.gameObject;
            UIKit.Dim(root, 0.6f);
            var panel = UIKit.Rect(root, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 620f));
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 620f));
            var ribbon = UIKit.Panel(panel, "red", new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(620f, 120f));
            UIKit.Text(ribbon.transform, "GAME OVER", 46f, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(600f, 90f), TextStyle.Heading, UIKit.Cream);
            var hero = LevelCatalog.Characters[Mathf.Clamp(SaveSystem.Data.character, 0, LevelCatalog.Characters.Length - 1)];
            var heroImg = UIKit.Image(panel, GameAssets.I.Sprite(hero.folder + "/Hit"), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(150f, 150f), "Hero");
            heroImg.gameObject.AddComponent<UIImageAnim>().SetFrames(GameAssets.I.Frames(hero.folder + "/Hit"));
            UIKit.Text(panel, "OUT OF HEARTS! TRY AGAIN?", 26f, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(760f, 50f), TextStyle.Dark);
            if (s != null)
            {
                UIKit.Image(panel, GameAssets.I.Sprite("PA1/Items/Fruits/Apple"), new Vector2(0.5f, 0.5f), new Vector2(-190f, -60f), new Vector2(72f, 72f));
                UIKit.Text(panel, $"{s.fruitsCollected}/{s.fruitsTotal}", 26f, new Vector2(0.5f, 0.5f), new Vector2(-80f, -60f), new Vector2(160f, 50f), TextStyle.Dark, null, TextAlignmentOptions.Left);
                UIKit.Image(panel, UIArt.Clock, new Vector2(0.5f, 0.5f), new Vector2(60f, -60f), new Vector2(44f, 44f)).color = new Color32(120, 90, 110, 255);
                UIKit.Text(panel, UIKit.FormatTime(s.time), 26f, new Vector2(0.5f, 0.5f), new Vector2(180f, -60f), new Vector2(200f, 50f), TextStyle.Dark, null, TextAlignmentOptions.Left);
            }
            Button retry;
            if (canRevive)
            {
                // RETRY and REVIVE share size, font and colour so the ad is clearly optional
                UIKit.Button(panel, "", "blue", new Vector2(0.5f, 0f), new Vector2(-360f, 110f), new Vector2(150f, 110f), () => Game.I.GoToMenu(MenuScreen.LevelSelect), UIArt.Levels, 0f, "back");
                retry = UIKit.Button(panel, "RETRY", "green", new Vector2(0.5f, 0f), new Vector2(-95f, 110f), new Vector2(340f, 110f), () => Game.I.RetryAfterBreak(), UIArt.Restart, 34f, "confirm");
                Button revive = null;
                revive = UIKit.Button(panel, "REVIVE", "green", new Vector2(0.5f, 0f), new Vector2(275f, 110f), new Vector2(340f, 110f), () => ReviveClicked(revive), UIArt.Video, 34f, "confirm");
                UIKit.Text(panel, "WATCH A VIDEO TO KEEP GOING", 16f, new Vector2(0.5f, 0f), new Vector2(275f, 188f), new Vector2(420f, 30f), TextStyle.Dark, new Color32(120, 90, 110, 255));
            }
            else
            {
                retry = UIKit.Button(panel, "RETRY", "green", new Vector2(0.5f, 0f), new Vector2(120f, 110f), new Vector2(360f, 110f), () => Game.I.RetryAfterBreak(), UIArt.Restart, 34f, "confirm");
                UIKit.Button(panel, "", "blue", new Vector2(0.5f, 0f), new Vector2(-190f, 110f), new Vector2(150f, 110f), () => Game.I.GoToMenu(MenuScreen.LevelSelect), UIArt.Levels, 0f, "back");
            }
            UIKit.PopIn(panel, 0.1f, 0.4f);
            Tween.Punch(ribbon.transform, 0.2f, 0.4f);
            Select(retry);
        }

        void ReviveClicked(Button revive)
        {
            if (m_Results == null || Platform.AdPlaying) return;
            var group = UIKit.Group(m_Results);
            group.interactable = false;
            Platform.Rewarded(() => Game.I.Revive(), () =>
            {
                if (group != null) group.interactable = true;
                if (revive != null) revive.gameObject.SetActive(false);
                Toast("NO VIDEO RIGHT NOW. TRY AGAIN LATER!");
            });
        }

        /// <summary>Removes the game-over popup and brings the HUD back (after a revive).</summary>
        public void CloseGameOver()
        {
            if (m_Results != null) Destroy(m_Results);
            m_Results = null;
            FadeHud(1f);
            if (m_Touch != null) m_Touch.SetActive(true);
        }
    }
}
