using System;
using System.Collections.Generic;

namespace ChainsAPM.Commands.Agent
{
    class FunctionsToProfile: Interfaces.ICommand<byte>
    {
        public List<string> Functions { get; set; }
        public long ThreadId { get; set; }

        public FunctionsToProfile(List<string> functions)
        {
            Functions = functions;
        }

        public string Name
        {
            get { return "Send a list of functions to profile."; }
        }

        public ushort Code
        {
            get { return 0x0006; }
        }

        public string Description
        {
            get { return "Event that sends a list of functions that we want the profiler to actively instrument."; }
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
            var function = segstream.GetInt64();
            var thread = segstream.GetInt64();
            var term = segstream.GetInt16();
            if (term != 0)
            {
                throw new System.Runtime.Serialization.SerializationException(
                    "Terminator is a non zero value. Please check the incoming byte stream for possible errors.");
            }
            return new FunctionsToProfile(new List<string>());
        }

        public byte[] Encode()
        {
            var bufferSize = 0;
            foreach (var item in Functions)
            {
                bufferSize += System.Text.Encoding.Unicode.GetByteCount(item);
                bufferSize += 4; // Include the bytes needed for the bytes integer
            }
            bufferSize += 2; // Include Terminator
            var buffer = new List<byte>(bufferSize);

            buffer.AddRange(BitConverter.GetBytes(bufferSize)); // 4 bytes for size, 2 byte for code, 8 bytes for data, 8 bytes for data, 2 bytes for term
            buffer.AddRange(BitConverter.GetBytes(Code));
            buffer.AddRange(BitConverter.GetBytes(Functions.Count));
            foreach (var item in Functions)
            {
                buffer.AddRange(BitConverter.GetBytes(System.Text.Encoding.Unicode.GetByteCount(item)));
                buffer.AddRange(System.Text.Encoding.Unicode.GetBytes(item));
            }
            buffer.AddRange(BitConverter.GetBytes((short)0));
            return buffer.ToArray();
        }
    }
}
