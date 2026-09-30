using System.Globalization;
using System.Xml.Serialization;

namespace S3.Client.Models
{
	/// <summary>
	/// Данные получения списка версий объектов
	/// </summary>
	public static class ListVersions
	{
		/// <summary>
		/// Параметры запроса списка
		/// </summary>
		public sealed class Options: OptionsCore
		{
			public string? Delimiter			{ get => Get("delimiter"); init => Set("delimiter", value); }

			public string? Prefix				{ get => Get("prefix"); init => Set("prefix", value); }

			public string? VersionIdMarker		{ get => Get("version-id-marker"); init => Set("version-id-marker", value); }

			public string? KeyMarker			{ get => Get("key-marker"); init => Set("key-marker", value); }

			public string? EncodingType			{ get => Get("encoding-type"); init => Set("encoding-type", value); }

			public int? MaxKeys
			{
				get
				{
					return Get("max-keys") is string maxKeys ? int.Parse(maxKeys, CultureInfo.InvariantCulture) : null;
				}
				init
				{
					Set("max-keys", value is int maxKeys ? maxKeys.ToString(CultureInfo.InvariantCulture) : null);
				}
			}
		}

		/// <summary>
		/// Полученные данные
		/// </summary>
		[XmlRoot("ListVersionsResult", Namespace = S3Client.Namespace)]
		public sealed class Result
		{
			[XmlElement("Name")]
			public string Name					{ get; init; } = default!;

			[XmlElement("KeyMarker")]
			public string KeyMarker				{ get; init; } = default!;

			[XmlElement("MaxKeys")]
			public int MaxKeys					{ get; init; }

			[XmlElement("Prefix")]
			public string Prefix				{ get; init; } = default!;

			[XmlElement("VersionIdMarker")]
			public string VersionIdMarker		{ get; init; } = default!;

			[XmlElement("IsTruncated")]
			public bool IsTruncated				{ get; init; }

			[XmlElement("DeleteMarker")]
			public DeleteMarker[]? DeleteMarkers { get; init; }

			[XmlElement("Version")]
			public ObjectVersion[] Versions		{ get; init; } = default!;

			public sealed class DeleteMarker
			{
				[XmlElement("Key")]
				public string Key				{ get; init; } = default!;

				[XmlElement("IsLatest")]
				public bool IsLatest			{ get; init; }

				[XmlElement("LastModified", DataType = "dateTime")]
				public DateTime LastModified	{ get; init; }

				[XmlElement("VersionId")]
				public string VersionId			{ get; init; } = default!;
			}

			public sealed class ObjectVersion
			{
				[XmlElement("Key")]
				public string Key				{ get; init; } = default!;

				[XmlElement("LastModified", DataType = "dateTime")]
				public DateTime LastModified	{ get; init; }

				[XmlElement("ETag")]
				public string ETag				{ get; init; } = default!;

				[XmlElement("Size")]
				public long Size				{ get; init; }

				[XmlElement("StorageClass")]
				public string StorageClass		{ get; init; } = default!;

				[XmlElement("Owner")]
				public Owner Owner				{ get; init; } = Owner.Empty;

				[XmlElement("IsLatest")]
				public bool IsLatest			{ get; init; }

				[XmlElement("VersionId")]
				public string VersionId			{ get; init; } = default!;
			}
		}
	}
}
