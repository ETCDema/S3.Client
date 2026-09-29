using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using S3.Client.Models;

namespace S3.Client.Test
{
	internal class S3TestBucket : S3Bucket
	{
		private readonly ITestOutputHelper _output;

		public S3TestBucket(S3Client client, string name, ITestOutputHelper output)
			: base(client, name, _beforeSend)
		{
			_output				= output;
		}

		public S3TestBucket(Config.S3TestOptions opt, ITestOutputHelper output)
			: this(opt.Client, opt.Bucket, output)
		{
		}

		protected override Task<HttpResponseMessage> Send(S3Request request, CancellationToken cancellationToken)
		{
			return base.Send(request, cancellationToken);
		}

		protected override Task<TResult> Send<TResult>(S3Request request, CancellationToken cancellationToken)
		{
			return base.Send<TResult>(request, cancellationToken);
		}

		private static void _beforeSend(S3Request req)
		{
			((S3TestBucket)req.Bucket)._dumpRequest(req);
		}

		private void _dumpRequest(S3Request req)
		{
			_output.WriteLine($"{req.Method} {req.RequestUri}");
			_output.WriteLine($"Headers: [\n\t{string.Join("\n\t", req.Headers.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries))}\n]");
			if (req.Content==null)
				_output.WriteLine("Body: - NO BODY -");
			else
				_output.WriteLine($"Body [{req.Content.Headers.ContentType}]: {req.Content.ReadAsStringAsync().Result}");
			_output.WriteLine("---");
		}
	}
}
