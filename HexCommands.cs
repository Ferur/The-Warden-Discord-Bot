using CardBot.HexPriceDataModels;
using Discord;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CardBot.DataModels;

namespace CardBot
{
    static class HexCommands
    {
        private static readonly string hexpriceURL = "http://hexprice.com/item/";
        private static readonly string cardURL = "https://hextcg.com/wp-content/themes/hex/images/autocard/";
        private static readonly string equipURL = "https://hextcg.com/wp-content/themes/hex/images/autocard/equipment/";
        private static readonly string cardEnding = ".png";
        private static readonly string equipEnding = ".jpg";

        public static Embed ProcessEventCommand(string eventType)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            List<HexEvent> upcomingEvents = Program.eventReminder.GetUpcomingEvents(10);

            string value = "```";
            for (int i = 0; i < 10 && i < upcomingEvents.Count; i++)
            {
                TimeSpan difference = upcomingEvents[i].EventDateTime.Subtract(Program.eventReminder.GetNowPST());
                value += string.Format("{0,-30} : {1,2}d {2,2}h {3,2}m \n", upcomingEvents[i].EventName, difference.Days, difference.Hours, difference.Minutes);
            }
            value += "```";

            embedBuilder.AddField("Upcoming Events:", value);

            return embedBuilder.WithColor(Program.embedColor).Build();
        }

