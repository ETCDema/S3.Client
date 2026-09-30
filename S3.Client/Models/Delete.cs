namespace S3.Client.Models;

/// <summary>
/// Данные удаления объектов
/// </summary>
public static class Delete
{
	/// <summary>
	/// Результат удаления
	/// </summary>
	/// <param name="deleteMarker"></param>
	/// <param name="requestCharged"></param>
	/// <param name="versionId"></param>
	public sealed class Result(string? deleteMarker, string? requestCharged, string? versionId)
	{
		public string? DeleteMarker		{ get; } = deleteMarker;

		public string? VersionId		{ get; } = versionId;

		public string? RequestCharged	{ get; } = requestCharged;

		public bool IsDeleteMarker		{ get; } = deleteMarker is "true";
	}
}
