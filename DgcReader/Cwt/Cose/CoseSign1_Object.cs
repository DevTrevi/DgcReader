using System;
using PeterO.Cbor;
using System.Security.Cryptography;
using DgcReader.Interfaces.TrustListProviders;
using DgcReader.Exceptions;

#if NET5_0_OR_GREATER
using System.Formats.Asn1;
#endif

#if NET452
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System.Collections.Generic;
using System.Linq;
#endif

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0
// Based on the original work of Myndigheten för digital förvaltning (DIGG)

namespace DgcReader.Cwt.Cose
{
    /// <summary>
    /// A representation of a COSE_Sign1 object.
    /// </summary>
    public class CoseSign1_Object
    {
        /// <summary>
        /// The COSE_Sign1 message tag.
        /// </summary>
        private const int MessageTag = 18;

        /// <summary>
        /// The protected attributes.
        /// </summary>
        private CBORObject ProtectedAttributes;

        /// <summary>
        /// The encoding of the protected attributes.
        /// </summary>
        private byte[]? ProtectedAttributesEncoding;

        /// <summary>
        /// The unprotected attributes.
        /// </summary>
        private CBORObject UnprotectedAttributes;

        /// <summary>
        /// The data content (data that is signed).
        /// </summary>
        private byte[]? Content;

        /// <summary>
        /// The signature.
        /// </summary>
        private byte[]? Signature;

        /// <summary>
        /// We don't support external data - so it's static.
        /// </summary>
        private static byte[] ExternalData = new byte[0];

        /// <summary>
        /// The COSE_Sign1 context string.
        /// </summary>
        private static string ContextString = "Signature1";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CoseSign1_Object()
        {
            ProtectedAttributes = CBORObject.NewMap();
            UnprotectedAttributes = CBORObject.NewMap();
        }

        /// <summary>
        /// Constructor that accepts the binary representation of a signed COSE_Sign1 object.
        /// </summary>
        /// <param name="data">the binary representation of the COSE_Sign1 object</param>
        /// <exception cref="CBORException">for invalid data</exception>
        public CoseSign1_Object(byte[] data)
        {
            CBORObject message = CBORObject.DecodeFromBytes(data);
            if (message.Type != CBORType.Array)
            {
                throw new CBORException("Supplied message is not a valid COSE security object");
            }

            // If the message is tagged, it must have the message tag for a COSE_Sign1 message.
            // We also handle the case where there is an outer CWT tag.
            //
            if (message.IsTagged)
            {
                if (message.GetAllTags().Length > 2)
                {
                    throw new CBORException("Invalid object - too many tags");
                }
                if (message.GetAllTags().Length == 2)
                {
                    if (CWT.MESSAGE_TAG != message.MostOuterTag.ToInt32Unchecked())
                    {
                        throw new CBORException(string.Format(
                          "Invalid COSE_Sign1 structure - Expected {0} tag - but was {1}",
                          CWT.MESSAGE_TAG, message.MostInnerTag.ToInt32Unchecked()));
                    }
                }
                if (MessageTag != message.MostInnerTag.ToInt32Unchecked())
                {
                    throw new CBORException(string.Format(
                      "Invalid COSE_Sign1 structure - Expected {0} tag - but was {1}",
                      MessageTag, message.MostInnerTag.ToInt32Unchecked()));
                }
            }

            if (message.Count != 4)
            {
                throw new CBORException(string.Format(
                  "Invalid COSE_Sign1 structure - Expected an array of 4 items - but array has {0} items", message.Count));
            }
            if (message[0].Type == CBORType.ByteString)
            {
                ProtectedAttributesEncoding = message[0].GetByteString();

                if (message[0].GetByteString().Length == 0)
                {
                    ProtectedAttributes = CBORObject.NewMap();
                }
                else
                {
                    ProtectedAttributes = CBORObject.DecodeFromBytes(ProtectedAttributesEncoding);
                    if (ProtectedAttributes.Count == 0)
                    {
                        ProtectedAttributesEncoding = new byte[0];
                    }
                }
            }
            else
            {
                throw new CBORException(string.Format("Invalid COSE_Sign1 structure - " +
                    "Expected item at position 1/4 to be a bstr which is the encoding of the protected attributes, but was {0}",
                  message[0].Type));
            }

            if (message[1].Type == CBORType.Map)
            {
                UnprotectedAttributes = message[1];
            }
            else
            {
                throw new CBORException(string.Format(
                  "Invalid COSE_Sign1 structure - Expected item at position 2/4 to be a Map for unprotected attributes, but was {0}",
                  message[1].Type));
            }

            if (message[2].Type == CBORType.ByteString)
            {
                Content = message[2].GetByteString();
            }
            else if (!message[2].IsNull)
            {
                throw new CBORException(string.Format(
                  "Invalid COSE_Sign1 structure - Expected item at position 3/4 to be a bstr holding the payload, but was {0}",
                  message[2].Type));
            }

            if (message[3].Type == CBORType.ByteString)
            {
                Signature = message[3].GetByteString();
            }
            else
            {
                throw new CBORException(string.Format(
                  "Invalid COSE_Sign1 structure - Expected item at position 4/4 to be a bstr holding the signature, but was {0}",
                  message[3].Type));
            }
        }


