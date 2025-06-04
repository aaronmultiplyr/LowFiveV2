/// Assets/LowFive/Core/Runtime/CoreLoop/CoreNetManager.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using UnityEngine;
using LowFive.Core.Input;
using LowFive.Core.Transport;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// 60 Hz deterministic lock-step manager.
    /// • Elects Host / Client and performs HELLO / ACK handshake  
    /// • Client sends local INPUT every tick  
    /// • Host executes every peer’s INPUT, moves cubes, then broadcasts SNAP  
    /// • Clients apply SNAP instantly (no interpolation yet)
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class CoreNetManager : MonoBehaviour
    {
        /* ───────── public API ───────── */
        public static CoreNetManager Instance { get; private set; }

        public enum Mode { Host, Client }
        public Mode mode { get; private set; } = Mode.Host;
        public uint CurrentTick => _timer.tick;

        /* ───────── inspector ───────── */
        [SerializeField] private InputSampler sampler;
        [SerializeField] private CubeRegistry cubeRegistry;
        [SerializeField] private ushort port = 7777;

        /* ───────── internals ───────── */
        private readonly TickTimer _timer = new();
        private INetTransport _transport;

        private readonly Dictionary<byte, PeerState> _peers = new();
        private readonly Dictionary<byte, Transform> _cubes = new();

        private byte _nextPeerId = 1;   // host allocates 1‥255
        private byte _myId = 0;   // client learns from ACK_ID

        /* ───────── Unity lifecycle ───────── */
        void Awake()
        {
            if (Instance != null) { enabled = false; return; }
            Instance = this;

            sampler ??= FindAnyObjectByType<InputSampler>();
            cubeRegistry ??= FindAnyObjectByType<CubeRegistry>();

            if (!cubeRegistry)
            {
                Debug.LogError("[CoreNetMgr] CubeRegistry missing in scene!");
                enabled = false; return;
            }

            // slot 0 = this peer
            _peers[0] = new PeerState { id = 0 };
            _cubes[0] = cubeRegistry.GetOrCreate(0);

            try
            {
                _transport = new UdpTransport();
                _transport.StartServer(port);
                mode = Mode.Host;
                Debug.Log($"[CoreNetMgr] Host mode on :{port}");
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

        /* ───────── main loop ───────── */
        void Update()
        {
            _transport?.Poll(OnData);

            if (!_timer.Step(Time.unscaledDeltaTime)) return;

            do
            {
                /* 1 ─ local input */
                LFInputStruct local = sampler.Current;
                _peers[0].ring[_timer.tick & 0xFF] = local;

                if (mode == Mode.Client && _myId != 0)
                    SendInput(local, _timer.tick);

                /* 2 ─ host executes remote inputs and snaps */
                if (mode == Mode.Host)
                {
                    foreach (var kv in _peers)
                    {
                        if (kv.Key == 0) continue; // skip self

                        LFInputStruct inp = kv.Value.ring[_timer.tick & 0xFF];
                        if (!_cubes.TryGetValue(kv.Key, out var tr))
                            tr = _cubes[kv.Key] = cubeRegistry.GetOrCreate(kv.Key);

                        tr.position = CubeMotor.Simulate(tr.position, inp);
                    }

                    BroadcastSnap(_timer.tick);
                }

                Tick?.Invoke(_timer.tick, local);
            }
            while (_timer.Step(0f));
        }

        /* ───────── network in ───────── */
        void OnData(ReadOnlySpan<byte> d)
        {
            if (d.IsEmpty) return;

            switch (d[0])
            {
                case NetPacket.HELLO:
                    if (mode == Mode.Host) HandleHello();
                    break;

                case NetPacket.ACK_ID:
                    if (mode == Mode.Client && d.Length >= 2)
                        _myId = d[1];
                    break;

                case NetPacket.INPUT:
                    if (mode == Mode.Host && d.Length == 12)
                        ReceiveInput(d);
                    break;

                case NetPacket.SNAP:
                    if (mode == Mode.Client)
                        ApplySnap(d);
                    break;
            }
        }

        /* ───────── client helpers ───────── */
        void SendInput(LFInputStruct inp, uint tick)
        {
            Span<byte> pkt = NetPacket.Tiny(NetPacket.INPUT, 12, out _);
            pkt[1] = _myId;
            BinaryPrimitives.WriteUInt16LittleEndian(pkt.Slice(2, 2), (ushort)tick);
            BinaryPrimitives.WriteUInt64LittleEndian(pkt.Slice(4, 8), inp.packed);
            _transport.Send(pkt, SendChannel.Unreliable);
        }

        /* ───────── host helpers ───────── */
        void ReceiveInput(ReadOnlySpan<byte> d)
        {
            byte peer = d[1];
            ushort tick = BinaryPrimitives.ReadUInt16LittleEndian(d.Slice(2, 2));
            ulong raw = BinaryPrimitives.ReadUInt64LittleEndian(d.Slice(4, 8));

            if (!_peers.TryGetValue(peer, out var ps)) return;
            ps.ring[tick & 0xFF].packed = raw;
            ps.lastRecvTick = tick;
        }

        void BroadcastSnap(uint tick)
        {
            if (_peers.Count <= 1) return; // no clients connected
            Span<byte> pkt = WorldSnapshot.Build(tick, _cubes, out _);
            _transport.Send(pkt, SendChannel.Unreliable);
        }

        /* ───────── client SNAP apply ───────── */
        void ApplySnap(ReadOnlySpan<byte> d)
        {
            if (d.Length < 6) return;

            int count = d[5];
            int offset = 6;

            for (int i = 0; i < count && offset + 13 <= d.Length; i++)
            {
                byte id = d[offset];
                float x = ReadFloatLE(d.Slice(offset + 1, 4));
                float y = ReadFloatLE(d.Slice(offset + 5, 4));
                float z = ReadFloatLE(d.Slice(offset + 9, 4));
                offset += 13;

                if (!_cubes.TryGetValue(id, out var tr))
                    tr = _cubes[id] = cubeRegistry.GetOrCreate(id);

                tr.position = new Vector3(x, y, z);
            }
        }

        /* ───────── handshake ───────── */
        void HandleHello()
        {
            byte id = _nextPeerId++;
            _peers[id] = new PeerState { id = id };
            _cubes[id] = cubeRegistry.GetOrCreate(id);

            Span<byte> pkt = NetPacket.Tiny(NetPacket.ACK_ID, 2, out _);
            pkt[1] = id;
            _transport.Send(pkt, SendChannel.Unreliable);

            Debug.Log($"[CoreNetMgr] Client joined → id {id}");
        }

        /* ───────── utils ───────── */
        static float ReadFloatLE(ReadOnlySpan<byte> src) =>
            BitConverter.Int32BitsToSingle(
                BinaryPrimitives.ReadInt32LittleEndian(src));

        /* ───────── tick event ───────── */
        public delegate void TickHandler(uint tick, LFInputStruct input);
        public event TickHandler Tick;
    }
}
