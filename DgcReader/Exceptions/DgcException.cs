using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{


    /// <summary>
    /// Generic exception for managing errors in the library.
    /// Used as base exception for all the more specific exceptions managed by the library.
    /// </summary>
    [Serializable]
    public class DgcException : Exception
    {
        public DgcException()
        {

        }

        public DgcException(string message) : base(message)
        {

        }

        public DgcException(string message, Exception innerException) : base(message, innerException)
        {

        }
        public DgcException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


}
