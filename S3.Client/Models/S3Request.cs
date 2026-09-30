using System.Net;
using System.Text;
using System.Text.Encodings.Web;

using S3.Client.Const;

namespace S3.Client.Models;

/// <summary>
/// Запрос к S3
/// </summary>
public class S3Request : HttpRequestMessage
{
	private readonly Dictionary<string, string>? _parameters;

	internal S3Request(HttpMethod method, S3Bucket bucket, string? objectName, string? versionId = null, string? actionName = default, Dictionary<string, string>? parameters = null)
    {
        ArgumentNullException.ThrowIfNull(bucket);

		Version                 = HttpVersion.Version20;

		Method                  = method;
		Bucket					= bucket;
		BucketName              = bucket.Name;
        ObjectName				= objectName;
		VersionId				= versionId;
		ActionName				= actionName;
		_parameters             = parameters;
	}

	public S3Bucket Bucket		{ get; }

	public string BucketName	{ get; }

    public string? ObjectName	{ get; }

	public string? ActionName	{ get; }

	public string? VersionId	{ get; }

	public HttpCompletionOption CompletionOption { get; set; } = HttpCompletionOption.ResponseHeadersRead;

	public void SetStorageClass(StorageClass storageClass)
	{
		Headers.Add(S3HeaderNames.StorageClass, storageClass.Name);
	}

	/// <summary>
	/// Обработчик, который будет вызван непосредственно перед отправкой
	/// </summary>
	internal Action<S3Request>? BeforeSend		{ get; set; }

	/// <summary>
	/// Инициализировать RequestUri запроса перед добавлением подписи
	/// </summary>
	/// <param name="host"></param>
	internal void BuildRequestUri(string host)
	{
		ArgumentException.ThrowIfNullOrEmpty(host);

		// https://{bucket}.s3.amazonaws.com/{key}

		var urlBuilder			= new StringBuilder();

		urlBuilder.Append(host);
		urlBuilder.Append('/');
		urlBuilder.Append(BucketName);

		// s3.dualstack.{region.Name}.amazonaws.com

		if (ObjectName is not null)
		{
			urlBuilder.Append('/');
			urlBuilder.Append(ObjectName);
		}

		var startQS				= true;
		if (!string.IsNullOrEmpty(ActionName))
		{
			urlBuilder.Append(ActionName);
			startQS				= false;
		}

		if (VersionId is { Length: > 0 })
		{
			urlBuilder.Append(startQS ? '?' : '&');
			urlBuilder.Append("versionId=");
			urlBuilder.Append(VersionId);
			startQS				= false;
		}

		if (0<_parameters?.Count)
		{
			foreach (var (k, v) in _parameters)
			{
				if (startQS)
				{
					urlBuilder.Append('?');
					startQS     = false;
				} else
				{
					urlBuilder.Append('&');
				}
				urlBuilder.Append(k);

				urlBuilder.Append('=');
				urlBuilder.Append(UrlEncoder.Default.Encode(v));
			}
		}

		RequestUri              = new Uri(urlBuilder.ToString());
	}
}
