namespace Fulgoribus.Luxae.BookwalkerImport
{
    public static class ExtensionMethods
    {
        public static string? ValueOrNull(this string? value) =>
            string.IsNullOrWhiteSpace(value) || value == "――"
            ? null
            : value;
    }
}
