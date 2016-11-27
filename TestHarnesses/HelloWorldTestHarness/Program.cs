using System;

namespace HelloWorldTestHarness
{
    public class Program
    {
        private static readonly AsyncCallback SyncCb = AsyncResult;

        private static void AsyncResult(IAsyncResult result)
        {
            var wr = (System.Net.HttpWebRequest)result.AsyncState;
            wr.EndGetResponse(result);
            var s = result;
        }

        static void Main(string[] args)
        {
            var sw = new System.Diagnostics.Stopwatch();
            var env = Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING");
            sw.Start();
            System.IO.TextWriter tw = new System.IO.StreamWriter(@"C:\Logfiles\logfile.txt", true);
            var StopTime = DateTime.Now.AddSeconds(5);
            System.Threading.Thread.CurrentThread.Name = "Main";
            var loops = 0;
            var t = System.Threading.Tasks.Task.Run(() =>
            {
                var i = 0x43434343;
                var b = 0x48484848;
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
            });
            Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
            Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
            Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
            while (loops < 100)
            {
                ++loops;
                //Console.ReadLine();
                Console.WriteLine("Hello World!");
                //System.Collections.SortedList sList = new System.Collections.SortedList(System.Environment.GetEnvironmentVariables());
                //foreach (System.Collections.DictionaryEntry item in sList)
                //{
                //    Console.WriteLine("{0}\t\t={1}", item.Key, item.Value);
                //}
                var i = 0x43434343;
                var b = 0x48484848;

                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                GC.Collect(0);
                Console.WriteLine(AddNumbers(ref i, ref b));
                GC.Collect(1);
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                GC.Collect(2);
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(ref i, ref b));
                Console.WriteLine(AddNumbers(ref i, ref b));
                for (var f = 0; f < 8; f++)
                {

                    var t2 = new System.Threading.Thread(new System.Threading.ThreadStart(
                   () =>
                   {
                       System.Threading.Thread.CurrentThread.Name = string.Format("Worker Thread {0}", f);
                       Console.WriteLine("Hello world from another thread!");
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(0x41414141, 0x42424242));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                       Console.WriteLine(AddNumbers(ref i, ref b));
                   }));
                    t2.Start();
                }
            }
            var wr = System.Net.WebRequest.Create("http://www.google.com");
            wr.BeginGetResponse(SyncCb, wr);
            Console.ReadLine();
            sw.Stop();
            tw.WriteLine("HelloWorld.exe ran {1} loops in {0} ms with profiling {2}", sw.ElapsedMilliseconds, loops, env == null ? "OFF": "ON");
            tw.Flush();
            tw.Dispose();
            Console.ReadLine();
        }

        static int AddNumbers(int i, int b)
        {
            return i + b;
        }

        static int AddNumbers(ref int i, ref int b)
        {
            System.Threading.Thread.Sleep(2);
            return i + b;
        }
    }
}
