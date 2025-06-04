// Assets/LowFive/Core/Runtime/CoreLoop/CoreNetManager.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using UnityEngine;
using LowFive.Core.Input;
using LowFive.Core.Transport;
using UnityEngine.SocialPlatforms;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// Deterministic 60 Hz lock-step manager.
    /// • Elects Host/Client at runtime (bind-or-connect port 7777)  
    /// • Handles HELLO / ACK_ID handshake and keeps a per-peer registry  
    /// • Local tick loop still runs even before remote inputs are wired
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class CoreNetManager : MonoBehaviour
    {
        /* ────────── public API ────────── */
        public static CoreNetManager Instance { get; private set; }

        public enum Mode { Host, Client }
        public Mode mode { get; private set; } = Mode.Host;

        public uint CurrentTick => _timer.tick;

        /* ────────── inspector ─────────── */
        [SerializeField] private InputSampler sampler;
        [SerializeField] private ushort port = 7777;

        /* ────────── internals ─────────── */
        private readonly TickTimer _timer = new();
        private INetTransport _transport;

        // peerId (byte) → state   ; host puts itself in id 0
        private readonly Dictionary<byte, PeerState> _peers = new();
        private byte _nextPeerId = 1;        // host assigns 1..255

        /* ────────── Unity lifecycle ───── */
        void Awake()
        {
            if (Instance != null) { enabled = false; return; }
            Instance = this;

            if (sampler == null)
                sampler = FindAnyObjectByType<InputSampler>();

            // Host-or-Client election
            try
            {
                _transport = new UdpTransport();
                _transport.StartServer(port);
                mode = Mode.Host;
                Debug.Log($"[CoreNetMgr] Host mode on :{port}");
                _peers[0] = new PeerState { id = 0 };            // self
            }
            catch
            {
                _transport = new UdpTransport();
                _transport.StartClient("127.0.0.1", port);
                mode = Mode.Client;
                Debug.Log($"[CoreNetMgr] Client mode → 127.0.0.1:{port}");
            }
        }

        void Start()
        {
            // Client sends initial HELLO once transport is up
            if (mode == Mode.Client)
            {
                Span<byte> pkt = NetPacket.Tiny(NetPacket.HELLO, 1, out _);
                _transport.Send(pkt, SendChannel.Unreliable);
                Debug.Log("[CoreNetMgr] Sent HELLO");
            }
        }

        void OnDestroy()
        {
            _transport?.Shutdown();
            if (Instance == this) Instance = null;
        }

        /* ────────── main Update ───────── */
        void Update()
        {
            // 1) pump network
            _transport?.Poll(OnData);

            // 2) deterministic tick
            float dt = Time.unscaledDeltaTime;
            if (!_timer.Step(dt)) return;

            do
            {
                var inp = sampler.Current;

                // store local input in ring for id 0
                if (_peers.TryGetValue(0, out var self))
                    self.ring[_timer.tick & 0xFF] = inp;

                Tick?.Invoke(_timer.tick, inp);
            }
            while (_timer.Step(0f));   // drain extra ticks
        }

        /* ────────── packet handler ────── */
        private void OnData(ReadOnlySpan<byte> data)
        {
            if (data.Length == 0) return;

            switch (data[0])
            {
                case NetPacket.HELLO:
                    if (mode == Mode.Host) HandleHello();
                    break;

                case NetPacket.ACK_ID:
                    if (mode == Mode.Client && data.Length >= 2)
                    {
                        byte id = data[1];
                        _peers[id] = new PeerState { id = id };
                        Debug.Log($"[CoreNetMgr] Got peer id = {id}");
                    }
                    break;

                    /* INPUT / SNAP handled in upcoming tasks */
            }
        }

        private void HandleHello()
        {
            byte peerId = _nextPeerId++;
            _peers[peerId] = new PeerState { id = peerId };
            Debug.Log($"[CoreNetMgr] Client joined → id {peerId}");

            // send ACK_ID back
            Span<byte> pkt = NetPacket.Tiny(NetPacket.ACK_ID, 2, out _);
            pkt[1] = peerId;
            _transport.Send(pkt, SendChannel.Unreliable);
        }

        /* ────────── tick event ─────────── */
        public delegate void TickHandler(uint tick, LFInputStruct input);
        public event TickHandler Tick;
    }
}
