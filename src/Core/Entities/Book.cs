using System;

namespace Fulgoribus.Luxae.Entities
{
    public sealed class Book : IEquatable<Book>
    {
        public int? BookId { get; set; }
        public string? Title { get; set; }
        public string? Author { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Label { get; set; }
        public bool HasCover { get; set; } = false;

        public static Book InvalidBook
        {
            get
            {
                return new Book
                {
                    BookId = -1
                };
            }
        }

        public bool Equals(Book other) => other?.BookId == this.BookId;
    }
}
