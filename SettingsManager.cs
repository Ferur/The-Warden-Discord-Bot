using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Discord.WebSocket;

namespace CardBot
{
    static class SettingsManager
    {
        private const string SettingsConnectionString = "Data Source = CardBotSettings.sqlite; " +
                                                       "Version = 3;";

        public static Dictionary<ulong, ServerSettings> settingsDictionary;

        public static void Load()
        {
            using (SQLiteConnection dbConnection = new SQLiteConnection(SettingsConnectionString))
            {
                dbConnection.Open();

                string sql = "SELECT * FROM GameSettings";
                using (SQLiteCommand command = new SQLiteCommand(sql, dbConnection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {

                        settingsDictionary = new Dictionary<ulong, ServerSettings>();
                        while (reader.Read())
                        {
                            settingsDictionary.Add(Convert.ToUInt64(reader["ServerId"]),
                                                   new ServerSettings()
                                                   {
                                                       Name = Convert.ToString(reader["ServerName"]),
                                                       ShowHex = Convert.ToBoolean(reader["ShowHex"]),
                                                       ShowShards = Convert.ToBoolean(reader["ShowShards"]),
                                                       EventReminderChannelId = Convert.IsDBNull(reader["EventReminderChannel"]) ? null : (ulong?)Convert.ToUInt64(reader["EventReminderChannel"]),
                                                       PatchVersion = Convert.ToString(reader["PatchVersion"]),
                                                   });
                        }
                    }
                }
            }
        }

        public static ServerSettings GetServerSettings(SocketGuild server)
        {
            if (!settingsDictionary.TryGetValue(server.Id, out ServerSettings settings))
            {
                settings = new ServerSettings()
                {
                    Name = server.Name,
                    ShowHex = true,
                    ShowShards = true,
                    EventReminderChannelId = null,
                    PatchVersion = PatchNotes.CurrentPatchVersion,
                };
                settingsDictionary.Add(server.Id, settings);
                InsertNewServer(server.Id, settings);
            }
            return settings;
        }

        public static void UpdatePatchVersion(ulong serverId)
        {
            using (SQLiteConnection dbConnection = new SQLiteConnection(SettingsConnectionString))
            {
                dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand("UPDATE GameSettings SET PatchVersion = @PatchVersion WHERE ServerId = @ServerId", dbConnection))
                {
                    command.Parameters.Add(new SQLiteParameter("@PatchVersion", PatchNotes.CurrentPatchVersion));
                    command.Parameters.Add(new SQLiteParameter("@ServerId", serverId));
                    command.ExecuteNonQuery();
                }
            }

            if (settingsDictionary.TryGetValue(serverId, out ServerSettings settings))
                settings.PatchVersion = PatchNotes.CurrentPatchVersion;
        }

        public static void SetEventReminderChannel(ulong serverId, ulong? eventReminderChannelId)
        {
            using (SQLiteConnection dbConnection = new SQLiteConnection(SettingsConnectionString))
            {
                dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand("UPDATE GameSettings SET EventReminderChannel = @ChannelName WHERE ServerId = @ServerId", dbConnection))
                {
                    command.Parameters.Add(new SQLiteParameter("@ChannelName", eventReminderChannelId));
                    command.Parameters.Add(new SQLiteParameter("@ServerId", serverId));
                    command.ExecuteNonQuery();
                }
            }

            if (settingsDictionary.TryGetValue(serverId, out ServerSettings settings))
                settings.EventReminderChannelId = eventReminderChannelId;
        }

        public static void SetShowHex(ulong serverId, bool showHex)
        {
            using (SQLiteConnection dbConnection = new SQLiteConnection(SettingsConnectionString))
            {
                dbConnection.Open();
                string sql = string.Format("UPDATE GameSettings  SET ShowHex = {0}" +
                                           "    WHERE ServerId = {1}", showHex ? 1 : 0, serverId);
                using (SQLiteCommand command = new SQLiteCommand(sql, dbConnection))
                    command.ExecuteNonQuery();
            }

            if (settingsDictionary.TryGetValue(serverId, out ServerSettings settings))
                settings.ShowHex = showHex;
        }

        public static void SetShowShards(ulong serverId, bool showShards)
        {
            using (SQLiteConnection dbConnection = new SQLiteConnection(SettingsConnectionString))
            {
                dbConnection.Open();
                string sql = string.Format("UPDATE GameSettings  SET ShowShards = {0}" +
                                           "    WHERE ServerId = {1}", showShards ? 1 : 0, serverId);
                using (SQLiteCommand command = new SQLiteCommand(sql, dbConnection))
                    command.ExecuteNonQuery();
            }

            if (settingsDictionary.TryGetValue(serverId, out ServerSettings settings))
                settings.ShowShards = showShards;
        }

        private static void InsertNewServer(ulong ServerId, ServerSettings settings)
        {

            using (SQLiteConnection dbConnection = new SQLiteConnection(SettingsConnectionString))
            {
                dbConnection.Open();
                using (SQLiteCommand command = new SQLiteCommand("INSERT INTO GameSettings VALUES (@ServerId, @Name, @ShowHex, @ShowShards, @EventReminderChannel, @PatchVersion)", dbConnection))
                {
                    command.Parameters.Add(new SQLiteParameter("@ServerId", ServerId));
                    command.Parameters.Add(new SQLiteParameter("@Name", settings.Name));
                    command.Parameters.Add(new SQLiteParameter("@ShowHex", settings.ShowHex ? 1 : 0));
                    command.Parameters.Add(new SQLiteParameter("@ShowShards", settings.ShowShards ? 1 : 0));
                    command.Parameters.Add(new SQLiteParameter("@EventReminderChannel", settings.EventReminderChannelId));
                    command.Parameters.Add(new SQLiteParameter("@PatchVersion", settings.PatchVersion));

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
