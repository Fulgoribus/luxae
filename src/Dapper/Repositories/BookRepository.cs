using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Principal;
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

        public async Task AddToCollection(int bookId, IPrincipal user)
        {
            var userId = user.GetUserId();

            if (userId != null)
            {
                var sql = $"IF NOT EXISTS (SELECT * FROM UserBooks WHERE UserId = @{nameof(userId)} AND BookId = @{nameof(bookId)})"
                    + " INSERT INTO UserBooks (UserId, BookId)"
                    + $" VALUES (@{nameof(userId)}, @{nameof(bookId)})";
                var cmd = new CommandDefinition(sql, new { bookId, userId });
                await db.ExecuteAsync(cmd);
            }
        }

        public async Task<IEnumerable<Series>> GetAllSeriesAsync()
        {
            var sql = $"SELECT s.*, p.* FROM Series s"
                + " LEFT JOIN SeriesAuthors sa ON sa.SeriesId = s.SeriesId"
                + " LEFT JOIN People p ON p.PersonId = sa.PersonId"
                + " ORDER BY Title";
            var cmd = new CommandDefinition(sql);

            var authors = new Dictionary<int, List<Person>>();
            return await db.QueryAsync<Series, Person, Series>(cmd, (s, p) =>
            {
                if (!authors.ContainsKey(s.SeriesId!.Value))
                {
                    authors[s.SeriesId!.Value] = new List<Person>();
                }
                authors[s.SeriesId!.Value].Add(p);

                s.Authors = authors[s.SeriesId!.Value];

                return s;
            }, "SeriesId,PersonId");
        }

        public async Task<Book?> GetBookAsync(int bookId, string cultureCode, IPrincipal user)
        {
            var userId = user.GetUserId();

            var sql = "SELECT b.*, CASE WHEN ub.UserId IS NULL THEN CAST(0 as bit) ELSE CAST(1 as bit) END AS HasBook"
                + " FROM BooksByCulture b"
                + $" LEFT JOIN UserBooks ub ON ub.BookId = b.BookId AND ub.UserId = @{nameof(userId)}"
                + $" WHERE b.BookId = @{nameof(bookId)} AND b.CultureCode = @{nameof(cultureCode)}";
            var cmd = new CommandDefinition(sql, new { bookId, cultureCode, userId });
            var result = await db.QuerySingleOrDefaultAsync<Book>(cmd);

            if (result != null)
            {
                sql = "SELECT ISNULL(ba.Name, p.Name) AS Name, ba.RoleDesc FROM BookAuthors ba"
                    + " JOIN People p ON p.PersonId = ba.PersonId"
                    + $" WHERE ba.BookId = @{nameof(bookId)}"
                    + " ORDER BY ba.SortOrder";
                cmd = new CommandDefinition(sql, new { bookId });
                result.Authors = await db.QueryAsync<Person>(cmd);

                sql = "SELECT ISNULL(bi.Name, p.Name) AS Name, bi.RoleDesc FROM BookIllustrators bi"
                    + " JOIN People p ON p.PersonId = bi.PersonId"
                    + $" WHERE bi.BookId = @{nameof(bookId)}"
                    + " ORDER BY bi.SortOrder";
                cmd = new CommandDefinition(sql, new { bookId });
                result.Illustrators = await db.QueryAsync<Person>(cmd);

                sql = "SELECT ISNULL(bt.Name, p.Name) AS Name, bt.RoleDesc FROM BookTranslators bt"
                    + " JOIN People p ON p.PersonId = bt.PersonId"
                    + $" WHERE bt.ReleaseId = @{nameof(result.ReleaseId)}"
                    + " ORDER BY bt.SortOrder";
                cmd = new CommandDefinition(sql, result);
                result.Translators = await db.QueryAsync<Person>(cmd);

                sql = "SELECT * FROM SeriesBooks sb"
                    + " JOIN Series s ON s.SeriesId = sb.SeriesId"
                    + $" WHERE sb.BookId = @{nameof(bookId)}";
                cmd = new CommandDefinition(sql, new { bookId });
                result.SeriesBooks = await db.QueryAsync<SeriesBook, Series, SeriesBook>(cmd, (sb, s) => { sb.Series = s; return sb; }, "SeriesId");
            }

            return result;
        }

        public async Task<Book?> GetBookByRetailerAsync(string retailerId, string retailerKey)
        {
            var sql = "SELECT b.* FROM BookRetailers br JOIN Books b ON b.BookId = br.BookId"
                + $" WHERE br.RetailerId = @{nameof(retailerId)} AND br.RetailerKey = @{nameof(retailerKey)}";
            var cmd = new CommandDefinition(sql, new { retailerId, retailerKey });
            return await db.QuerySingleOrDefaultAsync<Book>(cmd);
        }

        public async Task<BookCover?> GetBookCoverAsync(int releaseId, bool isFullResolution)
        {
            var sql = "SELECT * FROM BookCovers bc"
                + $" WHERE bc.ReleaseId = @{nameof(releaseId)}"
                + $" AND bc.IsFullResolution = @{nameof(isFullResolution)}";
            var cmd = new CommandDefinition(sql, new { releaseId, isFullResolution });
            return await db.QuerySingleOrDefaultAsync<BookCover>(cmd);
        }

        public async Task<Series?> GetSeriesAsync(string title)
        {
            var sql = $"SELECT * FROM Series WHERE Title = @{nameof(title)}";
            var cmd = new CommandDefinition(sql, new { title });
            return await db.QuerySingleOrDefaultAsync<Series>(cmd);
        }

        public async Task<IEnumerable<SeriesBook>> GetSeriesBooksAsync(int seriesId, string cultureCode)
        {
            var sql = "SELECT sb.*, s.*, b.*,"
                + " CASE WHEN bc.ReleaseId IS NULL THEN CAST(0 as bit) ELSE CAST(1 as bit) END AS HasCover,"
                + " ba.PersonId,"
                + " ISNULL(ba.Name, p.Name) AS Name"
                + " FROM SeriesBooks sb"
                + " JOIN Series s ON s.SeriesId = sb.SeriesId"
                + " JOIN BooksByCulture b ON b.BookId = sb.BookId"
                + " LEFT JOIN BookCovers bc ON bc.ReleaseId = b.ReleaseId AND bc.IsFullResolution = 0"
                + " LEFT JOIN BookAuthors ba ON ba.BookId = sb.BookId"
                + " LEFT JOIN People p ON p.PersonId = ba.PersonId"
                + $" WHERE sb.SeriesId = @{nameof(seriesId)} AND b.CultureCode = @{nameof(cultureCode)}"
                + " ORDER BY SortOrder";
            var cmd = new CommandDefinition(sql, new { seriesId, cultureCode });

            var authors = new Dictionary<int, List<Person>>();
            return await db.QueryAsync<SeriesBook, Series, Book, Person, SeriesBook>(cmd, (sb, s, b, p) =>
            {
                if (!authors.ContainsKey(b.BookId!.Value))
                {
                    authors[b.BookId!.Value] = new List<Person>();
                }
                authors[b.BookId!.Value].Add(p);

                sb.Series = s;
                sb.Book = b;
                b.Authors = authors[b.BookId!.Value];

                return sb;
            }, "SeriesId,BookId,PersonId");
        }

        public async Task<IEnumerable<Book>> GetUserBooksAsync(string cultureCode, IPrincipal user)
        {
            var userId = user.GetUserId();

            var sql = "SELECT b.*,"
                + " CASE WHEN bc.ReleaseId IS NULL THEN CAST(0 as bit) ELSE CAST(1 as bit) END AS HasCover,"
                + " ba.PersonId,"
                + " ISNULL(ba.Name, p.Name) AS Name"
                + " FROM UserBooks ub"
                + " JOIN BooksByCulture b ON b.BookId = ub.BookId"
                + " LEFT JOIN BookCovers bc ON bc.ReleaseId = b.ReleaseId AND bc.IsFullResolution = 0"
                + " LEFT JOIN BookAuthors ba ON ba.BookId = ub.BookId"
                + " LEFT JOIN People p ON p.PersonId = ba.PersonId"
                + $" WHERE ub.UserId = @{nameof(userId)} AND b.CultureCode = @{nameof(cultureCode)}"
                + " ORDER BY SortOrder";
            var cmd = new CommandDefinition(sql, new { cultureCode, userId });

            var authors = new Dictionary<int, List<Person>>();
            return await db.QueryAsync<Book, Person, Book>(cmd, (b, p) =>
            {
                if (!authors.ContainsKey(b.BookId!.Value))
                {
                    authors[b.BookId!.Value] = new List<Person>();
                }
                authors[b.BookId!.Value].Add(p);

                b.Authors = authors[b.BookId!.Value];

                return b;
            }, "BookId,PersonId");
        }

        public async Task RemoveFromCollection(int bookId, IPrincipal user)
        {
            var userId = user.GetUserId();

            if (userId != null)
            {
                var sql = $"DELETE FROM UserBooks WHERE UserId = @{nameof(userId)} AND BookId = @{nameof(bookId)}";
                var cmd = new CommandDefinition(sql, new { bookId, userId });
                await db.ExecuteAsync(cmd);
            }
        }

        public async Task SaveBookAsync(Book book)
        {
            if (book.BookId.HasValue)
            {
                throw new NotImplementedException("Updating books is not yet implemented.");
            }

            var sql = "INSERT INTO Books ([Title], [ReleaseDate], [Label])"
                + " OUTPUT INSERTED.BookId"
                + $" VALUES (@{nameof(book.Title)}, @{nameof(book.ReleaseDate)}, @{nameof(book.Label)})";
            var cmd = new CommandDefinition(sql, book);
            book.BookId = await db.QuerySingleAsync<int>(cmd);
        }

        public async Task SaveBookCoverAsync(BookCover cover)
        {
            var sql = $"IF EXISTS (SELECT * FROM BookCovers WHERE BookId = @{nameof(cover.BookId)} AND IsFullResolution = @{nameof(cover.IsFullResolution)})"
                    + $" UPDATE BookCovers SET Image = @{nameof(cover.Image)}, ContentType = @{nameof(cover.ContentType)}"
                    + $" WHERE BookId = @{nameof(cover.BookId)} AND IsFullResolution = @{nameof(cover.IsFullResolution)}"
                + " ELSE INSERT INTO BookCovers (BookId, Image, ContentType, IsFullResolution)"
                    + $" VALUES (@{nameof(cover.BookId)}, @{nameof(cover.Image)}, @{nameof(cover.ContentType)}, @{nameof(cover.IsFullResolution)})";
            var cmd = new CommandDefinition(sql, cover);
            await db.ExecuteAsync(cmd);
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
