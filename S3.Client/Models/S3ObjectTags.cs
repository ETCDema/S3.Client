using System.Xml.Serialization;

namespace S3.Client.Models
{
	[XmlRoot("Tagging", Namespace = S3Client.Namespace)]
	public class S3ObjectTags
	{
		[XmlArray("TagSet")]
		public required Tag[] TagSet { get; init; }

		public Dictionary<string, string>? TryToDictionary()
		{
			if (TagSet==null || TagSet.Length==0) return null;

			var result          = new Dictionary<string, string>();

			foreach (var tag in TagSet)
			{
				result.Add(tag.Key, tag.Value);
			}

			return result;
		}

		public class Tag
		{
			public required string Key		{ get; init; }
			public required string Value	{ get; init; }
		}
	}
}
