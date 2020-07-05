using CsvHelper.Configuration;

namespace Fulgoribus.Luxae.BookwalkerImport
{
    public class BookwalkerBookMap : ClassMap<BookwalkerBook>
    {
        public BookwalkerBookMap()
        {
            Map(m => m.Title).Name("タイトル");
            Map(m => m.Url).Name("URL");
            Map(m => m.ReleaseDate).Name("配信日");
            Map(m => m.Publisher).Name("発行元");
            Map(m => m.Label).Name("レーベル");
            Map(m => m.Series).Name("シリーズ");
            Map(m => m.Category).Name("カテゴリ");
            Map(m => m.Price).Name("価格");
        }
    }
}
