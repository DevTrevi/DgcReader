using DgcReader.Deserializers.Italy.Models;
using DgcReader.Models;
using GreenpassReader.Models;
using System;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace DgcReader.RuleValidators.Italy.Validation
{
    /// <summary>
    /// Model for <see cref="ICertificateEntryValidator"/>
    /// </summary>
    public class ValidationCertificateModel
    {
        /// <summary>
        /// The DGC to be vsalidated.
        /// Note that this could be the decoded by <see cref="DgcReaderService"/>, or <see cref="ItalianDGC"/> if issued by Italy
        /// </summary>
        public EuDGC Dgc { get; set; }

        /// <summary>
        /// The date/time of validation
        /// </summary>
        public DateTimeOffset ValidationInstant { get; set; }

        /// <summary>
        /// Signature validation data
        /// </summary>
        public SignatureValidationResult SignatureData { get; set; }
    }
}
