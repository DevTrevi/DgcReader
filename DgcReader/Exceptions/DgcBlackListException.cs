using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{
    [Serializable]
    public class DgcBlackListException : DgcException
    {
        public string CertificateIdentifier { get; }

        public DgcBlackListException(string message,
            string certificateIdentifier) :
            base(message)
        {
            CertificateIdentifier = certificateIdentifier;
        }

        public DgcBlackListException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
