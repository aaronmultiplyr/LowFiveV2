// Assets/LowFive/Core/Runtime/CoreLoop/WorldSnapshot.cs
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using UnityEngine;
using LowFive.Core.Transport;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// Static helper to serialise a minimal “all-cubes” snapshot packet.
    /// </summary>
    internal static class WorldSnapshot
    {
        private const int HEADER = 1 + 4 + 1;   // op (1) + tick (4) + count (1)
        private const int PER_PLAYER = 1 + 12;      // id (1) + Vector3 (3×4)

        /// <summary>
        /// Builds a SNAP packet that contains the position of every peer-controlled cube.
        /// </summary>
        /// <param name="tick">Authoritative simulation tick.</param>
        /// <param name="sources">Map peerId → cube <see cref="Transform"/>.</param>
        /// <param name="backing">Allocated byte[] that backs the returned Span.</param>
        public static Span<byte> Build(uint tick,
                                       IReadOnlyDictionary<byte, Transform> sources,
                                       out byte[] backing)
        {
            int count = sources.Count;                    // how many cubes encoded
            int totalSize = HEADER + count * PER_PLAYER;

            Span<byte> pkt = NetPacket.Tiny(NetPacket.SNAP, totalSize, out backing);

            /*── header ───────────────────────────────*/
            BinaryPrimitives.WriteUInt32LittleEndian(pkt.Slice(1, 4), tick);
            pkt[5] = (byte)count;

            /*── per-cube payload ─────────────────────*/
            int offset = 6;
            foreach (var kv in sources)
            {
                byte id = kv.Key;
                Vector3 pos = kv.Value.position;

                pkt[offset + 0] = id;

                // Write floats as 32-bit ints (Unity’s .NET profile lacks WriteSingleLittleEndian)
                BinaryPrimitives.WriteInt32LittleEndian(pkt.Slice(offset + 1, 4),
                    BitConverter.SingleToInt32Bits(pos.x));
                BinaryPrimitives.WriteInt32LittleEndian(pkt.Slice(offset + 5, 4),
                    BitConverter.SingleToInt32Bits(pos.y));
                BinaryPrimitives.WriteInt32LittleEndian(pkt.Slice(offset + 9, 4),
                    BitConverter.SingleToInt32Bits(pos.z));

                offset += PER_PLAYER;
            }

            return pkt;
        }
    }
}
