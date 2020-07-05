using System.Data;
using System.Threading.Tasks;
using Dapper;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;

namespace Fulgoribus.Luxae.Dapper.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly IDbConnection db;

        public BookRepository(IDbConnection db)
        {
            this.db = db;
        }

        public async Task<Book?> GetBookByRetailerAsync(string retailerId, string retailerKey)
        {
            var sql = "SELECT b.* FROM BookRetailers br JOIN Books b ON b.BookId = br.BookId"
                + $" WHERE br.RetailerId = @{nameof(retailerId)} AND br.RetailerKey = @{nameof(retailerKey)}";
            var cmd = new CommandDefinition(sql, new { retailerId, retailerKey });
            return await db.QuerySingleOrDefaultAsync<Book>(cmd);
        }

        public async Task SaveBookAsync(Book book)
        {
            var sql = $" INSERT INTO Books ([Title], [Author], [ReleaseDate], [Series], [Label])"
                + $" OUTPUT INSERTED.BookId"
                + $" VALUES (@{nameof(book.Title)}, @{nameof(book.Author)}, @{nameof(book.ReleaseDate)}, @{nameof(book.Series)}, @{nameof(book.Label)})";
            var cmd = new CommandDefinition(sql, book);
            book.BookId = await db.QuerySingleAsync<int>(cmd);
        }

        public async Task SaveBookRetailerAsync(BookRetailer bookRetailer)
        {
            var sql = $"DELETE FROM BookRetailers WHERE RetailerId = @{nameof(bookRetailer.RetailerId)}"
                + $" AND RetailerKey = @{nameof(bookRetailer.RetailerKey)} AND BookId = @{nameof(bookRetailer.BookId)};"
                + "INSERT INTO BookRetailers ([BookId], [RetailerId], [RetailerKey]) VALUES"
                + $" (@{nameof(bookRetailer.BookId)}, @{nameof(bookRetailer.RetailerId)}, @{nameof(bookRetailer.RetailerKey)})";
            var cmd = new CommandDefinition(sql, bookRetailer);
            await db.ExecuteAsync(cmd);
        }
    }
}
