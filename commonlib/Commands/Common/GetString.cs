﻿using System;

namespace ChainsAPM.Commands.Common
{
    internal class GetString : Interfaces.ICommand<byte>
    {
        public string Name
        {
            get { throw new NotImplementedException(); }
        }

        public ushort Code
        {
            get { throw new NotImplementedException(); }
        }

        public string Description
        {
            get { throw new NotImplementedException(); }
        }

        public Type CommandType
        {
            get { throw new NotImplementedException(); }
        }

        public Interfaces.ICommand<byte> Decode(ArraySegment<byte> input)
        {
            throw new NotImplementedException();
        }

        public byte[] Encode()
        {
            throw new NotImplementedException();
        }
    }
}
