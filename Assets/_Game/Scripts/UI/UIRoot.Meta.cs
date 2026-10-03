using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MiniPlatformer
{
    /// <summary>
    /// The come-back-tomorrow layer: daily login streak, lucky spin, daily missions, shop (boosters, free coins,
    /// heroes), the pre-level booster picker and the Daily Run result screen. Every video button is optional,
    /// marked with the video icon, and only shown when a rewarded ad can actually play.
    /// </summary>
    public partial class UIRoot
    {
        GameObject m_Meta;
        public static readonly int[] HeroPrices = { 0, 600, 1200, 2000 };
        const int FreeCoinsAmount = 60, FreeCoinsPerDay = 5;

        static readonly Color InkSoft = new Color32(120, 90, 110, 255);
        static readonly Color CoinInk = new Color32(194, 120, 20, 255);

        bool MetaOpen => m_Meta != null;

        void CloseMeta()
        {
            if (m_Meta != null) Destroy(m_Meta);
            m_Meta = null;
            RefreshTitleSide(); // ready dots may have changed
            OnMenuCoinsChanged();
        }

        /// <summary>Standard popup: dim, cream panel, ribbon title, close button. Returns the panel.</summary>
        RectTransform MetaFrame(string title, Vector2 size, string ribbon = "yellow")
        {
            if (m_Meta != null) Destroy(m_Meta);
            var root = UIKit.Stretch(m_PopupLayer, "Meta_" + title);
            m_Meta = root.gameObject;
            UIKit.Dim(root, 0.62f);
            var panel = UIKit.Rect(FitBox(root, size.x + 60f), "Panel", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), size);
            UIKit.Panel(panel, "panel", new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var rib = UIKit.Panel(panel, ribbon, new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(Mathf.Min(620f, size.x - 160f), 104f), "Ribbon");
            UIKit.Text(rib.transform, title, 40f, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(600f, 80f), ribbon == "yellow" ? TextStyle.Dark : TextStyle.Heading);
            var close = UIKit.IconButton(panel, UIArt.Close, "red", new Vector2(1f, 1f), new Vector2(-24f, -24f), 96f, CloseMeta, "back");
            UIKit.PopIn(panel);
            Select(close);
            return panel;
        }

        static TextMeshProUGUI Label(Transform parent, string text, float size, Vector2 pos, float width = 600f, Color? color = null, TextStyle style = TextStyle.Dark)
            => UIKit.Text(parent, text, size, new Vector2(0.5f, 0.5f), pos, new Vector2(width, size * 1.8f), style, color);

        static string UntilMidnight()
        {
            var left = DateTime.Today.AddDays(1) - DateTime.Now;
            return left.TotalHours >= 1 ? $"{(int)left.TotalHours}H {left.Minutes}M" : $"{Mathf.Max(1, left.Minutes)}M";
        }

        /// <summary>Coins flying out of a point (reward feedback).</summary>
        public void CoinBurst(Vector2 localPos, Transform parent, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var img = UIKit.Image(parent, UIArt.Coin, new Vector2(0.5f, 0.5f), localPos, new Vector2(56f, 56f), "Coin");
                var rt = img.rectTransform;
                var dir = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(160f, 320f) + Vector2.up * 120f;
                Vector2 from = localPos;
                Tween.To(rt, 0.7f, k =>
                {
                    rt.anchoredPosition = from + dir * k + Vector2.down * (400f * k * k);
                    img.color = new Color(1f, 1f, 1f, 1f - k * k);
                    rt.localRotation = Quaternion.Euler(0f, 0f, k * 360f);
                }, x => x, true, () => { if (rt != null) Destroy(rt.gameObject); }, i * 0.03f);
            }
            AudioManager.I?.Play("coin", 0.7f, 0.05f);
        }

        void OnMenuCoinsChanged()
        {
            m_CoinLabels.RemoveAll(t => t == null);
            foreach (var t in m_CoinLabels) t.text = Economy.Coins.ToString();
        }

        // every coin chip on screen (the title and any popup on top of it each have one)
        readonly System.Collections.Generic.List<TextMeshProUGUI> m_CoinLabels = new System.Collections.Generic.List<TextMeshProUGUI>();

        /// <summary>Coin counter chip used on menu screens.</summary>
        void CoinChip(Transform root, Vector2 anchor, Vector2 pos)
        {
            var rt = UIKit.Rect(root, "Coins", anchor, pos, new Vector2(300f, 90f));
            UIKit.Panel(rt, "glass", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(290f, 86f));
            UIKit.Image(rt, UIArt.Coin, new Vector2(0.5f, 0.5f), new Vector2(-100f, 0f), new Vector2(70f, 70f));
            m_CoinLabels.Add(UIKit.Text(rt, Economy.Coins.ToString(), 32f, new Vector2(0.5f, 0.5f), new Vector2(30f, 0f), new Vector2(190f, 60f), TextStyle.Heading, UIKit.Gold, TextAlignmentOptions.Left));
            UIKit.PopIn(rt, 0.5f);
        }

        /// <summary>Round icon button with a caption and an optional red "ready" dot.</summary>
        Button SideButton(Transform parent, Sprite icon, string color, string caption, Vector2 anchor, Vector2 pos, bool dot, UnityEngine.Events.UnityAction onClick)
        {
            var b = UIKit.IconButton(parent, icon, color, anchor, pos, 108f, onClick);
            UIKit.Text(b.transform, caption, 18f, new Vector2(0.5f, 0f), new Vector2(0f, -26f), new Vector2(200f, 30f), TextStyle.Body);
            if (dot)
            {
                var d = UIKit.Image(b.transform, UIArt.Dot, new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(40f, 40f), "Dot");
                UIFloat.Add(d.gameObject, UIFloat.Mode.Pulse, 1.4f, 4f);
            }
            return b;
        }

        // ---------------------------------------------------------------- daily reward

        public void OfferDailyReward(float delay)
        {
            if (!Economy.DailyReady) return;
            RunRealtime(delay, () =>
            {
                if (Game.I.InLevel || m_Screen != MenuScreen.Title || MetaOpen || m_Settings != null || m_Confirm != null) return;
                ShowDaily();
            });
        }

        public void ShowDaily()
        {
            bool tall = Portrait; // phones: two rows of big cards instead of one long row
            var panel = MetaFrame("DAILY REWARD", tall ? new Vector2(1060f, 1080f) : new Vector2(1500f, 700f));
            bool ready = Economy.DailyReady;
            int today = Economy.DailyIndex;
            int streak = SaveSystem.Data.dailyDay >= Economy.Today - 1 ? SaveSystem.Data.dailyStreak : 0;
            Label(panel, ready ? "COME BACK EVERY DAY FOR BIGGER REWARDS!" : "SEE YOU TOMORROW!", 24f, new Vector2(0f, tall ? 410f : 230f), 1000f, InkSoft);

            for (int i = 0; i < Economy.DailyRewards.Length; i++)
            {
                bool claimed = ready ? i < today : i <= today;
                bool isToday = i == today;
                var card = UIKit.Panel(panel, isToday ? (ready ? "yellow" : "green") : claimed ? "dim_card" : "card",
                    new Vector2(0.5f, 0.5f), tall ? new Vector2(i < 4 ? (i - 1.5f) * 240f : (i - 5f) * 240f, i < 4 ? 200f : -100f) : new Vector2((i - 3) * 196f, 20f), new Vector2(tall ? 220f : 180f, tall ? 270f : 250f), "Day" + i);
                Label(card.transform, "DAY " + (i + 1), 22f, new Vector2(0f, 90f), 170f, isToday ? (Color?)null : InkSoft);
                bool last = i == Economy.DailyRewards.Length - 1;
                UIKit.Image(card.transform, last ? UIArt.Gift : UIArt.Coin, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(92f, 92f)).color =
                    last ? (Color)new Color32(214, 64, 120, 255) : Color.white;
                Label(card.transform, Economy.DailyRewards[i].ToString(), 28f, new Vector2(0f, -70f), 170f, CoinInk, TextStyle.Dark);
                if (last) Label(card.transform, "+ BOOSTERS", 14f, new Vector2(0f, -100f), 170f, InkSoft);
                if (claimed && !isToday) UIKit.Image(card.transform, UIArt.Check, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(80f, 80f)).color = new Color32(70, 170, 70, 255);
                if (isToday && ready) UIFloat.Add(card.gameObject, UIFloat.Mode.Pulse, 1.2f, 2f);
                UIKit.SlideIn(card.rectTransform, new Vector2(0f, -60f), 0.05f + i * 0.05f);
            }

            Label(panel, streak > 1 ? $"LOGIN STREAK: {streak} DAYS - KEEP IT GOING!" : "MISS A DAY AND THE STREAK STARTS OVER", 20f, new Vector2(0f, tall ? -280f : -140f), 1000f, new Color32(214, 64, 84, 255));

            if (!ready)
            {
                Label(panel, "NEXT REWARD IN " + UntilMidnight(), 26f, new Vector2(0f, tall ? -380f : -230f), 900f, InkSoft);
                return;
            }
            int amount = Economy.DailyRewards[today];
            bool video = Platform.RewardedAvailable;
            Button claim = null, dbl = null;
            // CLAIM and the video x2 share size, font and colour, so the video is clearly optional (CrazyGames rule)
            claim = UIKit.Button(panel, "CLAIM", "green", new Vector2(0.5f, 0f), new Vector2(video ? -200f : 0f, 90f), new Vector2(340f, 110f), () =>
            {
                int got = Economy.ClaimDaily(1);
                ClaimFx(panel, got);
            }, UIArt.Check, 34f, "confirm");
            if (video)
            {
                dbl = UIKit.Button(panel, $"x2  {amount * 2}", "green", new Vector2(0.5f, 0f), new Vector2(200f, 90f), new Vector2(340f, 110f), () =>
                {
                    if (Platform.AdPlaying) return;
                    Platform.Rewarded(() =>
                    {
                        int got = Economy.ClaimDaily(2);
                        ClaimFx(panel, got);
                    }, () =>
                    {
                        if (dbl != null) dbl.gameObject.SetActive(false);
                        Toast("NO VIDEO RIGHT NOW - CLAIM THE NORMAL REWARD!");
                    });
                }, UIArt.Video, 34f, "confirm");
            }
            Select(claim);
        }

        void ClaimFx(RectTransform panel, int coins)
        {
            if (coins <= 0) return;
            AudioManager.I?.Play("jackpot", 0.8f, 0f);
            if (panel != null) CoinBurst(new Vector2(0f, -150f), panel, 14);
            Toast($"+{coins} COINS!");
            RunRealtime(1.1f, () => { if (MetaOpen) CloseMeta(); });
        }

        // ---------------------------------------------------------------- lucky spin

        static Sprite s_WheelFace;
        bool m_Spinning;

        static Sprite WheelFace()
        {
            if (s_WheelFace != null) return s_WheelFace;
            const int S = 512;
            float R = S / 2f - 4f;
            var px = new Color[S * S];
            var rim = new Color32(94, 45, 120, 255);
            var sep = new Color(1f, 1f, 1f, 1f);
            int n = Economy.Wheel.Length;
            float slice = 360f / n;
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float aa = Mathf.Clamp01(R - r + 0.5f); // anti-aliased outer edge
                    if (aa <= 0f) continue;
                    Color c;
                    if (r > R - 22f) c = rim;
                    else
                    {
                        float ang = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg + slice / 2f, 360f); // clockwise from top
                        int slot = Mathf.FloorToInt(ang / slice) % n;
                        float edge = Mathf.Min(Mathf.Repeat(ang, slice), slice - Mathf.Repeat(ang, slice)) * Mathf.Deg2Rad * r;
                        c = Color.Lerp(Color.white, Economy.Wheel[slot].color, Mathf.Lerp(0.55f, 1f, r / R));
                        if (edge < 2.5f) c = Color.Lerp(sep, c, edge / 2.5f);
                        if (r < 58f) c = r < 50f ? new Color32(255, 214, 74, 255) : rim;
                    }
                    c.a *= aa;
                    px[y * S + x] = c;
                }
            // rim bulbs
            for (int b = 0; b < n * 2; b++)
            {
                float a = b * Mathf.PI * 2f / (n * 2);
                int cx = Mathf.RoundToInt(S / 2f + Mathf.Sin(a) * (R - 11f)), cy = Mathf.RoundToInt(S / 2f + Mathf.Cos(a) * (R - 11f));
                for (int y = -6; y <= 6; y++)
                    for (int x = -6; x <= 6; x++)
                        if (x * x + y * y <= 30 && cx + x >= 0 && cx + x < S && cy + y >= 0 && cy + y < S)
                            px[(cy + y) * S + cx + x] = new Color32(255, 240, 170, 255);
            }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            tex.SetPixels(px);
            tex.Apply(true, true);
            s_WheelFace = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero, false); // no physics outline: GPU-only texture
            return s_WheelFace;
        }

        public void ShowSpin()
        {
            var panel = MetaFrame("LUCKY SPIN", new Vector2(1180f, 900f), "purple");
            var wheelRoot = UIKit.Rect(panel, "Wheel", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(560f, 560f));
            var glow = UIKit.Image(wheelRoot, UIArt.Rays, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f), "Rays");
            glow.color = new Color(1f, 0.85f, 0.4f, 0.55f);
            UIFloat.Add(glow.gameObject, UIFloat.Mode.Spin, 0.6f, 1f);
            var wheel = UIKit.Image(wheelRoot, WheelFace(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 560f), "Face").rectTransform;
            int n = Economy.Wheel.Length;
            for (int i = 0; i < n; i++)
            {
                var slot = Economy.Wheel[i];
                float a = i * 360f / n; // clockwise from top
                var holder = UIKit.Rect(wheel, "Slot" + i, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
                holder.localRotation = Quaternion.Euler(0f, 0f, -a);
                bool coins = slot.booster < 0;
                var ic = UIKit.Image(holder, coins ? UIArt.Coin : UIArt.BoosterIcon(slot.booster), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(64f, 64f));
                if (!coins) ic.color = new Color32(70, 40, 90, 255);
                UIKit.Text(holder, slot.label, slot.label.Length > 3 ? 20f : 30f, new Vector2(0.5f, 0.5f), new Vector2(0f, 128f), new Vector2(180f, 50f), TextStyle.Heading);
            }
            var pointer = UIKit.Image(wheelRoot, UIArt.Arrow(3), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(110f, 110f), "Pointer");
            pointer.color = new Color32(236, 79, 91, 255);
            UIKit.Image(wheelRoot, UIArt.Star, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f), "Hub");

            var buttons = UIKit.Rect(panel, "Buttons", new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(1000f, 120f));
            var info = Label(panel, "", 20f, new Vector2(0f, -262f), 900f, InkSoft);
            BuildSpinButtons(buttons, wheel, info);
        }

        void BuildSpinButtons(RectTransform buttons, RectTransform wheel, TextMeshProUGUI info)
        {
            for (int i = buttons.childCount - 1; i >= 0; i--) Destroy(buttons.GetChild(i).gameObject);
            if (Economy.FreeSpinReady)
            {
                var b = UIKit.Button(buttons, "FREE SPIN", "green", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 116f), () => DoSpin(true, buttons, wheel, info), UIArt.Wheel, 36f, "confirm");
                b.GetComponent<UIButtonFX>().pulse = 1f;
                info.text = "ONE FREE SPIN EVERY DAY!";
                Select(b);
            }
            else if (Economy.SpinTokens > 0)
            {
                var b = UIKit.Button(buttons, "SPIN", "green", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 116f), () => DoSpin(false, buttons, wheel, info), UIArt.Wheel, 36f, "confirm");
                b.GetComponent<UIButtonFX>().pulse = 1f;
                info.text = $"SPIN TOKENS: {Economy.SpinTokens}  (1 FOR EVERY {Economy.PuzzlesPerToken} PUZZLES SOLVED)";
                Select(b);
            }
            else if (Economy.AdSpinsLeft > 0 && Platform.RewardedAvailable)
            {
                Button b = null;
                b = UIKit.Button(buttons, "SPIN AGAIN", "purple", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 116f), () =>
                {
                    if (Platform.AdPlaying || m_Spinning) return;
                    Platform.Rewarded(() => DoSpin(false, buttons, wheel, info), () =>
                    {
                        Toast("NO VIDEO RIGHT NOW. TRY AGAIN LATER!");
                        if (buttons != null) BuildSpinButtons(buttons, wheel, info);
                    });
                }, UIArt.Video, 34f, "confirm");
                info.text = $"WATCH A VIDEO FOR AN EXTRA SPIN ({Economy.AdSpinsLeft} LEFT TODAY)";
                Select(b);
            }
            else
            {
                int left = Economy.PuzzlesPerToken - SaveSystem.Data.tokenProgress;
                info.text = $"SOLVE {left} MORE PUZZLE{(left == 1 ? "" : "S")} FOR A SPIN - FREE SPIN IN " + UntilMidnight();
            }
        }

        void DoSpin(bool free, RectTransform buttons, RectTransform wheel, TextMeshProUGUI info)
        {
            if (m_Spinning || wheel == null) return;
            if (free && !Economy.FreeSpinReady) return;
            m_Spinning = true;
            for (int i = buttons.childCount - 1; i >= 0; i--) Destroy(buttons.GetChild(i).gameObject);
            int slot = Economy.Spin(free); // prize is decided and saved up front
            StartCoroutine(SpinRoutine(slot, buttons, wheel, info));
        }

        IEnumerator SpinRoutine(int slot, RectTransform buttons, RectTransform wheel, TextMeshProUGUI info)
        {
            int n = Economy.Wheel.Length;
            float sliceDeg = 360f / n;
            float start = Mathf.Repeat(wheel.localEulerAngles.z, 360f);
            float target = 360f * 6f + slot * sliceDeg + UnityEngine.Random.Range(-sliceDeg * 0.3f, sliceDeg * 0.3f);
            const float dur = 4.2f;
            int lastTick = -1;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                if (wheel == null) { m_Spinning = false; yield break; }
                float k = 1f - Mathf.Pow(1f - t / dur, 3.2f);
                float z = Mathf.Lerp(start, target, k);
                wheel.localRotation = Quaternion.Euler(0f, 0f, z);
                int tick = Mathf.FloorToInt((z + sliceDeg / 2f) / sliceDeg);
                if (tick != lastTick) { lastTick = tick; AudioManager.I?.Play("spin", 0.6f, 0.02f); }
                yield return null;
            }
            if (wheel != null) wheel.localRotation = Quaternion.Euler(0f, 0f, target);
            m_Spinning = false;
            var prize = Economy.Wheel[slot];
            bool jackpot = prize.coins >= 500;
            AudioManager.I?.Play(jackpot ? "jackpot" : "bank", 0.85f, 0f);
            if (jackpot) Platform.HappyTime();
            string msg = prize.coins > 0 ? $"+{prize.coins} COINS!" : $"+1 {Boosters.Names[prize.booster]}!";
            Toast(jackpot ? "JACKPOT! " + msg : msg);
            if (wheel != null && prize.coins > 0) CoinBurst(Vector2.zero, wheel.parent, jackpot ? 24 : 10);
            if (buttons != null) BuildSpinButtons(buttons, wheel, info);
        }

        // ---------------------------------------------------------------- missions

        public void ShowMissions()
        {
            Missions.EnsureToday();
            var panel = MetaFrame("DAILY MISSIONS", new Vector2(1180f, 820f), "blue");
            BuildMissionRows(panel);
        }

        void BuildMissionRows(RectTransform panel)
        {
            var list = panel.Find("List");
            if (list != null) Destroy(list.gameObject);
            var root = UIKit.Rect(panel, "List", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 700f));
            for (int i = 0; i < Missions.PerDay; i++)
            {
                int slot = i;
                var def = Missions.Get(i);
                int prog = Missions.Progress(i);
                bool done = prog >= def.target, claimed = Missions.Claimed(i);
                var row = UIKit.Panel(root, claimed ? "dim_card" : "card", new Vector2(0.5f, 0.5f), new Vector2(0f, 180f - i * 170f), new Vector2(1040f, 150f), "Mission" + i);
                var icon = UIKit.Panel(row.transform, done ? "green" : "blue", new Vector2(0f, 0.5f), new Vector2(80f, 4f), new Vector2(110f, 110f));
                UIKit.Image(icon.transform, MissionIcon(def.kind), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(70f, 70f));
                UIKit.Text(row.transform, def.Text, 26f, new Vector2(0f, 0.5f), new Vector2(390f, 28f), new Vector2(480f, 50f), TextStyle.Dark, null, TextAlignmentOptions.Left);
                var track = UIKit.Panel(row.transform, "track", new Vector2(0f, 0.5f), new Vector2(390f, -30f), new Vector2(480f, 34f));
                var fill = UIKit.Panel(track.transform, "fill", new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(472f * Mathf.Clamp01(prog / (float)def.target), 26f));
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.color = done ? new Color32(120, 214, 84, 255) : new Color32(90, 170, 250, 255);
                UIKit.Text(track.transform, $"{prog}/{def.target}", 18f, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(300f, 30f), TextStyle.Body);
                // reward
                UIKit.Image(row.transform, UIArt.Coin, new Vector2(1f, 0.5f), new Vector2(-370f, 0f), new Vector2(56f, 56f));
                UIKit.Text(row.transform, def.reward.ToString(), 28f, new Vector2(1f, 0.5f), new Vector2(-285f, 0f), new Vector2(110f, 50f), TextStyle.Dark, CoinInk, TextAlignmentOptions.Left);
                // action
                if (claimed)
                    UIKit.Image(row.transform, UIArt.Check, new Vector2(1f, 0.5f), new Vector2(-110f, 0f), new Vector2(80f, 80f)).color = new Color32(70, 170, 70, 255);
                else if (done)
                {
                    var b = UIKit.Button(row.transform, "CLAIM", "green", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 96f), () =>
                    {
                        int got = Missions.Claim(slot);
                        if (got > 0) { AudioManager.I?.Play("bank", 0.8f, 0f); Toast($"+{got} COINS!"); CoinBurst(new Vector2(420f, 180f - slot * 170f), root, 10); }
                        BuildMissionRows(panel);
                    }, null, 28f, "confirm");
                    b.GetComponent<UIButtonFX>().pulse = 1f;
                }
                else if (Missions.CanReroll(slot) && !Platform.RewardedAvailable)
                {
                    UIKit.Button(row.transform, Missions.RerollPrice.ToString(), "yellow", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 96f), () =>
                    {
                        if (!Missions.CanReroll(slot)) return;
                        if (!Economy.Spend(Missions.RerollPrice)) { AudioManager.I?.Play("error", 0.7f); Toast("NOT ENOUGH COINS!"); return; }
                        Missions.Reroll(slot);
                        Toast("NEW MISSION!");
                        AudioManager.I?.Play("unlock", 0.8f);
                        BuildMissionRows(panel);
                    }, UIArt.Restart, 26f);
                }
                else if (Missions.CanReroll(slot) && Platform.RewardedAvailable)
                {
                    UIKit.Button(row.transform, "SWAP", "purple", new Vector2(1f, 0.5f), new Vector2(-120f, 0f), new Vector2(200f, 96f), () =>
                    {
                        if (Platform.AdPlaying) return;
                        Platform.Rewarded(() => { Missions.Reroll(slot); Toast("NEW MISSION!"); AudioManager.I?.Play("unlock", 0.8f); BuildMissionRows(panel); }, () => { Toast("NO VIDEO RIGHT NOW. TRY AGAIN LATER!"); BuildMissionRows(panel); });
                    }, UIArt.Video, 26f);
                }
                UIKit.SlideIn(row.rectTransform, new Vector2(0f, -60f), 0.05f + i * 0.07f);
            }
            UIKit.Text(root, "NEW MISSIONS IN " + UntilMidnight(), 20f, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(900f, 34f), TextStyle.Dark, InkSoft);
        }

        static Sprite MissionIcon(MissionKind k)
        {
            switch (k)
            {
                case MissionKind.ClearLevels: return UIArt.Check;
                case MissionKind.ThreeStars: return UIArt.Star;
                case MissionKind.Coins: return UIArt.Coin;
                case MissionKind.Stomp: return UIArt.Arrow(3);
                case MissionKind.FastClear: return UIArt.Clock;
                case MissionKind.FewSlides: return UIArt.Arrow(0);
                default: return UIArt.Gift;
            }
        }

        // ---------------------------------------------------------------- shop

        public void ShowShop()
        {
            bool tall = Portrait;
            var panel = MetaFrame("SHOP", tall ? new Vector2(1060f, 900f) : new Vector2(1500f, 860f), "green");
            CoinChip(panel, new Vector2(0f, 1f), new Vector2(200f, -80f));
            Label(panel, "BOOSTERS - USE THEM FROM THE BUTTONS IN A PUZZLE", 22f, new Vector2(0f, 260f), 1200f, InkSoft);
            for (int b = 0; b < Boosters.Count; b++)
            {
                int booster = b;
                var card = UIKit.Panel(panel, "card", new Vector2(0.5f, 0.5f), new Vector2((b - 1) * (tall ? 335f : 360f), 70f), new Vector2(tall ? 310f : 320f, 330f), "Booster" + b);
                var badge = UIKit.Panel(card.transform, "blue", new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(120f, 120f));
                UIKit.Image(badge.transform, UIArt.BoosterIcon(b), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(76f, 76f));
                UIKit.Text(card.transform, Boosters.Names[b], 26f, new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(300f, 40f), TextStyle.Dark);
                UIKit.Text(card.transform, Boosters.Descriptions[b], 16f, new Vector2(0.5f, 0.5f), new Vector2(0f, -46f), new Vector2(300f, 30f), TextStyle.Dark, InkSoft);
                var owned = UIKit.Text(card.transform, "OWNED: " + Boosters.Owned(b), 18f, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(300f, 30f), TextStyle.Dark, CoinInk);
                UIKit.Button(card.transform, Boosters.Prices[b].ToString(), "yellow", new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(240f, 86f), () =>
                {
                    if (Boosters.Buy(booster))
                    {
                        AudioManager.I?.Play("unlock", 0.8f);
                        owned.text = "OWNED: " + Boosters.Owned(booster);
                        Tween.Punch(owned.transform, 0.4f, 0.3f);
                    }
                    else { AudioManager.I?.Play("error", 0.7f); Toast("NOT ENOUGH COINS - SOLVE MORE PUZZLES!"); }
                }, UIArt.Coin, 30f, "confirm");
                UIKit.SlideIn(card.rectTransform, new Vector2(0f, -60f), 0.05f + b * 0.06f);
            }

            // free coins for a video (a few per day) + heroes
            var d = SaveSystem.Data;
            if (d.coinAdDay != Economy.Today) { d.coinAdDay = Economy.Today; d.coinAds = 0; }
            if (d.coinAds < FreeCoinsPerDay && Platform.RewardedAvailable)
            {
                Button free = null;
                free = UIKit.Button(panel, $"FREE +{FreeCoinsAmount}", "purple", new Vector2(0.5f, 0f), new Vector2(tall ? -220f : -230f, 100f), new Vector2(tall ? 400f : 400f, 110f), () =>
                {
                    if (Platform.AdPlaying) return;
                    Platform.Rewarded(() =>
                    {
                        d.coinAds++;
                        Economy.AddCoins(FreeCoinsAmount);
                        AudioManager.I?.Play("jackpot", 0.7f, 0f);
                        if (free != null) CoinBurst(((RectTransform)free.transform).anchoredPosition + new Vector2(0f, -330f), panel, 10);
                        if (free != null && d.coinAds >= FreeCoinsPerDay) free.gameObject.SetActive(false);
                    }, () => { if (free != null) free.gameObject.SetActive(false); Toast("NO VIDEO RIGHT NOW. TRY AGAIN LATER!"); });
                }, UIArt.Video, 32f, "confirm");
            }
            if (!Platform.RewardedAvailable)
            {
                Button gift = null;
                bool ready = Economy.GiftReady;
                gift = UIKit.Button(panel, ready ? "FREE GIFT" : "GIFT IN " + UntilMidnight(), ready ? "purple" : "grey", new Vector2(0.5f, 0f), new Vector2(tall ? -220f : -230f, 100f), new Vector2(400f, 110f), () =>
                {
                    if (!Economy.GiftReady) { AudioManager.I?.Play("error", 0.6f); return; }
                    int got = Economy.OpenGift();
                    AudioManager.I?.Play("jackpot", 0.7f, 0f);
                    Toast($"+1 {Boosters.Names[got]}!");
                    ShowShop(); // refresh counts and the button
                }, UIArt.Gift, 30f, "confirm");
                if (ready) gift.GetComponent<UIButtonFX>().pulse = 1f;
            }
            UIKit.Button(panel, "HEROES", "blue", new Vector2(0.5f, 0f), new Vector2(230f, 100f), new Vector2(400f, 110f), () =>
            {
                CloseMetaSilently();
                ShowMenu(MenuScreen.Characters);
            }, UIArt.Play, 34f);
        }

        void CloseMetaSilently()
        {
            if (m_Meta != null) Destroy(m_Meta);
            m_Meta = null;
        }
    }
}
