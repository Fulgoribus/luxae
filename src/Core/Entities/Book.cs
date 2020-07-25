using System;
using System.Collections.Generic;

namespace Fulgoribus.Luxae.Entities
{
    public class Book
    {
        public int? BookId { get; set; }
        public int? ReleaseId { get; set; }
        public string? Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Label { get; set; }
        public bool HasCover { get; set; } = false;
        public bool HasBook { get; set; } = false;

        public string AuthorDisplay => Person.ConvertListToText(Authors);
        public string IllustratorDisplay => Person.ConvertListToText(Illustrators);
        public string TranslatorDisplay => Person.ConvertListToText(Translators);

        public IEnumerable<Person> Authors { get; set; } = new Person[0];
        public IEnumerable<Person> Illustrators { get; set; } = new Person[0];
        public IEnumerable<Person> Translators { get; set; } = new Person[0];
        public IEnumerable<SeriesBook> SeriesBooks { get; set; } = new SeriesBook[0];

        public bool IsValid => BookId.HasValue;
    }
}
