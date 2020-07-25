using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;

namespace Fulgoribus.Luxae.Repositories
{
    public interface IBookRepository
    {
        Task AddToCollection(int bookId, IPrincipal user);

        Task<IEnumerable<Series>> GetAllSeriesAsync();

        Task<Book?> GetBookAsync(int bookId, string cultureCode, IPrincipal user);

        Task<Book?> GetBookByRetailerAsync(string retailerId, string retailerKey);

        Task<BookCover?> GetBookCoverAsync(int releaseId, bool isFullResolution);

        Task<Series?> GetSeriesAsync(string title);

        Task<IEnumerable<SeriesBook>> GetSeriesBooksAsync(int seriesId, string cultureCode);

        Task<IEnumerable<Book>> GetUserBooksAsync(string cultureCode, IPrincipal user);

        Task RemoveFromCollection(int bookId, IPrincipal user);

        Task SaveBookAsync(Book book);

        Task SaveBookCoverAsync(BookCover cover);

        Task SaveBookRetailerAsync(BookRetailer bookRetailer);

        Task SaveSeriesAsync(Series series);

        Task SaveSeriesBookAsync(SeriesBook seriesBook);
    }
}
