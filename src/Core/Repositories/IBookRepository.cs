using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;

namespace Fulgoribus.Luxae.Repositories
{
    public interface IBookRepository
    {
        Task<Book?> GetBookByRetailerAsync(string retailerId, string retailerKey);

        Task SaveBookAsync(Book book);

        Task SaveBookRetailerAsync(BookRetailer bookRetailer);
    }
}
