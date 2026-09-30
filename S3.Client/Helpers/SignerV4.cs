using System.Buffers;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using LinkDotNet.StringBuilder;

using S3.Client.Const;
using S3.Client.Models.Security;

namespace S3.Client.Helpers;

/// <summary>
/// Методы для работы с подписью
/// </summary>
public static class SignerV4
{
    private const string _algorithmName			= "AWS4-HMAC-SHA256";
    private const string _isoDateTimeFormat		= "yyyyMMddTHHmmssZ";  // ISO8601

	/// <summary>
	/// Подписать запрос и добавить пдпись в заголовки запроса
	/// </summary>
	/// <param name="credential">Данные авторизации</param>
	/// <param name="scope">Область подписи</param>
	/// <param name="request">Запрос</param>
	[SkipLocalsInit]
	public static void Sign(IS3Credential credential, in CredentialScope scope, HttpRequestMessage request)
	{
		if (!request.Headers.NonValidated.Contains(S3HeaderNames.ContentSHA256))
			request.Headers.TryAddWithoutValidation(S3HeaderNames.ContentSHA256, ComputeSHA256(request.Content));

		Span<byte> signingKey	= stackalloc byte[32];
		_computeSigningKey(credential.SecretAccessKey, scope, signingKey);

		var signedHeaderNames   = StringListPool.Get();

		// AWS4-HMAC-SHA256 Credential={credential.AccessKeyId}/{scope},SignedHeaders={signedHeaders},Signature={signature}
		var stringToSign        = GetStringToSign(request, scope, signedHeaderNames);
		var sb                  = new ValueStringBuilder (stackalloc char[512]); // ~235
		sb.Append("AWS4-HMAC-SHA256 Credential=");
		sb.Append(credential.AccessKeyId);
		sb.Append('/');
		scope.AppendTo(ref sb);
		sb.Append(",SignedHeaders=");
		sb.AppendJoin(';', signedHeaderNames);
		sb.Append(",Signature=");
		_computeAndWriteHMACSHA256Hex(signingKey, stringToSign, ref sb); // signature

		StringListPool.Return(signedHeaderNames);

		request.Headers.TryAddWithoutValidation("Authorization", sb.ToString());
		sb.Dispose();
	}


	[SkipLocalsInit]
    internal static string GetStringToSign(HttpRequestMessage request, in CredentialScope scope, List<string> signedHeaderNames)
    {
        ArgumentNullException.ThrowIfNull(request);

		var timestamp			= request.Headers.NonValidated.TryGetValues(S3HeaderNames.Date, out var xAmzDateHeader)
								? xAmzDateHeader.ToString()
								: throw new Exception($"Missing '{S3HeaderNames.Date}' header");

        var sb					= new ValueStringBuilder (stackalloc char[512]); // ~350

        WriteCanonicalRequest(ref sb, request, signedHeaderNames);

        var result				= GetStringToSign(
            scope				: scope,
            timestamp			: timestamp,
            canonicalRequest	: sb.ToString()
        );

		sb.Dispose();
        return result;
    }

    [SkipLocalsInit]
    public static string GetStringToSign(in CredentialScope scope, string timestamp, ReadOnlySpan<char> canonicalRequest)
    {
		var output				= new ValueStringBuilder (stackalloc char[256]);  // avg ~138

        output.Append(_algorithmName)  ; output.Append('\n'); // Algorithm + \n
        output.Append(timestamp)      ; output.Append('\n'); // Timestamp + \n
        scope.AppendTo(ref output)    ; output.Append('\n'); // Scope     + \n
        _sHA256_Hex(canonicalRequest, ref output); // Hex(SHA256(CanonicalRequest))

        var result				= output.ToString();
		output.Dispose();
		return result;
    }

	internal static void WriteCanonicalRequest(ref ValueStringBuilder output, HttpRequestMessage request, List<string> signedHeaderNames)
    {
        output.Append(request.Method.Method)                                ; output.Append('\n'); // HTTPRequestMethod      + \n
        _writeCanonicalizedUri(ref output, request.RequestUri!.AbsolutePath) ; output.Append('\n'); // CanonicalURI           + \n
        _writeCanonicalizedQueryString(ref output, request.RequestUri)       ; output.Append('\n'); // CanonicalQueryString   + \n
        _writeCanonicalizedHeaders(ref output, request, signedHeaderNames)   ; output.Append('\n'); // CanonicalHeaders       + \n
                                                                            ; output.Append('\n'); //                        + \n
        output.AppendJoin(';', signedHeaderNames)                           ; output.Append('\n'); // SignedHeaders          + \n
        output.Append(GetPayloadHash(request));                                                    // HexEncode(Hash(Payload))
    }

