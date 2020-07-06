using System;

namespace Fulgoribus.Luxae.BookwalkerImport
{
    public class BookwalkerBook
    {
        public string Title { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Publisher { get; set; }
        public string? Label { get; set; }
        public string? Series { get; set; }
        public string? Category { get; set; }
        public int Price { get; set; } = 0;
    }
}
