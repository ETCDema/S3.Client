using System.Xml.Serialization;

namespace S3.Client.Models;

public sealed class Owner
{
	public static readonly Owner Empty = new();

    [XmlElement("ID")]
    public string ID { get; init; } = default!;

	[XmlElement("DisplayName")]
    public string DisplayName { get; init; } = default!;
}