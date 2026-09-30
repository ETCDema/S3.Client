namespace S3.Client.Models.Security;

/// <summary>
/// Данные для авторизации на основе AccessKeyId + SecretAccessKey
/// </summary>
public sealed class S3Credential : IS3Credential
{
    public S3Credential(string accessKeyId, string secretAccessKey)
    {
        ArgumentNullException.ThrowIfNull(accessKeyId);
        ArgumentNullException.ThrowIfNull(secretAccessKey);

        AccessKeyId				= accessKeyId;
        SecretAccessKey			= secretAccessKey;
    }

    public string AccessKeyId		{ get; }

    public string SecretAccessKey	{ get; }

    public string? SecurityToken	=> null;

    public bool ShouldRenew			=> false;

    public Task<bool> Renew()	=> Task.FromResult(false);
}
