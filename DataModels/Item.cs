using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CardBot.HexPriceDataModels
{
    public enum ItemRarity
    {
        Unknown = -1,
        Common = 2,
        Uncommon = 3,
        Rare = 4,
        Promo = 5,
        Legendary = 6
    }

    public enum ItemType
    {
        Unknown = -1,
        Card = 1,
        Mercenary = 2,
        Equipment = 3,
        Booster = 4,
        Chest = 5,
        Dust = 6, 
        Champion = 7,
    }

    public class Item
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string LinkName { get; set; }
        public ItemRarity Rarity { get; set; }
        public ItemType Type { get; set; }
        public int SetId { get; set; }

        public Set Set { get; set; }
        public DailyDeltaItem Delta { get; set; }
        public List<Auction> Auctions { get; set; }

        public DateTime? LastUpdate { get; set; }
    }
}
