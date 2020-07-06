namespace Fulgoribus.Luxae.BookwalkerImport
{
    public static class ExtensionMethods
    {
        public static string? GetValueOrNull(this string? value) =>
            string.IsNullOrWhiteSpace(value) || value == "――"
            ? null
            : value;
    }
}
