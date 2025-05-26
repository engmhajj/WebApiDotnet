

using System.Data.SQLite;

namespace webapi.Db
{
	public class Sqlite
	{
		public void createConnection()
		{
			// this creates a zero-byte file
			SQLiteConnection.CreateFile("MyDatabase.sqlite");

			string connectionString = "Data Source=MyDatabase.sqlite";
			SQLiteConnection m_dbConnection = new SQLiteConnection(connectionString);
			m_dbConnection.Open();

			// varchar will likely be handled internally as TEXT
			// the (20) will be ignored
			// see https://www.sqlite.org/datatype3.html#affinity_name_examples
			string sql = "Create Table highscores (name varchar(20), score int)";
			// you could also write sql = "CREATE TABLE IF NOT EXISTS highscores ..."
			SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
			command.ExecuteNonQuery();

			sql = "Insert into highscores (name, score) values ('Me', 9001)";
			command = new SQLiteCommand(sql, m_dbConnection);
			command.ExecuteNonQuery();

			m_dbConnection.Close();

		}
	}
}
