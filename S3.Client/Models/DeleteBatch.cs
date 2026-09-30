using System.Xml.Serialization;

namespace S3.Client.Models;

/// <summary>
/// Данные пакетного удаления объектов
/// </summary>
public static class DeleteBatch
{
	/// <summary>
	/// Результат пакетного удаления
	/// </summary>
	[XmlRoot("DeleteResult", Namespace = S3Client.Namespace)]
	public sealed class Result
	{
		/// <summary>Удаленные объекты</summary>
		[XmlElement("Deleted")]
		public Item[]? Deleted	{ get; init; }

		/// <summary>Ошибки удаления объектов</summary>
		[XmlElement("Error")]
		public Error[]? Errors	{ get; init; }

		/// <summary>Признак наличия ошибок</summary>
		[XmlIgnore]
		public bool HasErrors => Errors is { Length: > 0 };

		/// <summary>Информация об удаленном объекте</summary>
		public readonly struct Item
		{
			public Item() { }

			public Item(string key)
			{
				Key				= key;
			}

			/// <summary>Идентификатор удаленного объекта</summary>
			[XmlElement]
			public string Key	{ get; init; } = default!;
		}

		/// <summary>
		/// Ошибка удаления объекта
		/// </summary>
		public sealed class Error
		{
			/// <summary>Идентификатор объекта</summary>
			[XmlElement]
			public string Key		{ get; init; } = default!;

			/// <summary>Код ошибки</summary>
			[XmlElement]
			public string Code		{ get; init; } = default!;

			/// <summary>Сообщение</summary>
			[XmlElement]
			public string Message	{ get; init; } = default!;
		}
	}
}
