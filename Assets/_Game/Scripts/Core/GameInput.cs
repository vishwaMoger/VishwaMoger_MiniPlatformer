using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>
    /// Unified input: keyboard (arrows/WASD/ZQSD + Space/Z/W/Up to jump, Shift/X to dash), gamepad (legacy axes)
    /// and the on-screen touch controls, which write into the Touch* fields.
    /// </summary>
    public static class GameInput
    {
        public static bool TouchLeft, TouchRight, TouchJump, TouchDash, TouchDown;
        static bool s_PrevTouchJump, s_PrevTouchDash;
        static bool s_JumpDown, s_DashDown;
        static int s_Frame = -1;

        static void Refresh()
        {
            if (s_Frame == Time.frameCount) return;
            s_Frame = Time.frameCount;
            s_JumpDown = (TouchJump && !s_PrevTouchJump);
            s_DashDown = (TouchDash && !s_PrevTouchDash);
            s_PrevTouchJump = TouchJump;
            s_PrevTouchDash = TouchDash;
        }

        public static float Horizontal
        {
            get
            {
                float x = 0f;
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q)) x -= 1f; // Q = AZERTY
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
                if (TouchLeft) x -= 1f;
                if (TouchRight) x += 1f;
                if (Mathf.Abs(x) < 0.01f)
                {
                    float pad = SafeAxis("Horizontal");
                    if (Mathf.Abs(pad) > 0.3f) x = Mathf.Sign(pad);
                }
                return Mathf.Clamp(x, -1f, 1f);
            }
        }

        public static bool Down => TouchDown || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) || SafeAxis("Vertical") < -0.5f;

        public static bool JumpDown
        {
            get
            {
                Refresh();
                return s_JumpDown || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W) ||
                       Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.JoystickButton0);
            }
        }

        public static bool JumpHeld =>
            TouchJump || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.JoystickButton0);

        public static bool DashDown
        {
            get
            {
                Refresh();
                return s_DashDown || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) ||
                       Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.JoystickButton2);
            }
        }

        public static bool PauseDown =>
            Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.JoystickButton7);

        public static void ClearTouch() { TouchLeft = TouchRight = TouchJump = TouchDash = TouchDown = false; }

        static float SafeAxis(string axis)
        {
            try { return Input.GetAxisRaw(axis); }
            catch { return 0f; }
        }
    }
}
