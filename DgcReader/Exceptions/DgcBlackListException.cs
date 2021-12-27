using DgcReader.Models;
using System;
using System.Runtime.Serialization;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace DgcReader.Exceptions
{
    [Serializable]
    public class DgcBlackListException : DgcException
    {
        /// <summary>
        /// The Blacklist validation result
        /// </summary>
        public BlacklistValidationResult Result { get; }

        public DgcBlackListException(string message,
            BlacklistValidationResult result) :
            base(message)
        {
            Result = result;
        }
#pragma warning disable CS8618
        public DgcBlackListException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#pragma warning restore
    }
}
