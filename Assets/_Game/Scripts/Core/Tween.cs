using System;
using System.Collections;
using UnityEngine;

namespace MiniPlatformer
{
    public static class Ease
    {
        public static float OutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);
        public static float InCubic(float t) => t * t * t;
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }
    }

    /// <summary>Minimal coroutine tweening. Runs on a hidden persistent host.</summary>
    public class Tween : MonoBehaviour
    {
        static Tween s_Host;

        static Tween Host
        {
            get
            {
                if (s_Host == null)
                {
                    var go = new GameObject("[Tween]");
                    DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideInHierarchy;
                    s_Host = go.AddComponent<Tween>();
                }
                return s_Host;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Host.StartCoroutine(routine);
        public static void Stop(Coroutine c) { if (c != null && s_Host != null) s_Host.StopCoroutine(c); }

        /// <summary>Calls step(eased 0..1) over duration. Stops automatically if 'owner' is destroyed.</summary>
        public static Coroutine To(UnityEngine.Object owner, float duration, Action<float> step, Func<float, float> ease = null,
            bool unscaled = true, Action done = null, float delay = 0f)
        {
            return Run(Routine(owner, duration, step, ease ?? Ease.OutCubic, unscaled, done, delay));
        }

        static IEnumerator Routine(UnityEngine.Object owner, float duration, Action<float> step, Func<float, float> ease, bool unscaled, Action done, float delay)
        {
            if (delay > 0f)
            {
                if (unscaled) yield return new WaitForSecondsRealtime(delay);
                else yield return new WaitForSeconds(delay);
            }
            float t = 0f;
            while (t < duration)
            {
                if (owner == null) yield break;
                step(ease(Mathf.Clamp01(t / duration)));
                yield return null;
                t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            }
            if (owner == null) yield break;
            step(ease(1f));
            done?.Invoke();
        }

        public static Coroutine Delay(float seconds, Action a, bool unscaled = false) => Run(DelayRoutine(seconds, a, unscaled));

        static IEnumerator DelayRoutine(float s, Action a, bool unscaled)
        {
            if (unscaled) yield return new WaitForSecondsRealtime(s);
            else yield return new WaitForSeconds(s);
            a?.Invoke();
        }

        /// <summary>
        /// Scale "punch" (squash/pop) on a transform. Rapid repeated punches restart the effect from the
        /// original rest scale instead of stacking on top of each other.
        /// </summary>
        public static void Punch(Transform t, float amount = 0.2f, float duration = 0.25f, bool unscaled = true)
        {
            if (t == null) return;
            var p = t.GetComponent<TweenPunch>();
            if (p == null)
            {
                p = t.gameObject.AddComponent<TweenPunch>();
                p.restScale = t.localScale;
            }
            if (p.routine != null) Stop(p.routine);
            var baseScale = p.restScale;
            p.routine = To(t, duration, k =>
            {
                float s = 1f + amount * Mathf.Sin(k * Mathf.PI) * (1f - k * 0.5f);
                t.localScale = baseScale * s;
            }, x => x, unscaled, () => { if (t != null) { t.localScale = baseScale; p.routine = null; } });
        }
    }
}
