using System;
using System.Collections.Generic;
using System.Text;

namespace CardBot
{
    public class ShardsCard
    {
        public string Code { get; set; }
        public int SetNumber { get; set; }
        public string Name { get; set; }
        public string CardType { get; set; }
        public List<string> Subtypes { get; set; }
        public int NumCopies { get; set; }
        public string FullText { get; set; }
        public string Cost { get; set; }
        public string Victory { get; set; }
        public string ThumbnailURL { get; set; }
        public string ImageURL { get; set; }
    }
}
