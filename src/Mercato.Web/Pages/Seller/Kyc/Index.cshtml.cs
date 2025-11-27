using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Kyc;

[Authorize(Roles = "Seller")]
public class IndexModel : PageModel
{
    private readonly IKycService _kycService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IKycService kycService, ILogger<IndexModel> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    public IReadOnlyList<KycSubmission> Submissions { get; private set; } = [];
    public bool IsKycApproved { get; private set; }

    public async Task OnGetAsync()
    {
        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            return;
        }

        Submissions = await _kycService.GetSubmissionsBySellerAsync(sellerId);
        IsKycApproved = await _kycService.IsSellerKycApprovedAsync(sellerId);
    }
}
