using LanguageExt;
using static LanguageExt.Prelude;
using System;
using System.Data.SQLite;
using static Gatherer.ConnectionHelper;

namespace Gatherer
{
    static class ConnectionHelper
    {
        public static R Connect<R>(string connString,
            Func<SQLiteConnection, R> f)
        {
            using (var conn = new SQLiteConnection(connString))
            {
                conn.Open();
                return f(conn);
            }
        }
        public static Lst<R> ExecuteQuery<R>(string sql,
            SQLiteConnection conn, Func<SQLiteDataReader, R> f)
        {
            Lst<R> list = new Lst<R>();
            var cmd = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list = list.Add(f(reader));
            }
            reader.Close();
            return list;
        }
        public static int ExecuteNonQuery(string sql, SQLiteConnection conn)
        {
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            return cmd.ExecuteNonQuery();
        }
        public static int ExecuteNonQuery(string sql, SQLiteConnection conn, params SQLiteParameter[] parameters)
        {
            SQLiteCommand cmd = new SQLiteCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            return cmd.ExecuteNonQuery();
        }
    }

    static class Procedures
    {
        public static string Init { get; } = 
            $"CREATE TABLE IF NOT EXISTS articles( project INTEGER, link TEXT PRIMARY KEY, visited BOOLEAN)";
        public static string AddPage { get; } =
            $"INSERT OR IGNORE INTO articles (project, link, visited) VALUES(@prj, @link, @visited)";
        public static string SetVisited { get; } =
            "UPDATE articles SET visited=1 WHERE link=@link";
        public static string GetUnvisited { get; } =
            "SELECT rowid, project, link FROM articles WHERE visited=0";
    }

    static class Repository
    {
        static string connString = string.Format("Data Source=projects.db;");

        public static Try<Unit> InitializeTables() => () =>
        {
            Connect(connString, conn
                => ExecuteNonQuery(Procedures.Init, conn));
            return unit;
        };

        public static Try<Unit> AddPage(int projectId, string link) => () =>
        {
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@prj", projectId),
                new SQLiteParameter("@link", link),
                new SQLiteParameter("@visited", false)
            };
            Connect(connString, conn
                => ExecuteNonQuery(Procedures.AddPage, conn, parameters));
            return unit;
        };

        public static Try<Unit> SetVisited(string url) => () =>
        {
            Connect(connString, conn
                => ExecuteNonQuery(Procedures.SetVisited, conn,
                   new SQLiteParameter("@link", url)));
            return unit;
        };

        public static Try<Lst<(int Id, int ProjectId, string Link)>> GetUnvisistedPages() => ()
            => Connect(connString, conn
                => ExecuteQuery(Procedures.GetUnvisited, conn, reader
                    =>(
                        Id: reader.GetInt32(0),
                        ProjectId: reader.GetInt32(1),
                        Link: reader.GetString(2)
                    )));
    }
}
