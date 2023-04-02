using PeterO.Cbor;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0
// Based on the original work of Myndigheten för digital förvaltning (DIGG)

namespace DgcReader.Cwt.Cose;

/// <summary>
/// Representation of COSE header parameter keys.
/// Only those relevant for our use case is represented.
/// </summary>
public class HeaderParameterKey
{
    /// <summary>
    /// Algorithm used for security processing.
    /// </summary>
    public static readonly CBORObject ALG = CBORObject.FromObject(1);

    /// <summary>
    /// Critical headers to be understood.
    /// </summary>
    public static readonly CBORObject CRIT = CBORObject.FromObject(2);

    /// <summary>
    /// This parameter is used to indicate the content type of the data in the payload or ciphertext fields.
    /// </summary>
    public static readonly CBORObject CONTENT_TYPE = CBORObject.FromObject(3);

    /// <summary>
    /// This parameter identifies one piece of data that can be used as input to find the needed cryptographic key.
    /// </summary>
    public static readonly CBORObject KID = CBORObject.FromObject(4);
}
