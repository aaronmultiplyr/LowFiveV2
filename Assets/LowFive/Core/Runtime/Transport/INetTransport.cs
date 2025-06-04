// Assets/LowFive/Core/Runtime/Transport/INetTransport.cs
using System;

namespace LowFive.Core.Transport
{
    public enum SendChannel : byte { Unreliable = 0, Reliable = 1 }

    /// <summary>Callback signature for inbound datagrams.</summary>
    /// <param name="data">The received bytes; valid until the handler returns.</param>
    public delegate void NetDataHandler(ReadOnlySpan<byte> data);

    public interface INetTransport : IDisposable
    {
        void StartServer(ushort port);
        void StartClient(string address, ushort port);

        void Send(ReadOnlySpan<byte> data, SendChannel channel);

        /// <summary>Poll socket; invoke <paramref name="onData"/> for each packet.</summary>
        void Poll(NetDataHandler onData);

        void Shutdown();
    }
}
