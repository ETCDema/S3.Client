using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using S3.Client.Const;
using S3.Client.Helpers;
using S3.Client.Models;
using S3.Client.Models.Errors;
using S3.Client.Models.Security;
using S3.Client.Services;

namespace S3.Client
{
	public class S3Bucket
	{
		protected readonly S3Client Client;
		private readonly Action<S3Request>? _beforeSendHandler;

		protected internal S3Bucket(S3Client client, string name)
		{
			Client				= client;
			Name				= name;
		}

		protected S3Bucket(S3Client client, string name, Action<S3Request>? beforeSendHandler)
		{
			Client              = client;
			Name                = name;
			_beforeSendHandler	= beforeSendHandler;
		}

		public string Name		{ get; }

		#region List and get

		public async Task<ListBucket.Result> ListBucket(ListBucket.Options options, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Get, this, null, parameters: options.Items)
			{
				CompletionOption		= HttpCompletionOption.ResponseContentRead
			};
			return await Send<ListBucket.Result>(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task<ListVersions.Result> ListObjectVersions(ListVersions.Options options, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Get, this, null, actionName: S3Action.Versions, parameters: options.Items)
			{
				CompletionOption        = HttpCompletionOption.ResponseContentRead
			};
			return await Send<ListVersions.Result>(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task<S3Object> GetObject(string key, GetObjectOptions? opt, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Get, this, key)
			{
				CompletionOption = HttpCompletionOption.ResponseHeadersRead
			};
			opt?.SetupHeaders(request.Headers);

			// TODO: Use _setCustomerEncryptionKey(request.Headers, ...);

			var response        = await Send(request, cancellationToken).ConfigureAwait(false);
			return new S3Object(key, response);
		}

		public async Task<S3Object> GetObject(string key, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Get, this, key)
			{
				CompletionOption = HttpCompletionOption.ResponseHeadersRead
			};

			// TODO: Use _setCustomerEncryptionKey(request.Headers, ...);

			var response        = await Send(request, cancellationToken).ConfigureAwait(false);
			return new S3Object(key, response);
		}

		public async Task<S3ObjectInfo> GetObjectHead(string key, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Head, this, key)
			{
				CompletionOption        = HttpCompletionOption.ResponseHeadersRead
			};

			// TODO: Use _setCustomerEncryptionKey(request.Headers, ...);

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
			return new S3ObjectInfo(key, response);
		}

		#endregion

		#region Multipart put

