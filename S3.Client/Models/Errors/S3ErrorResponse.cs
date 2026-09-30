using System.Xml.Serialization;

namespace S3.Client.Models.Errors;

/// <summary>
/// Нестандартный ответ с ошибкой, например из Wasabi
/// </summary>
[XmlRoot("ErrorResponse", Namespace = "https://iam.amazonaws.com/doc/2010-05-08/")]
public sealed class S3ErrorResponse
{
	/// <summary>Непосредственно ошибка</summary>
    public S3Error Error { get; init; } = default!;
}
