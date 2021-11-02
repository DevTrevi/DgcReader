using DgcReader.TrustListProviders;
using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{
    [Serializable]
    public class DgcSignatureValidationException : DgcException
    {
        /// <summary>
        /// The public key data used to validate the signature
        /// </summary>
        public ITrustedCertificateData? PublicKeyData { get; }

        public DgcSignatureValidationException(string message,
            ITrustedCertificateData? publicKeyData = null) :
            base(message)
        {
            PublicKeyData = publicKeyData;
        }

        public DgcSignatureValidationException(string message,
            Exception innerException,
            ITrustedCertificateData? publicKeyData = null) :
            base(message, innerException)
        {
            PublicKeyData = publicKeyData;
        }

        public DgcSignatureValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


}
