using System.Collections.Generic;
using System.Text;

namespace Fulgoribus.Luxae.Entities
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public string RoleDesc { get; set; } = string.Empty;

        public static string ConvertListToText(IEnumerable<Person> people)
        {
            var result = new StringBuilder();
            var isFirst = false;
            foreach (var person in people)
            {
                if (isFirst)
                {
                    result.Append(", ");
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
