using System.Globalization;
using System.Text;

using LinkDotNet.StringBuilder;

namespace S3.Client.Models.Security;

public readonly struct CredentialScope : ISpanFormattable
{
    public CredentialScope(DateOnly date, string region, string service)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(service);

        Date = date;
        Region = region;
        Service = service;
    }

    public DateOnly Date { get; }

	public string Region { get; }

	public string Service { get; }

    // 20120228/us-east-1/iam/aws4_request
    public readonly override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Date:yyyyMMdd}/{Region}/{Service}/aws4_request");
    }

    private const string _dateFormat = "yyyyMMdd";

    internal void FormatDateTo(Span<byte> utf8Destination)
    {
        Date.TryFormat(utf8Destination, out _, _dateFormat, CultureInfo.InvariantCulture);
    }

	internal void FormatDateTo(ref ValueStringBuilder utf8Destination)
	{
		utf8Destination.Append(Date.ToString(_dateFormat, CultureInfo.InvariantCulture));
	}

	internal void FormatDateTo(Span<char> utf16Destination)
    {
        Date.TryFormat(utf16Destination, out _, _dateFormat, CultureInfo.InvariantCulture);
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return destination.TryWrite(CultureInfo.InvariantCulture, $"{Date:yyyyMMdd}/{Region}/{Service}/aws4_request", out charsWritten);
    }

    internal readonly void AppendTo(ref ValueStringBuilder output)
    {
        FormatDateTo(ref output);
        output.Append('/');
        output.Append(Region);
        output.Append('/');
        output.Append(Service);
        output.Append('/');
        output.Append("aws4_request");
    }
}