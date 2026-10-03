using System;
using System.Collections;
using CrazyGames;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Crash-proof wrapper around the CrazyGames SDK: gameplay events, video ads, cloud saves and device info.
    /// Everything degrades to a harmless no-op (or local PlayerPrefs) when the SDK isn't there, e.g. in Android
    /// builds, on other hosts, or if the SDK fails to initialise. SDK exceptions never reach the game.
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Master switch for video ads. OFF for CrazyGames' Basic Launch, where monetization is disabled: no midgame
        /// ads and no video buttons; every reward is earned by playing instead (bonus chests, spin tokens, coin
        /// revives, daily gift). Flip to true for Full Launch: midgame ads at natural breaks and optional rewarded
        /// videos reappear next to the play-earned options.
        /// </summary>
        public static readonly bool AdsEnabled = false;

        /// <summary>SDK initialised and usable.</summary>
        public static bool Ready { get; private set; }
        public static bool AdPlaying { get; private set; }

        static bool s_Gameplay;
        static string s_Device = "";
        static float s_RewardedPausedUntil;
        const float RewardedRetryDelay = 180f;
        const string BasicLaunchError = "adsDisabledBasicLaunch";
        /// <summary>CrazyGames said monetization is off (Basic Launch): rewards are granted directly, midgames skipped.</summary>
        public static bool AdsDisabledByHost { get; private set; }

        static bool Supported => Application.isEditor || Application.platform == RuntimePlatform.WebGLPlayer;

        public static IEnumerator Init(float timeout = 8f)
        {
            if (!Supported) yield break;
            bool available = false;
            Try(() => available = CrazySDK.IsAvailable);
            if (!available) yield break;

            bool done = false;
            if (!Try(() => CrazySDK.Init(() => done = true))) yield break;
            for (float t = 0f; !done && t < timeout; t += Time.unscaledDeltaTime) yield return null;
            Ready = done;
            if (!Ready) { Debug.LogWarning("[Platform] CrazySDK init timed out, continuing without it"); yield break; }
            Try(() => s_Device = CrazySDK.User.SystemInfo?.device?.type ?? "");
            // CrazyGames' mute toggle takes priority over the in-game volume sliders
            Try(() => HostMuted = CrazySDK.Game.Settings?.muteAudio ?? false);
            Try(() => CrazySDK.Game.AddSettingsChangeListener(s => HostMuted = s != null && s.muteAudio));
        }

        /// <summary>The CrazyGames site asked the game to be silent (its mute setting).</summary>
        public static bool HostMuted { get; private set; }

        /// <summary>Phone or tablet (from the CrazyGames device info when available).</summary>
        public static bool IsMobileDevice => s_Device == "mobile" || s_Device == "tablet" || Application.isMobilePlatform;

        // ---------------------------------------------------------------- game events

        public static void GameplayStart()
        {
            if (!Ready || s_Gameplay) return;
            s_Gameplay = true;
            Try(() => CrazySDK.Game.GameplayStart());
        }

        public static void GameplayStop()
        {
            if (!Ready || !s_Gameplay) return;
            s_Gameplay = false;
            Try(() => CrazySDK.Game.GameplayStop());
        }

        /// <summary>Celebrate a real milestone (boss beaten, perfect clear, hero unlocked). Use sparingly.</summary>
        public static void HappyTime()
        {
            if (Ready) Try(() => CrazySDK.Game.HappyTime());
        }

        /// <summary>Attached to player feedback reports on CrazyGames, so a report says where it happened.</summary>
        public static void SetLevelContext(string level)
        {
            if (Ready) Try(() => CrazySDK.Game.SetGameContext(new System.Collections.Generic.Dictionary<string, string> { { "level", level } }));
        }

        public static void ClearLevelContext()
        {
            if (Ready) Try(() => CrazySDK.Game.ClearGameContext());
        }

        // ---------------------------------------------------------------- ads

        /// <summary>True when a rewarded ad can reasonably be offered (SDK up, no adblocker detected).</summary>
        public static bool RewardedAvailable
        {
            get
            {
                if (!AdsEnabled || !Ready || AdPlaying || Time.realtimeSinceStartup < s_RewardedPausedUntil) return false;
                var status = AdblockStatus.Detecting;
                Try(() => status = CrazySDK.Ad.AdblockStatus);
                return status != AdblockStatus.Present;
            }
        }

        /// <summary>Midgame ad at a natural break. 'done' always runs (ad shown, skipped, unfilled or blocked).</summary>
        public static void Midgame(Action done)
        {
            if (AdsDisabledByHost) { Invoke(done); return; }
            ShowAd(CrazyAdType.Midgame, done, code =>
            {
                if (code == BasicLaunchError) AdsDisabledByHost = true;
                Invoke(done);
            });
        }

        /// <summary>
        /// Rewarded ad. 'rewarded' runs only when the ad finished, or when the host has ads switched off entirely
        /// (Basic Launch), so the button still does what it says. Any other error (unfilled, adblock, cooldown) runs
        /// 'failed' and hides the video buttons for a few minutes.
        /// </summary>
        public static void Rewarded(Action rewarded, Action failed)
        {
            if (AdsDisabledByHost) { Invoke(rewarded); return; }
            ShowAd(CrazyAdType.Rewarded, rewarded, code =>
            {
                if (code == BasicLaunchError) { AdsDisabledByHost = true; Invoke(rewarded); return; }
                s_RewardedPausedUntil = Time.realtimeSinceStartup + RewardedRetryDelay;
                Invoke(failed);
            });
        }

        static void ShowAd(CrazyAdType type, Action ok, Action<string> fail)
        {
            if (!AdsEnabled || !Ready || AdPlaying) { InvokeFail(fail, null); return; }
            AdPlaying = true;
            bool ended = false;
            bool wasGameplay = s_Gameplay;
            GameplayStop();
            UIRoot.I?.SetAdBlocker(true);

            void End(bool success, string code)
            {
                if (ended) return;
                ended = true;
                AdPlaying = false;
                UIRoot.I?.SetAdBlocker(false);
                if (wasGameplay) GameplayStart();
                if (!success) Debug.Log("[Platform] ad not shown: " + (code ?? "?"));
                if (success) Invoke(ok); else InvokeFail(fail, code);
            }

            // failsafe: never leave the player stuck behind the blocker if the SDK doesn't call back
            UIRoot.I?.RunRealtime(90f, () => { if (!ended) { Debug.LogWarning("[Platform] ad callback timeout"); End(false, "timeout"); } });
            if (!Try(() => CrazySDK.Ad.RequestAd(type, null, err => End(false, err?.code), () => End(true, null)))) End(false, "exception");
        }

        static void InvokeFail(Action<string> fail, string code)
        {
            if (fail == null) return;
            try { fail(code); }
            catch (Exception e) { Debug.LogException(e); }
        }

        // ---------------------------------------------------------------- saves

        // Progress is written to BOTH the CrazyGames data module (cloud, follows logged-in players across devices) and
        // local PlayerPrefs (browser IndexedDB), so it survives even if the data module isn't enabled for the game.

        public static string LoadString(string key)
        {
            string cloud = "";
            if (Ready) Try(() => cloud = CrazySDK.Data.GetString(key, "") ?? "");
            return !string.IsNullOrEmpty(cloud) ? cloud : PlayerPrefs.GetString(key, "");
        }

        public static void SaveString(string key, string value)
        {
            if (Ready) Try(() => CrazySDK.Data.SetString(key, value));
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        // ---------------------------------------------------------------- helpers

        static bool Try(Action a)
        {
            try { a(); return true; }
            catch (Exception e) { Debug.LogWarning("[Platform] " + e.Message); return false; }
        }

        static void Invoke(Action a)
        {
            if (a == null) return;
            try { a(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
