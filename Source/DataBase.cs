using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace haze
{
    public static class DataBase
    {
        private const string DataSource = "SqliteDB.db";
        private const string SerializedLastIdPath = "LattestId.dat";
        private const string CommaSplitter = ", ";

        private static bool hasLastIdWasLoaded = false;
        private static int  cachedLastId = int.MinValue;

        private static readonly SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();

        private static readonly string createTableIfNotExistsCommand = new StringBuilder()
            .Append("CREATE TABLE IF NOT EXISTS Users")
            .Append("(")
            .Append("id INTEGER PRIMARY KEY, ")
            .Append("tags TEXT NOT NULL, ")
            .Append("form TEXT NOT NULL, ")
            .Append("form_id INTEGER, ")
            .Append("vieved_forms TEXT, ")
            .Append("discord_link TEXT NOT NULL, ")
            .Append("attachment_url TEXT NOT NULL")
            .Append(")")
            .ToString();

        private static readonly string insertValuesIntoUsersCommandToFormat = new StringBuilder()
            .Append("INSERT INTO ")
            .Append("Users(")
            .Append("id, tags, form, form_id, discord_link, attachment_url) ")
            .Append("VALUES(")
            .Append("{0}, {1}, {2}, {3}, {4}, {5})")
            .ToString();

        public static int LastFormId
        {
            get
            {
                if (hasLastIdWasLoaded) return cachedLastId;
                cachedLastId = LoadLastFormId();
                hasLastIdWasLoaded = true;
                return cachedLastId;
            }
        }

        public static Dictionary<ulong, string[]> Initialize()
        {
            connectionStringBuilder.DataSource = DataSource;

            var idTagsPairs = new Dictionary<ulong, string[]>();

            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();

                using var command = new SqliteCommand(createTableIfNotExistsCommand, connection);

                command.ExecuteNonQuery();

                using var command1 = new SqliteCommand($"SELECT id,tags FROM Users", connection);
                var reader = command1.ExecuteReader();

                while (reader.Read())
                {
                    idTagsPairs.Add(Convert.ToUInt64(reader.GetValue(0)), reader.GetString(1).Split(CommaSplitter));
                }
                connection.Close();
                return idTagsPairs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public static void SaveForm(Respondent respondent)
        {
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                var command = new SqliteCommand(
                    string.Format("DELETE FROM Users WHERE id = {0}", respondent.Id),
                    connection);
                command.ExecuteNonQuery();
                command = new SqliteCommand(string.Format(
                    insertValuesIntoUsersCommandToFormat,
                    respondent.Id,
                    string.Join(CommaSplitter, respondent.Tags),
                    respondent.Form,
                    respondent.FormId,
                    respondent.DiscordLink,
                    respondent.AttachmentUrl), connection);
                command.ExecuteNonQuery();
                connection.Close();

                using var stream = File.Open(SerializedLastIdPath, FileMode.Create);

                using var writer = new BinaryWriter(stream, Encoding.UTF8, false);

                writer.Write(respondent.FormId);

                cachedLastId = respondent.FormId;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static int LoadLastFormId()
        {
            var id = int.MinValue;

            if (!File.Exists(SerializedLastIdPath))
            {
                return id;
            }

            using var stream = File.Open(SerializedLastIdPath, FileMode.Open);

            using var reader = new BinaryReader(stream, Encoding.UTF8, false);

            id = reader.ReadInt32();

            return id;
        }

        public static int LoadFormId(ulong respondentId)
        {
            var formId = 0;
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                using var command = new SqliteCommand(
                    string.Format("SELECT form_id FROM Users WHERE id = {0}", respondentId),
                    connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    formId = reader.GetInt32(0);
                }
                connection.Close();
                return formId;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 0;
            }
        }

        public static Respondent LoadForm(ulong respondentId)
        {
            var respondent = new Respondent();
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();

                using var command = new SqliteCommand(
                    string.Format("SELECT id, tags, form, form_id, discord_link, attachment_url FROM Users WHERE id = {0}", respondentId),
                    connection);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    respondent.Id = Convert.ToUInt64(reader.GetValue(0));
                    respondent.Tags = reader.GetString(1).Split(CommaSplitter);
                    respondent.Form = reader.GetString(2);
                    respondent.FormId = reader.GetInt32(3);
                    respondent.DiscordLink = reader.GetString(4);
                    respondent.AttachmentUrl = reader.GetString(5);
                }
                connection.Close();

                return respondent;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public static void AddVieviedFormIdToRespondent(ulong respondentId, int formId)
        {
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                string response = null;

                using var command = new SqliteCommand(
                    string.Format("SELECT vieved_forms FROM Users WHERE id = {0}", respondentId),
                    connection);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    response = reader.GetString(0);
                }
                response += formId.ToString();

                using var command1 = new SqliteCommand(
                    string.Format("INSERT INTO Users (vieved_forms) VALUES ({0})", response),
                    connection);

                var reader1 = command1.ExecuteReader();
                while (reader1.Read())
                {
                    response = reader1.GetString(0);
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}