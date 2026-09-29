using System.Net.Http.Headers;

namespace S3.Client.Models;

public sealed class GetObjectOptions
{
	public DateTime? IfModifiedSince	{ get; set; }

	public string? IfNoneMatch			{ get; set; }

	public GetObjectOptions SetRange(long? from, long? to)
	{
		if (from.HasValue && to.HasValue)
			_range				= new RangeHeaderValue(from, to);
		else
			_range				= default;

		return this;
	}

	private RangeHeaderValue? _range;

	internal void SetupHeaders(HttpRequestHeaders headers)
	{
		if (IfModifiedSince.HasValue)
			headers.IfModifiedSince		= IfModifiedSince.Value.ToUniversalTime();

		if (!string.IsNullOrWhiteSpace(IfNoneMatch))
			headers.IfNoneMatch.Add(new EntityTagHeaderValue(IfNoneMatch));

		if (_range!=null)
			headers.Range		= _range;
	}
}
