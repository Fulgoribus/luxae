using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Fulgoribus.ConsoleUtilities;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;
using Lamar;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using static BookwalkerImport.Constants;

namespace Fulgoribus.Luxae.BookwalkerImport
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var configuration = ConsoleConfigurationBuilder.BuildConfiguration(args, assembly);

            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.Scan(s =>
            {
                // Look for any registry in a DLL we built.
                s.AssembliesAndExecutablesFromApplicationBaseDirectory(f => f?.FullName?.StartsWith("Fulgoribus.", StringComparison.OrdinalIgnoreCase) ?? false);

                s.LookForRegistries();
            });
            // Need to use a lamba to resolve the SqlConnection because trying to bind by type was going off into setter injection land.
            serviceRegistry.For<IDbConnection>().Use(_ => new SqlConnection(configuration.GetConnectionString("DefaultConnection"))).Scoped();
            var container = new Container(serviceRegistry);

            var bookRepo = container.GetInstance<IBookRepository>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var sqlConnection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")))
            using (var reader = new StreamReader(configuration["BookwalkerImport:ImportPath"], Encoding.GetEncoding(932)))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.RegisterClassMap<BookwalkerBookMap>();
                await foreach (var record in csv.EnumerateRecordsAsync(new BookwalkerBook()))
                {
                    if (record.Url?.StartsWith("https://global.bookwalker.jp") ?? false)
                    {
                        if (record.Category == Categories.ArtBook
                            || record.Category == Categories.BookshelfSkin
                            || record.Category == Categories.LiteratureAndNovels
                            || record.Category == Categories.Manga
                            || record.Category == Categories.NonFiction)
                        {
                            // Do nothing.
                        }
                        else if (record.Category == Categories.LightNovel)
                        {
                            var bookId = record.Url.Replace("https://global.bookwalker.jp/", string.Empty).Replace("/", string.Empty);

                            var book = await bookRepo.GetBookByRetailerAsync("BW", bookId);
                            if (book == null)
                            {
                                book = new Book
                                {
                                    Title = record.Title,
                                    ReleaseDate = record.ReleaseDate,
                                    Series = record.Series,
                                    Label = record.Label
                                };
                                await bookRepo.SaveBookAsync(book);

                                var bookRetailer = new BookRetailer
                                {
                                    BookId = book.BookId,
                                    RetailerId = "BW",
                                    RetailerKey = bookId
                                };
                                await bookRepo.SaveBookRetailerAsync(bookRetailer);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Unknown category: {record.Category}, {record.Title}, {record.Url}");
                        }
                    }
                }
            }
        }
    }
}
