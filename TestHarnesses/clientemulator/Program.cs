using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChainsAPM.Interfaces;
using ChainsAPM.Communication.Tcp;
using System.Runtime.Remoting.Messaging;


namespace ChainsAPM
{
    class Program
    {
        static readonly System.Threading.ManualResetEventSlim WaitHandle = new System.Threading.ManualResetEventSlim();
        static readonly System.Collections.Concurrent.ConcurrentDictionary<int, int> ItemCounter = new System.Collections.Concurrent.ConcurrentDictionary<int, int>(8, 128);
        private static int _counter2;
        private static int _counter3;
        private static int _totalMessagesSent;
        private static readonly object MsgLock = new object();
        private const int Max = 1;

        static void Main(string[] args)
        {
            var commandList = new Dictionary<int, ICommand<byte>>();
            var sendStringCommand = new Commands.Common.SendString("");
            commandList.Add(sendStringCommand.Code, sendStringCommand);
            CallContext.LogicalSetData("CommandProviders", commandList);

            for (var i = 0; i < Max; i++)
            {
                System.Threading.ThreadPool.QueueUserWorkItem((object o) =>
                {
                    var rand = new Random();
                    var hostname = System.Configuration.ConfigurationManager.AppSettings["hostname"];
                    var port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["port"]);
                    var tcpClient = new System.Net.Sockets.TcpClient(hostname, port);
                    var tcpByteAgentHandler = new TcpByteAgentHandler(new TcpByteTransport(tcpClient), TcpByteAgentHandler.HandlerType.SendHeavy);
                    tcpByteAgentHandler.AddCommand(new Commands.Common.SendString(""));
                    ItemCounter.GetOrAdd(tcpByteAgentHandler.GetHashCode(), System.Threading.Interlocked.Increment(ref _counter2));
                    System.Threading.Interlocked.Increment(ref _counter3);
                    tcpByteAgentHandler.HasData += tcbah_HasData;
                    tcpByteAgentHandler.Disconnected += tcbah_Disconnected;
                    
                    tcpClient.NoDelay = true;
                    var counter = 0;
                    var msgCounter = 0;
                    var stopCount = 10000;
                    do
                    {
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(5, 1000)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(5, 1000)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(5, 1000)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(5, 1000)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(5, 1000)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(10, 100)));
                        tcpByteAgentHandler.SendCommand(new Commands.Common.SendString(RandString(5, 1000)));
                        msgCounter += 72;
                        System.Threading.Thread.Sleep(rand.Next(5, 25));
                    } while (++counter != stopCount);

                    Console.WriteLine("Messages Sent: {0}", msgCounter);
                    tcpByteAgentHandler.SendCommand(new Commands.Common.SendString("Done!"));
                    Console.WriteLine("Done Sent!");
                    lock (MsgLock)
                    {
                        _totalMessagesSent += msgCounter + 1;
                    }
                });
            }
            WaitHandle.Wait();
            Console.WriteLine("Messages Sent: {0}", _totalMessagesSent);
        }

        private static string RandString(int min, int max)
        {
            var totalCount = new Random().Next(min, max);
            var charrand = new Random();
            var sb = new StringBuilder(max);
            for (var i = 0; i < totalCount; i++)
            {
                sb.Append((char)charrand.Next(0x41, 0x5A));
            }
            return sb.ToString();
        }

        private static void tcbah_Disconnected(object sender)
        {
            var tcbah = sender as TcpByteAgentHandler;
            Console.WriteLine("Client {0:000} finished.", ItemCounter[tcbah.GetHashCode()]);
            Console.WriteLine("Clients {0:000}.", System.Threading.Interlocked.Decrement(ref _counter3));
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(25000);
                tcbah.Disconnect();
            });
        }

        private static void tcbah_HasData(object sender)
        {
            var tcbah = sender as TcpByteAgentHandler;
            foreach (var item in tcbah.GetCommands())
            {
                if (!(item is Commands.Common.SendString)) continue;
                tcbah.Dispose();
                if (_counter3 == 0)
                {
                    WaitHandle.Set();
                }
            }
        }
    }
}
