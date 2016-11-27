using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChainsAPM.Interfaces;
using ChainsAPM.Commands.Agent;
using System.Runtime.Remoting.Messaging;

namespace ChainsAPM.Communication.Tcp
{
    public class TcpByteAgentHandler : IConnectionHandler, IDisposable
    {
        public enum HandlerType
        {
            SendHeavy,
            ReceiveHeavy,
            Balanced
        }
        public AgentInformation AgentInfo { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime DisconnectedTime { get; set; }

        private readonly Dictionary<int, ICommand<byte>> _commandList;
        private readonly object _cmdListLock;

        public Dictionary<long, string> StringList;
        public Dictionary<long, string> FunctionList;

        public Dictionary<long, long> ThreadDepth;

        // Thread Id, sequence, threadid
        public Dictionary<long, List<Tuple<long, long>>> ThreadEntryPoint;

        private readonly ITransport<byte[]> _packetHandler;
        
        private readonly object _lockingOutbound;
        private object _lockingInbound;
        private readonly Queue<byte[]> _blockingOutboundQueue;
        private readonly System.Threading.Timer _sendTimer; // Let's keep this guy around
        private readonly System.Threading.Timer _recvTimer; // Let's keep this guy around
        public delegate void HasDataEvent(object sender);
        public event HasDataEvent HasData;
        public delegate void DisconnectedEvent(object sender);
        public event DisconnectedEvent Disconnected;
        private readonly object _timerLock = new object();
        public int MessagesSent = 0;
        private const int MaxSendbuffer = 1024*70; // Keep this out of the LOH
        private readonly Queue<byte[]> _buffers;
        private readonly Queue<ArraySegment<byte>> _items;
        private readonly object _queuelock = new object();
        private readonly object _chunklock = new object();
        private bool disconnected = false;
        private readonly int _sendTimerInterval = 250;
        private readonly int _recvTimerInterval = 250;
        private bool _timersSuspended = false;
        private bool _inSend = false;
        private bool _inRecv = false;
        private readonly List<byte> _chunkList;

        public TcpByteAgentHandler(ITransport<byte[]> packethand, HandlerType handType = HandlerType.Balanced)
        {
            _packetHandler = packethand;
            _blockingOutboundQueue = new Queue<byte[]>();
            _lockingOutbound = new object();
            _lockingInbound = new object();
            _buffers = new Queue<byte[]>();
            _items = new Queue<ArraySegment<byte>>();
            _chunkList = new List<byte>(MaxSendbuffer);
            var sendTimerInterval = 250;
            var recvTimerInterval = 250;
            _commandList = CallContext.LogicalGetData("CommandProviders") as Dictionary<int, ICommand<byte>>;
            _cmdListLock = new object();

            StringList = new Dictionary<long, string>();
            FunctionList = new Dictionary<long, string>();

            ThreadDepth = new Dictionary<long, long>();

            // Thread Id, sequence, threadid
            ThreadEntryPoint = new Dictionary<long, List<Tuple<long, long>>>();

            switch (handType)
            {
                case HandlerType.SendHeavy:
                    sendTimerInterval /= 2;
                    recvTimerInterval *= 2;
                    break;
                case HandlerType.ReceiveHeavy:
                    recvTimerInterval /= 2;
                    sendTimerInterval *= 2;
                    break;
            }
            _sendTimer = new System.Threading.Timer(async (object o) =>
            {
                if (_inSend) return;
                _inSend = true;
                await SendData();
                _inSend = false;
            }, null, sendTimerInterval, sendTimerInterval);

            _recvTimer = new System.Threading.Timer(async (object o) =>
            {
                if (_inRecv) return;
                _inRecv = true;
                await RecvData();
                ExtractData();
                _inRecv = false;
            }, null, recvTimerInterval, recvTimerInterval);
        }

        public void PauseTimers()
        {
            lock (_timerLock)
            {
                if (_timersSuspended) return;

                try
                {
                    _sendTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                }
                catch (Exception)
                {

                }
                try
                {
                    _recvTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                }
                catch (Exception)
                {

                }
                _timersSuspended = true;
            }
        }

        public void RestartTimers()
        {
            lock (_timerLock)
            {
                if (!_timersSuspended) return;

                try
                {
                    _sendTimer.Change(_sendTimerInterval, _sendTimerInterval);
                }
                catch (Exception)
                {

                }
                try
                {
                    _recvTimer.Change(_recvTimerInterval, _recvTimerInterval);
                }
                catch (Exception)
                {

                }
                _timersSuspended = false;
            }
        }

        private async void SendRecvData(object none)
        {
            await SendData();
            await RecvData();
            ExtractData();
        }

        private async Task RecvData()
        {
            byte[] bytes = null;
            bytes = await _packetHandler.Receive();

            lock (_queuelock)
            {
                if (bytes != null)
                {
                    _buffers.Enqueue(bytes);
                }

            }
        }

        private async Task SendData()
        {
            List<byte> sendArray = null;
            lock (_lockingOutbound)
            {
                if (_blockingOutboundQueue.Count > 0)
                {
                    sendArray = new List<byte>(MaxSendbuffer);
                    try
                    {
                        while (_blockingOutboundQueue.Count > 0)
                        {
                            sendArray.AddRange(_blockingOutboundQueue.Dequeue());
                        }
                    }
                    catch (Exception)
                    {
                        sendArray = null;
                    }
                }
            }
            if (sendArray != null)
            {
                try
                {
                    await _packetHandler.Send(sendArray.ToArray());
                }
                catch (Exception)
                {
                    //TODO Add logging
                }
            }
        }

        public void AddCommand(ICommand<byte> command)
        {
            lock (_cmdListLock)
            {
                if (!_commandList.ContainsKey(command.Code))
                {
                    _commandList.Add(command.Code, command);
                }
            }
        }

        public void SendCommand(ICommand<byte> command)
        {
            lock (_lockingOutbound)
            {
                _blockingOutboundQueue.Enqueue(command.Encode());
                ++MessagesSent;
            }
        }

        public ICommand<byte> GetCommand()
        {
            var command = new ArraySegment<byte>();
            lock (_chunklock)
            {
                command = _items.Dequeue();
            }

            if (command != null)
            {
                var size = BitConverter.ToInt32(command.Array, command.Offset);
                var code = command.Array[command.Offset + 4];
                return _commandList[code].Decode(command);
            }

            return null;
        }

        public void SendCommands(ICommand<byte>[] command)
        {
            lock (_lockingOutbound)
            {
                foreach (var item in command)
                {
                    _blockingOutboundQueue.Enqueue(item.Encode());
                }
                ++MessagesSent;
            }
        }

        public ICommand<byte>[] GetCommands()
        {
            var outList = new List<ICommand<byte>>();
            lock (_chunklock)
            {
                var command = new ArraySegment<byte>();
                while (_items.Count > 0)
                {
                    command = _items.Dequeue();

                    if (command != null)
                    {
                        var size = BitConverter.ToInt32(command.Array, command.Offset);
                        var code = command.Array[command.Offset + 4];
                        outList.Add(_commandList[code].Decode(command));
                    }
                }
            }
            return outList.ToArray();
        }

        #region IConnectionHandler Members

        public bool Disconnect()
        {
            try
            {
                _packetHandler.Disconnect();
            }
            catch (Exception)
            {

            }
            return true;
        }

        public bool Recycle()
        {
            // TODO Implement method to tell agent to recycle connection
            return true;

        }

        public bool Flush()
        {
            SendRecvData(null);
            return true;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(Math.Max(_sendTimerInterval, _recvTimerInterval) * 3);
                    _sendTimer.Dispose();
                });

