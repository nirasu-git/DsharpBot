using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

namespace haze.Source
{
    public static class DataBase
    {
        private static SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();

        public static Dictionary<ulong, string[]> Initialize()
        {
            connectionStringBuilder.DataSource = "./SqliteDB.db";

            Dictionary<ulong, string[]> idTagsPairs = new Dictionary<ulong, string[]>();

            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();

                using var command = new SqliteCommand(
                    $"CREATE TABLE IF NOT EXISTS Users" +
                    $"(" +
                    $"id INTEGER PRIMARY KEY, " +
                    $"tags TEXT NOT NULL, " +
                    $"form TEXT NOT NULL, " +
                    $"form_id INTEGER, " +
                    $"vieved_forms TEXT, " +
                    $"discord_link TEXT NOT NULL, " +
                    $"attachment_url TEXT NOT NULL" +
                    $")", connection);
                command.ExecuteNonQuery();

                using var command1 = new SqliteCommand($"SELECT id,tags FROM Users", connection);
                var reader = command1.ExecuteReader();
                while (reader.Read())
                {
                    idTagsPairs.Add(Convert.ToUInt64(reader.GetValue(0)), reader.GetString(1).Split(", "));
                }
                connection.Close();
                return idTagsPairs;
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " Database exception");
                return null;
            }
        }

        public static string SaveForm(Respondent respondent)
        {
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                var command = new SqliteCommand($"DELETE FROM Users WHERE id = {respondent.Id}", connection);
                command.ExecuteNonQuery();

                command = new SqliteCommand("INSERT INTO " +
                    "Users(" +
                    "id, tags, form, form_id, discord_link, attachment_url) " +
                    "VALUES(" +
                    "@id, @tags, @form, @form_id, @discord_link, @attachment_url)", connection);
                command.Parameters.AddWithValue("@id", respondent.Id);
                command.Parameters.AddWithValue("@tags", string.Join(", ", respondent.Tags));
                command.Parameters.AddWithValue("@form", respondent.Form);
                command.Parameters.AddWithValue("@form_id", respondent.FormId);
                command.Parameters.AddWithValue("@discord_link", respondent.DiscordLink);
                command.Parameters.AddWithValue("@attachment_url", respondent.AttachmentUrl);
                command.ExecuteNonQuery();
                connection.Close();
                return "0";
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " Database exception");
                return null;
            }
        }

        public static int LoadFormId(ulong respondentId)
        {
            int formId = 0;
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                using var command = new SqliteCommand($"SELECT form_id FROM Users WHERE id = {respondentId}", connection);
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
                Console.WriteLine(e + " Database exception");
                return 0;
            }
        }

        public static Respondent LoadForm(ulong respondentId)
        {
            Respondent respondent = new Respondent();
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();
                using var command = new SqliteCommand($"SELECT id, tags, form, form_id, discord_link, attachment_url FROM Users WHERE id = {respondentId}", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    respondent.Id = Convert.ToUInt64(reader.GetValue(0));
                    respondent.Tags = reader.GetString(1).Split(", ");
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
                Console.WriteLine(e + " Database exception");
                return null;
            }
        }

        public static void AddVieviedFormIdToRespondent(ulong respondentId, int formId)
        {
            string response = null;
            try
            {
                using var connection = new SqliteConnection(connectionStringBuilder.ConnectionString);
                connection.Open();

                using var command = new SqliteCommand($"SELECT vieved_forms FROM Users WHERE id = {respondentId}", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    response = reader.GetString(0);
                }
                response += formId.ToString() + " ";
                using var command1 = new SqliteCommand($"INSERT INTO Users (vieved_forms) VALUES (@vieved_forms)", connection);
                command.Parameters.AddWithValue("@vieved_forms", response);
                var reader1 = command1.ExecuteReader();
                while (reader1.Read())
                {
                    response = reader1.GetString(0);
                }
                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " Database exception");
            }
        }

        public static string AddExpToUser5(ulong id, float value)
        {
            return "response";
        }

        private static string LogIn(string username, string password)
        {
            try
            {
                var connectionStringBuilder = new SqliteConnectionStringBuilder();
                connectionStringBuilder.DataSource = "./SqliteDB.db";

                using (var connection = new SqliteConnection(connectionStringBuilder.ConnectionString))
                {
                    connection.Open();
                    string elo;
                    bool accountFound = false;
                    bool wrongPassword = false;

                    using (var command = new SqliteCommand($"SELECT * FROM Accounts WHERE username = '{username}'", connection))
                    {
                        var reader = command.ExecuteReader();
                        if (reader.Read())
                        {
                            if (password == reader.GetValue(2).ToString())
                            {
                                elo = reader.GetValue(3).ToString();
                                accountFound = true;
                                connection.Close();
                                return elo;
                            }
                            else
                            {
                                wrongPassword = true;
                                connection.Close();
                                return "WRONG PASSWORD";
                            }
                        }
                        reader.Close();
                    }

                    if (!accountFound && !wrongPassword)
                    {
                        using (var command = new SqliteCommand($"INSERT INTO Accounts (username, password, elo) VALUES (@username, @password, @elo)", connection))
                        {
                            command.Parameters.AddWithValue("username", username);
                            command.Parameters.AddWithValue("password", password);
                            command.Parameters.AddWithValue("elo", 1000);
                            command.ExecuteNonQuery();
                            connection.Close();
                            return "1000";
                        }
                    }
                    connection.Close();
                    return "Empty";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e + " Database ex");
                return e.Message;
            }
            finally
            {
            }
        }
    }
}