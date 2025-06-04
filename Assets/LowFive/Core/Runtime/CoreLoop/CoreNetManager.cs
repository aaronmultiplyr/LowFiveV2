// Assets/LowFive/Core/Runtime/CoreLoop/CoreNetManager.cs
using System;
using UnityEngine;
using LowFive.Core.Input;
using LowFive.Core.CoreLoop;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// Local-only version of the deterministic net-loop.
    /// Networking gets injected on Day 4.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CoreNetManager : MonoBehaviour
    {
        public static CoreNetManager Instance { get; private set; }

        // piping
        [SerializeField] private InputSampler sampler;

        // tick system
        public uint CurrentTick => _timer.tick;
        private readonly TickTimer _timer = new();

        // simple 256-entry ring buffer of inputs
        private readonly LFInputStruct[] _inputRing = new LFInputStruct[256];

        // expose read-only access for debug
        public ReadOnlySpan<LFInputStruct> LastInputs => _inputRing;

        /*── Unity lifecycle ──────────────────────────*/
        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("CoreNetManager duplicate");
                enabled = false;
                return;
            }
            Instance = this;

            if (sampler == null)
                sampler = FindAnyObjectByType<InputSampler>();
            if (sampler == null)
                Debug.LogError("CoreNetManager: no InputSampler found.");
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /*── Main update ──────────────────────────────*/
        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // first call consumes this frame's delta-time
            if (!_timer.Step(dt))
                return;

            // drain any extra ticks without re-adding dt
            do
            {
                var inp = sampler.Current;
                _inputRing[_timer.tick & 0xFF] = inp;
                Tick?.Invoke(_timer.tick, inp);
            }
            while (_timer.Step(0f));   // pass 0 so we only drain
        }

        /*── Public events ────────────────────────────*/
        public delegate void TickHandler(uint tick, LFInputStruct input);
        public event TickHandler Tick;
    }
}
