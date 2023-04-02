using System;
using System.Collections.Generic;
using PeterO.Cbor;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0
// Based on the original work of Myndigheten för digital förvaltning (DIGG)

namespace DgcReader.Cwt;

/// <summary>
/// A representation of a CWT (CBOR Web Token) according to <see href="https://tools.ietf.org/html/rfc8392">RFC 8392</see>.
/// </summary>
public class CWT
{
    /// <summary>
    /// HCERT message tag. 
    /// </summary>
    public static int HCERT_CLAIM_KEY = -260;

    /// <summary>
    /// The message tag for eu_hcert_v1 that is added under the HCERT claim.
    /// </summary>
    public static int EU_HCERT_V1_MESSAGE_TAG = 1;

    /// <summary>
    /// The CBOR CWT message tag.
    /// </summary>
    public static int MESSAGE_TAG = 61;

    /// <summary>
    /// For handling of DateTime.
    /// </summary>
    private static CBORDateTimeConverter dateTimeConverter = new CBORDateTimeConverter();

    /// <summary>
    /// The CBOR representation of the CWT.
    /// </summary>
    private CBORObject CwtObject;


    /// <summary>
    /// Constructor creating an empty CWT.
    /// </summary>
    public CWT()
    {
        CwtObject = CBORObject.NewMap();
    }

    /// <summary>
    /// Constructor creating a CWT from a supplied encoding.
    /// </summary>
    /// <param name="data">the encoding</param>
    /// <exception cref="CBORException">if the supplied encoding is not a valid CWT</exception>
    public CWT(byte[] data)
    {
        CBORObject obj = CBORObject.DecodeFromBytes(data);
        if (obj.Type != CBORType.Map)
        {
            throw new CBORException("Not a valid CWT");
        }
        CwtObject = obj;
    }


    /// <summary>
    /// Decodes the supplied data into a Cwt object.
    /// </summary>
    /// <param name="data">the encoded data</param>
    /// <returns>a Cwt object</returns>
    /// <exception cref="CBORException">if the supplied encoding is not a valid CWT</exception>
    public static CWT Decode(byte[] data)
    {
        return new CWT(data);
    }

    /// <summary>
    /// Gets the binary representation of the CWT.
    /// </summary>
    /// <returns>a byte array</returns>
    public byte[] Encode()
    {
        return CwtObject.EncodeToBytes();
    }

    /// <summary>
    /// Gets the "iss" (issuer) claim.
    /// </summary>
    /// <returns>The issuer value, or null</returns>
    public string? GetIssuer()
    {
        CBORObject cbor = CwtObject[1];
        if (cbor == null)
        {
            return null;
        }
        return cbor.AsString();
    }

    /// <summary>
    /// Gets the "sub" (subject) claim.
    /// </summary>
    /// <returns>The subject value, or null</returns>
    public string? GetSubject()
    {
        CBORObject cbor = CwtObject[2];
        if (cbor == null)
        {
            return null;
        }
        return cbor.AsString();
    }

    /// <summary>
    /// Gets the values for the "aud" claim
    /// </summary>
    /// <returns>The value, or null</returns>
    public List<string>? GetAudience()
    {
        CBORObject aud = CwtObject[3];
        if (aud == null)
        {
            return null;
        }
        if (aud.Type == CBORType.Array)
        {
            ICollection<CBORObject> values = aud.Values;
            List<string> audList = new List<string>();
            foreach (CBORObject o in values)
            {
                audList.Add(o.AsString());
            }
            return audList;
        }
        else
        {
            return new List<string> { aud.AsString() };
        }
    }

    /// <summary>
    /// Gets the value of the "exp" (expiration time) claim.
    /// </summary>
    /// <returns>the instant, or null</returns>
    public DateTime? GetExpiration()
    {
        return dateTimeConverter.FromCBORObject(CwtObject[4]);
    }

    /// <summary>
    /// Gets the value of the "nbf" (not before) claim.
    /// </summary>
    /// <returns>the instant, or null</returns>
    public DateTime? GetNotBefore()
    {
        return dateTimeConverter.FromCBORObject(CwtObject[5]);
    }

    /// <summary>
    /// Gets the value of the "iat" (issued at) claim.
    /// </summary>
    /// <returns>the instant, or null</returns>
    public DateTime? GetIssuedAt()
    {
        return dateTimeConverter.FromCBORObject(CwtObject[6]);
    }

    /// <summary>
    /// Gets the value of the "cti" (CWT ID) claim.
    /// </summary>
    /// <returns>the ID, or null</returns>
    public byte[]? GetCwtId()
    {
        CBORObject cbor = CwtObject[7];
        if (cbor == null)
        {
            return null;
        }
        return cbor.GetByteString();
    }

    /// <summary>
    /// Gets the binary representation of a EU HCERT v1 structure.
    /// </summary>
    /// <returns>the binary representation of a EU HCERT or null</returns>
    public byte[]? GetDgcV1()
    {
        CBORObject hcert = CwtObject[HCERT_CLAIM_KEY];
        if (hcert == null)
        {
            return null;
        }
        return hcert[EU_HCERT_V1_MESSAGE_TAG].EncodeToBytes();
    }

    /// <summary>
    /// Gets the claim identified by claimKey
    /// </summary>
    /// <param name="claimKey">the claim key</param>
    /// <returns>the claim value (in its CBOR binary encoding), or null</returns>
    public CBORObject GetClaim(int claimKey)
    {
        return CwtObject[claimKey];
    }

    /// <summary>
    /// Gets the claim identified by claimKey
    /// </summary>
    /// <param name="claimKey">the claim key</param>
    /// <returns>the claim value (in its CBOR binary encoding), or null</returns>
    public CBORObject GetClaim(string claimKey)
    {
        return CwtObject[claimKey];
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return CwtObject.ToString();
    }
}
