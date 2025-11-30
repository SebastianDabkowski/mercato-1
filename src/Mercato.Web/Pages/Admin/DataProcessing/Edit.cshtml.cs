using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for editing a data processing activity.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IDataProcessingRegistryService _registryService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="registryService">The data processing registry service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        IDataProcessingRegistryService registryService,
        ILogger<EditModel> logger)
    {
        _registryService = registryService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for editing a data processing activity.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the activity ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the processing activity.
        /// </summary>
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
        [Display(Name = "Activity Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the purpose of processing.
        /// </summary>
        [Required(ErrorMessage = "Purpose is required.")]
        [StringLength(2000, ErrorMessage = "Purpose must not exceed 2000 characters.")]
        [Display(Name = "Purpose of Processing")]
        public string Purpose { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the legal basis.
        /// </summary>
        [Required(ErrorMessage = "Legal basis is required.")]
        [StringLength(500, ErrorMessage = "Legal basis must not exceed 500 characters.")]
        [Display(Name = "Legal Basis")]
        public string LegalBasis { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data categories.
        /// </summary>
        [Required(ErrorMessage = "Data categories is required.")]
        [StringLength(2000, ErrorMessage = "Data categories must not exceed 2000 characters.")]
        [Display(Name = "Categories of Personal Data")]
        public string DataCategories { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the data subject categories.
        /// </summary>
        [Required(ErrorMessage = "Data subject categories is required.")]
        [StringLength(2000, ErrorMessage = "Data subject categories must not exceed 2000 characters.")]
        [Display(Name = "Categories of Data Subjects")]
        public string DataSubjectCategories { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the recipients.
        /// </summary>
        [Required(ErrorMessage = "Recipients is required.")]
        [StringLength(2000, ErrorMessage = "Recipients must not exceed 2000 characters.")]
        [Display(Name = "Recipients")]
        public string Recipients { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets third country transfers information.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Third country transfers must not exceed 2000 characters.")]
        [Display(Name = "Third Country Transfers")]
        public string? ThirdCountryTransfers { get; set; }

        /// <summary>
        /// Gets or sets the retention period.
        /// </summary>
        [Required(ErrorMessage = "Retention period is required.")]
        [StringLength(500, ErrorMessage = "Retention period must not exceed 500 characters.")]
        [Display(Name = "Retention Period")]
        public string RetentionPeriod { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the technical measures.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Technical measures must not exceed 2000 characters.")]
        [Display(Name = "Technical Security Measures")]
        public string? TechnicalMeasures { get; set; }

        /// <summary>
        /// Gets or sets the organizational measures.
        /// </summary>
        [StringLength(2000, ErrorMessage = "Organizational measures must not exceed 2000 characters.")]
        [Display(Name = "Organizational Security Measures")]
        public string? OrganizationalMeasures { get; set; }

        /// <summary>
        /// Gets or sets the processor name.
        /// </summary>
        [StringLength(200, ErrorMessage = "Processor name must not exceed 200 characters.")]
        [Display(Name = "Processor Name")]
        public string? ProcessorName { get; set; }

        /// <summary>
        /// Gets or sets the processor contact.
        /// </summary>
        [StringLength(500, ErrorMessage = "Processor contact must not exceed 500 characters.")]
        [Display(Name = "Processor Contact")]
        public string? ProcessorContact { get; set; }

        /// <summary>
        /// Gets or sets the reason for the update.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Reason must not exceed 1000 characters.")]
        [Display(Name = "Reason for Change")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    /// <param name="id">The activity ID.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _registryService.GetActivityByIdAsync(id);

        if (!result.Succeeded || result.Activity == null)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            return NotFound();
        }

        if (!result.Activity.IsActive)
        {
            TempData["ErrorMessage"] = "Cannot edit an inactive processing activity.";
            return RedirectToPage("Details", new { id });
        }

        Input = new InputModel
        {
            Id = result.Activity.Id,
            Name = result.Activity.Name,
            Purpose = result.Activity.Purpose,
            LegalBasis = result.Activity.LegalBasis,
            DataCategories = result.Activity.DataCategories,
            DataSubjectCategories = result.Activity.DataSubjectCategories,
            Recipients = result.Activity.Recipients,
            ThirdCountryTransfers = result.Activity.ThirdCountryTransfers,
            RetentionPeriod = result.Activity.RetentionPeriod,
            TechnicalMeasures = result.Activity.TechnicalMeasures,
            OrganizationalMeasures = result.Activity.OrganizationalMeasures,
            ProcessorName = result.Activity.ProcessorName,
            ProcessorContact = result.Activity.ProcessorContact
        };

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the data processing activity.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new UpdateDataProcessingActivityCommand
        {
            Id = Input.Id,
            Name = Input.Name,
            Purpose = Input.Purpose,
            LegalBasis = Input.LegalBasis,
            DataCategories = Input.DataCategories,
            DataSubjectCategories = Input.DataSubjectCategories,
            Recipients = Input.Recipients,
            ThirdCountryTransfers = Input.ThirdCountryTransfers,
            RetentionPeriod = Input.RetentionPeriod,
            TechnicalMeasures = Input.TechnicalMeasures,
            OrganizationalMeasures = Input.OrganizationalMeasures,
            ProcessorName = Input.ProcessorName,
            ProcessorContact = Input.ProcessorContact,
            UpdatedByUserId = userId,
            UpdatedByUserEmail = userEmail,
            Reason = Input.Reason
        };

        var result = await _registryService.UpdateActivityAsync(command);

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

            return Page();
        }

        _logger.LogInformation(
            "Updated data processing activity {ActivityId} by user {UserId}",
            Input.Id,
            userId);

        TempData["SuccessMessage"] = $"Processing activity '{Input.Name}' updated successfully.";
        return RedirectToPage("Details", new { id = Input.Id });
    }
}
