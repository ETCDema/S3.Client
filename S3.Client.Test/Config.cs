using System;
using System.Collections.Concurrent;

using Microsoft.Extensions.Configuration;

using S3.Client.Models.Security;

namespace S3.Client.Test
{
	internal static class Config
	{
		private static readonly ConcurrentDictionary<string, S3TestOptions> _opts = [];
		private static IConfigurationRoot _config;

		static Config()
		{
			_config				= new ConfigurationBuilder()
									.AddJsonFile("appsettings.json", optional: false)
									.AddJsonFile("appsettings.secret.json", optional: false)
									.Build();
		}

		public static S3TestOptions Get(string service)
		{
			return _opts.GetOrAdd(service, (service) => _config.GetSection(service).Get<S3TestOptions>() ?? throw new NullReferenceException($"No config for service {service}"));
		}

		public class S3TestOptions
		{
			private readonly Lazy<S3Client> _client;

			public S3TestOptions()
			{
				_client			= new (_build);
			}

			public required string Endpoint		{ get; init; }
			
			public required string AccessKey	{ get; init; }
			
			public required string SecretKey	{ get; init; }
			
			public required string Bucket		{ get; init; }

			public S3Client Client
			{
				get
				{
					return _client.Value;
				}
			}

			private S3Client _build()
			{
				var credential  = new S3Credential(AccessKey, SecretKey);
				var svc         = new S3Service(Endpoint, credential);
				var client      = new S3Client(svc);
				return client;
			}
		}
	}
}
