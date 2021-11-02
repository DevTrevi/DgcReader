// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.DgcTestData.Test.Models
{
    /// <summary>
    /// Keys for <see cref="TestEntry.ExpectedResults"/>
    /// </summary>
    public static class ExpectedResultsKeys
    {
        /// <summary>
        /// Load the picture and extract the prefixed BASE45content.
        /// Mandatory: PREFIX , 2DCode
        /// </summary>
        public const string EXPECTEDPICTUREDECODE = "EXPECTEDPICTUREDECODE";

        /// <summary>
        /// Load Prefix Object from RAW Content and remove the prefix. Validate against the BASE45 raw content.
        /// Mandatory: PREFIX, BASE45
        /// </summary>
        public const string EXPECTEDUNPREFIX = "EXPECTEDUNPREFIX";

        /// <summary>
        /// Decode the BASE45 RAW Content and validate the COSE content against the RAW content.
        /// Mandatory: BASE45, COSE
        /// </summary>
        public const string EXPECTEDB45DECODE = "EXPECTEDB45DECODE";

        /// <summary>
        /// Check the EXP Field for expiring against the VALIDATIONCLOCK time.
        /// Mandatory: COSE, VALIDATIONCLOCK
        /// </summary>
        public const string EXPECTEDEXPIRATIONCHECK = "EXPECTEDEXPIRATIONCHECK";

        /// <summary>
        /// Verify the signature of the COSE Object against the JWK Public Key.
        /// Mandatory: COSE, JWK
        /// </summary>
        public const string EXPECTEDVERIFY = "EXPECTEDVERIFY";

        /// <summary>
        /// Extract the CBOR content and validate the CBOR content against the RAW CBOR content field. See note 2 below.
        /// MAndatory: COSE, CBOR
        /// </summary>
        public const string EXPECTEDDECODE = "EXPECTEDDECODE";

        /// <summary>
        /// Transform CBOR into JSON and validate against the RAW JSON content. See note 3 below.
        /// Mandatory: CBOR, JSON
        /// </summary>
        public const string EXPECTEDVALIDJSON = "EXPECTEDVALIDJSON";

        /// <summary>
        /// Validate the extracted JSON against the schema defined in the test context.
        /// Mandatory: CBOR, JSON
        /// </summary>
        public const string EXPECTEDSCHEMAVALIDATION = "EXPECTEDSCHEMAVALIDATION";

        /// <summary>
        /// The value given in COMPRESSED has to be decompressed by zlib and must match to the value given in COSE
        /// MAndatory: 
        /// </summary>
        public const string EXPECTEDCOMPRESSION = "EXPECTEDCOMPRESSION";


        /// <summary>
        /// Data from input in COSE can be verified, and the key usage (defined by the OIDs) from certificate matches the content (i.e. it is a test statement, vaccination statement, or recovery statement) 
        /// </summary>
        public const string EXPECTEDKEYUSAGE = "EXPECTEDKEYUSAGE";


        /// <summary>
        /// Load RAW File and load JSON Object, validate against the referenced JSON schema in the test context(SCHEMA field).
        /// Mandatory: JSON
        /// </summary>
        public const string EXPECTEDVALIDOBJECT = "EXPECTEDVALIDOBJECT";

        /// <summary>
        /// Create CBOR from JSON Object. Validate against the CBOR content in the RAW File. See note 2 below.
        /// Mandatory: JSON, CBOR
        /// </summary>
        public const string EXPECTEDENCODE = "EXPECTEDENCODE";

    }
}