        /// <summary>
        /// Decodes the supplied data into a CoseSign1_Object object.
        /// </summary>
        /// <param name="data">the encoded data</param>
        /// <returns>a CoseSign1_Object object</returns>
        /// <exception cref="CBORException">if the supplied encoding is not a valid CoseSign1_Object</exception>
        public static CoseSign1_Object Decode(byte[] data)
        {
            return new CoseSign1_Object(data);
        }


        /// <summary>
        /// A utility method that looks for the key identifier (kid) in the protected (and unprotected) attributes.
        /// </summary>
        /// <returns>the key identifier as a byte string</returns>
        public byte[]? GetKeyIdentifier()
        {
            CBORObject kid = ProtectedAttributes[HeaderParameterKey.KID];
            if (kid == null)
            {
                kid = UnprotectedAttributes[HeaderParameterKey.KID];
            }

            if (kid == null)
            {
                return null;
            }
            return kid.GetByteString();
        }

        /// <summary>
        /// A utility method that gets the contents as a <see cref="CWT"/>
        /// </summary>
        /// <returns>the CWT or null if no contents is available</returns>
        /// <exception cref="CBORException">if the contents do not hold a valid CWT</exception>
        public CWT? GetCwt()
        {
            if (Content == null)
            {
                return null;
            }
            return CWT.Decode(Content);
        }

        /// <summary>
        /// Returns the signed data of the object, to be verifyed comparing the signature and the public key of the signer
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public byte[] GetSignedData()
        {
            if (Signature == null)
            {
                throw new Exception("Object is not signed");
            }

            CBORObject obj = CBORObject.NewArray();
            obj.Add(ContextString);
            obj.Add(ProtectedAttributesEncoding);
            obj.Add(ExternalData);
            if (Content != null)
            {
                obj.Add(Content);
            }
            else
            {
                obj.Add(null);
            }
            byte[] signedData = obj.EncodeToBytes();

            return signedData;
        }

        /// <summary>
        /// Get the signature algorithm used to sign the data
        /// </summary>
        /// <returns></returns>
        private CoseSignatureAlgorithm GetSignatureAlgorithm()
        {
            // First find out which algorithm to use by searching for the algorithm ID in the protected attributes.
            CBORObject registeredAlgorithmCbor = ProtectedAttributes[HeaderParameterKey.ALG];
            if (registeredAlgorithmCbor == null)
            {
                throw new Exception("No algorithm ID stored in protected attributes - cannot sign");
            }

            var registeredAlgorithm = CoseSignatureAlgorithmParser.GetAlgorithm(registeredAlgorithmCbor);
            return registeredAlgorithm;
        }

