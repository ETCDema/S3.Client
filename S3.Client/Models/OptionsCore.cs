namespace S3.Client.Models
{
	public abstract class OptionsCore
	{
		private readonly Dictionary<string, string> _items = [];

		protected string? Get(string name)
		{	
			_items.TryGetValue(name, out string? value);
			return value;
		}

		protected void Set(string name, string? value)
		{
			if (value is null)
				_items.Remove(name);
			else
				_items[name]	= value;
		}

		internal Dictionary<string, string> Items => _items;
	}
}