            if (!disconnected)
            {
                disconnected = true;
                Disconnected(this);
            }

        }

        #endregion

        public void Received(byte[][] ReceievedData)
        {
            lock (_queuelock)
            {
                foreach (var item in ReceievedData)
                {
                    _buffers.Enqueue(item);
                }

            }
        }

        private void ExtractData()
        {

            var position = 0;
            var finalSize = 0;
            byte[] queueToBreak = null;
            var segmentList = new List<ArraySegment<byte>>();
            lock (_queuelock)
            {
                if (_buffers.Count > 0)
                {

                    while (_buffers.Count > 0)
                    {
                        var localqueueChunk = _buffers.Dequeue();
                        _chunkList.AddRange(localqueueChunk);
                        position += localqueueChunk.Length;
                    }

                    var queueChunk = _chunkList.ToArray();
                    _chunkList.Clear();
                    queueToBreak = new byte[queueChunk.Length];
                    segmentList = new List<ArraySegment<byte>>();
                    for (var i = 0; i < queueChunk.Length; )
                    {
                        var size = 0;
                        if (queueChunk.Length - 4 >= i + 4)
                        {
                            size = BitConverter.ToInt32(queueChunk, i);
                        }
                        if (size > 0 && queueChunk.Length >= i + size && BitConverter.ToUInt32(queueChunk, (i + size) - 4) == 0xCCCCCCCC)
                        {

                            segmentList.Add(new ArraySegment<byte>(queueChunk, i + 4, size - 8));
                            finalSize += (size - 8);
                            i += size;
                        }
                        else
                        {
                            var remainder = new byte[queueChunk.Length - i];
                            Array.Copy(queueChunk, i, remainder, 0, queueChunk.Length - i);
                            _buffers.Enqueue(remainder);
                            break;
                        }
                    }
                }
            }

            lock (_chunklock)
            {
                if (finalSize <= 0) return;

                var listOfSizes = new List<int>();
                Array.Resize(ref queueToBreak, finalSize);
                foreach (var item in segmentList)
                {
                    for (var i = 0; i < item.Count; )
                    {
                        var startOffset = i + item.Offset;
                        var size = BitConverter.ToInt32(item.Array, startOffset);
                        if (size > 0 && size < 4096)
                        {
                            listOfSizes.Add(size);
                            var segment = new ArraySegment<byte>(item.Array, startOffset, size);
                            _items.Enqueue(segment);
                            i += size;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                HasData(this);
            }
        }
        public void Sent()
        {
            throw new NotImplementedException();
        }
    }
}
