namespace S3.Client.Models
{
	/// <summary>
	/// Основа для классов - переметров
	/// </summary>
	public abstract class OptionsCore
	{
		private readonly Dictionary<string, string> _items = [];

		/// <summary>
		/// Получить значение параметра
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected string? Get(string name)
		{	
			_items.TryGetValue(name, out string? value);
			return value;
		}

		/// <summary>
		/// Установить значение параметра
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		protected void Set(string name, string? value)
		{
			if (value is null)
				_items.Remove(name);
			else
				_items[name]	= value;
		}

		/// <summary>
		/// Все параметры
		/// </summary>
		internal Dictionary<string, string> Items => _items;
	}
}