        /// <summary>
        /// Get the signature of te signed data
        /// </summary>
        /// <returns></returns>
        public byte[]? GetSignature()
        {
            return Signature;
        }


#if NET452
        /// <summary>
        /// Verifies the signature of the COSE_Sign1 object.
        /// Note: This method only verifies the signature. Not the payload.
        /// </summary>
        /// <param name="publicKeyData">the key to use when verifying the signature</param>
        /// <returns></returns>
        /// <exception cref="Exception">for signature verification errors</exception>
        public void VerifySignature(ITrustedCertificateData publicKeyData)
        {
            var signedData = GetSignedData();
            var signature = GetSignature();
            var signatureAlgorithm = GetSignatureAlgorithm();

            if (signature == null)
            {
                throw new Exception("Object is not signed");
            }

            // Signature check
            if (signatureAlgorithm.IsECDsaAlgorithm())
            {
                var ec = publicKeyData.GetECParameters();
                if (ec == null)
                    throw new Exception($"Certificate {publicKeyData.Kid} does not have ECDsa Public Key parameters");

                var oids = ECNamedCurveTable.Names.Cast<string>()
                    .Select(r => ECNamedCurveTable.GetOid(r));

                var curveName = oids.FirstOrDefault(r => r.Id == ec.Curve);

                var x9 = ECNamedCurveTable.GetByOid(curveName);
                var point = x9.Curve.CreatePoint(new BigInteger(1, ec.X), new BigInteger(1, ec.Y));
                var dParams = new ECDomainParameters(x9);
                var pubKeyParameters = new ECPublicKeyParameters(point, dParams);

                var keyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKeyParameters).GetEncoded();
                var pubkey = PublicKeyFactory.CreateKey(keyBytes);

                // If ECDSA, convert signature in DER format
                signature = ToDerSignature(signature);

                // Check signature
                var verifier = SignerUtilities.GetSigner(signatureAlgorithm.GetAlgorithmName());
                verifier.Init(false, pubkey);
                verifier.BlockUpdate(signedData, 0, signedData.Length);
                var result = verifier.VerifySignature(signature);

                if (!result)
                    throw new Exception($"Signature validation failed");
            }
            else if (signatureAlgorithm.IsRsaAlgorithm())
            {
                var rsaParameters = publicKeyData.GetRSAParameters();
                if (rsaParameters == null)
                    throw new Exception($"Certificate {publicKeyData.Kid} does not have RSA Public Key parameters");

                var pubKeyParameters = new RsaKeyParameters(false, new BigInteger(1, rsaParameters.Modulus), new BigInteger(1, rsaParameters.Exponent));
                var keyBytes = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pubKeyParameters).GetEncoded();
                var pubkey = PublicKeyFactory.CreateKey(keyBytes);

                // Check signature
                var verifier = SignerUtilities.GetSigner(signatureAlgorithm.GetAlgorithmName());
                verifier.Init(false, pubkey);
                verifier.BlockUpdate(signedData, 0, signedData.Length);
                var result = verifier.VerifySignature(signature);

                if (!result)
                    throw new Exception($"Signature validation failed");
            }
            else
            {
                throw new NotSupportedException($"Signature algorithm not supported (CBOR value: {signatureAlgorithm})");
            }
        }
