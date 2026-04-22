using YamlDotNet.Serialization;

namespace KubeNavigator;

internal static class YamlSerializerFactory
{
    public static ISerializer Serializer { get; } = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public static IDeserializer Deserializer { get; } = new DeserializerBuilder().Build();
}
