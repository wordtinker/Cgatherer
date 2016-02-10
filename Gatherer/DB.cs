using System.Collections.Generic;
using System.Data.SQLite;

namespace Gatherer
{
    class Record
    {
        public int ProjectId { get; set; }
        public string Link { get; set; }
        public int Id { get; set; }
    }

    class DB
    {
        // DB connection
        private SQLiteConnection dbConn;

        public DB(string dbFile)
        {
            string connString = string.Format("Data Source={0};", dbFile);
            dbConn = new SQLiteConnection(connString);
            dbConn.Open();
            InitializeTables();
        }

        private void InitializeTables()
        {
            string sql = "CREATE TABLE IF NOT EXISTS articles( project INTEGER," +
                "link TEXT PRIMARY KEY, visited BOOLEAN)";
            using(SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public void AddPAge(int projectId, string link)
        {
            string sql = "INSERT OR IGNORE INTO articles " +
                "(project, link, visited) VALUES(@prj, @link, @visited)";
            using(SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.AddWithValue("@prj", projectId);
                cmd.Parameters.AddWithValue("@link", link);
                cmd.Parameters.AddWithValue("@visited", false);
                cmd.ExecuteNonQuery();
            }
        }

        public List<Record> GetUnvisistedPages()
        {
            List<Record> links = new List<Record>();
            string sql = "SELECT rowid, project, link FROM articles WHERE visited=0";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                SQLiteDataReader reader =  cmd.ExecuteReader();
                while (reader.Read())
                {
                    links.Add(new Record() {
                        Id = reader.GetInt32(0),
                        ProjectId = reader.GetInt32(1),
                        Link = reader.GetString(2)
                    });
                }
                reader.Close();
            }
            return links;
        }

        public void SetVisited(string url)
        {
            string sql = "UPDATE articles SET visited=1 WHERE link=@link";
            using (SQLiteCommand cmd = new SQLiteCommand(sql, dbConn))
            {
                cmd.Parameters.AddWithValue("@link", url);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
