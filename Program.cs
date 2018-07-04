using System;
using Discord;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using CardBot.HexPriceDataModels;
using System.Data.SQLite;
using Discord.Rest;
using CardBot.DataModels;

namespace CardBot
{
    public class Program
    {
        public static readonly Color embedColor = new Color(112, 141, 241);
        public static SQLiteConnection dbConnection;
        public static EventManager eventReminder;
        public static HttpClient httpClient;
        
        private static readonly string hexpriceapiURL = "http://hexprice.com/api/";

        private DiscordSocketClient discordClient;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();


        public async Task MainAsync()
        {
            SettingsManager.Load();

            // open database connection
            dbConnection = new SQLiteConnection("Data Source = CardBotData.sqlite; " +
                                                "Version = 3;");
            dbConnection.Open();

            // Init HttpClient

            httpClient = new HttpClient
            {
                BaseAddress = new Uri(hexpriceapiURL)
            };
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Init Discord.net and connect
            discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                WebSocketProvider = WS4NetProvider.Instance,
            });

            // Start EventReminder
            eventReminder = new EventManager();
            eventReminder.EventStartingSoon += SendEventReminderMessage;

            discordClient.Log             += Log;
            discordClient.MessageReceived += MessageReceived;

            discordClient.Ready += () =>
            {
                CheckForUpdates();
                ClearEventReminderChannel();
                return Task.CompletedTask;
            };
            
            string token = "Enter Discord Bot Token here"; // Remember to keep this private!
            await discordClient.LoginAsync(TokenType.Bot, token);
            await discordClient.StartAsync();


            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task SendEventReminderMessage(HexEvent hexEvent, TimeSpan span, bool newEventReminderMessage)
        {
            if (newEventReminderMessage)
            {
                foreach (KeyValuePair<ulong, ServerSettings> keyValuePair in SettingsManager.settingsDictionary)
                {
                    if (keyValuePair.Value.EventReminderChannelId != null)
                        hexEvent.EventMessages.Add(await ((SocketTextChannel)discordClient.GetChannel(keyValuePair.Value.EventReminderChannelId.Value)).SendMessageAsync(string.Empty, false, CreateEventReminderEmbed(hexEvent.EventName, span)));
                }
            }
            else
            {
                foreach (RestUserMessage message in hexEvent.EventMessages)
                {
                    await message.ModifyAsync(msg => msg.Embed = CreateEventReminderEmbed(hexEvent.EventName, span));
                }
            }
        }

        private void CheckForUpdates()
        {
            foreach(KeyValuePair<ulong, ServerSettings> keyValuePair in SettingsManager.settingsDictionary)
            {
                if (keyValuePair.Value.PatchVersion != PatchNotes.CurrentPatchVersion)
                {
                    Task.Run(() => discordClient.GetGuild(keyValuePair.Key).Owner.SendMessageAsync(string.Empty, false, PatchNotes.CreatePatchNotesEmbed()));
                    SettingsManager.UpdatePatchVersion(keyValuePair.Key);
                }
            }
        }

        private async void ClearEventReminderChannel()
        {
            SocketTextChannel channel;
            IEnumerable<IMessage> messages;
            foreach (KeyValuePair<ulong, ServerSettings> keyValuePair in SettingsManager.settingsDictionary)
            {
                if(keyValuePair.Value.EventReminderChannelId != null)
                {
                    channel = (SocketTextChannel)discordClient.GetChannel(keyValuePair.Value.EventReminderChannelId.Value);
                    messages = await (channel.GetMessagesAsync()).FlattenAsync();
                    await channel.DeleteMessagesAsync(messages);
                }
            }
        }

        private async Task MessageReceived(SocketMessage message)
        {
            string[]       splitMessage = message.Content.Trim().Split(' ', 2);
            string         command      = string.Empty;
            string         parameter = string.Empty;
            bool           admin        = false;
            ServerSettings settings;

            // getting server status and admin status to check which commands
            // are available
            SocketGuild server = (message.Channel as SocketGuildChannel)?.Guild;
            if (server != null)
            {
                settings = SettingsManager.GetServerSettings(server);

                if (message.Author is SocketGuildUser user)
                {
                    admin = user.GuildPermissions.Administrator;
                    if (server.OwnerId == user.Id)
                        admin = true;
                }
            }
            else
                settings = new ServerSettings()
                {
                    Name = "Default",
                    ShowHex = true,
                    ShowShards = true,
                    EventReminderChannelId = null,
                    PatchVersion = PatchNotes.CurrentPatchVersion,
                };


            if (splitMessage.Length > 0)
                command = splitMessage[0].Trim();
            if (splitMessage.Length > 1)
                parameter = splitMessage[1].Trim();
            
            try
            {
                await ProcessCommand(settings, admin, message, server, command, parameter);
            }
            catch(Exception e)
            {
                Console.WriteLine(string.Format("{0}:\n{1}", e.Message, e.StackTrace));
            }
        }

