using System.Threading.Tasks;
using Fulgoribus.Luxae.Entities;
using Fulgoribus.Luxae.Repositories;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fulgoribus.Luxae.Web.Pages.Books
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int? BookId { get; set; }

        public Book Book { get; set; } = new Book();

        private readonly IBookRepository bookRepository;

        public IndexModel(IBookRepository bookRepository)
        {
            this.bookRepository = bookRepository;
        }

        public async Task<IActionResult> OnGet()
        {
            if (BookId.HasValue)
            {
                var cultureFeature = Request.HttpContext.Features.Get<IRequestCultureFeature>();
                Book = await bookRepository.GetBookAsync(BookId.Value, cultureFeature.RequestCulture.UICulture.ToString(), User) ?? new Book();
            }

            return Book.IsValid
                ? (IActionResult)Page()
                : NotFound();
        }

        public async Task<IActionResult> OnPostAdd()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Forbid();
            }
            else if (!BookId.HasValue)
            {
                return BadRequest();
            }

            await bookRepository.AddToCollection(BookId.Value, User);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemove()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Forbid();
            }
            else if (!BookId.HasValue)
            {
                return BadRequest();
            }

            await bookRepository.RemoveFromCollection(BookId.Value, User);

            return RedirectToPage();
        }
    }
}