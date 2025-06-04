// Assets/LowFive/Core/Runtime/CoreLoop/CoreNetManager.cs
using System;
using UnityEngine;
using LowFive.Core.Input;
using LowFive.Core.Transport;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// Deterministic 60 Hz tick loop.
    /// Day 4-1: now selects Host or Client at runtime and owns a transport.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class CoreNetManager : MonoBehaviour
    {
        /*──────────── public API ─────────────────────*/
        public static CoreNetManager Instance { get; private set; }

        public enum Mode { Host, Client }
        public Mode mode { get; private set; } = Mode.Host;

        public uint CurrentTick => _timer.tick;

        /*──────────── inspector refs ─────────────────*/
        [SerializeField] private InputSampler sampler;
        [SerializeField] private ushort port = 7777;

        /*──────────── internals ──────────────────────*/
        private readonly TickTimer _timer = new();
        private INetTransport _transport;

        private readonly LFInputStruct[] _inputRing = new LFInputStruct[256];

        /*──────────── lifecycle ──────────────────────*/
        void Awake()
        {
            // singleton guard
            if (Instance != null) { enabled = false; return; }
            Instance = this;

            // find sampler if not linked
            if (sampler == null)
                sampler = FindAnyObjectByType<InputSampler>();

            // host-or-client election
            try
            {
                _transport = new UdpTransport();
                _transport.StartServer(port);
                mode = Mode.Host;
                Debug.Log($"[CoreNetMgr] Host mode on :{port}");
            }
            catch (Exception)
            {
                _transport = new UdpTransport();
                _transport.StartClient("127.0.0.1", port);
                mode = Mode.Client;
                Debug.Log($"[CoreNetMgr] Client mode → 127.0.0.1:{port}");
            }
        }

        void OnDestroy()
        {
            _transport?.Shutdown();
            if (Instance == this) Instance = null;
        }

        /*──────────── main Update ────────────────────*/
        void Update()
        {
            // 1) service network (no handlers yet – stub)
            _transport?.Poll(OnData);

            // 2) fixed-tick simulation
            float dt = Time.unscaledDeltaTime;

            if (!_timer.Step(dt))
                return;

            do
            {
                var inp = sampler.Current;
                _inputRing[_timer.tick & 0xFF] = inp;

                Tick?.Invoke(_timer.tick, inp);
            }
            while (_timer.Step(0f));   // drain extra ticks without re-adding dt
        }

        /*──────────── networking stub ───────────────*/
        private void OnData(ReadOnlySpan<byte> data)
        {
            // Task 4-2 will decode packets here
        }

        /*──────────── public event ───────────────────*/
        public delegate void TickHandler(uint tick, LFInputStruct input);
        public event TickHandler Tick;
    }
}
