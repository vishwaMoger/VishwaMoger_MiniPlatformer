using UnityEngine;

namespace MiniPlatformer
{
    /// <summary>Remembers a transform's rest scale so punches never accumulate.</summary>
    public class TweenPunch : MonoBehaviour
    {
        public Vector3 restScale = Vector3.one;
        public Coroutine routine;

        void OnDisable()
        {
            if (routine != null) { Tween.Stop(routine); routine = null; }
            transform.localScale = restScale;
        }
    }
}
