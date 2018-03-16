﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using static Gatherer.ConnectionHelper;

namespace Gatherer
{
    class Record
    {
        public int ProjectId { get; set; }
        public string Link { get; set; }
        public int Id { get; set; }
    }

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
        public static List<R> ExecuteQuery<R>(string sql,
            SQLiteConnection conn, Func<SQLiteDataReader, R> f)
        {
            List<R> list = new List<R>();
            var cmd = new SQLiteCommand(sql, conn);
            SQLiteDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(f(reader));
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

    class Repository
    {
        string connString = string.Format("Data Source=projects.db;");
        public void InitializeTables()
            => Connect(connString, conn
                => ExecuteNonQuery(Procedures.Init, conn));

        public void AddPAge(int projectId, string link)
        {
            SQLiteParameter[] parameters =
            {
                new SQLiteParameter("@prj", projectId),
                new SQLiteParameter("@link", link),
                new SQLiteParameter("@visited", false)
            };
            Connect(connString, conn
                => ExecuteNonQuery(Procedures.AddPage, conn, parameters));
        }

        public void SetVisited(string url)
            => Connect(connString, conn
                => ExecuteNonQuery(Procedures.SetVisited, conn,
                   new SQLiteParameter("@link", url)));

        public List<Record> GetUnvisistedPages()
            => Connect(connString, conn
                => ExecuteQuery(Procedures.GetUnvisited, conn, reader
                    => new Record
                    {
                        Id = reader.GetInt32(0),
                        ProjectId = reader.GetInt32(1),
                        Link = reader.GetString(2)
                    }));
    }
}
