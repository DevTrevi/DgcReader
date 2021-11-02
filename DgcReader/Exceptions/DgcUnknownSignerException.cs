using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{
    [Serializable]
    public class DgcUnknownSignerException : DgcException
    {
        /// <summary>
        /// The certificate key identifier used for searching the public key
        /// </summary>
        public string? Kid { get; }

        /// <summary>
        /// The issuer country for the signing certificate
        /// </summary>
        public string? Issuer { get; }

        public DgcUnknownSignerException(string message, string? kid, string? issuer)
            : base(message)
        {
            Kid = kid;
            Issuer = issuer;
        }

        public DgcUnknownSignerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
