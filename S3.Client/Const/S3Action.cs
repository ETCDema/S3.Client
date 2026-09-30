namespace S3.Client.Const;

/// <summary>Действия для подстановки в запрос</summary>
internal static class S3Action
{
    public static readonly string Delete		= "?delete";
	public static readonly string Restore		= "?restore";
	public static readonly string Tagging		= "?tagging";
	public static readonly string Uploads		= "?uploads";
	public static readonly string Versions		= "?versions";
}