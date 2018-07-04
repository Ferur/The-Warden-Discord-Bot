using System;
using System.Collections.Generic;
using Discord.Rest;

namespace CardBot.DataModels
{
    public class HexEvent : IComparable<HexEvent>
    {
        public const string NameBashClash = "HEX Bash & Clash";
        public const string NameLadderRollover = "Cosmic Ladder Rolls Over";
        public const string NameBash = "HEX Bash";
        public const string NameClash = "HEX Clash";
        public const string MerryMeleeWhosTheBoss = "Merry Melee - Who's the Boss";

        public DateTime EventDateTime { get; set; }
        public string EventName { get; set; }
        public List<RestUserMessage> EventMessages { get; set; }

        public override string ToString()
        {
            return string.Format("{0,-20}: {1}", EventDateTime.ToString(), EventName);
        }
        
        public static List<HexEvent> ParseEventStrings(string dateRaw, string nameRaw, string timeRaw)
        {
            List<HexEvent> hexEvents = new List<HexEvent>();

            string name = nameRaw.Trim().Replace("&amp;", "&");
            timeRaw = timeRaw.Trim();

            int year = 0;
            int month = 0;
            int day = 0;
            int hour = 0;
            int minute = 0;

            string weekDay = dateRaw.Trim().Substring(0, 3);

            // Fri, May 4th 2018
            dateRaw = dateRaw.Split(",")[1].Trim();
            switch(dateRaw.Substring(0, 3))
            {
                case "Jan":
                    month = 1;
                    break;
                case "Feb":
                    month = 2;
                    break;
                case "Mar":
                    month = 3;
                    break;
                case "Apr":
                    month = 4;
                    break;
                case "May":
                    month = 5;
                    break;
                case "Jun":
                    month = 6;
                    break;
                case "Jul":
                    month = 7;
                    break;
                case "Aug":
                    month = 8;
                    break;
                case "Sep":
                    month = 9;
                    break;
                case "Oct":
                    month = 10;
                    break;
                case "Nov":
                    month = 11;
                    break;
                case "Dec":
                    month = 12;
                    break;
            }

            dateRaw = dateRaw.Substring(4);

            day = Convert.ToInt32(dateRaw.Substring(0, dateRaw.Length - 7));
            year = Convert.ToInt32(dateRaw.Substring(dateRaw.Length - 4, 4));

            if (name == NameBashClash)
            {
                if (weekDay == "Sat")
                {
                    hexEvents.Add(new HexEvent()
                    {
                        EventDateTime = new DateTime(year, month, day, 8, 0, 0),
                        EventName = NameBash,
                        EventMessages = null,
                    });
                    hexEvents.Add(new HexEvent()
                    {
                        EventDateTime = new DateTime(year, month, day, 16, 0, 0),
                        EventName = NameClash,
                        EventMessages = null,
                    });
                }
                else if (weekDay == "Sun")
                {
                    hexEvents.Add(new HexEvent()
                    {
                        EventDateTime = new DateTime(year, month, day, 8, 0, 0),
                        EventName = NameClash,
                        EventMessages = null,
                    });
                    hexEvents.Add(new HexEvent()
                    {
                        EventDateTime = new DateTime(year, month, day, 16, 0, 0),
                        EventName = NameBash,
                        EventMessages = null,
                    });
                }
            }
            else if (name == NameLadderRollover)
            {
                if (day == 1)
                {
                    hexEvents.Add(new HexEvent()
                    {

                        EventDateTime = new DateTime(year, month, day, 0, 0, 0),
                        EventName = NameLadderRollover,
                        EventMessages = null,
                    });
                }
            }
            else if(name == MerryMeleeWhosTheBoss)
            {

            }
            else if(timeRaw == "full-day")
            {
                hexEvents.Add(new HexEvent()
                {
                    EventDateTime = new DateTime(year, month, day, 0, 0, 0),
                    EventName = name,
                    EventMessages = null,
                });
            }
            else
            {
                string[] splits = timeRaw.Split(":");
                hour = Convert.ToInt32(splits[0]);
                minute = Convert.ToInt32(splits[1].Substring(0, 2));

                if (splits[1].Substring(2, 2) == "pm")
                    hour += 12;

                hexEvents.Add(new HexEvent()
                {
                    EventDateTime = new DateTime(year, month, day, hour, minute, 0),
                    EventName = name,
                    EventMessages = null,
                });
            }

            return hexEvents;
        }
        
        public int CompareTo(HexEvent other)
        {
            return this.EventDateTime.CompareTo(other.EventDateTime);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            HexEvent other = (HexEvent)obj;
            return (this.EventDateTime.Equals(other.EventDateTime)) && (this.EventName.Equals(other.EventName));
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + EventDateTime.GetHashCode();
            hash = (hash * 7) + EventName.GetHashCode();
            return hash;
        }

        private HexEvent() { }
    }
}
