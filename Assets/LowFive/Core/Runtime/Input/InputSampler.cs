using UnityEngine;

namespace LowFive.Core.Input
{
    /// <summary>
    /// Collects Unity input each frame and produces a 64-bit LFInputStruct.
    /// In Core this feeds CoreNetManager; for now we just Debug-log it.
    /// </summary>
    public sealed class InputSampler : MonoBehaviour
    {
        public LFInputStruct Current { get; private set; }   // exposed for Inspector watch

        /// <summary>Call this once per simulation tick to get a fresh snapshot.</summary>
        public LFInputStruct ReadCurrent()
        {
            var i = new LFInputStruct();

            // --- digital buttons (8 bits) ---
            i.SetButton(0, UnityEngine.Input.GetKey(KeyCode.W));
            i.SetButton(1, UnityEngine.Input.GetKey(KeyCode.S));
            i.SetButton(2, UnityEngine.Input.GetKey(KeyCode.A));
            i.SetButton(3, UnityEngine.Input.GetKey(KeyCode.D));
            i.SetButton(4, UnityEngine.Input.GetKey(KeyCode.Space));          // Jump
            i.SetButton(5, UnityEngine.Input.GetMouseButton(0));              // Fire
            i.SetButton(6, UnityEngine.Input.GetMouseButton(1));              // Alt-fire
            i.SetButton(7, UnityEngine.Input.GetKey(KeyCode.LeftShift));      // Sprint

            // --- signed 8-bit axes ---
            i.HatX = (sbyte)Mathf.RoundToInt(Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Horizontal") * 127, -128, 127));
            i.HatY = (sbyte)Mathf.RoundToInt(Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Vertical") * 127, -128, 127));

            // coarse mouse delta (scaled down)
            i.Mdx = (sbyte)Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Mouse X") * 10, -128, 127);
            i.Mdy = (sbyte)Mathf.Clamp(UnityEngine.Input.GetAxisRaw("Mouse Y") * 10, -128, 127);

            Current = i;
            return i;
        }

        // For today’s manual sanity check we just sample every Update.
        void Update()
        {
            ReadCurrent();
#if UNITY_EDITOR
            if (Time.frameCount % 60 == 0)   // spam once per second
                Debug.Log($"[InputSampler] packed=0x{Current.packed:X16}");
#endif
        }
    }
}
