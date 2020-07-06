using System.Collections.Generic;
using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;

namespace Fulgoribus.Luxae.Repositories
{
    public interface IBookRepository
    {
        Task<Book?> GetBookByRetailerAsync(string retailerId, string retailerKey);

        Task<Series?> GetSeriesAsync(string title);

        Task<IEnumerable<SeriesBook>> GetSeriesBooksAsync(int seriesId);

        Task SaveBookAsync(Book book);

        Task SaveBookRetailerAsync(BookRetailer bookRetailer);

        Task SaveSeriesAsync(Series series);

        Task SaveSeriesBookAsync(SeriesBook seriesBook);
    }
}