    [SkipLocalsInit]
    public static string CanonicalizeUri(string path)
    {
        if (path is "/") return path;

		var output				= new ValueStringBuilder (stackalloc char[256]);

		_writeCanonicalizedUri(ref output, path);

		var result              = output.ToString();
		output.Dispose();
		return result;

	}

	private static void _writeCanonicalizedUri(ref ValueStringBuilder output, ReadOnlySpan<char> path)
    {
        if (path is "/")
        {
            output.Append('/');
            return;
        }

        foreach (var segmentRange in path.Split('/'))
        {
            var segment = path[segmentRange];

            if (segment.IsEmpty) continue;

            output.Append('/');

            // Do not double escape
            if (segment.Contains('%'))
            {
                output.Append(segment);
            }
            else
            {
                output.Append(Uri.EscapeDataString(segment));
            }
        }
    }

    [SkipLocalsInit]
    public static string GetCanonicalRequest(HttpMethod method, string canonicalURI, string canonicalQueryString, string canonicalHeaders, string signedHeaders, string payloadHash)
    {
		using var sb            = new ValueStringBuilder (stackalloc char[512]); // ~350

		sb.Append(method.Method);        sb.Append('\n'); // HTTPRequestMethod           + \n
        sb.Append(canonicalURI);         sb.Append('\n'); // CanonicalURI                + \n
        sb.Append(canonicalQueryString); sb.Append('\n'); // CanonicalQueryString        + \n
        sb.Append(canonicalHeaders);     sb.Append('\n'); // CanonicalHeaders            + \n
        sb.Append(string.Empty);         sb.Append('\n'); //                             + \n
        sb.Append(signedHeaders);        sb.Append('\n'); // SignedHeaders               + \n
        sb.Append(payloadHash);                           // HexEncode(Hash(Payload))

        return sb.ToString();
    }

    // HexEncode(Hash(Payload))
    // If the payload is empty, use an empty string
    internal static string GetPayloadHash(HttpRequestMessage request)
    {
        return request.Headers.NonValidated.TryGetValues(S3HeaderNames.ContentSHA256, out var contentSha256Header)
            ? contentSha256Header.ToString()
            : ComputeSHA256(request.Content);
    }

    private static ReadOnlySpan<byte> _aws4_request => "aws4_request"u8;
    private static ReadOnlySpan<byte> _aWS4 => "AWS4"u8;

    [SkipLocalsInit]
    private static void _computeSigningKey(string secretAccessKey, in CredentialScope scope, Span<byte> signingKey)
    {
        if (secretAccessKey.Length > 256)
        {
            throw new ArgumentException("Must be 256 or fewer chars", nameof(secretAccessKey));
        }

        Span<byte> kSecret = stackalloc byte[secretAccessKey.Length + 4];

        _aWS4.CopyTo(kSecret);
        Ascii.FromUtf16(secretAccessKey, kSecret[4..], out _);

        Span<byte> formattedDateBytes = stackalloc byte[8];

        scope.FormatDateTo(formattedDateBytes);

        HMACSHA256.HashData(kSecret, formattedDateBytes,        signingKey);
        HMACSHA256.HashData(signingKey, Encoding.ASCII.GetBytes(scope.Region)/*.Utf8Name*/,  signingKey);
        HMACSHA256.HashData(signingKey, Encoding.ASCII.GetBytes(scope.Service)/*.Utf8Name*/, signingKey);
        HMACSHA256.HashData(signingKey, _aws4_request,         signingKey);
    }

    // http://docs.aws.amazon.com/general/latest/gr/sigv4-add-signature-to-request.html

