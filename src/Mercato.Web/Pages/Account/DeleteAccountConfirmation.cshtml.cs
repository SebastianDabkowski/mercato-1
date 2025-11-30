using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Account;

/// <summary>
/// Page model for the account deletion confirmation page.
/// Shown after a user successfully deletes their account.
/// </summary>
public class DeleteAccountConfirmationModel : PageModel
{
    /// <summary>
    /// Handles the GET request for the confirmation page.
    /// </summary>
    public void OnGet()
    {
        // This page is shown after successful account deletion
        // No specific logic needed - just display the confirmation
    }
}
