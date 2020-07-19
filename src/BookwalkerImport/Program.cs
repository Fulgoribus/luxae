using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using Fulgoribus.ConsoleUtilities;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;
using Lamar;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            serviceRegistry.AddHttpClient();
            // Need to use a lamba to resolve the SqlConnection because trying to bind by type was going off into setter injection land.
            serviceRegistry.For<IDbConnection>().Use(_ => new SqlConnection(configuration.GetConnectionString("DefaultConnection"))).Scoped();
            using var container = new Container(serviceRegistry);

            var bookRepo = container.GetInstance<IBookRepository>();
            var regexVolume = new Regex("[0-9]+[0-9.]*", RegexOptions.Compiled);
            var regexCover = new Regex(@"""https:\/\/c\.bookwalker\.jp\/([0-9]+)\/.*\.jpg""", RegexOptions.Compiled);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var httpClientFactory = container.GetInstance<IHttpClientFactory>();
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
                                    Label = record.Label
                                };
                                await bookRepo.SaveBookAsync(book);

                                var seriesTitle = record.Series.GetValueOrNull();
                                if (seriesTitle != null)
                                {
                                    var series = await bookRepo.GetSeriesAsync(seriesTitle);
                                    if (series == null)
                                    {
                                        series = new Series
                                        {
                                            Title = seriesTitle
                                        };
                                        await bookRepo.SaveSeriesAsync(series);
                                    }

                                    // Try to get the sort order as the first number in the title (to handle Kokoro Connect Volumes 9/10) that is not
                                    // part of the series (to handle 86).
                                    decimal? sortOrder = null;
                                    var titleForParsing = book.Title.Replace(seriesTitle, string.Empty);
                                    var volumeCandidates = regexVolume.Matches(titleForParsing);
                                    var books = await bookRepo.GetSeriesBooksAsync(series.SeriesId!.Value);
                                    if (volumeCandidates.Any())
                                    {
                                        // Take the first one.
                                        sortOrder = Convert.ToDecimal(volumeCandidates.First().Value);

                                        // Make sure this doesn't collide with an existing volume. (e.g. Ascendance of a Bookworm)
                                        if (books.Any(b => b.SortOrder == sortOrder))
                                        {
                                            sortOrder = null;
                                        }
                                    }

                                    if (!sortOrder.HasValue)
                                    {
                                        Console.WriteLine($"Unknown series order for series {seriesTitle}, book {book.Title}. Picking next available integer.");
                                        if (books.Any())
                                        {
                                            var lastSortOrder = books.Last().SortOrder!.Value;
                                            sortOrder = Math.Ceiling(lastSortOrder);
                                            if (sortOrder == lastSortOrder)
                                            {
                                                sortOrder += 1;
                                            }
                                        }
                                        else
                                        {
                                            sortOrder = 1;
                                        }
                                    }
                                    var seriesBook = new SeriesBook
                                    {
                                        Series = series,
                                        Book = book,
                                        SortOrder = sortOrder,
                                        Volume = sortOrder.ToString()
                                    };
                                    await bookRepo.SaveSeriesBookAsync(seriesBook);
                                }

                                var bookRetailer = new BookRetailer
                                {
                                    BookId = book.BookId,
                                    RetailerId = "BW",
                                    RetailerKey = bookId
                                };
                                await bookRepo.SaveBookRetailerAsync(bookRetailer);
                            }

                            if (book.BookId.HasValue)
                            {
                                var cover = await bookRepo.GetBookCoverAsync(book.BookId.Value);
                                if (cover == null)
                                {
                                    cover = new BookCover
                                    {
                                        BookId = book.BookId.Value
                                    };

                                    try
                                    {
                                        var client = httpClientFactory.CreateClient();
                                        var body = await client.GetStringAsync(record.Url);
                                        var matches = regexCover.Matches(body);
                                        if (matches.Any() && int.TryParse(new string(matches.First().Groups[1].Value.Reverse().ToArray()), out var id))
                                        {
                                            var imageUrl = "http://c.bookwalker.jp/coverImage_" + (id - 1) + ".jpg";
                                            var result = await client.GetAsync(imageUrl);
                                            if (result.StatusCode == HttpStatusCode.Forbidden)
                                            {
                                                // Try using the URL as-is. Not as good as the full-fat cover but it will do.
                                                var quotedUrl = matches.First().Value;
                                                var url = quotedUrl[1..^1];
                                                result = await client.GetAsync(url);
                                            }
                                            result.EnsureSuccessStatusCode();
                                            cover.Image = await result.Content.ReadAsByteArrayAsync();
                                            cover.ContentType = result.Content.Headers.ContentType.ToString();
                                            await bookRepo.SaveBookCoverAsync(cover);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine($"Error retrieving cover data for {record.Title} from {record.Url}");
                                    }
                                }
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
