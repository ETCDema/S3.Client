namespace S3.Client.Models;

public sealed class GetPresignedUrlRequest
{
    public GetPresignedUrlRequest(string method, string bucketName, string objectKey, TimeSpan expiresIn)
    {
        ArgumentException.ThrowIfNullOrEmpty(method);
        ArgumentException.ThrowIfNullOrEmpty(bucketName);

        Method					= method;
        BucketName				= bucketName;
        Key						= objectKey;
        ExpiresIn				= expiresIn;
    }

    public string Method		{ get; }

    public string BucketName	{ get; }

    public string Key			{ get; }

    public TimeSpan ExpiresIn	{ get; }

    internal string GetUrl(string host) => $"{host}/{BucketName}/{Key}";
}