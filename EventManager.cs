

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CardBot.DataModels;
using System.Timers;
using Discord.Rest;

namespace CardBot
{
    public class EventManager
    {
        public List<HexEvent> eventList;
        public List<HexEvent> activeEvents;
        public event Func<HexEvent, TimeSpan, bool, Task> EventStartingSoon;

        private Timer checkEventsTimer;
        private DateTime lastUpdated;

        public DateTime GetNowPST()
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Pacific Standard Time");
        }

        public List<HexEvent> GetUpcomingEvents(int maxEntries)
        {
            List<HexEvent> upcomingEvents = new List<HexEvent>();
            TimeSpan diff;
            DateTime now = GetNowPST();
            for (int i = 0; i < activeEvents.Count && upcomingEvents.Count < maxEntries; i++)
            {
                diff = activeEvents[i].EventDateTime.Subtract(now);
                if (diff.TotalMilliseconds > 0)
                    upcomingEvents.Add(activeEvents[i]);
            }

            for(int i = 0; i < eventList.Count && upcomingEvents.Count < maxEntries; i++)
                upcomingEvents.Add(eventList[i]);

            return upcomingEvents;
        }

        public void UpdateEvents()
        {
            DateTime now = GetNowPST();
            UpdateEvents(now);
        }
        
        [Conditional("DEBUG")]
        public void DebugCheckForReminders()
        {
            CheckForReminders();
        }

        [Conditional("DEBUG")]
        public void DebugAddNextEvent()
        {
            if (eventList.Count > 0)
            {
                activeEvents.Add(eventList[0]);
                eventList.RemoveAt(0);
            }
        }

        public EventManager()
        {
            eventList = new List<HexEvent>();
            activeEvents = new List<HexEvent>();

            DateTime now = GetNowPST();
            UpdateEvents(now);

            InitCheckEventsTimer();
            StartCheckEventsTimer();
        }

        #region TIMER
        private void InitCheckEventsTimer()
        {
            checkEventsTimer = new Timer();
            checkEventsTimer.Elapsed += new ElapsedEventHandler(OnCheckEvents);
            checkEventsTimer.AutoReset = false;
        }

        private void StartCheckEventsTimer()
        {
            DateTime now = GetNowPST();
            double millisecondsToFullFiveMinutes;
            //if (activeEvents.Count == 0)
            //    millisecondsToFullFiveMinutes = (300 - ((now.Minute % 5) * 60 + now.Second)) * 1000 - now.Millisecond;
            //else
                millisecondsToFullFiveMinutes = (60 - now.Second) * 1000 - now.Millisecond;
            checkEventsTimer.Interval = millisecondsToFullFiveMinutes;
            checkEventsTimer.Start();
        }

        private void OnCheckEvents(object source, ElapsedEventArgs e)
        {
            CheckForReminders();
            DateTime now = GetNowPST();
            if (now.Subtract(lastUpdated).TotalHours >= 24)
                UpdateEvents(now);

            StartCheckEventsTimer();
        }
        #endregion

        private void CheckForReminders()
        {
            if (EventStartingSoon != null)
            {
                DateTime now = GetNowPST();

                TimeSpan diff;
                for (int i = eventList.Count - 1; i >= 0; i--)
                {
                    diff = eventList[i].EventDateTime.Subtract(now);

                    if(diff.TotalSeconds <= 7200)
                    {
                        if(!activeEvents.Contains(eventList[i]))
                            activeEvents.Add(eventList[i]);
                        eventList.RemoveAt(i);
                    }

                    //if (diff.TotalMilliseconds <= 0)
                    //{
                    //    eventList.RemoveAt(i);
                    //}
                    //else
                    //{
                    //    if (!eventList[i].LateWarningSent && diff.TotalSeconds <= 1800)
                    //    {
                    //        EventStartingSoon(eventList[i].EventName, diff);
                    //        eventList[i].LateWarningSent = true;
                    //        eventList[i].EarlyWarningSent = true;
                    //        upcomingEventsCount++;
                    //    }
                    //    else if (!eventList[i].EarlyWarningSent && diff.TotalSeconds <= 7200)
                    //    {
                    //        EventStartingSoon(eventList[i].EventName, diff);
                    //        eventList[i].EarlyWarningSent = true;
                    //        upcomingEventsCount++;
                    //    }
                    //}
                }

                if (activeEvents.Count == 0)
                    DebugAddNextEvent();

                for (int i = activeEvents.Count - 1; i >= 0; i--)
                {
                    diff = activeEvents[i].EventDateTime.Subtract(now);

                    if (diff.TotalSeconds < -3600)
                    {
                        activeEvents.RemoveAt(i);
                    }
                    else
                    {
                        bool newEventReminderMessage = false;
                        if (activeEvents[i].EventMessages == null)
                        {
                            activeEvents[i].EventMessages = new List<Discord.Rest.RestUserMessage>();
                            newEventReminderMessage = true;
                        }
                        EventStartingSoon(activeEvents[i], diff, newEventReminderMessage);
                    }
                }
                
            }
        }

