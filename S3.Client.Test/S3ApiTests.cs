using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using S3.Client.Models;

using S3.Client.Models.Errors;

namespace S3.Client.Test
{
	public abstract class S3ApiTests
	{
		private static readonly string _TEST_KEY        = "2026/09-24/16-07-14@test.txt";

		[Fact]
		[Trait("Op", "Get")]
		public async Task ListBucket()
		{
			var bucket          = GetBucket();
			var result          = await bucket.ListBucket(new ListBucket.Options() { MaxKeys = 100 }, TestContext.Current.CancellationToken);

			Assert.NotNull(result);
			Assert.NotNull(result.Items);
			Assert.Contains(result.Items, item => item.Key==_TEST_KEY);
		}

		[Fact]
		[Trait("Op", "Get")]
		public async Task ListBucketWithPrefix()
		{
			var bucket          = GetBucket();
			var result          = await bucket.ListBucket(new ListBucket.Options() { MaxKeys = 10, Prefix = _TEST_KEY[..19]}, TestContext.Current.CancellationToken);

			Assert.NotNull(result);
			Assert.NotNull(result.Items);
			Assert.Contains(result.Items, item => item.Key==_TEST_KEY);
		}

		[Fact]
		[Trait("Op", "Get")]
		public async Task GetTestFileHead()
		{
			var bucket          = GetBucket();
			var result          = await bucket.GetObjectHead(_TEST_KEY, TestContext.Current.CancellationToken);

			Assert.NotNull(result);
			Assert.Equal(_TEST_KEY, result.Key);
		}

		[Fact]
		[Trait("Op", "Get")]
		public async Task GetTestFile()
		{
			var bucket          = GetBucket();
			using var result    = await bucket.GetObject(_TEST_KEY, TestContext.Current.CancellationToken);

			Assert.NotNull(result);
			Assert.Equal(_TEST_KEY, result.Key);

			var content         = await result.ReadAsByteArray();
			Assert.NotEmpty(content);
			Assert.Equal("Empty test file", Encoding.UTF8.GetString(content, 3, content.Length-3));
		}

		[Fact]
		[Trait("Op", "Put")]
		public async Task PutAndDeleteTextFile()
		{
			var key             = string.Concat("temp/", DateTime.Now.ToString("yyyy-MM-dd'/'HH-mm-ss'@'"), Guid.NewGuid().ToString("n"), ".txt");
			var tags            = new Dictionary<string, string>()
			{
				{ "Tag1", "Tag 1 value"},
				{ "tag2", "V2" }
			};

			var bucket          = GetBucket();
			var result          = await bucket.PutObject(key, async req =>
			{
				req .SetContent("Test file "+key)
					.SetTagSet(tags);
			}, TestContext.Current.CancellationToken);

			Assert.NotNull(result);
			Assert.NotEmpty(result.ETag);

			var getTags         = await bucket.TryGetObjectTags(key, TestContext.Current.CancellationToken);
			// VaultS3 BUG: tags from header not decoded
			//Assert.Equal(tags, getTags);

			var delResult       = await bucket.DeleteObject(key, TestContext.Current.CancellationToken);
			Assert.NotNull(delResult);
		}

		[Fact]
		[Trait("Op", "Put")]
		public async Task MultipartPutAndDelete()
		{
			var key             = string.Concat("multipart/", DateTime.Now.ToString("yyyy-MM-dd'/'HH-mm-ss'@'"), Guid.NewGuid().ToString("n"), ".txt");
			var bucket          = GetBucket();

			var initResult      = await bucket.InitiateMultipartPut(key, null, TestContext.Current.CancellationToken);
			Assert.NotNull(initResult);
			Assert.NotNull(initResult.UploadId);

			var part1           = await bucket.PutPart(initResult, 1, async req => { req.SetContent("+ Part 1"); }, TestContext.Current.CancellationToken);
			Assert.NotNull(part1);
			Assert.Equal(1, part1.PartNumber);

			var part2           = await bucket.PutPart(initResult, 2, async req => { req.SetContent("+ Part 2"); }, TestContext.Current.CancellationToken);
			Assert.NotNull(part2);
			Assert.Equal(2, part2.PartNumber);

			var complete        = await bucket.CompleteMultipartPut(initResult, [ part1, part2 ], TestContext.Current.CancellationToken);
			Assert.NotNull(complete);
			Assert.NotNull(complete.Location);
			Assert.Equal(key, complete.Key);

			var delResult       = await bucket.DeleteObject(key, TestContext.Current.CancellationToken);
			Assert.NotNull(delResult);
		}

		[Fact]
		[Trait("Op", "Put")]
		public async Task MultipartPutAndAbort()
		{
			var key             = string.Concat("multipart/", DateTime.Now.ToString("yyyy-MM-dd'/'HH-mm-ss'@'"), Guid.NewGuid().ToString("n"), ".txt");
			var bucket          = GetBucket();

			var initResult      = await bucket.InitiateMultipartPut(key, null, TestContext.Current.CancellationToken);
			Assert.NotNull(initResult);
			Assert.NotNull(initResult.UploadId);

			var part1           = await bucket.PutPart(initResult, 1, async req => { req.SetContent("+ Part 1"); }, TestContext.Current.CancellationToken);
			Assert.NotNull(part1);
			Assert.Equal(1, part1.PartNumber);

			var part2           = await bucket.PutPart(initResult, 2, async req => { req.SetContent("+ Part 2"); }, TestContext.Current.CancellationToken);
			Assert.NotNull(part2);
			Assert.Equal(2, part2.PartNumber);

			await bucket.AbortMultipartPut(initResult, TestContext.Current.CancellationToken);

			await Assert.ThrowsAsync<S3Exception>(async () =>
			{
				await bucket.GetObjectHead(key, TestContext.Current.CancellationToken);
			}, e =>
			{
				return e.HttpStatusCode==HttpStatusCode.NotFound ? null : e.Message;
			});
		}

		[Fact]
		[Trait("Op", "Tagging")]
		public async Task ObjectTagging()
		{
			var key             = string.Concat("tagging/", DateTime.Now.ToString("yyyy-MM-dd'/'HH-mm-ss'@'"), Guid.NewGuid().ToString("n"), ".txt");
			var bucket          = GetBucket();

			var result          = await bucket.PutObject(key, async req =>
			{
				req .SetContent("Test file "+key);
			}, TestContext.Current.CancellationToken);
			Assert.NotNull(result);
			Assert.NotEmpty(result.ETag);

			var tags            = new Dictionary<string, string>()
			{
				{ "Tag1", "Tag 1 value"},
				{ "tag2", "V2" }
			};

			await bucket.PutObjectTags(key, tags, TestContext.Current.CancellationToken);

			var getTags			= await bucket.TryGetObjectTags(key, TestContext.Current.CancellationToken);
			Assert.Equal(tags, getTags);

			var obj				= await bucket.GetObjectHead(key, TestContext.Current.CancellationToken);

			await bucket.DeleteObjectTags(key, TestContext.Current.CancellationToken);
			var noTags			= await bucket.TryGetObjectTags(key, TestContext.Current.CancellationToken);
			Assert.Null(noTags);

			var delResult       = await bucket.DeleteObject(key, TestContext.Current.CancellationToken);
			Assert.NotNull(delResult);
		}

		protected abstract S3Bucket GetBucket();
	}
}
