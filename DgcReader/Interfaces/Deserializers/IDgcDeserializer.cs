using GreenpassReader.Models;

namespace DgcReader.Interfaces.Deserializers
{
    /// <summary>
    /// Deserializer for DGC objects
    /// </summary>
    public interface IDgcDeserializer
    {
        /// <summary>
        /// If specified, restrict the usage of this deserializer to certificates issed by listed countries
        /// </summary>
        /// <returns></returns>
        string[]? SupportedCountryCodes { get; }

        /// <summary>
        /// Deserialize the <see cref="EuDGC"/> from the provided Json
        /// </summary>
        /// <param name="json">The raw json extracted from the CBOR object</param>
        /// <param name="issuerCountry">The issuer of the certificate, read from the signature</param>
        /// <returns></returns>
        EuDGC? DeserializeDgc(string json, string? issuerCountry);
    }
}
