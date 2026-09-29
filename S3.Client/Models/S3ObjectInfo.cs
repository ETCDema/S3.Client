using System.Globalization;
using System.Net.Http.Headers;

using S3.Client.Const;

namespace S3.Client.Models;

public class S3ObjectInfo
{
	internal S3ObjectInfo(string key, HttpResponseMessage response)
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(response);

		var properties          = _getProperties(response);

		Key                     = key;
		ETag                    = response.Headers.ETag is EntityTagHeaderValue et
								? new ETag(et.Tag)
								: default;
		ContentLength			= long.Parse(properties["Content-Length"], NumberStyles.None, CultureInfo.InvariantCulture);
		Modified				= DateTimeOffset.ParseExact(properties["Last-Modified"], "R", CultureInfo.InvariantCulture).UtcDateTime;
		ContentType             = properties["Content-Type"];
		VersionId               = properties.TryGetValue(S3HeaderNames.VersionId, out var version) ? version : null;
		StorageClass            = properties.TryGetValue(S3HeaderNames.StorageClass, out var storageClass) ? storageClass : null;

		VisitProperties(properties);
	}

	protected S3ObjectInfo(string key, long contentLength, DateTime modified, ETag eTag, string? contentType, string? versionId, string? storageClass)
    {
        Key						= key;
		ETag                    = eTag;
		ContentLength           = contentLength;
		ContentType             = contentType;
		Modified                = modified;
		VersionId				= versionId;
		StorageClass			= storageClass;
	}

    public string Key			{ get; }

    public ETag ETag			{ get; }

    public long ContentLength	{ get; }

    public string? ContentType	{ get; }

    public DateTime Modified	{ get; }

	public string? VersionId	{ get; }

	public string? StorageClass	{ get; }

	protected virtual void VisitProperties(Dictionary<string, string> props)
	{
	}

	private static Dictionary<string, string> _getProperties(HttpResponseMessage response)
	{
		var baseHeaders			= response.Headers.NonValidated;
		var contentHeaders		= response.Content.Headers.NonValidated;

		var result				= new Dictionary<string, string>(baseHeaders.Count + contentHeaders.Count);

		foreach (var header in baseHeaders)
		{
			result.Add(header.Key, header.Value.ToString());
		}

		foreach (var header in contentHeaders)
		{
			result.Add(header.Key, header.Value.ToString());
		}

		return result;
	}
}
