using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using ChainsAPM.Interfaces;
using System.Runtime.CompilerServices;


namespace ChainsAPM.Communication.Tcp
{

    public sealed class SocketAwaitable : INotifyCompletion
    {
        private static readonly Action Sentinel = () => { };

        internal bool WasCompleted;
        internal Action Continuation;
        internal SocketAsyncEventArgs EventArgs;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException("eventArgs");
            EventArgs = eventArgs;
            eventArgs.Completed += delegate
            {
                (Continuation ?? Interlocked.CompareExchange(ref Continuation, Sentinel, null))?.Invoke();
            };
        }

        internal void Reset()
        {
            WasCompleted = false;
            Continuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return WasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if (Continuation == Sentinel ||
                Interlocked.CompareExchange(
                    ref Continuation, continuation, null) == Sentinel)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }
    }

    public static class SocketExtensions
    {
        public static SocketAwaitable ReceiveAsync(this Socket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.ReceiveAsync(awaitable.EventArgs))
                awaitable.WasCompleted = true;
            return awaitable;
        }

        public static SocketAwaitable SendAsync(this Socket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();
            if (!socket.SendAsync(awaitable.EventArgs))
                awaitable.WasCompleted = true;
            return awaitable;
        }

        // ... 
    }
    public class TcpByteTransport : ITransport<byte[]>, IDisposable
    {
        private readonly NetworkStream _socket;
        private readonly TcpClient _client;
        private readonly byte[] _internalBuffer;
        private Queue<byte[]> _inboundQueue;
        private readonly object _queueLock;
        private object _inRecv;
        private bool _disposed;
        public Socket Socket { get { return _client.Client; } }

        public TcpByteTransport(TcpClient socket)
        {
            socket.ReceiveTimeout = 5000;
            socket.SendTimeout = 5000;
            socket.ReceiveBufferSize = 1 * 1024 * 1024;
            socket.SendBufferSize = 1 * 1024 * 1024;
            _client = socket;
            _socket = socket.GetStream();
            socket.Client.UseOnlyOverlappedIO = true;
            _internalBuffer = new byte[10 * 1024 * 1024];
            _inboundQueue = new Queue<byte[]>();
            _queueLock = new object();
            socket.Client.Blocking = false;
            _inRecv = new object();
        }

        public async Task<bool> Send(byte[] data)
        {
            if (_disposed || !_client.Connected) return false;

            var dataToSend = new byte[data.Length + 8];
            Array.Copy(BitConverter.GetBytes(dataToSend.Length), dataToSend, 4); // PackageLength
            Array.Copy(data, 0, dataToSend, 4, data.Length); // Messages
            Array.Copy(new byte[] { 0xCC, 0xCC, 0xCC, 0xCC }, 0, dataToSend, data.Length + 4, 4); // PostAmble

            try
            {
                await _socket.WriteAsync(dataToSend, 0, dataToSend.Length);
            }
            catch (Exception)
            {
                _disposed = true;
            }

            return true;
        }

        public async Task<byte[]> Receive()
        {
            if (_disposed) return null;
            // Reusable SocketAsyncEventArgs and awaitable wrapper

            try
            {
                var bytes = await _socket.ReadAsync(_internalBuffer, 0, _internalBuffer.Length);
                if (bytes > 0)
                {
                    var queueBuffer = new byte[bytes];
                    Array.Copy(_internalBuffer, queueBuffer, bytes);
                    return queueBuffer;
                }
            }
            catch (Exception)
            {
                _disposed = true;
            }
            return null;
        }

        public bool Disconnect()
        {
            try
            {
                Dispose();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _client.Close();
            }

            _disposed = true;
        }

        #region ITransport<byte[]> Members
        public bool Connected
        {
            get
            {
                try
                {
                    return _client.Client.Connected;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion

        #region ITransport<byte[]> Members
        public bool HasData
        {
            get
            {
                lock (_queueLock)
                {
                    return _inboundQueue.Count > 0;
                }
            }

        }
        #endregion
    }
}
