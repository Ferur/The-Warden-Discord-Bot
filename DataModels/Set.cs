using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot.HexPriceDataModels
{ 
    public class Set
    {
        public int id { get; set; }
        public string name { get; set; }
        public string externalName { get; set; }
        public int orderRank { get; set; }
        public string iconUrl { get; set; }
        public int setType { get; set; }
    }
}
