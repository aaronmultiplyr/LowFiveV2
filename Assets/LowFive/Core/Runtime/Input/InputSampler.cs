// Assets/LowFive/Core/Runtime/Input/InputSampler.cs
using UnityEngine;

namespace LowFive.Core.Input
{
    /// <summary>
    /// Collects Unity input each frame and produces a 64-bit LFInputStruct.
    /// CoreNetManager reads <see cref="Current"/> once per simulation tick.
    /// </summary>
    public sealed class InputSampler : MonoBehaviour
    {
        public LFInputStruct Current { get; private set; }

        /// <summary>Updates <see cref="Current"/> and returns the new value.</summary>
        public LFInputStruct ReadCurrent()
        {
            var i = new LFInputStruct();

            // -------- digital buttons (bits 0-7) --------
            i.SetButton(0, UnityEngine.Input.GetKey(KeyCode.W));          // forward
            i.SetButton(1, UnityEngine.Input.GetKey(KeyCode.S));          // back
            i.SetButton(2, UnityEngine.Input.GetKey(KeyCode.A));          // left
            i.SetButton(3, UnityEngine.Input.GetKey(KeyCode.D));          // right
            i.SetButton(4, UnityEngine.Input.GetMouseButton(0));          // LMB  (jump / fire)
            i.SetButton(5, UnityEngine.Input.GetKey(KeyCode.Space));      // Space (alt-fire)
            i.SetButton(6, UnityEngine.Input.GetMouseButton(1));          // RMB
            i.SetButton(7, UnityEngine.Input.GetKey(KeyCode.LeftShift));  // sprint

            // -------- signed 8-bit axes --------
            i.HatX = (sbyte)Mathf.RoundToInt(
                         Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Horizontal") * 127, -128, 127));
            i.HatY = (sbyte)Mathf.RoundToInt(
                         Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Vertical") * 127, -128, 127));

            // coarse mouse delta (scaled down)
            i.Mdx = (sbyte)Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Mouse X") * 10, -128, 127);
            i.Mdy = (sbyte)Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Mouse Y") * 10, -128, 127);

            Current = i;
            return i;
        }

        /*──────────────────────────────────────────────*/
        void Update()
        {
            ReadCurrent();

#if UNITY_EDITOR
            // Spam once per second for sanity-check
            if (Time.frameCount % 60 == 0)
                Debug.Log($"[InputSampler] packed = 0x{Current.packed:X16}");
#endif
        }
    }
}
