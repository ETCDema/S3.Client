using Microsoft.Extensions.ObjectPool;

namespace S3.Client.Helpers
{
	/// <summary>
	/// Пул массивов строк для повторного использования
	/// </summary>
	internal class StringListPool: IPooledObjectPolicy<List<string>>
	{
		private static readonly StringListPool _INSTANCE = new();

		private readonly ObjectPool<List<string>> _pool;

		private StringListPool()
		{
			_pool				= new DefaultObjectPool<List<string>>(this, Environment.ProcessorCount*3);
		}

		public static List<string> Get()
		{
			return _INSTANCE._pool.Get();
		}

		public static void Return(List<string> obj)
		{
			_INSTANCE._pool.Return(obj);
		}

		List<string> IPooledObjectPolicy<List<string>>.Create()
		{
			return new List<string>(3);
		}

		bool IPooledObjectPolicy<List<string>>.Return(List<string> obj)
		{
			obj.Clear();
			return true;
		}
	}
}
