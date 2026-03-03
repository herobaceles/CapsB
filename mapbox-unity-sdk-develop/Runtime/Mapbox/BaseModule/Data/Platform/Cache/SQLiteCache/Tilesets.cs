using Mapbox.BaseModule.Data.Platform.SQLite;

namespace Mapbox.BaseModule.Data.Platform.Cache.SQLiteCache
{

	/// <summary>
	/// Don't change the class name: sqlite-net uses it for table creation
	/// </summary>
	public class tilesets
	{

		//hrmpf: multiple PKs not supported by sqlite.net
		//https://github.com/praeclarum/sqlite-net/issues/282
		//TODO: do it via plain SQL
		[PrimaryKey, AutoIncrement]
		public int id { get; set; }

		public string name { get; set; }
	}
}
