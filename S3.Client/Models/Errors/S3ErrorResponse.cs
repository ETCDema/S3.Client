using System.Xml.Serialization;

namespace S3.Client.Models.Errors;

[XmlRoot("ErrorResponse", Namespace = "https://iam.amazonaws.com/doc/2010-05-08/")]
public sealed class S3ErrorResponse
{
    public S3Error Error { get; init; } = default!;
}

// Wasabi returns a non-compliant exception under the iam namespace