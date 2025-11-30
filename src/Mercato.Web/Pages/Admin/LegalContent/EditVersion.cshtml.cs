using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.LegalContent;

/// <summary>
/// Page model for editing a legal document version.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditVersionModel : PageModel
{
    private readonly ILegalContentManagementService _legalContentService;
    private readonly ILogger<EditVersionModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditVersionModel"/> class.
    /// </summary>
    /// <param name="legalContentService">The legal content management service.</param>
    /// <param name="logger">The logger.</param>
    public EditVersionModel(
        ILegalContentManagementService legalContentService,
        ILogger<EditVersionModel> logger)
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
    /// Gets or sets the version being edited.
    /// </summary>
    public LegalDocumentVersion? Version { get; set; }

    /// <summary>
    /// Gets or sets whether the version is published.
    /// </summary>
    public bool IsPublished { get; set; }

    /// <summary>
    /// Gets or sets the input model.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for editing a version.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the version ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the document type.
        /// </summary>
        public int DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        [Required(ErrorMessage = "Version number is required.")]
        [StringLength(50, ErrorMessage = "Version number must not exceed 50 characters.")]
        [Display(Name = "Version Number")]
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content.
        /// </summary>
        [Required(ErrorMessage = "Content is required.")]
        [Display(Name = "Content (HTML)")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the effective date.
        /// </summary>
        [Required(ErrorMessage = "Effective date is required.")]
        [Display(Name = "Effective Date")]
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow.Date;

        /// <summary>
        /// Gets or sets the effective time.
        /// </summary>
        [Display(Name = "Effective Time (UTC)")]
        public TimeSpan EffectiveTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the change summary.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Change summary must not exceed 2000 characters.")]
        [Display(Name = "Change Summary (optional)")]
        public string? ChangeSummary { get; set; }
    }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    /// <param name="id">The version ID to edit.</param>
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
        IsPublished = Version.IsPublished;

        if (Document != null)
        {
            DocumentType = Document.DocumentType;
        }

        // Cannot edit published versions that are already effective
        if (Version.IsPublished && Version.EffectiveDate <= DateTimeOffset.UtcNow)
        {
            TempData["ErrorMessage"] = "Cannot edit a published version that is already effective. Create a new version instead.";
            return RedirectToPage("Versions", new { documentType = (int)DocumentType });
        }

        Input = new InputModel
        {
            Id = Version.Id,
            DocumentType = (int)DocumentType,
            VersionNumber = Version.VersionNumber,
            Content = Version.Content,
            EffectiveDate = Version.EffectiveDate.UtcDateTime.Date,
            EffectiveTime = Version.EffectiveDate.UtcDateTime.TimeOfDay,
            ChangeSummary = Version.ChangeSummary
        };

        return Page();
    }

    /// <summary>
    /// Handles POST requests.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadVersionAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var effectiveDateTime = new DateTimeOffset(
            Input.EffectiveDate.Add(Input.EffectiveTime),
            TimeSpan.Zero);

        var command = new UpdateLegalDocumentVersionCommand
        {
            Id = Input.Id,
            VersionNumber = Input.VersionNumber,
            Content = Input.Content,
            EffectiveDate = effectiveDateTime,
            ChangeSummary = Input.ChangeSummary,
            UpdatedByUserId = userId
        };

        var result = await _legalContentService.UpdateVersionAsync(command);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            await LoadVersionAsync();
            return Page();
        }

        _logger.LogInformation(
            "Updated version {VersionId} by user {UserId}",
            command.Id,
            userId);

        TempData["SuccessMessage"] = $"Version {command.VersionNumber} updated successfully.";
        return RedirectToPage("Versions", new { documentType = Input.DocumentType });
    }

    private async Task LoadVersionAsync()
    {
        var versionResult = await _legalContentService.GetVersionByIdAsync(Input.Id);
        if (versionResult.Succeeded)
        {
            Version = versionResult.Version;
            Document = versionResult.Document;
            IsPublished = Version?.IsPublished ?? false;
            if (Document != null)
            {
                DocumentType = Document.DocumentType;
            }
        }
    }
}
