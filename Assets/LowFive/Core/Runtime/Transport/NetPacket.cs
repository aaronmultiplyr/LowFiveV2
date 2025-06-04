// Assets/LowFive/Core/Runtime/Transport/NetPacket.cs
using System;

namespace LowFive.Core.Transport
{
    /// <summary>
    /// Single-byte op-codes the CoreNetManager uses for low-level messages,
    /// plus a tiny helper for building compact packets.
    /// </summary>
    public static class NetPacket
    {
        // ───────── handshake (already working) ─────────
        public const byte HELLO = 0x01;   // client → host
        public const byte ACK_ID = 0x02;   // host   → client  (payload: 1 byte peerId)

        // ───────── Task 4-2 ─────────
        public const byte INPUT = 0x03;   // client → host   (peerId,u16 tick, 8-byte LFInputStruct)

        // ───────── Task 4-3 ─────────
        public const byte SNAP = 0x04;   // host   → clients (authoritative snapshot)

        /*───────────────────────────────────────────────────
         * Tiny() helper — returns a Span<byte> that points to
         * a freshly-allocated <backing> array:
         *
         *   • backing[0] is pre-filled with <op>.
         *   • Caller writes its payload starting at backing[1].
         *   • Keep allocations small (INPUT = 11 bytes, etc.).
         *──────────────────────────────────────────────────*/
        public static Span<byte> Tiny(byte op, int size, out byte[] backing)
        {
            backing = new byte[size];
            backing[0] = op;
            return backing;            // Span<byte>(backing) implicit
        }
    }
}
