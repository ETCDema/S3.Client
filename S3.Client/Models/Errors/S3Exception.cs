using System.Net;

namespace S3.Client.Models.Errors;

public sealed class S3Exception: Exception, IException
{
    private readonly S3Error? _error;

    public S3Exception(string message, HttpStatusCode statusCode)
        : base(message)
	{
		HttpStatusCode          = statusCode;
	}

    public S3Exception(string message, Exception innerException, HttpStatusCode statusCode)
        : base(message, innerException)
    {
        if (innerException is S3Exception s3Exception)
        {
            _error				= s3Exception.Error;
        }

		HttpStatusCode          = statusCode;
	}

	public S3Exception(S3Error error, HttpStatusCode statusCode)
        : this(error.Message, statusCode)
    {
        _error					= error;
    }

	public HttpStatusCode HttpStatusCode { get; }

    public S3Error? Error		=> _error;

    public bool IsTransient		=> HttpStatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable; // 500 || 503
}