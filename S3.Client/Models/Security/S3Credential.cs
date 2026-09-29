namespace S3.Client.Models.Security;

public sealed class S3Credential : IS3Credential
{
    public S3Credential(string accessKeyId, string secretAccessKey)
    {
        ArgumentNullException.ThrowIfNull(accessKeyId);
        ArgumentNullException.ThrowIfNull(secretAccessKey);

        AccessKeyId				= accessKeyId;
        SecretAccessKey			= secretAccessKey;
    }

    // 16 - 32 characters
    public string AccessKeyId		{ get; }

    public string SecretAccessKey	{ get; }

    public string? SecurityToken	=> null;

    public bool ShouldRenew			=> false;

    public Task<bool> Renew()	=> Task.FromResult(false);
}
