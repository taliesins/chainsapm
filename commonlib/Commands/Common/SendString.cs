using System;
using System.Collections.Generic;

namespace ChainsAPM.Commands.Common
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class SendString : Interfaces.ICommand<byte>
    {
        private readonly string _stringData;
        private readonly Helpers.Fnv1a64 _hashHelper;
        private byte[] _hash;

        public SendString(string data)
        {
            _stringData = data;
            _hashHelper = new Helpers.Fnv1a64();
        }
        public SendString(string data, byte[] hash)
            : this(data)
        {
            _hash = hash;
        }
        public string Name
        {
            get { return "Add String"; }
        }
        public ushort Code
        {
            get { return 0x0011; }
        }
        public string Description
        {
            get { return "Creates a has of a string and sends it to the server to be referenced later. TH"; }
        }
        public Type CommandType
        {
            get { return typeof(string); }
        }
        public Interfaces.ICommand<byte> Decode(ArraySegment<byte> input)
        {
            if (input.Count == 0) return null;
            var segstream = new Helpers.ArraySegmentStream(input);
            var size = segstream.GetInt32();
            if (input.Count != size) return null;
            int code = segstream.GetInt16();
            if (!(code == Code | code == Code + 1)) return null;
            var stringLength = segstream.GetInt32();
            var hashCode = segstream.GetInt64();
            
            var sendString = code == Code + 1 ? segstream.GetUnicode(stringLength) : segstream.GetAscii(stringLength);

            return new SendString(sendString);
        }
        public byte[] Encode()
        {
            var bufferBytes = System.Text.Encoding.Unicode.GetBytes(_stringData);
            var buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes((bufferBytes.Length * 2) + 20)); // 4 bytes for size, 2 byte for code, 4 bytes for strlen, 8 bytes for hash, Xbytes for string 2 bytes for term
            buffer.AddRange(BitConverter.GetBytes((short)3));
            if (_hash == null)
            {
                _hash = _hashHelper.ComputeHash(bufferBytes);
            }
            buffer.AddRange(BitConverter.GetBytes(_stringData.Length));
            buffer.AddRange(_hash);
            buffer.AddRange(bufferBytes);
            buffer.AddRange(new byte[] { 0x00, 0x00 });
            return buffer.ToArray();

        }
        public string StringDataData { get { return _stringData; } }
    }
}
