using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Cart
{
    // TODO: Implement actual page logic and UI
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // TODO: Implement actual page logic and UI
        }
    }
}
