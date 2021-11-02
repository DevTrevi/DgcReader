using System;
using System.Security.Cryptography;
using PeterO.Cbor;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.Cwt.Cose
{

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Signature values used in DGC CBOR Objects
    /// </summary>
    public enum CoseSignatureAlgorithm : int
    {
        SHA256withECDSA = -7,
        SHA384withECDSA = -35,
        SHA512withECDSA = -36,

        SHA256withRSA = -37,
        SHA384withRSA = -38,
        SHA512withRSA = -39,
    }

    public static class CoseSignatureAlgorithmParser
    {
        /// <summary>
        /// Get the <see cref="CoseSignatureAlgorithm"/> represented by the <see cref="CBORObject" /> specified
        /// </summary>
        /// <param name="cborValue">The CBOR object representing the algorithm</param>
        /// <returns></returns>
        public static CoseSignatureAlgorithm GetAlgorithm(CBORObject cborValue)
        {
            return (CoseSignatureAlgorithm)cborValue.AsInt32();
        }


#if NET452
        /// <summary>
        /// Get the AlgorithmName to be used by the BouncyCastle verifyier
        /// </summary>
        /// <param name="algo">The CBOR signature algorithm value</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string GetAlgorithmName(this CoseSignatureAlgorithm algo)
        {
            switch (algo)
            {
                case CoseSignatureAlgorithm.SHA256withRSA:
                    return "SHA256withRSA/PSS";
                case CoseSignatureAlgorithm.SHA384withECDSA:
                    return "SHA384withRSA/PSS";
                case CoseSignatureAlgorithm.SHA512withRSA:
                    return "SHA512withRSA/PSS";
                case CoseSignatureAlgorithm.SHA256withECDSA:
                    return "SHA256withECDSA";
                case CoseSignatureAlgorithm.SHA384withRSA:
                    return "SHA384withECDSA";
                case CoseSignatureAlgorithm.SHA512withECDSA:
                    return "SHA512withECDSA";

            }
            throw new NotSupportedException($"Unsupported signature algorithm with CBOR value {algo}");
        }
#else
        /// <summary>
        /// Get the <see cref="HashAlgorithmName"/> to be used with the <see cref="CoseSignatureAlgorithm"/> specified when checking a signature
        /// </summary>
        /// <param name="algo">The CBOR signature algorithm value</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static HashAlgorithmName GetHashAlgorithmName(this CoseSignatureAlgorithm algo)
        {
            switch (algo)
            {
                case CoseSignatureAlgorithm.SHA256withRSA:
                case CoseSignatureAlgorithm.SHA256withECDSA:
                    return HashAlgorithmName.SHA256;
                case CoseSignatureAlgorithm.SHA384withRSA:
                case CoseSignatureAlgorithm.SHA384withECDSA:
                    return HashAlgorithmName.SHA384;
                case CoseSignatureAlgorithm.SHA512withRSA:
                case CoseSignatureAlgorithm.SHA512withECDSA:
                    return HashAlgorithmName.SHA512;
            }
            throw new NotSupportedException($"Unsupported signature algorithm with CBOR value {algo}");
        }
#endif

        /// <summary>
        /// Check if the signature algorithm is one of the supported RSA algorithms
        /// </summary>
        /// <param name="algo">The CBOR signature algorithm value</param>
        /// <returns></returns>
        public static bool IsRsaAlgorithm(this CoseSignatureAlgorithm algo)
        {
            return algo == CoseSignatureAlgorithm.SHA256withRSA ||
                algo == CoseSignatureAlgorithm.SHA384withRSA ||
                algo == CoseSignatureAlgorithm.SHA512withRSA;
        }

        /// <summary>
        /// Check if the signature algorithm is one of the supported ECDsa algorithms
        /// </summary>
        /// <param name="algo">The CBOR signature algorithm value</param>
        /// <returns></returns>
        public static bool IsECDsaAlgorithm(this CoseSignatureAlgorithm algo)
        {
            return algo == CoseSignatureAlgorithm.SHA256withECDSA ||
                algo == CoseSignatureAlgorithm.SHA384withECDSA ||
                algo == CoseSignatureAlgorithm.SHA512withECDSA;
        }
    }
}
