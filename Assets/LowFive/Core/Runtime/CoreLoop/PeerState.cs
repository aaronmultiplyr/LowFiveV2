// Assets/LowFive/Core/Runtime/CoreLoop/PeerState.cs
using LowFive.Core.Input;

namespace LowFive.Core.CoreLoop
{
    internal sealed class PeerState
    {
        public byte id;                               // 0 = host self
        public readonly LFInputStruct[] ring =
            new LFInputStruct[256];                   // 256-tick circular buffer
    }
}
