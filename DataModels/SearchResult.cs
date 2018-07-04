
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardBot.HexPriceDataModels
{
    public class SearchResult
    {
        public Item Item { get; set; }
        public int CurrentCheapestPlat { get; set; }
        public int CurrentCheapestGold { get; set; }
        public int OpenAuctionsCount { get; set; }
    }
}
