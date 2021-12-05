using Newtonsoft.Json;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data
{
    public class RuleEntryDescription
    {
        [JsonProperty("lang")]
        public string Language { get; set; }

        [JsonProperty("desc")]
        public string Descrption { get; set; }

        public override string ToString()
        {
            return $"{Language}: {Descrption}";
        }
    }
}
