using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Mercato.Web.Pages.Seller.Kyc;

[Authorize(Roles = "Seller")]
public class SubmitModel : PageModel
{
    private readonly IKycService _kycService;
    private readonly ILogger<SubmitModel> _logger;

    public SubmitModel(IKycService kycService, ILogger<SubmitModel> logger)
    {
        _kycService = kycService;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Please select a document type.")]
    public KycDocumentType DocumentType { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Please select a document file.")]
    public IFormFile? Document { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var sellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(sellerId))
        {
            _logger.LogWarning("User ID not found in claims");
            ModelState.AddModelError(string.Empty, "Unable to identify the current user.");
            return Page();
        }

        if (Document == null || Document.Length == 0)
        {
            ModelState.AddModelError(nameof(Document), "Please select a document file.");
            return Page();
        }

        // Validate file size before reading to prevent memory exhaustion
        const int maxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        if (Document.Length > maxFileSizeBytes)
        {
            ModelState.AddModelError(nameof(Document), $"Document size exceeds the maximum allowed size of {maxFileSizeBytes / (1024 * 1024)}MB.");
            return Page();
        }

        // Read file content
        using var memoryStream = new MemoryStream();
        await Document.CopyToAsync(memoryStream);
        var documentData = memoryStream.ToArray();

        var command = new SubmitKycCommand
        {
            SellerId = sellerId,
            DocumentType = DocumentType,
            DocumentFileName = Document.FileName,
            DocumentContentType = Document.ContentType,
            DocumentData = documentData
        };

        var result = await _kycService.SubmitAsync(command);

        if (result.Succeeded)
        {
            _logger.LogInformation("KYC document submitted successfully for seller {SellerId}", sellerId);
            return RedirectToPage("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }

        return Page();
    }
}
