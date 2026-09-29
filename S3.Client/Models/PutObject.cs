using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using LinkDotNet.StringBuilder;

using S3.Client.Const;
using S3.Client.Services;

namespace S3.Client.Models;

public static class PutObject
{
	public class Request : S3Request
	{
		internal Request(S3Bucket bucket, string key)
			: base(HttpMethod.Put, bucket, key)
		{
			ArgumentNullException.ThrowIfNull(key);

			CompletionOption = HttpCompletionOption.ResponseContentRead;
		}

		public Request SetContent(byte[] content, string contentType = "application/octet-stream")
		{
			Content             = new ByteArrayContent(content)
			{
				Headers         = { { "Content-Type", contentType } }
			};

			Content.Headers.ContentLength		= content.Length;
			return this;
		}

		public Request SetContent(string content, string contentType = "text/plain")
		{
			Content             = new StringContent(content, Encoding.UTF8, contentType);
			return this;
		}

		public Request SetContent(Stream stream, string contentType = "application/octet-stream")
		{
			SetContent(stream, HashHelper.TryComputeSHA256(stream), contentType);
			return this;
		}

		public Request SetContent(Stream stream, byte[]? sha256Hash, string contentType = "application/octet-stream")
		{
			ArgumentNullException.ThrowIfNull(stream);
			ArgumentException.ThrowIfNullOrEmpty(contentType);

			if (stream.Length is 0)
				throw new ArgumentException("Must not be empty", nameof(stream));

			if (contentType.Length is 0)
				throw new ArgumentException("Required", nameof(contentType));

			Content             = new StreamContent(stream)
			{
				Headers         = { { "Content-Type", contentType } }
			};

			Content.Headers.ContentLength		= stream.Length;

			Headers.TryAddWithoutValidation(S3HeaderNames.ContentSha256, 0<sha256Hash?.Length ? Convert.ToHexStringLower(sha256Hash) : S3Client._UNSIGNED_PAYLOAD);
			return this;
		}

		public Request SetContent(Stream stream, long length, string contentType = "application/octet-stream")
		{
			ArgumentNullException.ThrowIfNull(stream);
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

			Content             = new StreamContent(stream)
			{
				Headers         = { { "Content-Type", contentType } }
			};

			Content.Headers.ContentLength		= length;

			var sha256Hash		= HashHelper.TryComputeSHA256(stream);
			Headers.TryAddWithoutValidation(S3HeaderNames.ContentSha256, 0<sha256Hash?.Length ? Convert.ToHexStringLower(sha256Hash) : S3Client._UNSIGNED_PAYLOAD);
			return this;
		}

		public Request SetTagSet(IReadOnlyDictionary<string, string> tags)
		{
			if (tags is null || tags.Count is 0) return this;

			if (tags.Count > 10) throw new ArgumentException("Must be less than 10", nameof(tags));

			// The tag-set for the object. The tag-set must be encoded as URL Query parameters. (For example, "Key1=Value1")
			using var writer	= new ValueStringBuilder(stackalloc char[256]);

			var first           = true;
			foreach (var tag in tags)
			{
				if (first)
					first       = false;
				else
					writer.Append('&');

				if (tag.Key.Length   > 128) throw new ArgumentException($"Tag key > 128 chars. Was '{tag.Key}'");
				if (tag.Value.Length > 256) throw new ArgumentException($"Tag '{tag.Key}' value > 256 chars. Was '{tag.Value}'");

				writer.Append(UrlEncoder.Default.Encode(tag.Key));
				writer.Append('=');
				writer.Append(UrlEncoder.Default.Encode(tag.Value));
			}

			Headers.Add(S3HeaderNames.Tagging, writer.ToString());
			return this;
		}
	}

	public sealed class Result
	{
		public required string Key		{ get; init; }

		public required string ETag		{ get; init; }

		public string? VersionId		{ get; init; }
	}

	// NOTES: 
	// Amazon's ETag is the a hexidecimal encoded MD5 digest of the blobs bytes wrapped in quotes
	// For all PUT requests, Amazon S3 computes its own MD5, stores it with the object, 
	// and then returns the computed MD5 as part of the PUT response code in the ETag.  
}