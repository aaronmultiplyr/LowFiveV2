
using System;
using System.Runtime.InteropServices;

namespace LowFive.Core.Input
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InputFrame
    {
        public uint tick;   // simulation tick
        public LFInputStruct data;   // 8-byte payload

        public const int Size = sizeof(uint) + sizeof(ulong); // 12

        public void ToBytes(Span<byte> dst)
        {
            if (dst.Length < Size) throw new ArgumentException("span too small");
            BitConverter.TryWriteBytes(dst, tick);
            BitConverter.TryWriteBytes(dst[4..], data.packed);
        }

        public static InputFrame FromBytes(ReadOnlySpan<byte> src)
        {
            if (src.Length < Size) throw new ArgumentException("span too small");
            return new InputFrame
            {
                tick = BitConverter.ToUInt32(src),
                data = new LFInputStruct { packed = BitConverter.ToUInt64(src[4..]) }
            };
        }
    }
}
