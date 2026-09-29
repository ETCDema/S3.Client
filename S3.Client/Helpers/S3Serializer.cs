using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace S3.Client.Services;

internal static class S3Serializer<T>
    where T : class
{
    private static readonly XmlSerializer _SERIALIZER	= new(typeof(T));

    public static T Deserialize(byte[] xmlText)
    {
        using var stream = new MemoryStream(xmlText);

        return (T)_SERIALIZER.Deserialize(stream)!;
    }

    public static bool TryDeserialize(byte[] xmlText, [NotNullWhen(true)] out T? result)
    {
        using var reader = new MemoryStream(xmlText);

        try
        {
            result				= (T)_SERIALIZER.Deserialize(reader)!;

            return true;
        }
        catch
        {
            result = null;

            return false;
        }
    }
}