		public async Task<MultipartUpload.IMiltipartUpload> InitiateMultipartPut(string key, string? contentType = null, CancellationToken cancellationToken = default)
		{
			var request         = new MultipartUpload.InitiateRequest(this, key)
			{
				ContentType		= contentType,
			};
			return await Send<MultipartUpload.InitiateResult>(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task<MultipartUpload.UploadPartResult> PutPart(MultipartUpload.IMiltipartUpload dst, int partNumber, Func<PutObject.Request, Task> contentBuilder, CancellationToken cancellationToken = default)
		{
			var request         = new PutObject.Request(this, string.Create(CultureInfo.InvariantCulture, $"{dst.Key}?partNumber={partNumber}&uploadId={dst.UploadId}"));
			await contentBuilder(request);

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
			return new MultipartUpload.UploadPartResult(dst, partNumber, response.Headers.ETag!.Tag);
		}

		public async Task<MultipartUpload.CompleteResult> CompleteMultipartPut(MultipartUpload.IMiltipartUpload dst, IReadOnlyList<MultipartUpload.UploadPartResult> parts, CancellationToken cancellationToken = default)
		{
			string xml;
			using (var sb = new XmlStringBuilder(true))
			{
				sb.WriteTagStart("CompleteMultipartUpload");
				{
					foreach (var part in parts)
					{
						sb.WriteTagStart("Part");
						{
							sb.WriteTag("PartNumber", part.PartNumber.ToString(CultureInfo.InvariantCulture));
							sb.WriteTag("ETag", part.ETag);
						}
						sb.WriteTagEnd("Part");
					}
				}
				sb.WriteTagEnd("CompleteMultipartUpload");
				xml             = sb.ToString();
			}
			
			var request			= new S3Request(HttpMethod.Post, this, $"{dst.Key}?uploadId={dst.UploadId}")
			{
				CompletionOption		= HttpCompletionOption.ResponseContentRead,
				Content					= new StringContent(xml, Encoding.UTF8, "text/xml"),
			}; 
			
			return await Send<MultipartUpload.CompleteResult>(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task AbortMultipartPut(MultipartUpload.IMiltipartUpload dst, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Delete, this, $"{dst.Key}?uploadId={dst.UploadId}")
			{
				CompletionOption        = HttpCompletionOption.ResponseContentRead
			};
			using var response  = await Send(request, cancellationToken);
		}

		#endregion

		#region Put, copy and Delete

		public async Task<PutObject.Result> PutObject(string key, Func<PutObject.Request, Task> contentBuilder, CancellationToken cancellationToken = default)
		{
			var request         = new PutObject.Request(this, key);
			await contentBuilder(request);

			// TODO: Use _setCustomerEncryptionKey(request.Headers, ...);

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
			return new PutObject.Result
			{
				Key				= key,
				ETag            = response.Headers.ETag!.Tag!,
				VersionId       = response.Headers.TryGetValue(S3HeaderNames.VersionId)
			};
		}
		
		public async Task<CopyObject.Result> CopyObject(string dstKey, string srcBucket, string srcKey, CopyObject.Policy cp = Models.CopyObject.Policy.Copy, CancellationToken cancellationToken = default)
		{
			string? cpVal       = cp switch
			{
				Models.CopyObject.Policy.Copy    => "COPY",
				Models.CopyObject.Policy.Replace => "REPLACE",
				_ => null
			};

			var request         = new S3Request(HttpMethod.Put, this, dstKey)
			{
				CompletionOption        = HttpCompletionOption.ResponseContentRead
			};
			request.Headers.Add(S3HeaderNames.CopySource, $"/{srcBucket}/{srcKey}");
			if (cpVal!=null) request.Headers.TryAddWithoutValidation(S3HeaderNames.MetadataDirective, cpVal);

			return await Send<CopyObject.Result>(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task<Delete.Result> DeleteObject(string key, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Delete, this, key)
			{
				CompletionOption        = HttpCompletionOption.ResponseHeadersRead
			};

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);

			if (response.StatusCode is not HttpStatusCode.NoContent)
				throw new S3Exception("Expected 204", response.StatusCode);

			return new Delete.Result(
				deleteMarker: response.Headers.TryGetValue(S3HeaderNames.DeleteMarker),
				requestCharged: response.Headers.TryGetValue(S3HeaderNames.RequestCharged),
				versionId: response.Headers.TryGetValue(S3HeaderNames.VersionId)
			);
		}

		public async Task<Delete.Result> DeleteObject(string key, string? versionId, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Delete, this, key, versionId: versionId)
			{
				CompletionOption        = HttpCompletionOption.ResponseHeadersRead
			};

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);

			if (response.StatusCode is not HttpStatusCode.NoContent)
				throw new S3Exception("Expected 204", response.StatusCode);

			return new Delete.Result(
				deleteMarker: response.Headers.TryGetValue(S3HeaderNames.DeleteMarker),
				requestCharged: response.Headers.TryGetValue(S3HeaderNames.RequestCharged),
				versionId: response.Headers.TryGetValue(S3HeaderNames.VersionId)
			);
		}

		public async Task<DeleteBatch.Result> DeleteObjects(IReadOnlyList<string> keys, bool quite = false, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNull(keys);

			if (keys.Count > 1_000) throw new ArgumentException($"Must not exceed 1,000 items. Was {keys.Count} items.", nameof(keys));

			byte[] data;
			using (var sb = new XmlStringBuilder(true))
			{
				sb.WriteTagStart("Delete");

				if (quite) sb.WriteTag("Quiet", "true");

				foreach (var key in keys)
				{
					sb.WriteTagStart("Object");
					sb.WriteTag("Key", key);
					sb.WriteTagEnd("Object");
				}

				sb.WriteTagEnd("Delete");
				data			= Encoding.UTF8.GetBytes(sb.ToString());
			}

			var request   = new S3Request(HttpMethod.Post, this, null, actionName: S3Action.Delete)
			{
				CompletionOption = HttpCompletionOption.ResponseContentRead,
				Content         = new ByteArrayContent(data)
				{
					Headers     = { { "Content-Type", "text/xml" } }
				}
			};

			request.Content.Headers.ContentMD5	= MD5.HashData(data);

			return await Send<DeleteBatch.Result>(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task<RestoreObjectResult> RestoreObject(string key, string? version = null, int days = 7, GlacierJobTier tier = GlacierJobTier.Standard, CancellationToken cancellationToken = default)
		{
			var xml				= string.Create(CultureInfo.InvariantCulture,
$"""
<RestoreRequest>
    <Days>{days}</Days>
    <GlacierJobParameters><Tier>{tier}</Tier></GlacierJobParameters>
</RestoreRequest>
""");

			var request         = new S3Request(HttpMethod.Post, this, key, versionId: version, actionName: S3Action.Restore)
			{
				CompletionOption		= HttpCompletionOption.ResponseContentRead,
				Content					= new StringContent(xml, Encoding.UTF8, "text/xml")
			};
			request.Content.Headers.ContentMD5	= HashHelper.ComputeMD5(xml);

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
			return new RestoreObjectResult(response.StatusCode);
		}

		#endregion

		#region Tagging

		public async Task<Dictionary<string, string>?> TryGetObjectTags(string key, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Get, this, key, actionName: S3Action.Tagging)
			{
				CompletionOption        = HttpCompletionOption.ResponseHeadersRead
			};

			// TODO?: Use _setCustomerEncryptionKey(request.Headers, ...);

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
			var content         = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
			return S3Serializer<S3ObjectTags>.Deserialize(content).TryToDictionary();
		}

		public async Task PutObjectTags(string key, IReadOnlyDictionary<string, string> tags, CancellationToken cancellationToken = default)
		{
			if (tags is null || tags.Count == 0) throw new ArgumentNullException(nameof(tags));

			if (tags.Count > 10) throw new ArgumentException("Must be less than 10", nameof(tags));

			byte[] data;
			using (var xml = new XmlStringBuilder(true))
			{
				xml.WriteTagStart("Tagging");
				xml.WriteTagStart("TagSet");
				foreach (var kv in tags)
				{
					if (kv.Key.Length   > 128) throw new ArgumentException($"Tag key > 128 chars. Was '{kv.Key}'");
					if (kv.Value.Length > 256) throw new ArgumentException($"Tag {kv.Key} value > 256 chars. Was '{kv.Value}'");

					xml.WriteTagStart("Tag");
					xml.WriteTag("Key", kv.Key);
					xml.WriteTag("Value", kv.Value);
					xml.WriteTagEnd("Tag");
				}
				xml.WriteTagEnd("TagSet");
				xml.WriteTagEnd("Tagging");

				data            = Encoding.UTF8.GetBytes(xml.ToString());
			}

			var request			= new S3Request(HttpMethod.Put, this, key, actionName: S3Action.Tagging)
			{
				CompletionOption = HttpCompletionOption.ResponseContentRead,
				Content         = new ByteArrayContent(data)
				{
					Headers     = { { "Content-Type", "text/xml" } }
				}
			};

			request.Content.Headers.ContentMD5  = MD5.HashData(data);

			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
		}

		public async Task DeleteObjectTags(string key, CancellationToken cancellationToken = default)
		{
			var request         = new S3Request(HttpMethod.Delete, this, key, actionName: S3Action.Tagging)
			{
				CompletionOption = HttpCompletionOption.ResponseHeadersRead
			};
			using var response  = await Send(request, cancellationToken).ConfigureAwait(false);
		}

		#endregion

		protected virtual Task<HttpResponseMessage> Send(S3Request request, CancellationToken cancellationToken)
		{
			request.BeforeSend  = _beforeSendHandler;
			return Client.Send(request, cancellationToken);
		}

		protected virtual Task<TResult> Send<TResult>(S3Request request, CancellationToken cancellationToken)
			where TResult : class
		{
			request.BeforeSend  = _beforeSendHandler;
			return Client.Send<TResult>(request, cancellationToken);
		}

		private static void _setCustomerEncryptionKey(HttpRequestHeaders headers, in ServerSideEncryptionKey key)
		{
			headers.Add(S3HeaderNames.ServerSideEncryptionCustomerAlgorithm,	key.Algorithm);
			headers.Add(S3HeaderNames.ServerSideEncryptionCustomerKey,			Convert.ToBase64String(key.Key));
			headers.Add(S3HeaderNames.ServerSideEncryptionCustomerKeyMD5,		Convert.ToBase64String(key.KeyMD5));
		}
	}
}
