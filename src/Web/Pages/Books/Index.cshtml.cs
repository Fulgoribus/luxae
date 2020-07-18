using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fulgoribus.Luxae.Web.Pages.Books
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int? BookId { get; set; }

        public Book Book { get; set; } = Book.InvalidBook;

        private readonly IBookRepository bookRepository;

        public IndexModel(IBookRepository bookRepository)
        {
            this.bookRepository = bookRepository;
        }

        public async Task<IActionResult> OnGet()
        {
            if (BookId.HasValue)
            {
                Book = await bookRepository.GetBookAsync(BookId.Value) ?? Book.InvalidBook;
            }

            if (this.Book == Book.InvalidBook)
            {
                return NotFound();
            }

            return Page();
        }
    }
}