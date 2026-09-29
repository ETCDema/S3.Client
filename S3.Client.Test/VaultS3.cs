namespace S3.Client.Test
{
	[Trait("Service", nameof(VaultS3))]
	public class VaultS3(ITestOutputHelper output) : S3ApiTests
	{
		protected override S3Bucket GetBucket()
		{
			return new S3TestBucket(Config.Get(nameof(VaultS3)), output);
		}
	}
}
