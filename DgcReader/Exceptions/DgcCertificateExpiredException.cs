using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{

    [Serializable]
    public class DgcExpiredException : DgcException
    {
        public DateTime ExpirationDate { get; set; }

        public DgcExpiredException()
        {
        }

        public DgcExpiredException(string message, DateTime expirationDate) : base(message)
        {
            ExpirationDate = expirationDate;
        }

        public DgcExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}