// Assets/LowFive/Core/Runtime/Transport/PingHarness.cs
using System;
using System.Buffers.Binary;
using UnityEngine;
using LowFive.Core.Transport;

public sealed class PingHarness : MonoBehaviour
{
    // ── config ──────────────────────────────────────────────
    [SerializeField] private ushort port = 7777;
    [SerializeField] private float interval = 1f; // seconds between pings

    private INetTransport server;
    private INetTransport client;

    // state
    private double nextSendTime;
    private uint seq;
    private readonly System.Diagnostics.Stopwatch sw = new();

    /*─────────────────────────────────────────────────────────*/
    void Awake()
    {
        sw.Start();

        server = new UdpTransport();
        client = new UdpTransport();

        server.StartServer(port);
        client.StartClient("127.0.0.1", port);

        Debug.Log("[PingHarness] server+client ready");
        nextSendTime = Time.unscaledTime + 0.5f;   // first ping half-second after start
    }

    /*─────────────────────────────────────────────────────────*/
    void Update()
    {
        // ── send ping periodically ──
        if (Time.unscaledTime >= nextSendTime)
        {
            Span<byte> pkt = stackalloc byte[13];     // 1(type)+4(seq)+8(ticks)
            pkt[0] = 0;                               // 0 = PING
            BinaryPrimitives.WriteUInt32LittleEndian(pkt[1..5], seq);
            BinaryPrimitives.WriteUInt64LittleEndian(pkt[5..13], (ulong)sw.ElapsedTicks);

            client.Send(pkt, SendChannel.Unreliable);

            nextSendTime += interval;
            seq++;
        }

        // ── pump sockets ──
        server.Poll(Server_OnData);
        client.Poll(Client_OnData);
    }

    /*─────────────────────────────────────────────────────────*/
    private void Server_OnData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 13 || data[0] != 0) return;   // expect PING

        Span<byte> echo = stackalloc byte[data.Length];
        data.CopyTo(echo);
        echo[0] = 1;                                    // 1 = PONG
        server.Send(echo, SendChannel.Unreliable);
    }

    private void Client_OnData(ReadOnlySpan<byte> data)
    {
        if (data.Length < 13 || data[0] != 1) return;   // expect PONG

        uint rSeq = BinaryPrimitives.ReadUInt32LittleEndian(data[1..5]);
        ulong sent = BinaryPrimitives.ReadUInt64LittleEndian(data[5..13]);
        double rtt = (sw.ElapsedTicks - (long)sent) * (1000.0 / System.Diagnostics.Stopwatch.Frequency);

        Debug.Log($"[Ping] RTT = {rtt:F2} ms   (seq {rSeq})");
    }

    /*─────────────────────────────────────────────────────────*/
    void OnDestroy()
    {
        client?.Shutdown();
        server?.Shutdown();
    }
}
