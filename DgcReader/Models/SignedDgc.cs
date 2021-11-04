using GreenpassReader.Models;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Models
{
    /// <summary>
    /// Eu Digital Green Certificate result
    /// </summary>
    public class SignedDgc
    {
        /// <summary>
        /// The Digital Green Certificate data
        /// </summary>
        public EuDGC Dgc { get; internal set; }

        /// <summary>
        /// The issuer of the signed COSE object
        /// </summary>
        public string? Issuer { get; internal set; }

        /// <summary>
        /// Expiration date of the signed object.
        /// NOTE: this is NOT the effective expiration date of the certificate for a given country.
        /// Validity of the certificate must be checked against the business rules of the country where the certificate is verified.
        /// </summary>
        public DateTime? ExpirationDate { get; internal set; }

        /// <summary>
        /// The issue date of the signed object.
        /// </summary>
        public DateTime? IssuedDate { get; internal set; }

        /// <summary>
        /// The result of the signature verification
        /// </summary>
        public bool HasValidSignature { get; internal set; } = false;

        /// <summary>
        /// The validation instant when checks where performed on the certificate
        /// </summary>
        public DateTimeOffset ValidationInstant { get; internal set; }
    }


}
