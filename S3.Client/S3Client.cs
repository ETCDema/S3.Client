using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text;

using S3.Client.Const;
using S3.Client.Helpers;
using S3.Client.Models;
using S3.Client.Models.Errors;
using S3.Client.Models.Security;
using S3.Client.Services;

namespace S3.Client;

public class S3Client
{
    public const string Namespace = "http://s3.amazonaws.com/doc/2006-03-01/";
	internal static readonly string _UNSIGNED_PAYLOAD	= "UNSIGNED-PAYLOAD";

	private readonly S3Service _service;
	private readonly IS3Credential _credential;
	private readonly HttpClient _httpClient;
	private readonly string _host;

	private readonly ConcurrentDictionary<string, S3Bucket> _buckets;

	public S3Client(S3Service service, HttpClient? httpClient = null)
    {
        ArgumentNullException.ThrowIfNull(service);

		_service				= service;
		_credential				= service.Credential;
		_host                   = service.Host;
		_buckets				= [];

		_httpClient             = httpClient ?? new HttpClient(new SocketsHttpHandler
		{
			ConnectTimeout						= TimeSpan.FromSeconds(5),
			UseCookies							= false,
			EnableMultipleHttp2Connections		= true,
			KeepAlivePingPolicy					= HttpKeepAlivePingPolicy.Always,
		})
		{
			DefaultRequestHeaders = {
				{ "User-Agent", "S3TinyClient/1.0" }
			}
		};
	}

    public S3Client WithTimeout(TimeSpan timeout)
    {
        _httpClient.Timeout		= timeout;

        return this;
    }

	public S3Service Service	{ get { return _service; } }

	public S3Bucket GetBucket(string name)
	{
		return _buckets.GetOrAdd(name, name => new S3Bucket(this, name));
	}

	public string GetPresignedUrl(GetPresignedUrlRequest request)
	{
		return S3Helper.GetPresignedUrl(_service, request, _credential);
	}

	protected internal async Task<HttpResponseMessage> Send(S3Request request, CancellationToken cancellationToken)
    {
		request.BuildRequestUri(_host);
        await Sign(request).ConfigureAwait(false);

		request.BeforeSend?.Invoke(request);

		var response			= await _httpClient.SendAsync(request, request.CompletionOption, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.NotModified) return response;

        if (!response.IsSuccessStatusCode)
        {
            using (response)
            {
                await ThrowException(response).ConfigureAwait(false);
            }
        }

        return response;
    }

	protected internal async Task<TResult> Send<TResult>(S3Request request, CancellationToken cancellationToken)
		where TResult : class
	{
		using var response		= await Send(request, cancellationToken).ConfigureAwait(false);
		var content				= await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
		return S3Serializer<TResult>.Deserialize(content);
	}

	protected async ValueTask Sign(HttpRequestMessage request)
	{
		if (_credential.ShouldRenew)
		{
			await _credential.Renew().ConfigureAwait(false);
		}

		var date				= DateTimeOffset.UtcNow;

		request.Headers.Host	= request.RequestUri!.Host;
		request.Headers.Date	= date;

		if (_credential.SecurityToken is not null)
		{
			request.Headers.Add(S3HeaderNames.SecurityToken, _credential.SecurityToken);
		}

		request.Headers.Add(S3HeaderNames.Date, date.UtcDateTime.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));

		SignerV4.Sign(_credential, scope: _getCredentialScope(request), request: request);
	}

	private CredentialScope _getCredentialScope(HttpRequestMessage httpRequest)
	{
		if (httpRequest.Headers.Date is null)
		{
			throw new Exception("Headers.Date must be set");
		}

		var date				= httpRequest.Headers.Date.Value.UtcDateTime;

		return new CredentialScope(DateOnly.FromDateTime(date), _service.Region, _service.Type);
	}

	protected static async Task ThrowException(HttpResponseMessage response)
	{
		if (response.StatusCode is HttpStatusCode.NotFound)
		{
			var key				= response.RequestMessage!.RequestUri!.AbsolutePath;

			if (key.Length > 0 && key[0] is '/')
				key				= key[1..];

			throw new S3Exception($"{key} not found", response.StatusCode);
		}

		var responseBytes		= await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

		// Wasabi returns a non-standard ErrorResponse
		if (responseBytes.AsSpan().IndexOf("<ErrorResponse"u8) > -1 && S3Serializer<S3ErrorResponse>.TryDeserialize(responseBytes, out var wasabiError))
		{
			throw new S3Exception(wasabiError.Error, response.StatusCode);
		}
		
		if (responseBytes.AsSpan().IndexOf("<Error>"u8) > -1 && S3Serializer<S3Error>.TryDeserialize(responseBytes, out var error))
		{
			throw new S3Exception(error!, response.StatusCode);
		}

		throw new S3Exception($"Unexpected S3 error. status = {response.StatusCode} | {Encoding.UTF8.GetString(responseBytes)}", response.StatusCode);
	}
}