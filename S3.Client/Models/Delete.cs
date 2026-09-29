namespace S3.Client.Models
{
	public static class Delete
	{
		public sealed class Result(string? deleteMarker, string? requestCharged, string? versionId)
		{
			public string? DeleteMarker			{ get; } = deleteMarker;

			public string? VersionId			{ get; } = versionId;

			public string? RequestCharged		{ get; } = requestCharged;

			public bool IsDeleteMarker			{ get; } = deleteMarker is "true";
		}
	}
}
