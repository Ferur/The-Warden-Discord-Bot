using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot
{
    static class MiscCommands
    {

        public static Embed ShowCommands(string searchedName, ServerSettings settings)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithColor(new Color(112, 141, 241));
            builder.WithTitle("Command List");
            if (settings.ShowHex)
                builder.AddField("__Hex__",
                                 "\u2022 !card *<cardname>* - shows a small image, auction house data and equipment\n"
                               + "\u2022 !cardlarge *<cardname>* - shows a large image, auction house data and equipment\n"
                               + "\u2022 !cl *<cardname>* - same as !cardlarge\n"
                               + "\u2022 !equip *<equipmentname>* - shows equipment image and auction house data\n"
                               + "\u2022 !events - shows a list of upcoming tournaments and events",
                                 false);
            if (settings.ShowShards)
                builder.AddField("__Shards__",
                                 "\u2022 !shards *<cardname>* - shows Shards the Deckbuilder cards\n"
                               + "\u2022 !shardslarge *<cardname>* - shows a large image of Shards the Deckbuilder cards\n"
                               + "\u2022 !shardstype *<type>* - shows all Shards cards of a type or a list of types if no type is specified\n"
                               + "\u2022 !shardslist - shows a link to a list of all cards",
                                 false);
            builder.AddField("__Misc__",
                             "\u2022 !tl *<twitchname>* - provide a link to the twitch channel\n"
                           + "\u2022 !privatechannel - opens a private channel with the bot. Use this to check cards without spamming the chat.\n"
                           + "\u2022 !help - list all commands\n",
                             false);

            return builder.Build();
        }

        public static string ProcessTwitchLinkCommand(string twitchname)
        {
            return string.Format("https://www.twitch.tv/{0}", twitchname);
        }

        public static Embed ProcessTestCommand(int counter)
        {
            return new EmbedBuilder().WithColor(new Color(112, 141, 241))
                                     .AddField("Test Counter", counter, false)
                                     .Build();
        }


    }

}
