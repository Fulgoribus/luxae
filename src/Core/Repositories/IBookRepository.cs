using System.Collections.Generic;
using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;

namespace Fulgoribus.Luxae.Repositories
{
    public interface IBookRepository
    {
        Task<IEnumerable<Series>> GetAllSeriesAsync();

        Task<Book?> GetBookAsync(int bookId);

        Task<Book?> GetBookByRetailerAsync(string retailerId, string retailerKey);

        Task<BookCover?> GetBookCoverAsync(int bookId);

        Task<Series?> GetSeriesAsync(string title);

        Task<IEnumerable<SeriesBook>> GetSeriesBooksAsync(int seriesId);

        Task SaveBookAsync(Book book);

        Task SaveBookCoverAsync(BookCover cover);

        Task SaveBookRetailerAsync(BookRetailer bookRetailer);

        Task SaveSeriesAsync(Series series);

        Task SaveSeriesBookAsync(SeriesBook seriesBook);
    }
}
