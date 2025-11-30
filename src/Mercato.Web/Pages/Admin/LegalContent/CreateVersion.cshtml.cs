using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.LegalContent;

/// <summary>
/// Page model for creating a new legal document version.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateVersionModel : PageModel
{
    private readonly ILegalContentManagementService _legalContentService;
    private readonly ILogger<CreateVersionModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateVersionModel"/> class.
    /// </summary>
    /// <param name="legalContentService">The legal content management service.</param>
    /// <param name="logger">The logger.</param>
    public CreateVersionModel(
        ILegalContentManagementService legalContentService,
        ILogger<CreateVersionModel> logger)
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
    /// Gets or sets the input model.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for creating a new version.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the document ID.
        /// </summary>
        public Guid LegalDocumentId { get; set; }

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

        if (!docResult.Succeeded || docResult.Document == null)
        {
            if (docResult.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", docResult.Errors);
            return RedirectToPage("Index");
        }

        Document = docResult.Document;

        // Get existing versions to suggest next version number
        var versionsResult = await _legalContentService.GetDocumentVersionsAsync(Document.Id);
        string suggestedVersion = "1.0";
        if (versionsResult.Succeeded && versionsResult.Versions.Count > 0)
        {
            var latestVersion = versionsResult.Versions.First();
            suggestedVersion = SuggestNextVersion(latestVersion.VersionNumber);
        }

        Input = new InputModel
        {
            LegalDocumentId = Document.Id,
            DocumentType = documentType,
            VersionNumber = suggestedVersion,
            EffectiveDate = DateTime.UtcNow.Date.AddDays(1)
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
            await LoadDocumentAsync();
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

        var effectiveDateTime = new DateTimeOffset(
            Input.EffectiveDate.Add(Input.EffectiveTime),
            TimeSpan.Zero);

        var command = new CreateLegalDocumentVersionCommand
        {
            LegalDocumentId = Input.LegalDocumentId,
            VersionNumber = Input.VersionNumber,
            Content = Input.Content,
            EffectiveDate = effectiveDateTime,
            ChangeSummary = Input.ChangeSummary,
            CreatedByUserId = userId
        };

        var result = await _legalContentService.CreateVersionAsync(command);

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

            await LoadDocumentAsync();
            return Page();
        }

        _logger.LogInformation(
            "Created version {VersionNumber} for document {DocumentId} by user {UserId}",
            command.VersionNumber,
            command.LegalDocumentId,
            userId);

        TempData["SuccessMessage"] = $"Version {command.VersionNumber} created successfully.";
        return RedirectToPage("Versions", new { documentType = Input.DocumentType });
    }

    private async Task LoadDocumentAsync()
    {
        if (Enum.IsDefined(typeof(LegalDocumentType), Input.DocumentType))
        {
            DocumentType = (LegalDocumentType)Input.DocumentType;
            var docResult = await _legalContentService.GetDocumentByIdAsync(Input.LegalDocumentId);
            if (docResult.Succeeded)
            {
                Document = docResult.Document;
            }
        }
    }

    private static string SuggestNextVersion(string currentVersion)
    {
        // Try to parse version like "1.0", "2.1", etc.
        var parts = currentVersion.Split('.');
        if (parts.Length >= 1 && int.TryParse(parts[0], out var major))
        {
            return $"{major + 1}.0";
        }

        // Fallback
        return currentVersion + ".1";
    }
}
