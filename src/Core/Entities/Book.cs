using System;
using System.Collections.Generic;
using System.Text;

namespace Fulgoribus.Luxae.Entities
{
    public class Book
    {
        public int? BookId { get; set; }
        public string? Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string? Label { get; set; }
        public bool HasCover { get; set; } = false;

        public string AuthorDisplay => GetPersonText(Authors);
        public string IllustratorDisplay => GetPersonText(Illustrators);
        public string TranslatorDisplay => GetPersonText(Translators);

        public IEnumerable<Person> Authors { get; set; } = new Person[0];
        public IEnumerable<Person> Illustrators { get; set; } = new Person[0];
        public IEnumerable<Person> Translators { get; set; } = new Person[0];
        public IEnumerable<SeriesBook> SeriesBooks { get; set; } = new SeriesBook[0];

        public bool IsValid => BookId.HasValue;

        private static string GetPersonText(IEnumerable<Person> people)
        {
            var result = new StringBuilder();
            var isFirst = false;
            foreach (var person in people)
            {
                if (isFirst)
                {
                    result.Append("");
                }
                else
                {
                    isFirst = true;
                }
                result.Append(person.Name);
                if (!string.IsNullOrWhiteSpace(person.RoleDesc))
                {
                    result.Append(" (");
                    result.Append(person.RoleDesc);
                    result.Append(")");
                }
            }
            return result.ToString();
        }
    }
}
