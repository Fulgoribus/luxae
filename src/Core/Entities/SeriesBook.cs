namespace Fulgoribus.Luxae.Entities
{
    public class SeriesBook
    {
        public Series? Series { get; set; }
        public Book Book { get; set; } = new Book();
        public string? Volume { get; set; }
        public decimal? SortOrder { get; set; }
    }
}
