using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot
{
    static class PatchNotes
    {
        public static string CurrentPatchVersion
        {
            get { return patchV1_1; }
        }

        public static string CurrentPatchnotes
        {
            get { return patchnotesV1_1;  }
        }

        private static readonly string patchV1_1 = "V1.1";
        private static readonly string patchnotesV1_1 = "\u2022Added !events command\n"
                                             + "\u2022Added EventReminder functionality. Activate it by setting up a channel and using the !AdminSetEventReminderChannel <ChannelName> command.You can remove it by using !AdminDisableEventReminders\n"
                                             + "\u2022Added !cl as a shortcut for !cardlarge\n"
                                             + "\u2022Added the ability to disable shards or hex commands separatly.Use !ShowShards or !ShowHex with 0, 1, true or false as parameter to activate/deactivate shards or hex commands.\n"
                                             + "\u2022When a search fails the response now includes the searchstring";

        public static Embed CreatePatchNotesEmbed()
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Patchnotes");

            builder.AddField(CurrentPatchVersion, CurrentPatchnotes);

            builder.WithFooter("Send feedback/suggestions to either @CoachFliperon#4473 or @Ferur#4195.");

            return builder.WithColor(Program.embedColor)
                          .Build();
        }
    }
}
