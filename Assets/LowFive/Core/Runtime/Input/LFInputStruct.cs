
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LowFive.Core.Input
{
    /// <summary>
    /// 64-bit packed player input.  Layout (little-endian):
    /// bits  0-7   : 8 digital buttons
    /// bits  8-15  : sbyte Hat X   (-128…127)  – e.g. Horizontal axis
    /// bits 16-23  : sbyte Hat Y   (-128…127)  – e.g. Vertical axis
    /// bits 24-31  : sbyte Mouse ΔX (-128…127) – coarse mouse movement
    /// bits 32-39  : sbyte Mouse ΔY
    /// bits 40-63  : reserved (future triggers / analogs)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct LFInputStruct : IEquatable<LFInputStruct>
    {
        // raw view (fast copy / hashing)
        [FieldOffset(0)] public ulong packed;

        // ----- button helpers (0-7) -----
        public bool GetButton(int bit) => (packed & (1UL << bit)) != 0;
        public void SetButton(int bit, bool v) => packed = v ? packed | (1UL << bit)
                                                             : packed & ~(1UL << bit);

        // ----- signed 8-bit helpers -----
        static sbyte GetS8(ulong val, int sh) => unchecked((sbyte)((val >> sh) & 0xFF));
        static ulong PutS8(ulong val, int sh, sbyte v)
            => (val & ~(0xFFUL << sh)) | ((ulong)(byte)v << sh);

        public sbyte HatX { get => GetS8(packed, 8); set => packed = PutS8(packed, 8, value); }
        public sbyte HatY { get => GetS8(packed, 16); set => packed = PutS8(packed, 16, value); }
        public sbyte Mdx { get => GetS8(packed, 24); set => packed = PutS8(packed, 24, value); }
        public sbyte Mdy { get => GetS8(packed, 32); set => packed = PutS8(packed, 32, value); }

        // equality / hashing
        public bool Equals(LFInputStruct other) => packed == other.packed;
        public override bool Equals(object o) => o is LFInputStruct i && Equals(i);
        public override int GetHashCode() => packed.GetHashCode();
    }
}
