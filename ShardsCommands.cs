using Discord;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace CardBot
{
    static class ShardsCommands
    {
        public static Embed ProcessShardslistCommand(string searchedName)
        {
            return new EmbedBuilder().WithTitle("Full **Shards the Deckbuilder** card list")
                                    .WithColor(new Color(112, 141, 241))
                                    .WithUrl("https://docs.google.com/document/d/17nr4qOuUcSyWfbVeqVk-3JpTqoLk_xn-cyFqtf09moE/edit?usp=sharing")
                                    .Build();
        }

        public static Embed ProcessShardsCommand(string searchedCard, bool small = false)
        {
            Embed embed;
            List<ShardsCard> matchingCards = new List<ShardsCard>();
            string sqlCards = "SELECT DISTINCT * FROM ShardsCards WHERE Name LIKE '%" + searchedCard.Replace("\'", "\'\'") + "%'";
            string sqlSubTypes = "SELECT DISTINCT SubType FROM ShardsSubtypes WHERE Code = @code";

            SQLiteCommand command = new SQLiteCommand(sqlCards, Program.dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            SQLiteCommand subtypeCommand = new SQLiteCommand(sqlSubTypes, Program.dbConnection);
            SQLiteDataReader subtypeReader;
            List<string> subtypes;

            ShardsCard card;
            while (reader.Read())
            {
                card = new ShardsCard
                {
                    Code = Convert.ToString(reader["Code"]),
                    SetNumber = Convert.ToInt16(reader["SetNumber"]),
                    Name = Convert.ToString(reader["Name"]),
                    CardType = Convert.ToString(reader["CardType"]),
                    NumCopies = Convert.ToInt16(reader["NumCopies"]),
                    FullText = Convert.ToString(reader["FullText"]),
                    Cost = Convert.ToString(reader["Cost"]),
                    Victory = Convert.ToString(reader["Victory"]),
                    ThumbnailURL = Convert.ToString(reader["ThumbnailURL"]),
                    ImageURL = Convert.ToString(reader["ImageURL"])
                };

                subtypeCommand.Parameters.Add("@code", System.Data.DbType.String).Value = card.Code;
                subtypeReader = subtypeCommand.ExecuteReader();
                subtypes = new List<string>();
                while (subtypeReader.Read())
                    subtypes.Add(Convert.ToString(subtypeReader["SubType"]));
                subtypeReader.Close();
                card.Subtypes = subtypes;
                subtypeCommand.Parameters.Clear();

                matchingCards.Add(card);
            }

            if (matchingCards.Count == 0)
                embed = new EmbedBuilder().WithColor(new Color(112, 141, 241)).WithTitle(string.Format("Couldn't find a cardname containing \"{0}\"", searchedCard)).Build();
            else if (matchingCards.Count == 1)
                embed = CreateSingleShardsCardEmbedOutput(matchingCards[0], small);
            else
            {
                int exactMatchIndex = -1;
                for (int i = 0; i < matchingCards.Count; i++)
                {
                    if (matchingCards[i].Name.Equals(searchedCard, StringComparison.OrdinalIgnoreCase))
                    {
                        exactMatchIndex = i;
                        break;
                    }
                }

                if (exactMatchIndex >= 0)
                    embed = CreateSingleShardsCardEmbedOutput(matchingCards[exactMatchIndex], small);
                else
                    embed = CreateMultipleShardsCardEmbedOutput(matchingCards);
            }

            return embed;
        }

        public static Embed ProcessShardstypeCommand(string searchedType)
        {
            Embed embed;
            List<string> matchingTypes = new List<string>();
            string sqlType = "SELECT CardType FROM ShardsTypes WHERE CardType LIKE '%" + searchedType.Replace("\'", "\'\'") + "%'";

            SQLiteCommand command = new SQLiteCommand(sqlType, Program.dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                matchingTypes.Add(Convert.ToString(reader["CardType"]));

            if (matchingTypes.Count == 0)
                embed = new EmbedBuilder().WithColor(new Color(112, 141, 241)).WithTitle(string.Format("Couldn't find a cardtype containing \"{0}\"", searchedType)).Build();
            else if (matchingTypes.Count == 1)
                embed = CreateSingleTypeEmbedOutput(matchingTypes[0]);
            else
            {
                int exactMatchIndex = -1;
                for (int i = 0; i < matchingTypes.Count; i++)
                {
                    if (matchingTypes[i].Equals(searchedType, StringComparison.OrdinalIgnoreCase))
                    {
                        exactMatchIndex = i;
                        break;
                    }
                }

                if (exactMatchIndex >= 0)
                    embed = CreateSingleTypeEmbedOutput(matchingTypes[exactMatchIndex]);
                else
                    embed = CreateMultipleTypesEmbedOutput(matchingTypes);
            }
            return embed;
        }

        private static Embed CreateSingleTypeEmbedOutput(string type)
        {
            EmbedBuilder builder = new EmbedBuilder();
            string sql = "SELECT Name, ImageUrl FROM ShardsCards WHERE ShardsCards.Code IN " +
                                         "(SELECT Code FROM ShardsCards WHERE CardType = @type " +
                                         "UNION SELECT Code FROM ShardsSubtypes WHERE SubType = @type)";


            SQLiteCommand command = new SQLiteCommand(sql, Program.dbConnection);
            command.Parameters.Add("@type", System.Data.DbType.String).Value = type;
            SQLiteDataReader reader = command.ExecuteReader();

            List<string[]> matchingCards = new List<string[]>();
            while (reader.Read())
                matchingCards.Add(new string[] { Convert.ToString(reader["Name"]), Convert.ToString(reader["ImageUrl"]) });

            if (matchingCards.Count == 1)
            {
                builder.AddField("All " + type + " Cards:", string.Format("[{0}]({1})", matchingCards[0][0], matchingCards[0][1]), false);
            }
            else
            {
                string column1 = string.Empty;
                string column2 = string.Empty;

                for (int i = 0; i < matchingCards.Count; i++)
                {
                    if (i <= (int)((matchingCards.Count - 1) * 0.5f))
                        column1 += string.Format("[{0}]({1})\n", matchingCards[i][0], matchingCards[i][1]);
                    else
                        column2 += string.Format("[{0}]({1})\n", matchingCards[i][0], matchingCards[i][1]);
                }

                builder.AddField("All " + type + " Cards:", column1, true);
                builder.AddField("\u200b", column2, true);
            }

            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static Embed CreateMultipleTypesEmbedOutput(List<string> matchingTypes)
        {

            EmbedBuilder builder = new EmbedBuilder();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();


            string column1 = string.Empty;
            string column2 = string.Empty;

            for (int i = 0; i < matchingTypes.Count; i++)
            {
                if (i <= (int)((matchingTypes.Count - 1) * 0.5f))
                    column1 += matchingTypes[i] + "\n";
                else
                    column2 += matchingTypes[i] + "\n";
            }

            builder.AddField("Found multiple matching types:", column1, true);
            builder.AddField("\u200b", column2, true);

            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static Embed CreateSingleShardsCardEmbedOutput(ShardsCard card, bool small)
        {
            EmbedBuilder builder = new EmbedBuilder();

            if (small)
            {
                builder.WithUrl(card.ImageURL);
                builder.WithThumbnailUrl(card.ThumbnailURL);
                builder.WithTitle(card.Name);



                string types = string.Empty;
                if (card.Subtypes.Count > 0)
                {
                    for (int i = 0; i < card.Subtypes.Count; i++)
                    {
                        types += card.Subtypes[i];
                        if (i < card.Subtypes.Count - 1)
                            types += ", ";
                    }
                }
                else
                {
                    types = card.CardType;
                }
                builder.AddField(types,
                                 card.FullText.Replace("<b>", "**")
                                              .Replace("</b>", "**")
                                              .Replace("<lb>", "\n")
                                              .Replace("<i>", "*")
                                              .Replace("</i>", "*"),
                                 false);

                builder.WithFooter(string.Format("Copies: {0}", card.NumCopies));
            }
            else
            {
                builder.WithImageUrl(card.ImageURL);
            }

            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

        private static Embed CreateMultipleShardsCardEmbedOutput(List<ShardsCard> cards)
        {
            EmbedBuilder builder = new EmbedBuilder();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();

            if (cards.Count <= 5)
            {
                fieldBuilder.WithName("Found multiple matching cards:");
                string value = string.Empty;
                for (int i = 0; i < 5 && i < cards.Count; i++)
                    value += cards[i].Name + "\n";
                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(false);
                builder.AddField(fieldBuilder);
            }
            else
            {
                fieldBuilder.WithName("Found multiple matching cards:");
                string value = string.Empty;
                for (int i = 0; i < 5; i++)
                    value += cards[i].Name + "\n";
                if (cards.Count > 10)
                    value += string.Format("\n*and {0} more matches*", cards.Count - 10);

                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(true);
                builder.AddField(fieldBuilder);

                fieldBuilder = new EmbedFieldBuilder();
                fieldBuilder.WithName("\u200b");
                value = string.Empty;
                for (int i = 5; i < 10 && i < cards.Count; i++)
                    value += cards[i].Name + "\n";
                fieldBuilder.WithValue(value);
                fieldBuilder.WithIsInline(true);
                builder.AddField(fieldBuilder);
            }
            return builder.WithColor(new Color(112, 141, 241)).Build();
        }

    }
}
