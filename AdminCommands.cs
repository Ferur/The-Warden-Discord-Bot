using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot
{
    static class AdminCommands
    {
        public static Embed ProcessDisabelEventReminders(ulong serverId, ServerSettings settings)
        {
            if(settings.EventReminderChannelId != null)
            {
                SettingsManager.SetEventReminderChannel(serverId, null);
                return Response("Sending EventReminders successfully disabled.");
            }
            else
                return Response("No EventReminderChannel is currently set.");
        }

        public static Embed ProcessSetEventReminderChannel(string input, SocketGuild server, ServerSettings settings)
        {
            ulong? channelId = null;
            bool channelExists = false;

            foreach(SocketTextChannel channel in server.TextChannels)
            {
                if(channel.Name == input)
                {
                    channelId = channel.Id;
                    channelExists = true;
                    break;
                }
            }

            if (!channelExists && input != string.Empty)
                return Response("The given TextChannel doesn't exist.");
            else if (input == string.Empty)
            {
                if (settings.EventReminderChannelId != null)
                    return Response(string.Format("The current EventReminderChannel is \"{0}\"", server.GetChannel(settings.EventReminderChannelId.Value).Name));
                else
                    return Response("No EventReminderChannel is currently set.");
            }
            else
            {
                SettingsManager.SetEventReminderChannel(server.Id, channelId);
                return Response(string.Format("EventReminderChannel successfully set to channel \"{0}\"", server.GetChannel(settings.EventReminderChannelId.Value).Name));
            }
        }

        public static Embed ProcessShowHexCommand(string input, SocketGuild server)
        {
            if (input == "0" || input == "false")
                SettingsManager.SetShowHex(server.Id, false);
            else if (input == "1" || input == "true")
                SettingsManager.SetShowHex(server.Id, true);
            else
                return Response("Command parameter must be **1**, **true**, **0** or **false**.");
            return Response("Settings successfully changed.");
        }

        public static Embed ProcessShowShardsCommand(string input, SocketGuild server)
        {
            if (input == "0" || input == "false")
                SettingsManager.SetShowShards(server.Id, false);
            else if (input == "1" || input == "true")
                SettingsManager.SetShowShards(server.Id, true);
            else
                return Response("Command parameter must be **1**, **true**, **0** or **false**.");
            return Response("Settings successfully changed.");
        }


        private static Embed Response(string answer)
        {
            return new EmbedBuilder().WithColor(Program.embedColor)
                                      .WithTitle(answer)
                                     .Build();
        }
    }
}