    [SkipLocalsInit]
    public static string GetPresignedUrl(IS3Credential credential, CredentialScope scope, DateTime date, TimeSpan expires, HttpMethod method, Uri requestUri, string payloadHash = _emptySha256)
    {
        const string signedHeaders = "host";

        Span<byte> signingKey = stackalloc byte[32];
        
        _computeSigningKey(credential.SecretAccessKey, scope, signingKey);

        SortedDictionary<string, string> queryParameters = requestUri.Query is { Length: > 0 } queryString
            ? _parseQueryString(queryString)
            : [];
      
        // 16 chars
        string timestamp = date.ToString(format: _isoDateTimeFormat, CultureInfo.InvariantCulture);

        queryParameters[SigningParameterNames.Algorithm]  = _algorithmName;
        queryParameters[SigningParameterNames.Credential] = $"{credential.AccessKeyId}/{scope}";
        queryParameters[SigningParameterNames.Date]       = timestamp;
        queryParameters[SigningParameterNames.Expires]    = expires.TotalSeconds.ToString(CultureInfo.InvariantCulture); // in seconds

        if (credential.SecurityToken is not null)
        {
            queryParameters[SigningParameterNames.SecurityToken] = credential.SecurityToken;
        }

        queryParameters[SigningParameterNames.SignedHeaders] = signedHeaders;

        string canonicalHeaders = requestUri.IsDefaultPort
            ? $"host:{requestUri.Host}"
            : string.Create(CultureInfo.InvariantCulture, $"host:{requestUri.Host}:{requestUri.Port}");

        string canonicalRequest = GetCanonicalRequest(
            method               : method,
            canonicalURI         : CanonicalizeUri(requestUri.AbsolutePath),
            canonicalQueryString : _canonicalizeQueryString(queryParameters),
            canonicalHeaders     : canonicalHeaders,
            signedHeaders        : signedHeaders,
            payloadHash          : payloadHash
        );

        string stringToSign = GetStringToSign(scope, timestamp, canonicalRequest);
                        
        /*
        queryString = Action=action
        queryString += &X-Amz-Algorithm=algorithm
        queryString += &X-Amz-Credential=urlencode({accessKeyId}/{credentialScope})
        queryString += &X-Amz-Date=date
        queryString += &X-Amz-Expires=timeout interval
        queryString += &X-Amz-SignedHeaders=signed_headers
        */

		var urlBuilder    = new ValueStringBuilder (stackalloc char[512]); // ~350

		urlBuilder.Append(requestUri.Scheme);
		urlBuilder.Append("://");
		urlBuilder.Append(requestUri.Host);

        if (!requestUri.IsDefaultPort)
        {
            urlBuilder.Append(':');
            urlBuilder.Append(requestUri.Port);
        }

        urlBuilder.Append(requestUri.AbsolutePath);
        urlBuilder.Append('?');

        foreach (KeyValuePair<string, string> pair in queryParameters)
        {
            urlBuilder.Append(UrlEncoder.Default.Encode(pair.Key));
            urlBuilder.Append('=');
            urlBuilder.Append(UrlEncoder.Default.Encode(pair.Value));
            urlBuilder.Append('&');         
        }

        urlBuilder.Append(SigningParameterNames.Signature);
        urlBuilder.Append('=');
        _computeAndWriteHMACSHA256Hex(signingKey, stringToSign, ref urlBuilder); //signature

		var result				= urlBuilder.ToString();
		urlBuilder.Dispose();
		return result;
    }

    [SkipLocalsInit]
    private static string _canonicalizeQueryString(SortedDictionary<string, string> sortedValues)
    {
        if (sortedValues.Count is 0)
        {
            return string.Empty;
        }

		var output				= new ValueStringBuilder (stackalloc char[256]);

		_writeCanonicalQueryString(ref output, sortedValues);

		var result              = output.ToString();
		output.Dispose();
		return result;
	}

	private static void _writeCanonicalizedQueryString(ref ValueStringBuilder output, Uri uri)
    {
        if (string.IsNullOrEmpty(uri.Query) || uri.Query is "?")
        {
            return;
        }

        _writeCanonicalQueryString(ref output, _parseQueryString(uri.Query));
    }

    private static void _writeCanonicalQueryString(ref ValueStringBuilder output, SortedDictionary<string, string> sortedValues)
    {
        int i = 0;

        foreach (var pair in sortedValues)
        {
            if (i > 0)
            {
                output.Append('&');
            }

            output.Append(UrlEncoder.Default.Encode(pair.Key));
            output.Append('=');
            output.Append(UrlEncoder.Default.Encode(pair.Value));

            i++;
        }
    }

    private static SortedDictionary<string, string> _parseQueryString(string query)
    {
        return _parseQueryString(query.AsSpan());
    }

