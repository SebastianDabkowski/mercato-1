using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.DataProcessing;

/// <summary>
/// Page model for creating a new data processing activity.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IDataProcessingRegistryService _registryService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="registryService">The data processing registry service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IDataProcessingRegistryService registryService,
        ILogger<CreateModel> logger)
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
    /// Input model for creating a new data processing activity.
    /// </summary>
    public class InputModel
    {
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
    }

    /// <summary>
    /// Handles GET requests.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Handles POST requests to create a new data processing activity.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new CreateDataProcessingActivityCommand
        {
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
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail
        };

        var result = await _registryService.CreateActivityAsync(command);

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
            "Created data processing activity {ActivityId} '{Name}' by user {UserId}",
            result.Activity?.Id,
            Input.Name,
            userId);

        TempData["SuccessMessage"] = $"Processing activity '{Input.Name}' created successfully.";
        return RedirectToPage("Index");
    }
}
