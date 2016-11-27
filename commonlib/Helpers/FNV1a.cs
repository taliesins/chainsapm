using System;
using System.Security.Cryptography;

namespace ChainsAPM.Helpers
{
    public sealed class Fnv1a32 : HashAlgorithm
    {
        private const uint FnvPrime = 16777619;

        private const uint FnvOffsetBasis = 2166136261;

        private uint _hash;

        public Fnv1a32()
        {
            Reset();
        }

        public override void Initialize()
        {
            Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            for (var i = ibStart; i < cbSize; i++)
            {
                unchecked
                {
                    _hash ^= array[i];
                    _hash *= FnvPrime;
                }
            }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_hash);
        }

        private void Reset()
        {
            _hash = FnvOffsetBasis;
        }
    }
    public sealed class Fnv1a64 : HashAlgorithm
    {
        private const ulong FnvPrime = unchecked(1099511628211);

        private const ulong FnvOffsetBasis = unchecked(14695981039346656037);

        private ulong _hash;

        public Fnv1a64()
        {
            Reset();
        }

        public override void Initialize()
        {
            Reset();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            for (var i = ibStart; i < cbSize; i++)
            {
                unchecked
                {
                    _hash ^= array[i];
                    _hash *= FnvPrime;
                }
            }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_hash);
        }

        private void Reset()
        {
            _hash = FnvOffsetBasis;
        }
    }
}
