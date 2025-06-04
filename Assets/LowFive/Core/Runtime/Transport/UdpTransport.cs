// Assets/LowFive/Core/Runtime/Transport/UdpTransport.cs
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace LowFive.Core.Transport
{
    /// <summary>
    /// Lightweight non-blocking UDP transport that fulfils INetTransport.
    /// • One socket per process
    /// • SendChannel is ignored for now (always unreliable)
    /// • On the server, we auto-connect to the first remote endpoint so Send() works
    /// </summary>
    public sealed class UdpTransport : INetTransport
    {
        private Socket _sock;                 // underlying UDP socket
        private EndPoint _remote;               // cached default destination
        private readonly byte[] _rxBuf = new byte[1500];   // MTU-sized temp buffer

        /*──────────────────────────────────────────────*/
        public void StartServer(ushort port)
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false
            };
            _sock.Bind(new IPEndPoint(IPAddress.Any, port));
            _remote = null;
            Debug.Log($"[UdpTransport] Server listening on *:{port}");
        }

        public void StartClient(string address, ushort port)
        {
            _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false
            };
            _remote = new IPEndPoint(IPAddress.Parse(address), port);
            _sock.Connect(_remote);                           // set default destination
            Debug.Log($"[UdpTransport] Client → {address}:{port}");
        }

        /*──────────────────────────────────────────────*/
        public void Send(ReadOnlySpan<byte> data, SendChannel _)
        {
            if (_sock == null) throw new InvalidOperationException("Socket not started.");

            try
            {
                _sock.Send(data);                             // non-blocking
            }
            catch (SocketException se) when (se.SocketErrorCode == SocketError.WouldBlock)
            {
                // outbound buffer full; drop silently for now
            }
        }

        /*──────────────────────────────────────────────*/
        public void Poll(NetDataHandler onData)
        {
            if (_sock == null) return;

            while (_sock.Available > 0)
            {
                try
                {
                    EndPoint anyEP = new IPEndPoint(IPAddress.Any, 0);
                    int len = _sock.ReceiveFrom(_rxBuf, 0, _rxBuf.Length, SocketFlags.None, ref anyEP);

                    // Server: cache first sender so Send() knows where to go
                    if (_remote == null)
                    {
                        _remote = anyEP;
                        try { _sock.Connect(_remote); }
                        catch (SocketException) { /* already connected; ignore */ }
                    }

                    onData(new ReadOnlySpan<byte>(_rxBuf, 0, len));
                }
                catch (SocketException se) when (se.SocketErrorCode == SocketError.WouldBlock)
                {
                    break; // no more packets this frame
                }
            }
        }

        /*──────────────────────────────────────────────*/
        public void Shutdown()
        {
            _sock?.Close();
            _sock = null;
            _remote = null;
            Debug.Log("[UdpTransport] Shutdown.");
        }

        public void Dispose() => Shutdown();
    }
}
