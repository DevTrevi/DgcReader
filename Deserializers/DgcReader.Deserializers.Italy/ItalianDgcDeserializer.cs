using DgcReader.Deserializers.Italy.Models;
using DgcReader.Interfaces.Deserializers;
using GreenpassReader.Models;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace DgcReader.Deserializers.Italy
{
    /// <summary>
    /// Italian implementation of <see cref="IDgcDeserializer"/>
    /// This deserializer will deserialize certificates issued by Italy as <see cref="ItalianDGC" />, including exemptions entries
    /// </summary>
    public class ItalianDgcDeserializer : DefaultDgcDeserializer
    {
        /// <inheritdoc/>
        public override string[]? SupportedCountryCodes => new[] { "IT" };

        /// <inheritdoc/>
        public override EuDGC? DeserializeDgc(string json, string? issuerCountry)
        {
            if (!SupportedCountryCodes.Contains(issuerCountry))
            {

                return base.DeserializeDgc(json, issuerCountry);
            }

            return JsonConvert.DeserializeObject<ItalianDGC>(json, SerializerSettings);
        }
    }
}
