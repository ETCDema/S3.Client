using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace S3.Client.Services;

internal static class HashHelper
{
    public static byte[] ComputeMD5(ReadOnlySpan<char> text)
    {
        byte[] rentedBuffer		= ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(text.Length));
        int bufferLength		= Encoding.UTF8.GetBytes(text, rentedBuffer);
        var result				= MD5.HashData(rentedBuffer.AsSpan(0, bufferLength));

        ArrayPool<byte>.Shared.Return(rentedBuffer);

        return result;
    }

	public static byte[]? TryComputeSHA256(Stream stream)
	{
		if (!stream.CanSeek) return null;
		byte[] hash				= SHA256.HashData(stream);
		stream.Position			= 0;
		return hash;
	}
}