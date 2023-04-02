namespace DgcReader.Interfaces.Deserializers;

/// <summary>
/// Allow a service to specify a custom deserizlizer that will be registered automatically
/// when the service is injected to <see cref="DgcReaderService"/>
/// </summary>
public interface ICustomDeserializerDependentService
{
    /// <summary>
    /// Returns an instance of the custom deserializer
    /// </summary>
    /// <returns></returns>
    IDgcDeserializer GetCustomDeserializer();
}
