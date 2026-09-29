using System.Xml.Serialization;

namespace S3.Client.Models
{
	public static class DeleteBatch
	{
		[XmlRoot("DeleteResult", Namespace = S3Client.Namespace)]
		public sealed class Result
		{
			[XmlElement("Deleted")]
			public Item[]? Deleted { get; init; }

			[XmlElement("Error")]
			public Error[]? Errors { get; init; }

			[XmlIgnore]
			public bool HasErrors => Errors is { Length: > 0 };

			public readonly struct Item
			{
				public Item() { }

				public Item(string key)
				{
					Key = key;
				}

				[XmlElement]
				public string Key { get; init; } = default!;
			}

			public sealed class Error
			{
				[XmlElement]
				public string Key { get; init; } = default!;

				[XmlElement]
				public string Code { get; init; } = default!;

				[XmlElement]
				public string Message { get; init; } = default!;
			}
		}
	}
}
