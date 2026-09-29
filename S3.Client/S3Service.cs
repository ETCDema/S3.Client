using S3.Client.Models.Security;

namespace S3.Client
{
	public class S3Service
	{
		public S3Service(string baseUri, IS3Credential credential, string? region = null)
		{
			Host                = baseUri.IndexOf("://")<0 ? "https://"+baseUri : baseUri;
			Credential			= credential;
			Region				= region ?? "local";
		}

		protected S3Service(IS3Credential credential)
		{
			Credential			= credential;
		}

		public virtual string Type		{ get; } = "s3";

		public virtual string Region	{ get; } = "local";

		public virtual string Host		{ get; } = default!;
		
		public IS3Credential Credential { get; }
	}
}
