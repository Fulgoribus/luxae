using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Fulgoribus.Luxae.Web.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet() => RedirectToPage("Books/Series");
    }
}
