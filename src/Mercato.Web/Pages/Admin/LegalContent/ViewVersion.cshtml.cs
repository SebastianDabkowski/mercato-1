using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.LegalContent;

/// <summary>
/// Page model for viewing a legal document version.
/// </summary>
[Authorize(Roles = "Admin")]
public class ViewVersionModel : PageModel
{
    private readonly ILegalContentManagementService _legalContentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViewVersionModel"/> class.
    /// </summary>
    /// <param name="legalContentService">The legal content management service.</param>
    public ViewVersionModel(ILegalContentManagementService legalContentService)
    {
        _legalContentService = legalContentService;
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
    /// Gets or sets the version being viewed.
    /// </summary>
    public LegalDocumentVersion? Version { get; set; }

    /// <summary>
    /// Gets or sets the upcoming version if one exists.
    /// </summary>
    public LegalDocumentVersion? UpcomingVersion { get; set; }

    /// <summary>
    /// Gets or sets whether this version is the active version.
    /// </summary>
    public bool IsActiveVersion { get; set; }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    /// <param name="id">The version ID to view.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var versionResult = await _legalContentService.GetVersionByIdAsync(id);

        if (!versionResult.Succeeded || versionResult.Version == null)
        {
            if (versionResult.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", versionResult.Errors);
            return RedirectToPage("Index");
        }

        Version = versionResult.Version;
        Document = versionResult.Document;
        UpcomingVersion = versionResult.UpcomingVersion;

        if (Document != null)
        {
            DocumentType = Document.DocumentType;

            // Check if this is the active version
            var activeResult = await _legalContentService.GetActiveVersionAsync(Document.Id);
            if (activeResult.Succeeded && activeResult.Version != null)
            {
                IsActiveVersion = activeResult.Version.Id == Version.Id;
            }
        }

        return Page();
    }
}