        public static async Task<Embed> ProcessCardCommand(string searchedCard, bool small = false)
        {
            Embed embed;
            List<string> matchingCards = new List<string>();
            string sql = "SELECT DISTINCT Name FROM HexpriceData WHERE Name LIKE '%" + searchedCard.Replace("\'", "\'\'") + "%' AND Type = 1";

            SQLiteCommand command = new SQLiteCommand(sql, Program.dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
                matchingCards.Add(Convert.ToString(reader["Name"]));

            if (matchingCards.Count == 0)
                embed = new EmbedBuilder().WithColor(new Color(112, 141, 241)).WithTitle(string.Format("Couldn't find a cardname containing \"{0}\"", searchedCard)).Build();
            else if (matchingCards.Count == 1)
                embed = await CreateSingleCardEmbedOutput(matchingCards[0], small);
            else
            {
                // check if one match is an exact match so we can actually find a card like "Burn" whose name is completly part of another card
                int exactMatchIndex = -1;
                for (int i = 0; i < matchingCards.Count; i++)
                {
                    if (matchingCards[i].Equals(searchedCard, StringComparison.OrdinalIgnoreCase))
                    {
                        exactMatchIndex = i;
                        break;
                    }
                }

                if (exactMatchIndex >= 0)
                    embed = await CreateSingleCardEmbedOutput(matchingCards[exactMatchIndex], small);
                else
                    embed = CreateMultipleCardEmbedOutput(matchingCards);
            }

            return embed;
        }

        public static async Task<Embed> ProcessEquipCommand(string searchedEquip)
        {
            Embed embed;
            List<string> matchingEquips = new List<string>();
            string sql = "SELECT DISTINCT Name From HexpriceData WHERE Name Like '%" + searchedEquip.Replace("\'", "\'\'") + "%' AND Type = 3";

            SQLiteCommand command = new SQLiteCommand(sql, Program.dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
                matchingEquips.Add(Convert.ToString(reader["Name"]));

            if (matchingEquips.Count == 0)
                embed = new EmbedBuilder().WithColor(new Color(112, 141, 241)).WithTitle(string.Format("Couldn't find an equipname containing \"{0}\"", searchedEquip)).Build();
            else if (matchingEquips.Count == 1)
                embed = await CreateEquipEmbedOutput(matchingEquips[0]);
            else
            {
                // check for exact match 
                int exactMatchIndex = -1;
                for (int i = 0; i < matchingEquips.Count; i++)
                {
                    if (matchingEquips[i].Equals(searchedEquip, StringComparison.OrdinalIgnoreCase))
                    {
                        exactMatchIndex = i;
                        break;
                    }
                }

                if (exactMatchIndex >= 0)
                    embed = await CreateEquipEmbedOutput(matchingEquips[0]);
                else
                    embed = CreateMultipleEquipEmbedOutput(matchingEquips);
            }

            return embed;
        }

        private static async Task<Embed> CreateSingleCardEmbedOutput(string cardName, bool small)
        {
            EmbedBuilder builder = new EmbedBuilder();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();

            fieldBuilder.WithName(string.Format("{0}(Card)", cardName));
            fieldBuilder.WithIsInline(false);

            // get ah prices from the hexprice api
            SearchRequest searchQuery = new SearchRequest() { SearchQuery = cardName, };
            HttpResponseMessage response = await Program.httpClient.PostAsJsonAsync("items/search", searchQuery);

            if (response.IsSuccessStatusCode)
            {
                SearchResult[] resultList = await response.Content.ReadAsAsync<SearchResult[]>();
                int resultIndex;
                bool foundExactMatch = false;
                for (resultIndex = 0; resultIndex < resultList.Length; resultIndex++)
                {
                    if (resultList[resultIndex].Item.LinkName.Equals(cardName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundExactMatch = true;
                        break;
                    }
                }

                if (foundExactMatch)
                {
                    int platBuyout = resultList[resultIndex].CurrentCheapestPlat;
                    int goldBuyout = resultList[resultIndex].CurrentCheapestGold;

                    fieldBuilder.WithValue(string.Format("Lowest Buyout: [{0} | {1}]({2})", platBuyout == -1 ? "none" : string.Format("{0}p", platBuyout), goldBuyout == -1 ? "none" : string.Format("{0}g", goldBuyout), CreateHexPriceLink(resultList[resultIndex])));
                }
                else
                {
                    fieldBuilder.WithValue("Couldn't find AH data");
                }
            }
            else
            {
                fieldBuilder.WithValue("Couldn't connect to hexprice.com");
            }

            builder.AddField(fieldBuilder);


            // get equipment/card pairs out of the db
            string sql = "SELECT Equipment FROM EquipCardPairs WHERE EquipCardPairs.Card = '" + cardName.Replace("\'", "\'\'") + "'";
            SQLiteCommand command = new SQLiteCommand(sql, Program.dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();

            List<string> equipsOfMatchingCard = new List<string>();
            while (reader.Read())
                equipsOfMatchingCard.Add(Convert.ToString(reader["Equipment"]));
            string equipments = string.Empty;
            if (equipsOfMatchingCard.Count > 0)
            {
                equipments += "Equipment: ";
                for (int i = 0; i < equipsOfMatchingCard.Count; i++)
                {
                    equipments += equipsOfMatchingCard[i];
                    if (i < equipsOfMatchingCard.Count - 1)
                        equipments += ", ";
                }
            }
            else
            {
                equipments = "No Equipment";
            }

            // add equipment/card image
            if (small)
            {
                fieldBuilder = new EmbedFieldBuilder().WithName(equipments)
                                                      .WithValue(string.Format("[ImageLink]({0})", CreateCardLink(cardName)));
                builder.AddField(fieldBuilder);
                builder.WithUrl(CreateCardLink(cardName));
                builder.WithThumbnailUrl(CreateCardLink(cardName));
            }
            else
            {
                builder.WithImageUrl(CreateCardLink(cardName));
                builder.WithFooter(equipments);
            }
            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static Embed CreateMultipleCardEmbedOutput(List<string> cardNames)
        {
            EmbedBuilder builder = new EmbedBuilder();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();

            if (cardNames.Count <= 5)
            {
                fieldBuilder.WithName("Found multiple matching cards:");
                string value = string.Empty;
                for (int i = 0; i < 5 && i < cardNames.Count; i++)
                {
                    value += cardNames[i] + "\n";
                }
                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(false);
                builder.AddField(fieldBuilder);
            }
            else
            {
                fieldBuilder.WithName("Found multiple matching cards:");
                string value = string.Empty;
                for (int i = 0; i < 5; i++)
                {
                    value += cardNames[i] + "\n";
                }
                if (cardNames.Count > 10)
                    value += string.Format("\n*and {0} more matches*", cardNames.Count - 10);

                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(true);
                builder.AddField(fieldBuilder);

                fieldBuilder = new EmbedFieldBuilder();
                fieldBuilder.WithName("\u200b");
                value = string.Empty;
                for (int i = 5; i < 10 && i < cardNames.Count; i++)
                    value += cardNames[i] + "\n";
                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(true);
                builder.AddField(fieldBuilder);
            }

            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static async Task<Embed> CreateEquipEmbedOutput(string equipName)
        {
            EmbedBuilder builder = new EmbedBuilder();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();

            builder.WithImageUrl(CreateEquipLink(equipName));
            fieldBuilder.WithIsInline(false);
            fieldBuilder.WithName(string.Format("{0}(Equipment)", equipName));

            // get ah prices from the hexprice api
            SearchRequest searchQuery = new SearchRequest() { SearchQuery = equipName, };
            HttpResponseMessage response = await Program.httpClient.PostAsJsonAsync("items/search", searchQuery);

            if (response.IsSuccessStatusCode)
            {
                SearchResult[] resultList = await response.Content.ReadAsAsync<SearchResult[]>();
                int resultIndex;
                bool foundExactMatch = false;
                for (resultIndex = 0; resultIndex < resultList.Length; resultIndex++)
                {
                    if (resultList[resultIndex].Item.LinkName.Equals(equipName, StringComparison.OrdinalIgnoreCase))
                    {
                        foundExactMatch = true;
                        break;
                    }
                }

                if (foundExactMatch)
                {
                    int platBuyout = resultList[resultIndex].CurrentCheapestPlat;
                    int goldBuyout = resultList[resultIndex].CurrentCheapestGold;

                    fieldBuilder.WithValue(string.Format("Lowest Buyout: [{0} | {1}]({2})", platBuyout == -1 ? "none" : string.Format("{0}p", platBuyout), goldBuyout == -1 ? "none" : string.Format("{0}g", goldBuyout), CreateHexPriceLink(resultList[resultIndex])));
                }
                else
                {
                    fieldBuilder.WithValue("Couldn't find AH data.");
                }
            }
            else
            {
                fieldBuilder.WithValue("Couldn't connect to hexprice.com.");
            }

            builder.AddField(fieldBuilder);
            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static Embed CreateMultipleEquipEmbedOutput(List<string> equipNames)
        {
            EmbedBuilder builder = new EmbedBuilder();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();

            if (equipNames.Count <= 5)
            {
                fieldBuilder.WithName("Found multiple matching equipment:");
                string value = string.Empty;
                for (int i = 0; i < 5 && i < equipNames.Count; i++)
                {
                    value += equipNames[i] + "\n";
                }
                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(false);
                builder.AddField(fieldBuilder);
            }
            else
            {
                fieldBuilder.WithName("Found multiple matching equipment:");
                string value = string.Empty;
                for (int i = 0; i < 5; i++)
                {
                    value += equipNames[i] + "\n";
                }
                if (equipNames.Count > 10)
                    value += string.Format("\n*and {0} more matches*", equipNames.Count - 5);

                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(true);
                builder.AddField(fieldBuilder);

                fieldBuilder = new EmbedFieldBuilder();
                fieldBuilder.WithName("\u200b");
                value = string.Empty;
                for (int i = 5; i < 10 && i < equipNames.Count; i++)
                    value += equipNames[i] + "\n";
                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(true);
                builder.AddField(fieldBuilder);
            }

            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static string CreateEquipLink(string equipName)
        {
            return equipURL + equipName.Trim().Replace(" ", "%20") + equipEnding;
        }

        private static string CreateCardLink(string cardName)
        {
            return cardURL + cardName.Trim().Replace(" ", "%20") + cardEnding;
        }

        private static string CreateHexPriceLink(SearchResult result)
        {
            return (hexpriceURL + result.Item.Set.name + "/" + result.Item.LinkName).Replace(" ", "%20");
        }
    }
}
