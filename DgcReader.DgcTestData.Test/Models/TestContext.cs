using Newtonsoft.Json;
using System;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0


namespace DgcReader.DgcTestData.Test.Models
{
    public class TestContext
    {
        [JsonProperty("VERSION")]
        public string Version { get; set; }
        [JsonProperty("SCHEMA")]
        public string Schema { get; set; }
        [JsonProperty("CERTIFICATE")]
        public byte[] Certificate { get; set; }
        [JsonProperty("VALIDATIONCLOCK")]
        public DateTimeOffset? ValidationClock { get; set; }
        [JsonProperty("DESCRIPTION")]
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Description} - Schema {Schema} v {Version} - Clock: {ValidationClock}";
        }
    }
}
