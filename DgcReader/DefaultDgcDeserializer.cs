// Copyright (c) 2021 Davide Trevisan
// Licensed under the Apache License, Version 2.0

using DgcReader.Interfaces.Deserializers;
using GreenpassReader.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Globalization;

namespace DgcReader
{
    /// <summary>
    /// Default implementation of <see cref="IDgcDeserializer"/>
    /// This will deserialize standard European Digital Green Certificates
    /// </summary>
    public class DefaultDgcDeserializer : IDgcDeserializer
    {
        /// <inheritdoc/>
        public DefaultDgcDeserializer()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                DateParseHandling = DateParseHandling.None,
                Converters = {
                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal },
                },
            };
        }

        /// <summary>
        /// Serializer settings used by <see cref="DeserializeDgc(string, string)"/>
        /// </summary>
        public virtual JsonSerializerSettings SerializerSettings { get; }

        /// <inheritdoc/>
        public virtual string[]? SupportedCountryCodes => null;

        /// <inheritdoc/>
        public virtual EuDGC? DeserializeDgc(string json, string? issuerCountry)
        {
            return JsonConvert.DeserializeObject<EuDGC>(json, SerializerSettings);
        }

    }
}