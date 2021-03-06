﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Fulgoribus.Luxae.Web.Pages.Books
{
    public class SeriesModel : PageModel
    {
        public IEnumerable<SeriesBook> Books { get; set; } = new List<SeriesBook>();

        [BindProperty(SupportsGet = true)]
        public int? SeriesId { get; set; }

        public Series? Series { get; set; }

        public IEnumerable<SelectListItem> AllSeries { get; set; } = new List<SelectListItem>();

        private readonly IBookRepository bookRepository;

        public SeriesModel(IBookRepository bookRepository)
        {
            this.bookRepository = bookRepository;
        }

        public async Task OnGet()
        {
            var allSeries = await bookRepository.GetAllSeriesAsync();
            AllSeries = allSeries
                .OrderBy(s => s.Title)
                .Select(s => new SelectListItem(s.Title, s.SeriesId.ToString(), s.SeriesId == SeriesId));

            if (SeriesId.HasValue)
            {
                var seriesBooks = await bookRepository.GetSeriesBooksAsync(SeriesId.Value);
                Books = seriesBooks.OrderBy(s => s.SortOrder);
                Series = seriesBooks.FirstOrDefault()?.Series;
            }
        }
    }
}