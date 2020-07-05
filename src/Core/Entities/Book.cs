using System;

namespace Fulgoribus.Luxae.Entities
{
    public class Book
    {
        public int? BookId { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Series { get; set; }
        public string? Label { get; set; }
    }
}
