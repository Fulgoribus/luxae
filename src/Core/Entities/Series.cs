using System.Collections.Generic;

namespace Fulgoribus.Luxae.Entities
{
    public class Series
    {
        public int? SeriesId { get; set; }
        public string? Title { get; set; }
        public IEnumerable<Person> Authors { get; set; } = new Person[0];

        public string AuthorDisplay => Person.ConvertListToText(Authors);
    }
}
