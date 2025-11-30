using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for creating a new feature flag.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IFeatureFlagManagementService _featureFlagService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="featureFlagService">The feature flag management service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IFeatureFlagManagementService featureFlagService,
        ILogger<CreateModel> logger)
    {
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for creating a flag.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Input model for creating a feature flag.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the unique key of the flag.
        /// </summary>
        [Required(ErrorMessage = "Flag key is required.")]
        [StringLength(100, ErrorMessage = "Flag key must not exceed 100 characters.")]
        [Display(Name = "Flag Key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the flag.
        /// </summary>
        [Required(ErrorMessage = "Flag name is required.")]
        [StringLength(200, ErrorMessage = "Flag name must not exceed 200 characters.")]
        [Display(Name = "Flag Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        [Required(ErrorMessage = "Environment is required.")]
        [Display(Name = "Environment")]
        public FeatureFlagEnvironment Environment { get; set; } = FeatureFlagEnvironment.Development;

        /// <summary>
        /// Gets or sets the target type.
        /// </summary>
        [Display(Name = "Target Type")]
        public FeatureFlagTargetType TargetType { get; set; } = FeatureFlagTargetType.None;

        /// <summary>
        /// Gets or sets the target value.
        /// </summary>
        [Display(Name = "Target Value")]
        public string? TargetValue { get; set; }

        /// <summary>
        /// Gets or sets whether this flag is enabled.
        /// </summary>
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = false;
    }

    /// <summary>
    /// Handles GET requests to the create page.
    /// </summary>
    public void OnGet()
    {
        // Initialize with default values
    }

    /// <summary>
    /// Handles POST requests to create a new feature flag.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new CreateFeatureFlagCommand
        {
            Key = Input.Key.ToLowerInvariant(),
            Name = Input.Name,
            Description = Input.Description,
            IsEnabled = Input.IsEnabled,
            Environment = Input.Environment,
            TargetType = Input.TargetType,
            TargetValue = Input.TargetValue,
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail
        };

        var result = await _featureFlagService.CreateFlagAsync(command);

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
            "Feature flag '{Key}' created by user {UserId} for environment {Environment}",
            command.Key,
            userId,
            command.Environment);

        TempData["SuccessMessage"] = $"Feature flag '{command.Key}' was created successfully.";
        return RedirectToPage("Index");
    }
}
