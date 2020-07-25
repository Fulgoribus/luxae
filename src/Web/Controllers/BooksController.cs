﻿using System.Threading.Tasks;
using Fulgoribus.Luxae.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fulgoribus.Luxae.Web.Controllers
{
    public class BooksController : Controller
    {
        private readonly IBookRepository bookRepository;

        public BooksController(IBookRepository bookRepository)
        {
            this.bookRepository = bookRepository;
        }

        /// <remarks>
        /// Tell the browser to cache images for 1 day.
        /// </remarks>
        [ResponseCache(Duration = 60 * 24, Location = ResponseCacheLocation.Client)]
        [Route("{controller}/{action}/{releaseId?}/{isFullResolution?}")]
        public async Task<IActionResult> Cover(int releaseId, bool isFullResolution = false)
        {
            var cover = await bookRepository.GetBookCoverAsync(releaseId, isFullResolution);

            if (cover == null)
            {
                return isFullResolution
                    ? await Cover(releaseId, false)
                    : NotFound();
            }

            return new FileContentResult(cover.Image, cover.ContentType);
        }
    }
}
