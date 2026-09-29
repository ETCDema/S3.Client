using System.Xml.Serialization;

namespace S3.Client.Models.Errors;

[XmlRoot("Error")]
public sealed class S3Error
{
    [XmlElement]
    public string Code { get; init; } = default!;

	[XmlElement]
    public string Message { get; init; } = default!;

	[XmlElement]
    public string Resource { get; init; } = default!;

	[XmlElement]
    public string RequestId { get; init; } = default!;

	[XmlElement]
    public string HostId { get; init; } = default!;

	// TODO?
	// RangeRequested
	// ActualObjectSize
}

/*

https://docs.aws.amazon.com/AmazonS3/latest/API/ErrorResponses.html

<?xml version="1.0" encoding="UTF-8"?>
<Error>
	<Code>NoSuchKey</Code>
	<Message>The resource you requested does not exist</Message>
	<Resource>/my-bucket/test.txt</Resource> 
	<RequestId>...</RequestId>
 	<HostId>...</HostId>
</Error>
*/