using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using ChainsAPM.Commands.Agent;
using ChainsAPM.Commands.Common;
using ChainsAPM.Communication.Tcp;
using ChainsAPM.Interfaces;

namespace ChainsAPM.ConsoleServer
{
    internal class Program
    {
        private static readonly object LockConsole = new object();
        private static readonly WaitCallback WCb = TimerCallback;
        private static readonly ConcurrentDictionary<int, TcpByteAgentHandler> ConcurrentAgentHandlerList = new ConcurrentDictionary<int, TcpByteAgentHandler>();
        private static long _clientsConnected;
        private static long _messagesRecvd;

        private static void Main(string[] args)
        {
            var commandList = new Dictionary<int, ICommand<byte>>();
            var sendStringCommand = new SendString("");
            var functionEnterQuickCommand = new FunctionEnterQuick(0, 0, 0);
            var functionLeaveQuickCommand = new FunctionLeaveQuick(0, 0, 0);
            var agentInformationCommand = new AgentInformation();
            var functionTailQuickCommand = new FunctionTailQuick(0, 0, 0);
            var defineFunctionCommand = new DefineFunction(0, 0, "", 0);

            commandList.Add(sendStringCommand.Code, sendStringCommand);
            commandList.Add(sendStringCommand.Code + 1, sendStringCommand); // SendString handles Unicode and ASCII
            commandList.Add(functionEnterQuickCommand.Code, functionEnterQuickCommand);
            commandList.Add(functionLeaveQuickCommand.Code, functionLeaveQuickCommand);
            commandList.Add(agentInformationCommand.Code, agentInformationCommand);
            commandList.Add(functionTailQuickCommand.Code, functionTailQuickCommand);
            commandList.Add(defineFunctionCommand.Code, defineFunctionCommand);
            CallContext.LogicalSetData("CommandProviders", commandList);
            GC.RegisterForFullGCNotification(30, 50);
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
            var timerCheck = new Thread(() =>
            {
                Console.WriteLine("GC Notification thread started.");
                while (true)
                {
                    var s = GC.WaitForFullGCApproach();
                    if (s == GCNotificationStatus.Succeeded)
                    {
                        Console.WriteLine("GC Notification raised.");
                        foreach (var item in ConcurrentAgentHandlerList)
                        {
                            item.Value.PauseTimers();
                        }
                    }
                    else if (s == GCNotificationStatus.Canceled)
                    {
                        Console.WriteLine("GC Notification cancelled.");
                        break;
                    }
                    else
                    {
                        // This can occur if a timeout period 
                        // is specified for WaitForFullGCApproach(Timeout)  
                        // or WaitForFullGCComplete(Timeout)   
                        // and the time out period has elapsed. 
                        Console.WriteLine("GC Notification not applicable.");
                        break;
                    }

                    // Check for a notification of a completed collection.
                    s = GC.WaitForFullGCComplete(500);
                    if (s == GCNotificationStatus.Succeeded)
                    {
                        Console.WriteLine("Full GC Complete");
                        foreach (var item in ConcurrentAgentHandlerList)
                        {
                            item.Value.RestartTimers();
                        }
                    }
                    else if (s == GCNotificationStatus.Canceled)
                    {
                        Console.WriteLine("GC Notification cancelled.");
                        break;
                    }
                    else if (s == GCNotificationStatus.Timeout || s == GCNotificationStatus.NotApplicable)
                    {
                        foreach (var item in ConcurrentAgentHandlerList)
                        {
                            item.Value.RestartTimers();
                        }
                    }
                    else
                    {
                        // Could be a time out.
                        Console.WriteLine("GC Notification not applicable.");
                        break;
                    }


                    Thread.Sleep(500);
                    // FinalExit is set to true right before   
                    // the main thread cancelled notification.
                }
            }) {Name = "GC Notification Thread"};
            //timerCheck.Start();
            var tcpListen = new TcpListener(IPAddress.Any, 8080);
            tcpListen.Start(200);
            //listenTimer = new System.Threading.Timer(TimerCallback, tcpListen, 0, 100);
            ThreadPool.QueueUserWorkItem(WCb, tcpListen);
            Console.WriteLine("Server started");
            Console.WriteLine("Listening on {0}", tcpListen.LocalEndpoint);
            while (true)
            {
                lock (LockConsole)
                {
                    Console.WriteLine("Current connected clients: {0}\t\tPackets: {1}", _clientsConnected, _messagesRecvd);
                }
                Thread.Sleep(1000);
            }

        }

        public static async void TimerCallback(object objt)
        {
            while (true)
            {
                try
                {
                    var tcpListen = objt as TcpListener;
                    var listenList = new List<Task<TcpClient>>();
                    tcpListen.Server.UseOnlyOverlappedIO = true;
                    while (tcpListen.Pending())
                    {
                        listenList.Add(tcpListen.AcceptTcpClientAsync());
                    }
                    //listenTimer.Change(100, 1); // Let's stop the timer from consuming too many threads
                    var restart = false;
                    for (var i = 0; i < listenList.Count; i++)
                    {
                        var item = listenList[i];
                        var listen = await item;
                        Interlocked.Increment(ref _clientsConnected);
                        if (item.IsCompleted)
                        {

                            var tcbah = new TcpByteAgentHandler(new TcpByteTransport(listen), TcpByteAgentHandler.HandlerType.ReceiveHeavy);
                            tcbah.AddCommand(new SendString(""));
                            tcbah.HasData += tcbah_HasDataEvent;
                            tcbah.Disconnected += tcbah_Disconnected;
                            ConcurrentAgentHandlerList.GetOrAdd(tcbah.GetHashCode(), tcbah);
                        }

                        if (i == listenList.Count - 1 && restart)
                        {
                            i = 0;
                            restart = false;
                        }
                    }

                }
                catch (Exception)
                {
                    throw;
                }
                Thread.Sleep(10);
            }
        }