#else
        /// <summary>
        /// Verifies the signature of the COSE_Sign1 object.
        /// Note: This method only verifies the signature. Not the payload.
        /// </summary>
        /// <param name="publicKeyData">the key to use when verifying the signature</param>
        /// <returns></returns>
        /// <exception cref="DgcSignatureValidationException">for signature verification errors</exception>
        public void VerifySignature(ITrustedCertificateData publicKeyData)
        {

            var signedData = GetSignedData();
            var signature = GetSignature();
            var signatureAlgorithm = GetSignatureAlgorithm();

            if (signature == null)
            {
                throw new Exception("Object is not signed");
            }

            // Signature check
            if (signatureAlgorithm.IsECDsaAlgorithm())
            {
                var ec = publicKeyData.GetECParameters();
                if (ec == null)
                    throw new Exception($"Certificate {publicKeyData.Kid} does not have ECDsa Public Key parameters");

                var parameters = new ECParameters
                {
                    Curve = string.IsNullOrEmpty(ec.Curve) ?
                        ECCurve.CreateFromFriendlyName(ec.CurveFriendlyName) :
                        ECCurve.CreateFromValue(ec.Curve),
                    Q = new ECPoint() { X = ec.X, Y = ec.Y }
                };

                var ecdsa = ECDsa.Create(parameters);


#if NET5_0_OR_GREATER

                    // For ECDSA, convert the signature according to section 8.1 of RFC8152.
                    // Available from net5.0
                    var derSignature = ToDerSignature(signature);

                    var result = ecdsa.VerifyData(signedData, derSignature,
                        signatureAlgorithm.GetHashAlgorithmName(),
                        DSASignatureFormat.Rfc3279DerSequence);
#else
                // Using the signature as is for netstandard2.0
                // There is no overload for verify DER encoded signatures

                var result = ecdsa.VerifyData(signedData, signature,
                    signatureAlgorithm.GetHashAlgorithmName());
#endif


                if (!result)
                    throw new Exception($"Signature validation failed");
            }
            else if (signatureAlgorithm.IsRsaAlgorithm())
            {
                var rsaParameters = publicKeyData.GetRSAParameters();
                if (rsaParameters == null)
                    throw new Exception($"Certificate {publicKeyData.Kid} does not have RSA Public Key parameters");

                var parameters = new RSAParameters
                {
                    Exponent = rsaParameters.Exponent,
                    Modulus = rsaParameters.Modulus,
                };


#if NET5_0_OR_GREATER
                    var rsa = RSA.Create(parameters);
#else
                // The default RSA class in .net framework (legacy) does not support PSS padding
                var rsa = new RSACng();
                rsa.ImportParameters(parameters);
#endif
                var result = rsa.VerifyData(signedData, signature,
                    signatureAlgorithm.GetHashAlgorithmName(), RSASignaturePadding.Pss);

                if (!result)
                    throw new Exception($"Signature validation failed");
            }
            else
            {
                throw new NotSupportedException($"Signature algorithm not supported (CBOR value: {signatureAlgorithm})");
            }

        }
#endif



#if NET452

        /// <summary>
        /// Given a signature according to section 8.1 in RFC8152 its corresponding DER encoding is returned.
        /// </summary>
        /// <param name="signature">the ECDSA signature</param>
        /// <returns>DER-encoded signature</returns>
        private static byte[] ToDerSignature(byte[] signature)
        {
            int len = signature.Length / 2;
            byte[] r = new byte[len];
            byte[] s = new byte[len];
            Array.Copy(signature, r, len);
            Array.Copy(signature, len, s, 0, len);

            var seq = new List<byte[]>();
            seq.Add(ASN1.ToUnsignedInteger(r));
            seq.Add(ASN1.ToUnsignedInteger(s));

            return ASN1.ToSequence(seq);
        }
#endif

#if NET5_0_OR_GREATER
        /// <summary>
        /// Given a signature according to section 8.1 in RFC8152 its corresponding DER encoding is returned.
        /// </summary>
        /// <param name="signature">the ECDSA signature</param>
        /// <returns>DER-encoded signature</returns>
        private static byte[] ToDerSignature(byte[] signature)
        {
            var span = new Span<byte>(signature);
            int len = signature.Length / 2;

            var writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteIntegerUnsigned(span.Slice(0, len).TrimStart((byte)0));
            writer.WriteIntegerUnsigned(span.Slice(len).TrimStart((byte)0));
            writer.PopSequence();
            var result = writer.Encode();

            return result;
        }
#endif

    }

}
