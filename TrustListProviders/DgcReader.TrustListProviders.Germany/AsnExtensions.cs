using System;

#if NET452
using System.Collections.Generic;
using DgcReader.Cwt.Cose;
#else
using System.Formats.Asn1;
#endif


// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.TrustListProviders.Germany
{
    public static class AsnExtensions
    {
#if NET452
        /// <summary>
        /// Given a signature according to section 8.1 in RFC8152 its corresponding DER encoding is returned.
        /// </summary>
        /// <param name="signature">the ECDSA signature</param>
        /// <returns>DER-encoded signature</returns>
        public static byte[] ToDerSignature(byte[] signature)
        {
            int len = signature.Length / 2;
            byte[] r = new byte[len];
            byte[] s = new byte[len];
            Array.Copy(signature, r, len);
            Array.Copy(signature, len, s, 0, len);

            List<byte[]> seq = new List<byte[]>();
            seq.Add(ASN1.ToUnsignedInteger(r));
            seq.Add(ASN1.ToUnsignedInteger(s));

            return ASN1.ToSequence(seq);
        }
#endif

#if NET47_OR_GREATER || NETSTANDARD2_0 || NET5_0_OR_GREATER
        /// <summary>
        /// Given a signature according to section 8.1 in RFC8152 its corresponding DER encoding is returned.
        /// </summary>
        /// <param name="signature">the ECDSA signature</param>
        /// <returns>DER-encoded signature</returns>
        public static byte[] ToDerSignature(byte[] signature)
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

#if NET47_OR_GREATER || NETSTANDARD2_0
        private static Span<byte> TrimStart(this Span<byte> span, byte trimElement)
        {
            int i = 0;
            for(i = 0; i < span.Length; i++)
            {
                if (span[i] != trimElement)
                    break;
            }

            if (i == 0)
                return span;

            return span.Slice(i, span.Length - i);
        }
#endif
    }
}
