using DgcReader.RuleValidators.Germany.CovpassDgcCertlogic.Data;
using System;
using System.Collections.Generic;

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Germany.CovpassDgcCertlogic
{

    public enum Result
    {
        PASSED,
        FAIL,
        OPEN
    }

    public class ValidationResult
    {
        public RuleEntry Rule { get; set; }
        public Result Result { get; set; }
        public string Current { get; set; }

        public IEnumerable<Exception>? ValidationErrors { get; set; }

        public override string ToString()
        {
            return $"{Result} {Rule} - Current: {Current}";
        }
    }
}
