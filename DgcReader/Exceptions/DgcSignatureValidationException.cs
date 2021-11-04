using DgcReader.Interfaces.TrustListProviders;
using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{
    [Serializable]
    public class DgcSignatureValidationException : DgcException
    {
        /// <summary>
        /// The issuer of the signed COSE object
        /// </summary>
        public string? Issuer { get; internal set; }

        /// <summary>
        /// Expiration date of the signed object.
        /// </summary>
        public DateTime? ExpirationDate { get; internal set; }

        /// <summary>
        /// The issue date of the signed object.
        /// </summary>
        public DateTime? IssueDate { get; internal set; }

        /// <summary>
        /// The public key data used to validate the signature
        /// </summary>
        public ITrustedCertificateData? PublicKeyData { get; }

        public DgcSignatureValidationException(string message,
            ITrustedCertificateData? publicKeyData = null,
            string? issuer = null,
            DateTime? issueDate = null,
            DateTime? expirationDate = null) :
            base(message)
        {
            PublicKeyData = publicKeyData;
            Issuer = issuer;
            IssueDate = issueDate;
            ExpirationDate = expirationDate;
        }

        public DgcSignatureValidationException(string message,
            Exception innerException,
            ITrustedCertificateData? publicKeyData = null,
            string? issuer = null,
            DateTime? issueDate = null,
            DateTime? expirationDate = null) :
            base(message, innerException)
        {
            PublicKeyData = publicKeyData;
            Issuer = issuer;
            IssueDate = issueDate;
            ExpirationDate = expirationDate;
        }

        public DgcSignatureValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }


    [Serializable]
    public class DgcUnknownSignerException : DgcSignatureValidationException
    {
        /// <summary>
        /// The certificate key identifier used for searching the public key
        /// </summary>
        public string? Kid { get; }

        public DgcUnknownSignerException(string message, string? kid,
            string? issuer = null,
            DateTime? issueDate = null,
            DateTime? expirationDate = null)
            : base(message, issuer: issuer, issueDate: issueDate, expirationDate: expirationDate)
        {
            Kid = kid;
        }

        public DgcUnknownSignerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [Serializable]
    public class DgcSignatureExpiredException : DgcSignatureValidationException
    {
        public DgcSignatureExpiredException(string message,
            ITrustedCertificateData? publicKeyData = null,
            string? issuer = null,
            DateTime? issueDate = null,
            DateTime? expirationDate = null) :
            base(message, publicKeyData, issuer, issueDate, expirationDate)
        {
          
        }

        public DgcSignatureExpiredException(string message,
            Exception innerException,
            ITrustedCertificateData? publicKeyData = null,
            string? issuer = null,
            DateTime? issueDate = null,
            DateTime? expirationDate = null) :
            base(message, innerException, publicKeyData, issuer, issueDate, expirationDate)
        {
          
        }

        public DgcSignatureExpiredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
