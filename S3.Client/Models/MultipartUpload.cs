using System.Net.Http.Headers;
using System.Xml.Serialization;

using S3.Client.Const;

namespace S3.Client.Models
{
	/// <summary>
	/// Данные для загрузки объектов по частям
	/// </summary>
	public static class MultipartUpload
	{
		/// <summary>
		/// Запрос начала загрузки
		/// </summary>
		public sealed class InitiateRequest : S3Request
		{
			public InitiateRequest(S3Bucket bucket, string key, IReadOnlyDictionary<string, string>? properties = null)
				: base(HttpMethod.Post, bucket, key, actionName: S3Action.Uploads)
			{
				ArgumentException.ThrowIfNullOrEmpty(key);

				CompletionOption = HttpCompletionOption.ResponseContentRead;
				Content			= new ByteArrayContent([]);
				_updateHeaders(properties);
			}

			/// <summary>Тип контента</summary>
			public string? ContentType
			{
				get => Content!.Headers.ContentType?.ToString();
				set
				{
					if (value is null)
					{
						Content!.Headers.ContentType = null;
					} else
					{
						Content!.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
					}
				}
			}

			private void _updateHeaders(IReadOnlyDictionary<string, string>? headers)
			{
				if (headers is null) return;

				foreach (var item in headers)
				{
					switch (item.Key)
					{
						case "Content-Encoding":
							Content!.Headers.ContentEncoding.Clear();
							Content!.Headers.ContentEncoding.Add(item.Value);
							break;
						case "Content-Type":
							Content ??= new ByteArrayContent([]);
							Content.Headers.ContentType = MediaTypeHeaderValue.Parse(item.Value);

							break;

						// Skip list...
						case "Accept-Ranges":
						case "Content-Length":
						case "Date":
						case "ETag":
						case "Server":
						case "Last-Modified":
						case "x-amz-id-2":
						case "x-amz-expiration":
						case "x-amz-request-id2":
						case "x-amz-request-id":
							break;

						default:
							Headers.Add(item.Key, item.Value);

							break;
					}
				}
			}
		}

		/// <summary>Данные загрузки по частям для продолжения, завершения и отмены загрузки</summary>
		public interface IMiltipartUpload
		{
			string Bucket		{ get; }

			string Key			{ get; }

			string UploadId		{ get; }
		}

		/// <summary>
		/// Ответ о начале загруки
		/// </summary>
		[XmlRoot("InitiateMultipartUploadResult", Namespace = S3Client.Namespace)]
		public sealed class InitiateResult: IMiltipartUpload
		{
			[XmlElement]
			public string Bucket		{ get; init; } = default!;

			[XmlElement]
			public string Key			{ get; init; } = default!;

			[XmlElement]
			public string UploadId		{ get; init; } = default!;
		}

		/// <summary>
		/// Ответ о загрузки части
		/// </summary>
		public sealed class UploadPartResult: IMiltipartUpload
		{
			public UploadPartResult(IMiltipartUpload dst, int partNumber, string eTag)
			{
				ArgumentException.ThrowIfNullOrEmpty(eTag);

				Bucket			= dst.Bucket;
				Key				= dst.Key;
				UploadId		= dst.UploadId;
				PartNumber		= partNumber;
				ETag			= eTag;
			}

			public string Bucket		{ get; }

			public string Key			{ get; }

			public string UploadId		{ get; }

			public int PartNumber		{ get; }

			public string ETag			{ get; }
		}

		/// <summary>
		/// Ответ о завершении загрузки
		/// </summary>
		[XmlRoot("CompleteMultipartUploadResult", Namespace = S3Client.Namespace)]
		public sealed class CompleteResult
		{
			[XmlElement]
			public string Location		{ get; init; } = default!;

			[XmlElement]
			public string Bucket		{ get; init; } = default!;

			[XmlElement]
			public required string Key	{ get; init; }

			/// <summary>The entity tag is an opaque string. The entity tag may or may not be an MD5 digest of the object data.</summary>
			[XmlElement]
			public required string ETag { get; init; }
		}
	}
}
