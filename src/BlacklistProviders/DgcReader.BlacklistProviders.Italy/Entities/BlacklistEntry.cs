// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.BlacklistProviders.Italy.Entities;

/// <summary>
/// Entry of the blacklist, containing the hashed value of a blacklisted UCVI
/// </summary>
public class BlacklistEntry
{
    /// <summary>
    /// The Sha256 hash of a UCVI
    /// </summary>
    public string HashedUCVI { get; set; } = "";
}
