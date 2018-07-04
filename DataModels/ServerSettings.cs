using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot
{
    class ServerSettings
    {
        public string Name { get; set; }
        public bool ShowHex { get; set; }
        public bool ShowShards { get; set; }
        public ulong? EventReminderChannelId { get; set; }
        public string PatchVersion { get; set; }
    }
}