        private async Task ProcessCommand(ServerSettings settings, bool admin, SocketMessage message, SocketGuild server, string command, string parameter)
        {
            if (admin)
            {
                if (command.Equals("!AdminShowHex", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, AdminCommands.ProcessShowHexCommand(parameter, server));
                if (command.Equals("!AdminShowShards", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, AdminCommands.ProcessShowShardsCommand(parameter, server));
                if (command.Equals("!AdminSetEventReminderChannel", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, AdminCommands.ProcessSetEventReminderChannel(parameter, server, settings));
                if (command.Equals("!AdminDisableEventReminders", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, AdminCommands.ProcessDisabelEventReminders(server.Id, settings));
            }

            if (settings.ShowHex)
            {
                if (command.Equals("!cardlarge", StringComparison.OrdinalIgnoreCase)
                    || command.Equals("!cl", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, await HexCommands.ProcessCardCommand(parameter));
                if (command.Equals("!card", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, await HexCommands.ProcessCardCommand(parameter, true));
                if (command.Equals("!equip", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, await HexCommands.ProcessEquipCommand(parameter));
                if (command.Equals("!equipment", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, await HexCommands.ProcessEquipCommand(parameter));
                if (command.Equals("!events", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, HexCommands.ProcessEventCommand(parameter));
            }

            if (settings.ShowShards)
            {
                if (command.Equals("!shardslarge", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, ShardsCommands.ProcessShardsCommand(parameter));
                if (command.Equals("!shards", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, ShardsCommands.ProcessShardsCommand(parameter, true));
                if (command.Equals("!shardstype", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, ShardsCommands.ProcessShardstypeCommand(parameter));
                if (command.Equals("!shardslist", StringComparison.OrdinalIgnoreCase))
                    await message.Channel.SendMessageAsync(string.Empty, false, ShardsCommands.ProcessShardslistCommand(parameter));
            }

            if (command.Equals("!tl", StringComparison.OrdinalIgnoreCase))
                await message.Channel.SendMessageAsync(MiscCommands.ProcessTwitchLinkCommand(parameter));
            if (command.Equals("!help", StringComparison.OrdinalIgnoreCase)
                || command.Equals("!commands", StringComparison.OrdinalIgnoreCase)
                || command.Equals("!commandlist", StringComparison.OrdinalIgnoreCase))
                await message.Channel.SendMessageAsync(string.Empty, false, MiscCommands.ShowCommands(parameter, settings));
            if (command.Equals("!privatechannel", StringComparison.OrdinalIgnoreCase))
                await message.Author.SendMessageAsync("Hi, i am the Warden Bot. Type **!help** for a list of commands. Have a nice day!");

            if (command.Equals("!test", StringComparison.OrdinalIgnoreCase))
            {
                eventReminder.DebugCheckForReminders();
                eventReminder.DebugAddNextEvent();
            }
        }
        

        private Embed CreateEventReminderEmbed(string eventName, TimeSpan span)
        {
            EmbedBuilder builder = new EmbedBuilder();

            string name = eventName;
            string value;

            if (span.TotalSeconds <= 0)
                value = "Event Started";
            else if (span.TotalSeconds < 3600)
                value = string.Format("in **{0,4:N0}m**", span.Minutes + (span.Seconds >= 30 ? 1 : 0));
            else
                value = string.Format("in **{0,2:N0}h {1,2:N0}m**", span.Hours, span.Minutes + (span.Seconds >= 30 ? 1 : 0));

            builder.AddField(name, value);

            string iconUrl = GetEventIcon(eventName);
            if (iconUrl != null)
                builder.WithThumbnailUrl(iconUrl);
            
            return builder.WithColor(embedColor)
                .Build();
        }

        private string GetEventIcon(string name)
        {
            switch(name)
            {
                case "Fight Night Hex":
                    return "https://www.hextcg.com/wp-content/uploads/2017/10/Hex-FNX-Tournament-Logo.png";
                case "HEX Clash":
                    return "https://www.hextcg.com/wp-content/uploads/2017/02/HEX.CLASH_.LOGO_.png";
                case "HEX Bash":
                    return "https://www.hextcg.com/wp-content/uploads/2017/08/Hex-Bash-Final-Edited_TransparentBG_Small.png";
                case "Singles Night":
                    return "https://www.hextcg.com/wp-content/uploads/2017/11/Hex-Singles-Night-Logo.png";
                case "Cosmic Crown Showdown Day 1":
                case "Cosmic Crown Showdown Day 2":
                    return "https://www.hextcg.com/wp-content/uploads/2016/07/COSMICCROWNLOGO.png";
                case "Immortal Weekly":
                    return "https://www.hextcg.com/wp-content/uploads/2017/10/Hex-Primal-Weekly-Tournament-Logo.png";
                case "HexPrimal Immortal Championship":
                    return "https://www.hextcg.com/wp-content/uploads/2017/10/Hex-Primal-Tournament-Logo.png";
                case "FiveShards Weekly Series #1":
                case "FiveShards Weekly Series #2":
                case "Arcanum Vault":
                    return "https://yt3.ggpht.com/-phKuWvt2LEs/AAAAAAAAAAI/AAAAAAAAAAA/cSkl9mpwfq4/s288-mo-c-c0xffffffff-rj-k-no/photo.jpg";
                default:
                    return null;
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}

