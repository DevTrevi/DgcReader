using DgcReader.Exceptions;
using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

namespace DgcReader.RuleValidators.Italy.Exceptions
{
    [Serializable]
    public class DgcRulesValidationException : DgcException
    {
        public DgcRulesValidationException()
        {

        }

        public DgcRulesValidationException(string message) : base(message)
        {
        }

        public DgcRulesValidationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }
    }

}