        private static void tcbah_Disconnected(object sender)
        {
            Interlocked.Decrement(ref _clientsConnected);
            var tcbah = sender as TcpByteAgentHandler;
            lock (LockConsole)
            {
                tcbah.DisconnectedTime = DateTime.Now;
                Console.WriteLine("Agent {0} disconnected. It was connected for {1}", tcbah.AgentInfo.AgentName, (tcbah.DisconnectedTime - tcbah.ConnectedTime));
            }

            var fstream = File.CreateText(string.Format(@"C:\Logfiles\{0}_{1}.txt", tcbah.AgentInfo.AgentName, tcbah.ConnectedTime.ToFileTime()));
            foreach (var item in tcbah.ThreadEntryPoint)
            {
                fstream.WriteLine("Starting Thread {0:X}", item.Key);
                foreach (var tpeList in item.Value)
                {
                    fstream.WriteLine("{0}{1}", "".PadLeft((int)tpeList.Item1), tcbah.FunctionList[tpeList.Item2]);
                }
            }
            fstream.Flush();
            fstream.Close();
            TcpByteAgentHandler refOut = null;
            ConcurrentAgentHandlerList.TryRemove(tcbah.GetHashCode(), out refOut);
            if (refOut != null)
            {
                lock (LockConsole)
                {
                    Console.WriteLine("<<<<Agent {0} removed from list.", tcbah.AgentInfo.AgentName);
                }
                Task.Factory.StartNew(async () =>
                {
                    await Task.Delay(30000);
                    tcbah.Disconnect();
                });
            }

        }

        private static void tcbah_HasDataEvent(object sender)
        {
            var tcbah = sender as TcpByteAgentHandler;
            var stringCmd = new SendString("Done!");
            var arr = tcbah.GetCommands();
            foreach (var item in arr)
            {
                Interlocked.Increment(ref _messagesRecvd);
                if (item == null) continue;

                if (item is DefineFunction)
                {
                    var defFunc = item as DefineFunction;
                    tcbah.FunctionList.Add(defFunc.FunctionId, defFunc.FunctionName);
                }

                var agentInformation = item as AgentInformation;
                if (agentInformation != null)
                {
                    tcbah.AgentInfo = agentInformation;
                    tcbah.ConnectedTime = DateTime.Now;
                    Console.WriteLine("Agent {0} connected with version {1} from machine {2}", agentInformation.AgentName, agentInformation.Version, agentInformation.MachineName);
                    var okCmd = new SendString("OK!");
                    tcbah.SendCommand(okCmd);
                }

                if (item is FunctionEnterQuick)
                {
                    var feq = item as FunctionEnterQuick;
                    if (!tcbah.ThreadDepth.ContainsKey(feq.ThreadId))
                        tcbah.ThreadDepth.Add(feq.ThreadId, 0);

                    if (!tcbah.ThreadEntryPoint.ContainsKey(feq.ThreadId))
                        tcbah.ThreadEntryPoint.Add(feq.ThreadId, new List<Tuple<long, long>>());

                    tcbah.ThreadEntryPoint[feq.ThreadId].Add(new Tuple<long, long>(tcbah.ThreadDepth[feq.ThreadId], feq.FunctionId));
                    tcbah.ThreadDepth[feq.ThreadId]++;
                }

                if (item is FunctionTailQuick)
                {
                    var feq = item as FunctionTailQuick;
                    if (!tcbah.ThreadDepth.ContainsKey(feq.ThreadId))
                        tcbah.ThreadDepth.Add(feq.ThreadId, 0);

                    if (tcbah.ThreadDepth[feq.ThreadId] > 0)
                    {
                        tcbah.ThreadDepth[feq.ThreadId]--;
                    }

                    if (!tcbah.ThreadEntryPoint.ContainsKey(feq.ThreadId))
                        tcbah.ThreadEntryPoint.Add(feq.ThreadId, new List<Tuple<long, long>>());

                    tcbah.ThreadEntryPoint[feq.ThreadId].Add(new Tuple<long, long>(tcbah.ThreadDepth[feq.ThreadId], feq.FunctionId));
                }

                if (item is FunctionLeaveQuick)
                {
                    var feq = item as FunctionLeaveQuick;
                    if (!tcbah.ThreadDepth.ContainsKey(feq.ThreadId))
                        tcbah.ThreadDepth.Add(feq.ThreadId, 0);

                    if (tcbah.ThreadDepth[feq.ThreadId] > 0)
                    {
                        tcbah.ThreadDepth[feq.ThreadId]--;
                    }

                    if (!tcbah.ThreadEntryPoint.ContainsKey(feq.ThreadId))
                        tcbah.ThreadEntryPoint.Add(feq.ThreadId, new List<Tuple<long, long>>());

                    tcbah.ThreadEntryPoint[feq.ThreadId].Add(new Tuple<long, long>(tcbah.ThreadDepth[feq.ThreadId], feq.FunctionId));
                }

                var sendString = item as SendString;
                if (sendString != null)
                {
                    var it = sendString;
                    Console.WriteLine("Agent {0} has sent string {1}", tcbah.AgentInfo.AgentName, it.StringDataData);
                    if (sendString.StringDataData == "Done!")
                    {
                        tcbah.SendCommand(stringCmd);
                        tcbah.Dispose();
                    }
                }
            }
        }
    }
}


