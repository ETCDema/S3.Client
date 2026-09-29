using System.Xml.Serialization;

namespace S3.Client.Models;

public static class CopyObject
{
	public enum Policy
	{
		Copy                    = 0,
		Replace                 = 1
	}

	[XmlRoot(Namespace = S3Client.Namespace)]
	public sealed class Result
	{
		[XmlElement(DataType = "dateTime")]
		public DateTime LastModified { get; init; }

		/// <summary>
		/// Returns the ETag of the new object. The ETag only reflects changes to the contents of an object, not its metadata.
		/// </summary>
		[XmlElement]
		public string ETag { get; init; } = default!;
	}
}