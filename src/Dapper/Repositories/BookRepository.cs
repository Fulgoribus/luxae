using System;
using System.Collections.Generic;
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

        public async Task<Series?> GetSeriesAsync(string title)
        {
            var sql = $"SELECT * FROM Series WHERE Title = @{nameof(title)}";
            var cmd = new CommandDefinition(sql, new { title });
            return await db.QuerySingleOrDefaultAsync<Series>(cmd);
        }

        public async Task<IEnumerable<SeriesBook>> GetSeriesBooksAsync(int seriesId)
        {
            var sql = $"SELECT * FROM SeriesBooks WHERE SeriesId = @{nameof(seriesId)} ORDER BY SortOrder";
            var cmd = new CommandDefinition(sql, new { seriesId });
            return await db.QueryAsync<SeriesBook>(cmd);
        }

        public async Task SaveBookAsync(Book book)
        {
            if (book.BookId.HasValue)
            {
                throw new NotImplementedException("Updating books is not yet implemented.");
            }

            var sql = $" INSERT INTO Books ([Title], [Author], [ReleaseDate], [Label])"
                + $" OUTPUT INSERTED.BookId"
                + $" VALUES (@{nameof(book.Title)}, @{nameof(book.Author)}, @{nameof(book.ReleaseDate)}, @{nameof(book.Label)})";
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

        public async Task SaveSeriesAsync(Series series)
        {
            if (series.SeriesId.HasValue)
            {
                throw new NotImplementedException("Updating series is not yet implemented.");
            }

            var sql = $" INSERT INTO Series ([Title])"
                + $" OUTPUT INSERTED.SeriesId"
                + $" VALUES (@{nameof(series.Title)})";
            var cmd = new CommandDefinition(sql, series);
            series.SeriesId = await db.QuerySingleAsync<int>(cmd);
        }

        public async Task SaveSeriesBookAsync(SeriesBook seriesBook)
        {
            var sql = $"DELETE FROM SeriesBooks WHERE SeriesId = @{nameof(seriesBook.Series.SeriesId)}"
                + $" AND BookId = @{nameof(seriesBook.Book.BookId)};"
                + "INSERT INTO SeriesBooks ([SeriesId], [BookId], [Volume], [SortOrder]) VALUES"
                + $" (@{nameof(seriesBook.Series.SeriesId)}, @{nameof(seriesBook.Book.BookId)}, @{nameof(seriesBook.Volume)}, @{nameof(seriesBook.SortOrder)})";
            var cmd = new CommandDefinition(sql, new { seriesBook.Series?.SeriesId, seriesBook.Book?.BookId, seriesBook.Volume, seriesBook.SortOrder });
            await db.ExecuteAsync(cmd);
        }
    }
}
