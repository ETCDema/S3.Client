using S3.Client.Models.Security;

namespace S3.Client
{
	/// <summary>
	/// Данные сервиса для подключения
	/// </summary>
	/// <param name="baseUri">Базовый адрес сервиса</param>
	/// <param name="credential">Данные для авторизации</param>
	/// <param name="region">Регион сервиса</param>
	public class S3Service(string baseUri, IS3Credential credential, string? region = null)
	{
		/// <summary>Тип сервиса, всегда равен s3</summary>
		public virtual string Type		{ get; } = "s3";

		/// <summary>Регион размещения</summary>
		public virtual string Region	{ get; } = region ?? "local";

		/// <summary>Адрес сервиса для использования в запросах</summary>
		public virtual string Endpoint	{ get; } = baseUri.IndexOf("://")<0 ? "https://"+baseUri : baseUri;

		/// <summary>Данные для авторизации</summary>
		public IS3Credential Credential { get; } = credential;
	}
}
