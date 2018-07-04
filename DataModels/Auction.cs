using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot.HexPriceDataModels
{
    public enum AuctionState
    {
        Unknown = -1,
        Posted = 1,
        UnderBid = 2,
        Buyout = 100,
        NoSale = 101,
        SoldBid = 102
    }

    public class Auction
    {
        public int Id { get; set; }
        public string OwnerId { get; set; }
        public string LastActorId { get; set; }
        public Guid ItemId { get; set; }
        public AuctionState CurrentState { get; set; }
        public bool IsGold { get; set; }
        public int StartingBid { get; set; }
        public int CurrentBid { get; set; }
        public int Buyout { get; set; }
        public DateTime PostedDateTime { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
