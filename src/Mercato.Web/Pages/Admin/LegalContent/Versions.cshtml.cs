using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.LegalContent;

/// <summary>
/// Page model for viewing and managing versions of a legal document.
/// </summary>
[Authorize(Roles = "Admin")]
public class VersionsModel : PageModel
{
    private readonly ILegalContentManagementService _legalContentService;
    private readonly ILogger<VersionsModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionsModel"/> class.
    /// </summary>
    /// <param name="legalContentService">The legal content management service.</param>
    /// <param name="logger">The logger.</param>
    public VersionsModel(
        ILegalContentManagementService legalContentService,
        ILogger<VersionsModel> logger)
    {
        _legalContentService = legalContentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the document type.
    /// </summary>
    public LegalDocumentType DocumentType { get; set; }

    /// <summary>
    /// Gets or sets the legal document.
    /// </summary>
    public LegalDocument? Document { get; set; }

    /// <summary>
    /// Gets or sets the list of versions.
    /// </summary>
    public IReadOnlyList<LegalDocumentVersion> Versions { get; set; } = [];

    /// <summary>
    /// Gets or sets the active version ID.
    /// </summary>
    public Guid? ActiveVersionId { get; set; }

    /// <summary>
    /// Gets or sets the success message.
    /// </summary>
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Handles GET requests to the versions page.
    /// </summary>
    /// <param name="documentType">The document type (as integer).</param>
    public async Task<IActionResult> OnGetAsync(int documentType)
    {
        if (!Enum.IsDefined(typeof(LegalDocumentType), documentType))
        {
            return NotFound();
        }

        DocumentType = (LegalDocumentType)documentType;

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var docResult = await _legalContentService.GetOrCreateDocumentByTypeAsync(DocumentType, userId);

        if (!docResult.Succeeded)
        {
            if (docResult.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", docResult.Errors);
            return RedirectToPage("Index");
        }

        Document = docResult.Document;

        if (Document != null)
        {
            var versionsResult = await _legalContentService.GetDocumentVersionsAsync(Document.Id);
            if (versionsResult.Succeeded)
            {
                Versions = versionsResult.Versions;
            }

            var activeResult = await _legalContentService.GetActiveVersionAsync(Document.Id);
            if (activeResult.Succeeded && activeResult.Version != null)
            {
                ActiveVersionId = activeResult.Version.Id;
            }
        }

        return Page();
    }

    /// <summary>
    /// Handles POST requests to publish a version.
    /// </summary>
    /// <param name="versionId">The version ID to publish.</param>
    /// <param name="documentType">The document type (as integer).</param>
    public async Task<IActionResult> OnPostPublishAsync(Guid versionId, int documentType)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var result = await _legalContentService.PublishVersionAsync(versionId, userId);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            ErrorMessage = string.Join(", ", result.Errors);
        }
        else
        {
            SuccessMessage = $"Version {result.Version?.VersionNumber} has been published successfully.";
            _logger.LogInformation("User {UserId} published version {VersionId}", userId, versionId);
        }

        return RedirectToPage(new { documentType });
    }
}
