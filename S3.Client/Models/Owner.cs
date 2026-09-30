using System.Xml.Serialization;

namespace S3.Client.Models;

/// <summary>
/// Информация о владельце объекта
/// </summary>
public sealed class Owner
{
	/// <summary>Нет владельца</summary>
	public static readonly Owner Empty = new();

    [XmlElement("ID")]
    public string ID			{ get; init; } = default!;

	[XmlElement("DisplayName")]
    public string DisplayName	{ get; init; } = default!;
}