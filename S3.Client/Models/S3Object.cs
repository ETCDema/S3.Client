using System.Net;
using System.Net.Http.Headers;

namespace S3.Client.Models;

/// <summary>
/// Объект в бакете
/// </summary>
public sealed class S3Object : S3ObjectInfo, IDisposable
{
    private Stream? _stream;
    private HttpResponseMessage? _response;

    public S3Object(string key, HttpResponseMessage response)
		: base(key, response)
    {
        StatusCode				= response.StatusCode;

		if (response.StatusCode == HttpStatusCode.NotModified)
        {
            response.Dispose();
            return;
        }

        _response				= response;
    }

	protected override void VisitProperties(Dictionary<string, string> props)
	{
		CacheControl            = props.TryGetValue("Cache-Control", out var cacheControl)
								? CacheControlHeaderValue.Parse(cacheControl)
								: null;
		ContentRange			= props.TryGetValue("Content-Range", out var contentRange)
								? ContentRangeHeaderValue.Parse(contentRange)
								: null;
	}

	public HttpStatusCode StatusCode			 { get; }

    public CacheControlHeaderValue? CacheControl { get; private set; }

    public ContentRangeHeaderValue? ContentRange { get; private set; }

	/// <summary>
	/// Открыть поток содержимого для чтения
	/// </summary>
	/// <returns></returns>
	public async ValueTask<Stream> Open()
    {
        ObjectDisposedException.ThrowIf(_response is null, this);

        return _stream ??= await _response.Content.ReadAsStreamAsync().ConfigureAwait(false);
    }

	/// <summary>
	/// Прочитать содержимое как массив байт
	/// </summary>
	/// <returns></returns>
    public Task<byte[]> ReadAsByteArray()
    {
        ObjectDisposedException.ThrowIf(_response is null, this);

        return _response.Content.ReadAsByteArrayAsync();
    }

    public async Task CopyTo(Stream output)
    {
        ObjectDisposedException.ThrowIf(_response is null, this);

        await _response.Content.CopyToAsync(output).ConfigureAwait(false);
    }

    public async Task CopyTo(Stream output, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_response is null, this);

        await _response.Content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _internalDispose();
    }

    ~S3Object()
    {
        _internalDispose();
    }

    private void _internalDispose()
    {
        _stream?.Dispose();
        _response?.Dispose();

        _stream					= null;
        _response				= null;
    }
}