    private static SortedDictionary<string, string> _parseQueryString(ReadOnlySpan<char> query)
    {
        if (query.IsEmpty)
        {
            return [];
        }

        var dictionary = new SortedDictionary<string, string>();

        if (query[0] is '?')
        {
            query = query[1..];
        }

        foreach (var segmentRange in query.Split('&'))
        {
            var segment = query[segmentRange];

            if (segment.IsEmpty) continue;

            int equalIndex = segment.IndexOf('=');

            string lhs  = equalIndex > -1 ? segment[..equalIndex].ToString() : segment.ToString();
            string? rhs = equalIndex > -1 ? segment[(equalIndex + 1)..].ToString() : null;

            dictionary[WebUtility.UrlDecode(lhs)] = rhs is not null
                ? WebUtility.UrlDecode(rhs)
                : string.Empty;
        }

        return dictionary;
    }

	private static void _writeCanonicalizedHeaders(ref ValueStringBuilder output, HttpRequestMessage request, List<string> signedHeaderNames)
    {
        if (request.Content is not null)
        {
            var contentHeaders = request.Content.Headers.NonValidated;

            /*
            note Content-Length header is may not yet be materialized
            see: https://github.com/dotnet/runtime/issues/25086
            if (contentHeaders.TryGetValues("Content-Length", out var contentLengthHeader))
            {
                signedHeaderNames.Add("content-length");

                output.Append("content-length:");
                output.Append(contentLengthHeader.ToString());
                output.Append('\n');
            }
            */

            if (contentHeaders.TryGetValues("Content-MD5", out var md5Header))
            {
                signedHeaderNames.Add("content-md5");

                output.Append("content-md5:");
                output.Append(md5Header.ToString());
                output.Append('\n');
            }

            if (contentHeaders.TryGetValues("Content-Type", out var contentTypeHeader))
            {
                signedHeaderNames.Add("content-type");

                output.Append("content-type:");
                output.Append(contentTypeHeader.ToString());
                output.Append('\n');
            }
        }

        if (!request.Headers.NonValidated.Contains(S3HeaderNames.Date) && request.Headers.NonValidated.TryGetValues("Date", out var dateHeader))
        {
            signedHeaderNames.Add("date");

            output.Append("date:");
            output.Append(dateHeader.ToString());
            output.Append('\n');
        }

        signedHeaderNames.Add("host");

        output.Append("host:");
        output.Append(request.Headers.Host);

        // Convert header names to lowercase
        // Order by name

        foreach (var header in request.Headers.NonValidated
            .Where(static item => item.Key.StartsWith("x-amz-", StringComparison.OrdinalIgnoreCase))
            .OrderBy(static item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            string headerName = header.Key.ToLowerInvariant();

            output.Append('\n');

            output.Append(headerName);
            output.Append(':');
            output.Append(header.Value.ToString());

            signedHeaderNames.Add(headerName);
        }
    }

    // The host header must be included as a signed header.

    #region SHA Helpers

    private const string _emptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    [SkipLocalsInit]
    public static string ComputeSHA256(HttpContent? content)
    {
		// TODO: Плохо, у нас может быть поток, который можем прочитать только один раз и он может быть огромным
		if (content?.ReadAsByteArrayAsync().Result is byte[] { Length: > 0 } source)
        {
            Span<byte> sha256 = stackalloc byte[32];

            SHA256.HashData(source, sha256);

            return Convert.ToHexStringLower(sha256);
        } else
        {
			return _emptySha256;
        }
    }

    [SkipLocalsInit]
    private static void _computeAndWriteHMACSHA256Hex(ReadOnlySpan<byte> key, ReadOnlySpan<char> data, ref ValueStringBuilder destination)
    {
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(data.Length * 4);

        int encodedByteCount = Encoding.UTF8.GetBytes(data, rentedBuffer);

        Span<byte> hash = stackalloc byte[32];

        HMACSHA256.HashData(key, rentedBuffer.AsSpan(0, encodedByteCount), hash);

        ArrayPool<byte>.Shared.Return(rentedBuffer);

		destination.Append(Convert.ToHexStringLower(hash));
    }

    [SkipLocalsInit]
    private static void _sHA256_Hex(ReadOnlySpan<char> data, ref ValueStringBuilder destination)
    {
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(data.Length * 4);

        int encodedByteCount = Encoding.UTF8.GetBytes(data, rentedBuffer);

        Span<byte> hash = stackalloc byte[32];

        SHA256.HashData(rentedBuffer.AsSpan(0, encodedByteCount), hash);

        ArrayPool<byte>.Shared.Return(rentedBuffer);

		destination.Append(Convert.ToHexStringLower(hash));
	}

	#endregion
}

// Signed Header Notes ---

// - Convert all header names to lowercase
// - Sort them by character code
// - Use a semicolon to separate the header names