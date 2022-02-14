using DgcReader.Models;
using DgcReader.RuleValidators.Italy.Models;
using GreenpassReader.Models;
using System;
using System.Collections.Generic;

namespace DgcReader.RuleValidators.Italy.Validation
{
    public class ValidationCertificateModel
    {
        /// <summary>
        /// The DGC to be vsalidated.
        /// Note that this could be the decoded by <see cref="DgcReaderService"/>, or <see cref="ItalianDGC"/> if issued by Italy
        /// </summary>
        public EuDGC Dgc { get; set; }

        /// <summary>
        /// The RAW json of the DGC
        /// </summary>
        public string DgcJson { get; set; }

        /// <summary>
        /// Italian validation rules
        /// </summary>
        public IEnumerable<RuleSetting> Rules { get; set; }

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
