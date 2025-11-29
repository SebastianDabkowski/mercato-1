using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.Refunds;

/// <summary>
/// Page model for viewing refund details (admin).
/// </summary>
public class DetailsModel : PageModel
{
    private readonly IRefundService _refundService;
    private readonly ILogger<DetailsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DetailsModel"/> class.
    /// </summary>
    /// <param name="refundService">The refund service.</param>
    /// <param name="logger">The logger.</param>
    public DetailsModel(IRefundService refundService, ILogger<DetailsModel> logger)
    {
        _refundService = refundService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the refund.
    /// </summary>
    public Refund? Refund { get; set; }

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    /// <param name="id">The refund ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            ErrorMessage = "Refund ID is required.";
            return Page();
        }

        var result = await _refundService.GetRefundAsync(id);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(", ", result.Errors);
            return Page();
        }

        Refund = result.Refund;
        return Page();
    }
}
