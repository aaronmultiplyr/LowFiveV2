using System;
using UnityEngine;

namespace LowFive.Core.Input
{
    /// <summary>
    /// Runtime smoke test: every 30 frames serialize the current
    /// LFInputStruct into a 12-byte InputFrame, read it back, and
    /// log a mismatch if anything changed.
    /// </summary>
    [RequireComponent(typeof(InputSampler))]
    public sealed class InputRoundTripTester : MonoBehaviour
    {
        private InputSampler _sampler;
        private int _cnt;

        void Awake() => _sampler = GetComponent<InputSampler>();

        void Update()
        {
            if (++_cnt % 30 != 0) return;               // ~0.5 s
            var original = new InputFrame
            {
                tick = (uint)Time.frameCount,
                data = _sampler.Current
            };

            Span<byte> buf = stackalloc byte[InputFrame.Size];
            original.ToBytes(buf);
            var clone = InputFrame.FromBytes(buf);

            if (!original.data.Equals(clone.data))
                Debug.LogError($"[RoundTrip] MISMATCH 0x{original.data.packed:X16} vs 0x{clone.data.packed:X16}");
#if UNITY_EDITOR
            else
                Debug.Log($"[RoundTrip] ok 0x{clone.data.packed:X16}");
#endif
        }
    }
}
