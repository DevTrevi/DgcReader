using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using Newtonsoft.Json;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.Models.Rules
{

    public class RulesIdentifiers : ValueSetBase
    {

        [JsonProperty("identifiers")]
        public IEnumerable<RuleIdentifier> Identifiers { get; set; }
    }
}
