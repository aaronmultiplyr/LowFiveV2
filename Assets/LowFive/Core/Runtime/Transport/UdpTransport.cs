
using System;
using System.Net;
using System.Net.Sockets;

namespace LowFive.Core.Transport
{
    /// <summary>
    /// Lightweight, non-blocking UDP transport.
    /// * One socket per process.
    /// * No reliability yet (SendChannel is ignored for now).
    /// </summary>
    public sealed class UdpTransport : INetTransport
    {
        private Socket _sock;
        private EndPoint _remote;     // cached for Send()
        private readonly byte[] _rxBuf = new byte[1500];   // MTU-sized

        /*──────────────────────────────────────────────*/
        public void StartServer(ushort port)
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sock.Blocking = false;
            _sock.Bind(new IPEndPoint(IPAddress.Any, port));
            _remote = null;                      // will be filled on first packet
            UnityEngine.Debug.Log($"[UdpTransport] Server listening on *:{port}");
        }

        public void StartClient(string address, ushort port)
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _sock.Blocking = false;
            _remote = new IPEndPoint(IPAddress.Parse(address), port);
            _sock.Connect(_remote);              // optional; enables Send() without EndPoint
            UnityEngine.Debug.Log($"[UdpTransport] Client → {address}:{port}");
        }

        /*──────────────────────────────────────────────*/
        public void Send(ReadOnlySpan<byte> data, SendChannel _)
        {
            if (_sock == null) throw new InvalidOperationException("Socket not started.");
            try
            {
                _sock.Send(data);   // non-blocking, ignore WouldBlock for now
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.WouldBlock) { }
        }

        /*──────────────────────────────────────────────*/
        public void Poll(NetDataHandler onData)
        {
            if (_sock == null) return;

            // Loop while packets are waiting
            while (_sock.Available > 0)
            {
                try
                {
                    EndPoint anyEP = new IPEndPoint(IPAddress.Any, 0);
                    int len = _sock.ReceiveFrom(_rxBuf, 0, _rxBuf.Length, SocketFlags.None, ref anyEP);

                    // Cache remote on first inbound datagram (server side)
                    if (_remote == null) _remote = anyEP;

                    onData(new ReadOnlySpan<byte>(_rxBuf, 0, len));
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.WouldBlock) { break; }
            }
        }

        /*──────────────────────────────────────────────*/
        public void Shutdown()
        {
            _sock?.Close();
            _sock = null;
            _remote = null;
            UnityEngine.Debug.Log("[UdpTransport] Shutdown.");
        }

        public void Dispose() => Shutdown();
    }
}
