using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Konto
{
    class Exceptions
    {
    }

    [Serializable]
    public class WrongRecordFormatException : Exception
    {
        public WrongRecordFormatException() { }
        public WrongRecordFormatException(string message) : base(message) { }
        public WrongRecordFormatException(string message, Exception inner) : base(message, inner) { }
        protected WrongRecordFormatException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
