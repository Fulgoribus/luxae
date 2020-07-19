namespace Fulgoribus.Luxae.Entities
{
    public class BookCover
    {
        public int BookId { get; set; } = 0;
        public byte[] Image { get; set; } = new byte[0];
        public string ContentType { get; set; } = string.Empty;
        public bool IsFullResolution { get; set; } = false;
    }
}