        private void ValidateEvents(DateTime now)
        {
            TimeSpan diff;
            for (int i = eventList.Count - 1; i >= 0; i--)
            {
                diff = eventList[i].EventDateTime.Subtract(now);

                if (diff.TotalSeconds < -3600)
                    eventList.RemoveAt(i);
            }
        }

        private void UpdateEvents(DateTime now)
        {
            lastUpdated = now;
            for (int i = 0; i < 7; i++)
            {
                GetEventInfo(now.AddDays(i));
            }
            eventList = new List<HexEvent>(eventList.OrderBy(hexEvent => hexEvent.EventDateTime).ToArray<HexEvent>());
            ValidateEvents(now);
        }

        // gets all events from one specific day in the hex calender
        private void GetEventInfo(DateTime dateTime)
        {
            string htmlSource;
            using (WebClient client = new WebClient())
            {
                htmlSource = client.DownloadString(GetHexCalenderUrl(dateTime));
            }

            List<HexEvent> events = new List<HexEvent>();

            string pattern          = "box48";
            string timeStartPattern = "\"\\s*badge\\s*\"\\s*>\\s*";
            string dataPattern      = "[^<]*";
            string nameStartPattern = "class\\s*=\\s*\"\\s*calendarEventLink\\s*\"\\s*[^>]*>";
            string datePattern      = "(Mon|Tue|Wed|Thu|Fri|Sat|Sun), (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec) \\d*(st|nd|rd|th) \\d*";
            // Fri, May 4th 2018, 11:00am - 2:00pm
            int lastMatchEnd = 0;
            Regex rgx          = new Regex(pattern, RegexOptions.IgnoreCase);
            Regex timeStartRgx = new Regex(timeStartPattern, RegexOptions.IgnoreCase);
            Regex dataRgx      = new Regex(dataPattern, RegexOptions.IgnoreCase);
            Regex nameStartRgx = new Regex(nameStartPattern, RegexOptions.IgnoreCase);
            Regex dateRgx      = new Regex(datePattern, RegexOptions.IgnoreCase);
            Match match;
            List<HexEvent> tempEvents;
            do
            {
                match = rgx.Match(htmlSource, lastMatchEnd);
                if(match.Success)
                {
                    lastMatchEnd = match.Index + match.Length;
                    Match timeStartMatch = timeStartRgx.Match(htmlSource, match.Index);
                    if (timeStartMatch.Success)
                    {
                        Match timeMatch = dataRgx.Match(htmlSource, timeStartMatch.Index + timeStartMatch.Length);
                        if (timeMatch.Success)
                        {
                            Match nameStartMatch = nameStartRgx.Match(htmlSource, timeMatch.Index + timeMatch.Length);
                            if(nameStartMatch.Success)
                            {
                                Match nameMatch = dataRgx.Match(htmlSource, nameStartMatch.Index + nameStartMatch.Length);
                                if(nameMatch.Success)
                                {
                                    Match dateMatch = dateRgx.Match(htmlSource, nameMatch.Index + nameMatch.Length);
                                    if(dateMatch.Success)
                                    {
                                        tempEvents = null;
                                        try
                                        {
                                            tempEvents = HexEvent.ParseEventStrings(dateMatch.Value, nameMatch.Value, timeMatch.Value);
                                            //System.Console.WriteLine(string.Format("{0}: {1}, {2}", nameMatch.Value, dateMatch.Value, timeMatch.Value));
                                        }
                                        catch (Exception)
                                        {

                                        }

                                        if(tempEvents != null)
                                            events.AddRange(tempEvents);
                                    }
                                }
                            }
                        }
                    }

                }
            } while (match.Success);

            foreach (HexEvent hexEvent in events)
            {
                if (hexEvent.EventDateTime.DayOfYear == dateTime.DayOfYear)
                {
                    if(!eventList.Contains(hexEvent))
                        eventList.Add(hexEvent);
                }
            }
        }


        private string GetHexCalenderUrl(DateTime time)
        {
            return string.Format("https://forums.hextcg.com/calendar/?daily/{0}/{1}/{2}", time.Year, time.Month, time.Day);
        }
    }
}
