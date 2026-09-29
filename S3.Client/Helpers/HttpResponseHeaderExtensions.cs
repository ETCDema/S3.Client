using System.Net.Http.Headers;

namespace S3.Client.Services;

internal static class HttpResponseHeaderExtensions
{
    public static string? TryGetValue(this HttpResponseHeaders headers, string name)
    {
        return headers.NonValidated.TryGetValues(name, out var values) ? values.ToString() : null;
    }
}