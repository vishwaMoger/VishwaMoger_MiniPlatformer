using System;
using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Lightweight frame animator for Pixel Adventure sprite strips (20 fps by default).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnim : MonoBehaviour
    {
        public SpriteRenderer Renderer { get; private set; }
        public Sprite[] Current { get; private set; }
        public int Frame { get; private set; }
        public bool Finished { get; private set; }
        public float Speed = 1f;
        public bool UseUnscaledTime;

        float m_Fps = 20f;
        bool m_Loop = true;
        float m_Time;
        Action m_OnDone;
        Action<int> m_OnFrame;

        void Awake() => Renderer = GetComponent<SpriteRenderer>();

        public SpriteAnim Play(Sprite[] frames, float fps = 20f, bool loop = true, Action onDone = null, bool restart = false, Action<int> onFrame = null)
        {
            if (Renderer == null) Renderer = GetComponent<SpriteRenderer>();
            if (frames == null || frames.Length == 0) return this;
            if (!restart && frames == Current && !Finished) { m_OnDone = onDone ?? m_OnDone; return this; }
            Current = frames;
            m_Fps = fps;
            m_Loop = loop;
            m_Time = 0f;
            Frame = 0;
            Finished = false;
            m_OnDone = onDone;
            m_OnFrame = onFrame;
            Renderer.sprite = frames[0];
            m_OnFrame?.Invoke(0);
            return this;
        }

        public void RandomizeStart()
        {
            if (Current == null || Current.Length == 0) return;
            m_Time = UnityEngine.Random.Range(0f, Current.Length / m_Fps);
        }

        public bool IsPlaying(Sprite[] frames) => Current == frames && !Finished;

        void Update()
        {
            if (Current == null || Current.Length == 0 || Finished) return;
            m_Time += (UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * Speed;
            int f = Mathf.FloorToInt(m_Time * m_Fps);
            if (m_Loop) f %= Current.Length;
            else if (f >= Current.Length)
            {
                f = Current.Length - 1;
                Finished = true;
            }
            if (f != Frame)
            {
                Frame = f;
                Renderer.sprite = Current[f];
                m_OnFrame?.Invoke(f);
            }
            if (Finished)
            {
                var cb = m_OnDone;
                m_OnDone = null;
                cb?.Invoke();
            }
        }
    }
}
