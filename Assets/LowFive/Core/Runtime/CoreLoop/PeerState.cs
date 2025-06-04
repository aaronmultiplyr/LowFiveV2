// Assets/LowFive/Core/Runtime/CoreLoop/PeerState.cs
using LowFive.Core.Input;

namespace LowFive.Core.CoreLoop
{
    /// <summary>
    /// Per-connection data slot.
    /// </summary>
    public sealed class PeerState
    {
        public byte id;            // 0 = host, 1…N = clients
        public uint lastRecvTick;  // newest tick we've accepted from this peer
        public readonly LFInputStruct[] ring = new LFInputStruct[256];

        // convenience constructor
        public PeerState(byte id = 0)
        {
            this.id = id;
        }
    }
}
