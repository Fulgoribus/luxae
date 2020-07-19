using System.Collections.Generic;
using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fulgoribus.Luxae.Web.Pages.Books
{
    public class CollectionModel : PageModel
    {
        public IEnumerable<Book> Books { get; set; } = new List<Book>();

        private readonly IBookRepository bookRepository;

        public CollectionModel(IBookRepository bookRepository)
        {
            this.bookRepository = bookRepository;
        }

        public async Task<IActionResult> OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Forbid();
            }

            Books = await bookRepository.GetUserBooksAsync(User);

            return Page();
        }
    }
}