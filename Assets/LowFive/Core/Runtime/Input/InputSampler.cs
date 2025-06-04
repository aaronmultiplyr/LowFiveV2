// Assets/LowFive/Core/Runtime/Input/InputSampler.cs
using UnityEngine;

namespace LowFive.Core.Input
{
    /// <summary>
    /// Collects Unity input each frame and packs it into <see cref="LFInputStruct"/>.
    ///
    /// • <see cref="Current"/> is refreshed every <c>Update</c>.  
    /// • Any code can quickly fetch the last snapshot via <see cref="Latest"/>.
    /// </summary>
    [DefaultExecutionOrder(-400)]   // run *before* CoreNetManager (-500) polls it
    public sealed class InputSampler : MonoBehaviour
    {
        /* ────────── singleton helper ────────── */
        public static InputSampler Instance { get; private set; }

        /// <summary>
        /// Most-recent input snapshot (equivalent to <c>Instance?.Current</c>).  
        /// If the sampler isn’t present, returns <c>default</c>.
        /// </summary>
        public static LFInputStruct Latest => Instance != null ? Instance.Current : default;

        /* ────────── public data ─────────────── */
        public LFInputStruct Current { get; private set; }

        /* ────────── Unity lifecycle ─────────── */
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /* ────────── main loop ───────────────── */
        void Update()
        {
            SampleInput();

#if UNITY_EDITOR
            if (Time.frameCount % 60 == 0)        // once per second
                Debug.Log($"[InputSampler] packed = 0x{Current.packed:X16}");
#endif
        }

        /* ────────── helpers ─────────────────── */
        /// <summary>Rebuilds <see cref="Current"/> and also returns it.</summary>
        public LFInputStruct SampleInput()
        {
            var i = new LFInputStruct();

            // -------- digital buttons (bits 0-7) ---------------------------
            i.SetButton(0, UnityEngine.Input.GetKey(KeyCode.W));          // forward
            i.SetButton(1, UnityEngine.Input.GetKey(KeyCode.S));          // back
            i.SetButton(2, UnityEngine.Input.GetKey(KeyCode.A));          // left
            i.SetButton(3, UnityEngine.Input.GetKey(KeyCode.D));          // right
            i.SetButton(4, UnityEngine.Input.GetMouseButton(0));          // LMB  (fire / jump)
            i.SetButton(5, UnityEngine.Input.GetKey(KeyCode.Space));      // Space (alt-fire)
            i.SetButton(6, UnityEngine.Input.GetMouseButton(1));          // RMB
            i.SetButton(7, UnityEngine.Input.GetKey(KeyCode.LeftShift));  // sprint

            // -------- 8-bit signed axes ------------------------------------
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
    }
}
