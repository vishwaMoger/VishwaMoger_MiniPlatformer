using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPlatformer
{
    /// <summary>In-game UI for the sliding-room puzzles: HUD, countdown, boosters, pause, results and fail screens.</summary>
    public partial class UIRoot
    {
        TrekBoard m_Board;
        TextMeshProUGUI m_TimeText, m_SlideText, m_CoinText, m_CountText;
        Image m_TimeIcon, m_SlideIcon;
        RectTransform m_CoinIcon;
        GameObject m_Pause, m_Results;
        readonly TextMeshProUGUI[] m_BoosterBadges = new TextMeshProUGUI[Boosters.Count];
        readonly Image[] m_BoosterVideo = new Image[Boosters.Count];
        CanvasGroup m_Tutorial;
        int m_ShownCoins, m_LastCount = -1;

        static readonly Color GoodInk = new Color32(255, 214, 74, 255), LostInk = new Color32(190, 180, 200, 255);

        // ---------------------------------------------------------------- HUD

        public void ShowTrekHUD(TrekBoard board)
        {
            HideInGame();
            m_Board = board;
            m_StarsBefore = SaveSystem.TotalStars();
            UIKit.Group(m_HudLayer.gameObject).alpha = 1f;
            var root = UIKit.Stretch(m_HudLayer, "HUD");
            var lv = board.Level;

            UIKit.IconButton(root, UIArt.Pause, "blue", new Vector2(0f, 1f), new Vector2(90f, -80f), 110f, () => Game.I.TogglePause());
            UIKit.IconButton(root, UIArt.Restart, "yellow", new Vector2(0f, 1f), new Vector2(220f, -80f), 110f, () => Game.I.RestartLevel());

            // level + timer + slides
            // portrait: the level bar gets its own row under the buttons
            var top = UIKit.Rect(root, "Top", new Vector2(0.5f, 1f), new Vector2(0f, Portrait ? -190f : -62f), new Vector2(820f, 90f));
            UIKit.Panel(top, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 84f));
            UIKit.Text(top, lv.daily ? "DAILY" : "LEVEL " + lv.number, 28f, new Vector2(0.5f, 0.5f), new Vector2(-280f, 0f), new Vector2(220f, 50f), TextStyle.Heading, UIKit.Gold);
            m_TimeIcon = UIKit.Image(top, UIArt.Clock, new Vector2(0.5f, 0.5f), new Vector2(-120f, 0f), new Vector2(42f, 42f));
            m_TimeText = UIKit.Text(top, "", 26f, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(200f, 50f), TextStyle.Heading, null, TextAlignmentOptions.Left);
            m_SlideIcon = UIKit.Image(top, UIArt.Arrow(0), new Vector2(0.5f, 0.5f), new Vector2(150f, 0f), new Vector2(40f, 40f));
            m_SlideText = UIKit.Text(top, "", 26f, new Vector2(0.5f, 0.5f), new Vector2(270f, 0f), new Vector2(200f, 50f), TextStyle.Heading, null, TextAlignmentOptions.Left);
            UIKit.SlideIn(top, new Vector2(0f, 150f), 0.3f);

            // wallet
            var coinRt = UIKit.Rect(root, "Coins", new Vector2(1f, 1f), new Vector2(-170f, -80f), new Vector2(280f, 90f));
            UIKit.Panel(coinRt, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(270f, 84f));
            m_CoinIcon = UIKit.Image(coinRt, UIArt.Coin, new Vector2(0.5f, 0.5f), new Vector2(-92f, 0f), new Vector2(72f, 72f)).rectTransform;
            m_CoinText = UIKit.Text(coinRt, "", 30f, new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(180f, 60f), TextStyle.Heading, UIKit.Gold, TextAlignmentOptions.Left);
            m_ShownCoins = Economy.Coins;
            m_CoinText.text = m_ShownCoins.ToString();
            UIKit.SlideIn(coinRt, new Vector2(0f, 150f), 0.4f);

            // boosters (bottom right): use one you own, else watch a video, else buy with coins
            Sprite[] icons = { UIArt.Clock, UIArt.Star, UIArt.Check };
            string[] colors = { "blue", "green", "purple" };
            for (int b = 0; b < Boosters.Count; b++)
            {
                int booster = b;
                var btn = UIKit.IconButton(root, icons[b], colors[b], new Vector2(1f, 0f), new Vector2(-100f - (Boosters.Count - 1 - b) * 140f, 90f), 116f, () => UseBooster(booster));
                UIKit.Text(btn.transform, Boosters.Names[b], 16f, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(160f, 28f), TextStyle.Body);
                var badge = UIKit.Panel(btn.transform, "red", new Vector2(1f, 1f), new Vector2(-8f, -8f), new Vector2(54f, 44f), "Badge");
                m_BoosterBadges[b] = UIKit.Text(badge.transform, "", 18f, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(60f, 34f), TextStyle.Body);
                m_BoosterVideo[b] = UIKit.Image(badge.transform, UIArt.Video, new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), new Vector2(34f, 34f), "Video");
                UIKit.SlideIn((RectTransform)btn.transform, new Vector2(0f, -200f), 0.4f + b * 0.06f);
            }
            RefreshBoosters();

            // first puzzles: say how to play until the first slide
            if (!lv.daily && lv.number <= 2)
            {
                var tip = UIKit.Rect(root, "Tutorial", new Vector2(0.5f, 0f), new Vector2(0f, Portrait ? 260f : 70f), new Vector2(980f, Portrait ? 120f : 80f)); // portrait: above the booster row
                UIKit.Panel(tip, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, Portrait ? 116f : 76f));
                UIKit.Text(tip, lv.number == 1 ? "TAP A ROOM NEXT TO THE GAP TO SLIDE IT. GUIDE THE HERO TO THE TROPHY!"
                                               : "THE HERO NEVER STOPS. KEEP THEM AWAY FROM TRAPS AND THE GAP!", 22f,
                    new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(960f, 60f), TextStyle.Heading, UIKit.Cream);
                m_Tutorial = UIKit.Group(tip.gameObject);
                m_Tutorial.blocksRaycasts = false;
                UIFloat.Add(tip.gameObject, UIFloat.Mode.Pulse, 1.2f, 1f);
            }

            // walking speed: 1x / 2x / 3x (remembered)
            Button speed = null;
            speed = UIKit.Button(root, "x" + Mathf.Clamp(SaveSystem.Data.speed, 1, 3), "yellow", new Vector2(0f, 0f), new Vector2(110f, 90f), new Vector2(170f, 110f), () =>
            {
                var d = SaveSystem.Data;
                d.speed = d.speed % 3 + 1;
                TrekHero.SpeedMul = d.speed;
                SaveSystem.Save();
                speed.GetComponentInChildren<TextMeshProUGUI>().text = "x" + d.speed;
                Tween.Punch(speed.transform, 0.2f, 0.25f);
            }, UIArt.Dash, 34f, "switch");
            UIKit.Text(speed.transform, "SPEED", 16f, new Vector2(0.5f, 0f), new Vector2(0f, -22f), new Vector2(160f, 28f), TextStyle.Body);
            UIKit.SlideIn((RectTransform)speed.transform, new Vector2(0f, -200f), 0.4f);

            m_CountText = UIKit.Text(root, "", 110f, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(800f, 180f), TextStyle.Title, UIKit.Gold);
            m_CountText.textWrappingMode = TextWrappingModes.NoWrap;
            m_LastCount = -1;

            board.OnChanged += RefreshTrek;
            Economy.OnCoinsChanged += RefreshCoins;
            RefreshTrek();
        }

        void RefreshBoosters()
        {
            for (int b = 0; b < Boosters.Count; b++)
            {
                if (m_BoosterBadges[b] == null) continue;
                int owned = Boosters.Owned(b);
                bool video = owned <= 0 && Platform.RewardedAvailable;
                m_BoosterBadges[b].text = owned > 0 ? owned.ToString() : video ? "" : Boosters.Prices[b].ToString();
                m_BoosterBadges[b].fontSize = owned > 0 ? 18f * 1.3f : 14f * 1.3f;
                if (m_BoosterVideo[b] != null) m_BoosterVideo[b].enabled = video;
            }
        }

        void UseBooster(int b)
        {
            var board = m_Board;
            if (board == null || board.Finished) return;
            void Apply()
            {
                if (board == null || board.Finished) return;
                switch (b)
                {
                    case Boosters.Freeze: board.Freeze(6f); Toast("FROZEN FOR 6 SECONDS!"); AudioManager.I?.Play("appear", 0.8f); break;
                    case Boosters.Hint: if (!board.ShowHint()) Toast("NO HINT FOUND - TRY A FRESH START!"); break;
                    case Boosters.Solve: board.AutoSolve(); Toast("SOLVING..."); break;
                }
                RefreshBoosters();
            }
            if (b == Boosters.Freeze && board.Frozen) return;
            if (b == Boosters.Solve && (board.PathComplete || board.AutoSolving)) return;
            if (Boosters.Use(b)) { Apply(); return; }
            if (Platform.RewardedAvailable)
            {
                Platform.Rewarded(Apply, () => { Toast("NO VIDEO RIGHT NOW. TRY AGAIN LATER!"); RefreshBoosters(); });
                return;
            }
            if (Economy.Spend(Boosters.Prices[b])) { Apply(); return; }
            AudioManager.I?.Play("error", 0.7f);
            Toast($"{Boosters.Names[b]} COSTS {Boosters.Prices[b]} COINS");
        }

        void RefreshTrek()
        {
            var b = m_Board;
            if (b == null || m_TimeText == null) return;
            var lv = b.Level;
            bool fast = b.Elapsed <= lv.targetTime, smart = b.Slides <= lv.par;
            m_SlideText.text = $"{b.Slides}<size=70%><alpha=#AA>/{lv.par}";
            m_SlideText.color = smart ? Color.white : LostInk;
            m_SlideIcon.color = smart ? GoodInk : LostInk;
            m_TimeIcon.color = fast ? GoodInk : LostInk;
            if (m_Tutorial != null && b.Slides > 0)
            {
                var g = m_Tutorial;
                m_Tutorial = null;
                Tween.To(g, 0.4f, k => g.alpha = 1f - k, null, true, () => { if (g != null) Destroy(g.gameObject); });
            }
        }

        void RefreshCoins()
        {
            if (m_CoinText == null) return;
            int target = Economy.Coins, from = m_ShownCoins;
            m_ShownCoins = target;
            var txt = m_CoinText;
            Tween.To(txt, 0.5f, k => txt.text = Mathf.RoundToInt(Mathf.Lerp(from, target, k)).ToString());
            if (target > from) Tween.Punch(m_CoinIcon, 0.35f, 0.3f);
            RefreshBoosters();
        }

        void Update()
        {
            var b = m_Board;
            if (b == null || m_TimeText == null || m_TimeIcon == null || b.Level == null) return;
            var lv = b.Level;
            m_TimeText.text = $"{b.Elapsed:0.0}<size=70%><alpha=#AA>/{lv.targetTime:0}s";
            bool fast = b.Elapsed <= lv.targetTime;
            m_TimeText.color = fast ? Color.white : LostInk;
            m_TimeIcon.color = fast ? GoodInk : LostInk;

            // 3, 2, 1, GO!
            if (m_CountText != null)
            {
                int n = b.Started ? 0 : Mathf.CeilToInt(b.CountdownLeft);
                if (n != m_LastCount)
                {
                    m_LastCount = n;
                    if (!b.Started && n > 0) { m_CountText.text = n.ToString(); AudioManager.I?.Play("tick", 0.7f, 0f, 1f); }
                    else if (b.Started && !b.Finished) { m_CountText.text = "GO!"; StartCoroutine(FadeCount()); }
                    m_CountText.rectTransform.localScale = Vector3.one * 1.4f;
                    var rt = m_CountText.rectTransform;
                    Tween.To(rt, 0.3f, k => rt.localScale = Vector3.one * Mathf.LerpUnclamped(1.4f, 1f, Ease.OutBack(k)));
                    m_CountText.alpha = 1f;
                }
            }
        }

        IEnumerator FadeCount()
        {
            yield return new WaitForSecondsRealtime(0.5f);
            for (float t = 0f; t < 0.3f && m_CountText != null; t += Time.unscaledDeltaTime)
            {
                m_CountText.alpha = 1f - t / 0.3f;
                yield return null;
            }
            if (m_CountText != null) m_CountText.text = "";
        }

        public void HideInGame()
        {
            if (m_Board != null) m_Board.OnChanged -= RefreshTrek;
            Economy.OnCoinsChanged -= RefreshCoins;
            m_Board = null;
            Clear(m_HudLayer);
            HidePause();
            if (m_Results != null) Destroy(m_Results);
            m_Results = null;
            CloseSettings();
            CloseMetaSilently();
            m_TimeText = null;
            m_CountText = null;
        }

        void FadeHud(float to)
        {
            var g = UIKit.Group(m_HudLayer.gameObject);
            float from = g.alpha;
            Tween.To(m_HudLayer, 0.25f, k => g.alpha = Mathf.Lerp(from, to, k));
        }

        // ---------------------------------------------------------------- pause

        public void ShowPause()
        {
            HidePause();
            var root = UIKit.Stretch(m_PopupLayer, "Pause");
            m_Pause = root.gameObject;
            UIKit.Dim(root, 0.55f);
            var panel = UIKit.Rect(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(640f, 760f));
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 760f));
            var ribbon = UIKit.Panel(panel, "yellow", new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(440f, 100f));
            UIKit.Text(ribbon.transform, "PAUSED", 42f, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(420f, 80f), TextStyle.Dark);
            var b = Game.I.Board;
            if (b != null)
                UIKit.Text(panel, b.Level.daily ? "DAILY PUZZLE" : "LEVEL " + b.Level.number, 22f, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(600f, 40f), TextStyle.Dark);
            float y = -200f;
            var resume = UIKit.Button(panel, "RESUME", "green", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.TogglePause(), UIArt.Play, 32f, "confirm");
            y -= 110f;
            UIKit.Button(panel, "RESTART", "yellow", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.RestartLevel(), UIArt.Restart, 32f);
            y -= 110f;
            UIKit.Button(panel, "SETTINGS", "blue", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => OpenSettings(true), UIArt.Gear, 32f);
            y -= 110f;
            UIKit.Button(panel, "MAIN MENU", "red", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 96f), () => Game.I.GoToMenu(MenuScreen.Title), UIArt.Home, 32f, "back");
            UIKit.Text(panel, "TAP OR SWIPE A ROOM NEXT TO THE GAP.\nKEYBOARD: ARROW KEYS OR WASD", 17f, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(600f, 60f), TextStyle.Dark, InkSoft);
            UIKit.PopIn(panel, 0f, 0.3f);
            Select(resume);
        }

        public void HidePause()
        {
            if (m_Pause != null) Destroy(m_Pause);
            m_Pause = null;
            CloseSettings();
        }

        // ---------------------------------------------------------------- results

        public void ShowTrekResults(TrekResult r)
        {
            if (m_Results != null) Destroy(m_Results);
            StartCoroutine(ResultsRoutine(r));
        }

        IEnumerator ResultsRoutine(TrekResult r)
        {
            FadeHud(0f);
            var root = UIKit.Stretch(m_PopupLayer, "Results");
            m_Results = root.gameObject;
            UIKit.Dim(root, 0.5f);
            var panel = UIKit.Rect(root, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(1000f, 780f));
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 780f));
            UIKit.Panel(panel, "card", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(940f, 640f));
            var ribbon = UIKit.Panel(panel, "green", new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(720f, 120f));
            UIKit.Text(ribbon.transform, r.daily ? "DAILY SOLVED!" : "SOLVED!", 46f, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(700f, 90f), TextStyle.Heading, UIKit.Gold);
            UIKit.PopIn(panel, 0f, 0.4f);
            Tween.Punch(ribbon.transform, 0.15f, 0.4f);

            string[] labels = { "SOLVED", $"UNDER {r.targetTime:0} SEC", $"{r.par} SLIDES OR LESS" };
            var starImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                starImgs[i] = UIKit.Image(panel, UIArt.StarEmpty, new Vector2(0.5f, 1f), new Vector2((i - 1) * 250f, -210f - (i == 1 ? 20f : 0f)), new Vector2(150f, 150f), "Star" + i);
                UIKit.Text(panel, labels[i], 18f, new Vector2(0.5f, 1f), new Vector2((i - 1) * 250f, -318f), new Vector2(240f, 30f), TextStyle.Dark);
            }

            var ink = new Color32(90, 60, 70, 255);
            UIKit.Image(panel, UIArt.Clock, new Vector2(0.5f, 0.5f), new Vector2(-330f, 10f), new Vector2(50f, 50f)).color = ink;
            UIKit.Text(panel, $"{r.time:0.0}s", 32f, new Vector2(0.5f, 0.5f), new Vector2(-200f, 10f), new Vector2(200f, 60f), TextStyle.Dark, null, TextAlignmentOptions.Left);
            UIKit.Image(panel, UIArt.Arrow(0), new Vector2(0.5f, 0.5f), new Vector2(-50f, 10f), new Vector2(46f, 46f)).color = ink;
            UIKit.Text(panel, $"{r.slides} SLIDES", 32f, new Vector2(0.5f, 0.5f), new Vector2(85f, 10f), new Vector2(240f, 60f), TextStyle.Dark, null, TextAlignmentOptions.Left);
            UIKit.Image(panel, UIArt.Coin, new Vector2(0.5f, 0.5f), new Vector2(240f, 10f), new Vector2(60f, 60f));
            var coinsText = UIKit.Text(panel, $"+{r.coins}", 32f, new Vector2(0.5f, 0.5f), new Vector2(360f, 10f), new Vector2(200f, 60f), TextStyle.Dark, CoinInk, TextAlignmentOptions.Left);

            // the near miss: exactly what would have made it better
            string nudge = null;
            var solvedBoard = Game.I.Board;
            if (solvedBoard != null && solvedBoard.AutoSolved) nudge = "SOLVE IT YOURSELF FOR ALL THREE STARS!";
            else if ((r.stars & 4) == 0) nudge = r.slides - r.par == 1 ? "SO CLOSE! JUST 1 SLIDE OVER PAR" : $"TRY IT IN {r.par} SLIDES FOR THE LAST STAR!";
            else if ((r.stars & 2) == 0) nudge = $"{r.time - r.targetTime:0.0} SECONDS TOO SLOW FOR THE SPEED STAR!";
            else if (r.newRecord) nudge = "PERFECT! ALL THREE STARS!";
            if (nudge != null)
                UIKit.Text(panel, nudge, 20f, new Vector2(0.5f, 0.5f), new Vector2(0f, -62f), new Vector2(900f, 36f), TextStyle.Dark, new Color32(214, 64, 84, 255));

            if (!Platform.RewardedAvailable) BonusChests(panel, r);
            if (r.spinToken) RunRealtime(1.6f, () => { Toast("+1 SPIN TOKEN! TRY YOUR LUCK ON THE WHEEL"); AudioManager.I?.Play("unlock", 0.8f); });
            if (r.coins > 0 && Platform.RewardedAvailable)
            {
                Button dbl = null;
                dbl = UIKit.Button(panel, $"x2 COINS  +{r.coins}", "purple", new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(460f, 92f), () =>
                {
                    if (Platform.AdPlaying || dbl == null) return;
                    dbl.interactable = false;
                    Platform.Rewarded(() =>
                    {
                        Economy.AddCoins(r.coins);
                        if (coinsText != null) coinsText.text = $"+{r.coins * 2}";
                        if (dbl != null) dbl.gameObject.SetActive(false);
                        AudioManager.I?.Play("jackpot", 0.7f, 0f);
                        CoinBurst(new Vector2(0f, -150f), panel, 10);
                        Toast($"+{r.coins} BONUS COINS!");
                    }, () =>
                    {
                        if (dbl != null) dbl.gameObject.SetActive(false);
                        Toast("NO VIDEO RIGHT NOW. TRY AGAIN LATER!");
                    });
                }, UIArt.Video, 30f, "confirm");
                dbl.GetComponent<UIButtonFX>().pulse = 1f;
            }

            UIKit.Button(panel, "", "blue", new Vector2(0.5f, 0f), new Vector2(-290f, 100f), new Vector2(150f, 110f), () => Game.I.GoToMenu(r.daily ? MenuScreen.Title : MenuScreen.LevelSelect), r.daily ? UIArt.Home : UIArt.Levels, 0f, "back");
            UIKit.Button(panel, "", "yellow", new Vector2(0.5f, 0f), new Vector2(-110f, 100f), new Vector2(150f, 110f), () => Game.I.RetryAfterBreak(), UIArt.Restart);
            Button next = r.daily
                ? UIKit.Button(panel, "MENU", "green", new Vector2(0.5f, 0f), new Vector2(170f, 100f), new Vector2(340f, 110f), () => Game.I.GoToMenu(MenuScreen.Title), UIArt.Home, 36f, "confirm")
                : UIKit.Button(panel, "NEXT", "green", new Vector2(0.5f, 0f), new Vector2(170f, 100f), new Vector2(340f, 110f), () => Game.I.NextLevel(), UIArt.Play, 36f, "confirm");
            next.GetComponent<UIButtonFX>().pulse = 1f;
            Select(next);

            // star reveal
            yield return new WaitForSecondsRealtime(0.45f);
            for (int i = 0; i < 3; i++)
            {
                if (root == null) yield break; // the player already moved on
                if ((r.stars & (1 << i)) == 0) { yield return new WaitForSecondsRealtime(0.15f); continue; }
                var img = starImgs[i];
                if (img == null) yield break;
                img.sprite = UIArt.Star;
                var rt = img.rectTransform;
                rt.localScale = Vector3.one * 2.2f;
                Tween.To(rt, 0.35f, k => rt.localScale = Vector3.one * Mathf.LerpUnclamped(2.2f, 1f, Ease.OutBack(k)));
                AudioManager.I?.Play("star", 0.8f, 0f, 1f + i * 0.12f);
                img.gameObject.AddComponent<UIWave>().single = true;
                yield return new WaitForSecondsRealtime(0.35f);
            }

            // new heroes unlocked by stars
            if (root == null) yield break;
            int now = SaveSystem.TotalStars();
            foreach (var c in LevelCatalog.Characters)
                if (c.starsRequired > 0 && m_StarsBefore < c.starsRequired && now >= c.starsRequired)
                {
                    AudioManager.I?.Play("unlock", 0.9f);
                    Toast($"NEW HERO: {c.name.ToUpperInvariant()}!");
                    Platform.HappyTime();
                    yield return new WaitForSecondsRealtime(1.2f);
                }
            m_StarsBefore = now;
        }

        int m_StarsBefore = -1;

        /// <summary>
        /// Pick one of three chests after every solve: coins (sometimes a big pile) or a booster. A small surprise
        /// that keeps the "one more puzzle" loop going without any ads.
        /// </summary>
        void BonusChests(RectTransform panel, TrekResult r)
        {
            UIKit.Text(panel, "PICK A BONUS CHEST!", 20f, new Vector2(0.5f, 0.5f), new Vector2(0f, -100f), new Vector2(600f, 34f), TextStyle.Dark, CoinInk);
            // decide the three prizes up front so the others can be revealed after the pick
            var prizes = new (int coins, int booster)[3];
            for (int i = 0; i < 3; i++)
            {
                float roll = Random.value;
                if (roll < 0.15f) prizes[i] = (60 + 5 * Mathf.Min(r.level, 20), -1);
                else if (roll < 0.4f) prizes[i] = (0, Random.Range(0, Boosters.Count - 1));
                else prizes[i] = (10 + Random.Range(0, 4) * 5 + Mathf.Min(r.level, 30), -1);
            }
            var chests = new Image[3];
            var buttons = new Button[3];
            bool picked = false;
            var frames = GameAssets.I.Frames("PA1/Items/Boxes/Box1/Idle");
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var pos = new Vector2((i - 1) * 190f, -165f);
                var holder = UIKit.Rect(panel, "Chest" + i, new Vector2(0.5f, 0.5f), pos, new Vector2(120f, 110f));
                var img = UIKit.Image(holder, frames != null && frames.Length > 0 ? frames[0] : UIArt.Gift, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 90f), "Box");
                img.raycastTarget = true;
                chests[i] = img;
                var btn = img.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                img.gameObject.AddComponent<UIButtonFX>().pulse = 1f;
                buttons[i] = btn;
                UIFloat.Add(holder.gameObject, UIFloat.Mode.Bob, 1.4f + i * 0.2f, 0.3f, i);
                btn.onClick.AddListener(() =>
                {
                    if (picked) return;
                    picked = true;
                    for (int k = 0; k < 3; k++)
                    {
                        var p = prizes[k];
                        bool mine = k == index;
                        var at = ((RectTransform)chests[k].transform.parent).anchoredPosition;
                        string label = p.coins > 0 ? "+" + p.coins : "+1 " + Boosters.Names[p.booster];
                        var t = UIKit.Text(panel, label, mine ? 26f : 18f, new Vector2(0.5f, 0.5f), at + new Vector2(0f, 6f), new Vector2(190f, 40f), TextStyle.Dark,
                            mine ? CoinInk : (Color)new Color32(150, 140, 160, 255));
                        chests[k].color = mine ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.8f);
                        chests[k].GetComponent<UIButtonFX>().pulse = 0f;
                        if (mine) Tween.Punch(t.transform, 0.5f, 0.4f);
                    }
                    var prize = prizes[index];
                    if (prize.coins > 0) { Economy.AddCoins(prize.coins); CoinBurst(((RectTransform)chests[index].transform.parent).anchoredPosition, panel, 8); }
                    else { Boosters.Give(prize.booster, 1); AudioManager.I?.Play("unlock", 0.8f); }
                    AudioManager.I?.Play("break", 0.6f, 0.05f);
                });
            }
        }

        // ---------------------------------------------------------------- fail

        public void ShowTrekFail()
        {
            if (m_Results != null) Destroy(m_Results);
            FadeHud(0f);
            var b = Game.I.Board;
            bool canRevive = b != null && !b.Revived && (b.Slides > 0 || b.Elapsed > 3f); // video revive in Full Launch, coins otherwise
            bool videoRevive = canRevive && Platform.RewardedAvailable;
            float w = canRevive ? 1000f : 820f;
            var root = UIKit.Stretch(m_PopupLayer, "Fail");
            m_Results = root.gameObject;
            UIKit.Dim(root, 0.55f);
            var panel = UIKit.Rect(root, "Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 600f));
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, 600f));
            var ribbon = UIKit.Panel(panel, "red", new Vector2(0.5f, 1f), new Vector2(0f, 20f), new Vector2(620f, 120f));
            UIKit.Text(ribbon.transform, "OUCH!", 50f, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(600f, 90f), TextStyle.Heading, UIKit.Cream);
            var hero = LevelCatalog.Characters[Mathf.Clamp(SaveSystem.Data.character, 0, LevelCatalog.Characters.Length - 1)];
            var heroImg = UIKit.Image(panel, GameAssets.I.Sprite(hero.folder + "/Hit"), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(140f, 140f), "Hero");
            heroImg.gameObject.AddComponent<UIImageAnim>().SetFrames(GameAssets.I.Frames(hero.folder + "/Hit"));
            string[] tips =
            {
                "KEEP THE HERO AWAY FROM TRAP ROOMS!",
                "A DEAD END IS SAFE - THE HERO JUST TURNS AROUND.",
                "SLIDE A ROOM IN BEFORE THE HERO REACHES THE GAP!",
                "STUCK? THE HINT BUTTON SHOWS THE NEXT SLIDE.",
            };
            UIKit.Text(panel, tips[Random.Range(0, tips.Length)], 22f, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(w - 80f, 50f), TextStyle.Dark, new Color32(214, 64, 84, 255));
            Button retry;
            if (canRevive)
            {
                // RETRY and REVIVE share size, font and colour so the video is clearly optional
                UIKit.Button(panel, "", "blue", new Vector2(0.5f, 0f), new Vector2(-360f, 110f), new Vector2(150f, 110f), () => Game.I.GoToMenu(MenuScreen.Title), UIArt.Home, 0f, "back");
                retry = UIKit.Button(panel, "RETRY", "green", new Vector2(0.5f, 0f), new Vector2(-95f, 110f), new Vector2(340f, 110f), () => Game.I.RetryAfterBreak(), UIArt.Restart, 34f, "confirm");
                Button revive = null;
                if (videoRevive)
                {
                    revive = UIKit.Button(panel, "REVIVE", "green", new Vector2(0.5f, 0f), new Vector2(275f, 110f), new Vector2(340f, 110f), () => ReviveClicked(revive), UIArt.Video, 34f, "confirm");
                    UIKit.Text(panel, "WATCH A VIDEO: HERO BACK, BOARD KEPT", 16f, new Vector2(0.5f, 0f), new Vector2(275f, 188f), new Vector2(440f, 30f), TextStyle.Dark, InkSoft);
                }
                else
                {
                    revive = UIKit.Button(panel, "REVIVE " + Game.RevivePrice, "yellow", new Vector2(0.5f, 0f), new Vector2(275f, 110f), new Vector2(340f, 110f), () =>
                    {
                        if (m_Board == null || m_Board.Revived || !m_Board.Finished) return; // double tap while the popup closes
                        if (!Economy.Spend(Game.RevivePrice)) { AudioManager.I?.Play("error", 0.7f); Toast("NOT ENOUGH COINS - SOLVE PUZZLES TO EARN MORE!"); return; }
                        Game.I.Revive();
                    }, UIArt.Coin, 30f, "confirm");
                    UIKit.Text(panel, "HERO BACK, BOARD KEPT", 16f, new Vector2(0.5f, 0f), new Vector2(275f, 188f), new Vector2(440f, 30f), TextStyle.Dark, InkSoft);
                }
            }
            else
            {
                retry = UIKit.Button(panel, "RETRY", "green", new Vector2(0.5f, 0f), new Vector2(120f, 110f), new Vector2(360f, 110f), () => Game.I.RetryAfterBreak(), UIArt.Restart, 34f, "confirm");
                UIKit.Button(panel, "", "blue", new Vector2(0.5f, 0f), new Vector2(-190f, 110f), new Vector2(150f, 110f), () => Game.I.GoToMenu(MenuScreen.Title), UIArt.Home, 0f, "back");
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

        public void CloseTrekFail()
        {
            if (m_Results != null) Destroy(m_Results);
            m_Results = null;
            FadeHud(1f);
        }
    }
}
