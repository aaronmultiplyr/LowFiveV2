// Assets/LowFive/Core/Runtime/Transport/NetPacket.cs
using System;

namespace LowFive.Core.Transport
{
    /// <summary>Const packet opcodes and tiny helpers.</summary>
    public static class NetPacket
    {
        public const byte HELLO = 0x01;   // client → host
        public const byte ACK_ID = 0x02;   // host   → client  (payload: 1 byte peerId)
        public const byte INPUT = 0x03;   // client → host  (payload: tick, LFInputStruct)
        public const byte SNAP = 0x04;   // host   → all   (future)

        // convenience for tiny packets -------------------------------------------------
        public static Span<byte> Tiny(byte op, int size, out byte[] backing)
        {
            backing = new byte[size];
            backing[0] = op;
            return backing;
        }
    }
}
