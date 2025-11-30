using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.LegalContent;

/// <summary>
/// Page model for the legal content management index page.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ILegalContentManagementService _legalContentService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="legalContentService">The legal content management service.</param>
    /// <param name="logger">The logger.</param>
    public IndexModel(
        ILegalContentManagementService legalContentService,
        ILogger<IndexModel> logger)
    {
        _legalContentService = legalContentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the list of document summaries.
    /// </summary>
    public List<DocumentSummary> DocumentSummaries { get; set; } = [];

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
    /// Summary information for a legal document.
    /// </summary>
    public class DocumentSummary
    {
        /// <summary>
        /// Gets or sets the document type.
        /// </summary>
        public LegalDocumentType DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the document description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the document has been created.
        /// </summary>
        public bool HasDocument { get; set; }

        /// <summary>
        /// Gets or sets the document ID if it exists.
        /// </summary>
        public Guid? DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the active version number.
        /// </summary>
        public string? ActiveVersionNumber { get; set; }

        /// <summary>
        /// Gets or sets the active version effective date.
        /// </summary>
        public DateTimeOffset? ActiveVersionEffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets the upcoming version number if one exists.
        /// </summary>
        public string? UpcomingVersionNumber { get; set; }

        /// <summary>
        /// Gets or sets the upcoming version effective date.
        /// </summary>
        public DateTimeOffset? UpcomingVersionEffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets the total number of versions.
        /// </summary>
        public int VersionCount { get; set; }
    }

    /// <summary>
    /// Handles GET requests to the legal content index page.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var documentTypes = Enum.GetValues<LegalDocumentType>();

        foreach (var docType in documentTypes)
        {
            var summary = new DocumentSummary
            {
                DocumentType = docType,
                Title = GetDefaultTitle(docType),
                Description = GetDefaultDescription(docType)
            };

            var docResult = await _legalContentService.GetDocumentByTypeAsync(docType);
            if (docResult.Succeeded && docResult.Document != null)
            {
                summary.HasDocument = true;
                summary.DocumentId = docResult.Document.Id;
                summary.Title = docResult.Document.Title;
                summary.Description = docResult.Document.Description;

                var versionsResult = await _legalContentService.GetDocumentVersionsAsync(docResult.Document.Id);
                if (versionsResult.Succeeded)
                {
                    summary.VersionCount = versionsResult.Versions.Count;
                }

                var activeResult = await _legalContentService.GetActiveVersionAsync(docResult.Document.Id);
                if (activeResult.Succeeded && activeResult.Version != null)
                {
                    summary.ActiveVersionNumber = activeResult.Version.VersionNumber;
                    summary.ActiveVersionEffectiveDate = activeResult.Version.EffectiveDate;

                    if (activeResult.UpcomingVersion != null)
                    {
                        summary.UpcomingVersionNumber = activeResult.UpcomingVersion.VersionNumber;
                        summary.UpcomingVersionEffectiveDate = activeResult.UpcomingVersion.EffectiveDate;
                    }
                }
            }

            DocumentSummaries.Add(summary);
        }

        return Page();
    }

    private static string GetDefaultTitle(LegalDocumentType documentType)
    {
        return documentType switch
        {
            LegalDocumentType.TermsOfService => "Terms of Service",
            LegalDocumentType.PrivacyPolicy => "Privacy Policy",
            LegalDocumentType.CookiePolicy => "Cookie Policy",
            LegalDocumentType.SellerAgreement => "Seller Agreement",
            _ => documentType.ToString()
        };
    }

    private static string GetDefaultDescription(LegalDocumentType documentType)
    {
        return documentType switch
        {
            LegalDocumentType.TermsOfService => "The terms and conditions for using the marketplace platform.",
            LegalDocumentType.PrivacyPolicy => "How we collect, use, and protect your personal information.",
            LegalDocumentType.CookiePolicy => "Information about how we use cookies and similar technologies.",
            LegalDocumentType.SellerAgreement => "The agreement between sellers and the marketplace platform.",
            _ => $"Legal document for {documentType}"
        };
    }
}
