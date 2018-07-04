using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot.HexPriceDataModels
{
    public class DailyDeltaItem
    {
        public Item Item { get; set; }
        public Guid ItemId { get; set; }
        public double MedianToday { get; set; }
        public double MedianYesterday { get; set; }
        public int VolumeToday { get; set; }
        public int VolumeYesterday { get; set; }

        public double DeltaMedian { get; set; }

        public double DeltaMedianPercentage { get; set; }
    }
}
