using S3.Client.Helpers;
using S3.Client.Models;
using S3.Client.Models.Security;

namespace S3.Client.Services;

// TODO: Можно убрать в S3Client
internal static class S3Helper
{
    public static string GetPresignedUrl(S3Service service, GetPresignedUrlRequest request, IS3Credential credential)
    {
        return GetPresignedUrl(service, request, credential, DateTime.UtcNow);
    }

    public static string GetPresignedUrl(S3Service service, GetPresignedUrlRequest request, IS3Credential credential, DateTime utcNow)
    {
        // TODO: support version querystring

        return SignerV4.GetPresignedUrl(
            credential  : credential, 
            scope       : new CredentialScope(DateOnly.FromDateTime(utcNow), service.Region, "s3"),
            date        : utcNow,
            expires     : request.ExpiresIn, 
            method      : _getHttpMethod(request.Method),
            requestUri  : new Uri(request.GetUrl(service.Endpoint)),
            payloadHash : S3Client._UNSIGNED_PAYLOAD
		);
    }

    private static readonly HttpMethod _MOVE = new ("MOVE");

    private static HttpMethod _getHttpMethod(string name) => name switch
    {
        "GET"  => HttpMethod.Get,
        "POST" => HttpMethod.Post,
        "MOVE" => _MOVE,                     // Used by Wasabi
        _      => new HttpMethod(name)
    };
}