using System;
using System.Collections.Generic;

namespace ChainsAPM.Commands.Agent
{
    public class FunctionLeaveQuick : Interfaces.ICommand<byte>
    {
        public long FunctionId { get; set; }
        public long ThreadId { get; set; }
        public DateTime TimeStamp { get; set; }

        public FunctionLeaveQuick(long functionid, long threadid, long timestamp)
        {
            FunctionId = functionid;
            ThreadId = threadid;
            TimeStamp = DateTime.FromFileTimeUtc(timestamp);
        }

        public string Name
        {
            get { return "Function Leave Quick"; }
        }

        public ushort Code
        {
            get { return 0x0019; }
        }

        public string Description
        {
            get { return "Event that represents a quick function enter--meaning there were no paramters captured by the agent."; }
        }

        public Type CommandType
        {
            get { return typeof(string); }
        }

        public Interfaces.ICommand<byte> Decode(ArraySegment<byte> input)
        {
            if (input.Count == 0)
            {
                throw new System.Runtime.Serialization.SerializationException(
                    "Size of message is zero. Please check the incoming byte stream for possible errors. ");
            }
            var segstream = new Helpers.ArraySegmentStream(input);
            var size = segstream.GetInt32();
            if (input.Count != size)
            {
                throw new System.Runtime.Serialization.SerializationException(
                    "Size of message does not match size of byte stream. Please check the incoming byte stream for possible errors.");
            }
            var code = segstream.GetInt16();
            if (code != Code)
            {
                throw new System.Runtime.Serialization.SerializationException(
                    "Invalid command code detected. Please check the incoming byte stream for possible errors.");
            }
            var timestamp = segstream.GetInt64();
            var function = segstream.GetInt64();
            var thread = segstream.GetInt64();
            var term = segstream.GetInt16();
            if (term != 0)
            {
                throw new System.Runtime.Serialization.SerializationException(
                    "Terminator is a non zero value. Please check the incoming byte stream for possible errors.");
            }
            return new FunctionLeaveQuick(function, thread, timestamp);
        }

        public byte[] Encode()
        {
            var buffer = new List<byte>(23);
            buffer.AddRange(BitConverter.GetBytes(23)); // 4 bytes for size, 2 byte for code, 8 bytes for data, 8 bytes for data, 2 bytes for term
            buffer.AddRange(BitConverter.GetBytes(Code));
            buffer.AddRange(BitConverter.GetBytes(FunctionId));
            buffer.AddRange(BitConverter.GetBytes(ThreadId));// 4 bytes for size, 1 byte for code, 8 bytes for data, 2 bytes for term
            buffer.AddRange(BitConverter.GetBytes((short)0));
            return buffer.ToArray();
        }
    }
}
