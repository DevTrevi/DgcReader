using GreenpassReader.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0


namespace DgcReader.DgcTestData.Test.Models
{
    public class TestEntry
    {

        [JsonProperty("JSON")]
        public EuDGC Json { get; set; }
        [JsonProperty("CBOR")]
        public string CBOR { get; set; }
        [JsonProperty("COSE")]
        public string COSE { get; set; }
        [JsonProperty("COMPRESSED")]
        public string COMPRESSED { get; set; }
        [JsonProperty("BASE45")]
        public string BASE45 { get; set; }
        [JsonProperty("PREFIX")]
        public string PREFIX { get; set; }
        [JsonProperty("2DCODE")]
        public string QRCode { get; set; }

        [JsonProperty("TESTCTX")]
        public TestContext TestContext { get; set; }

        [JsonProperty("EXPECTEDRESULTS")]
        public IDictionary<string, bool> ExpectedResults { get; set; }


        [JsonIgnore]
        public string Filename { get; set; }
    }
}
