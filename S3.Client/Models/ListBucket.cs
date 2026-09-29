using System.Globalization;
using System.Xml.Serialization;

namespace S3.Client.Models
{
	public static class ListBucket
	{
		public sealed class Options : OptionsCore
		{
			public Options()
			{
				Set("list-type", "2"); // https://docs.aws.amazon.com/AmazonS3/latest/API/API_ListObjectsV2.html
			}

			public string? Delimiter
			{
				get => Get("delimiter");
				init => Set("delimiter", value);
			}

			public string? Prefix
			{
				get => Get("prefix");
				init => Set("prefix", value);
			}

			public string? ContinuationToken
			{
				get => Get("continuation-token");
				init => Set("continuation-token", value);
			}

			public string? StartAfter
			{
				get => Get("start-after");
				init => Set("start-after", value);
			}

			/*
			public string? EncodingType
			{
				get => Get("encoding-type");
				set => Set("encoding-type", value);
			}
			*/

			public int? MaxKeys
			{
				get
				{
					if (Get("max-keys") is string maxKeys)
					{
						return int.Parse(maxKeys, NumberStyles.None, CultureInfo.InvariantCulture);
					}

					return null;
				}
				init
				{
					Set("max-keys", value is int maxKeys ? maxKeys.ToString(CultureInfo.InvariantCulture) : null);
				}
			}
		}

		[XmlRoot("ListBucketResult", Namespace = S3Client.Namespace)]
		public sealed class Result
		{
			[XmlElement("Name")]
			public string Name { get; init; } = default!;

			/// <summary>
			/// If StartAfter was sent with the request, it is included in the response.
			/// </summary>
			[XmlElement("StartAfter")]
			public string? StartAfter { get; init; }

			[XmlElement("KeyCount")]
			public int KeyCount { get; init; }

			[XmlElement("MaxKeys")]
			public int MaxKeys { get; init; }

			[XmlElement("Prefix")]
			public string? Prefix { get; init; }

			[XmlElement("NextContinuationToken")]
			public string? NextContinuationToken { get; init; }

			[XmlElement("IsTruncated")]
			public bool IsTruncated { get; init; }

			[XmlElement("Contents")]
			public Object[]? Items { get; init; }

			[XmlRoot("Contents", Namespace = S3Client.Namespace)]
			public sealed class Object
			{
				[XmlElement("Key")]
				public string Key { get; init; } = default!;

				[XmlElement("LastModified", DataType = "dateTime")]
				public DateTime LastModified { get; init; }

				[XmlElement("ETag")]
				public string ETag { get; init; } = default!;

				[XmlElement("Size")]
				public long Size { get; init; }

				[XmlElement("StorageClass")]
				public string StorageClass { get; init; } = default!;

				[XmlElement("Owner")]
				public Owner Owner { get; init; } = Owner.Empty;
			}
		}

		/*
		<?xml version="1.0" encoding="UTF-8"?>
		<ListBucketResult xmlns="http://s3.amazonaws.com/doc/2006-03-01/">
			<Name>my-test</Name>
			<Prefix></Prefix>
			<MaxKeys>100</MaxKeys>
			<IsTruncated>false</IsTruncated>
			<Contents>
				<Key>2026-08-17_11h53_52.png</Key>
				<LastModified>2026-09-21T11:06:44Z</LastModified>
				<ETag>&#34;09fda9e9111d647a763d5751998fca06&#34;</ETag>
				<Size>2019123</Size>
				<StorageClass>STANDARD</StorageClass>
			</Contents>
			<Contents>
				<Key>2026/09-24/16-07-14@test.txt</Key>
				<LastModified>2026-09-24T14:02:52Z</LastModified>
				<ETag>&#34;06e7ac5df15cfc96da3c542215f1be06&#34;</ETag>
				<Size>18</Size>
				<StorageClass>STANDARD</StorageClass>
			</Contents>
			<KeyCount>2</KeyCount>
		</ListBucketResult>			 
		*/

	}
}
