using System;

namespace ChainsAPM.Helpers
{
    public class ArraySegmentStream
    {
        private int _internalCounter;
        private ArraySegment<byte> _segmentRef;
        public ArraySegmentStream(ArraySegment<byte> streamInput)
        {
            _internalCounter = streamInput.Offset;
            _segmentRef = streamInput;
        }

        public byte GetByte() { return _segmentRef.Array[_internalCounter++]; }
        public short GetInt16() { var countercopy = _internalCounter; _internalCounter += 2; return BitConverter.ToInt16(_segmentRef.Array, countercopy); }
        public int GetInt32() { var countercopy = _internalCounter; _internalCounter += 4; return BitConverter.ToInt32(_segmentRef.Array, countercopy); }
        public long GetInt64() { var countercopy = _internalCounter; _internalCounter += 8; return BitConverter.ToInt64(_segmentRef.Array, countercopy); }
        public string GetUnicode(int length) { var countercopy = _internalCounter; _internalCounter += length * 2; return System.Text.Encoding.Unicode.GetString(_segmentRef.Array, countercopy, length*2); }
        public string GetAscii(int length) { var countercopy = _internalCounter; _internalCounter += length; return System.Text.Encoding.Unicode.GetString(_segmentRef.Array, countercopy, length); }
        public char GetChar() { return (char)_segmentRef.Array[_internalCounter++]; }
    }
}
