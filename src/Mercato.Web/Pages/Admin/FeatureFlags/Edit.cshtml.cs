using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mercato.Web.Pages.Admin.FeatureFlags;

/// <summary>
/// Page model for editing an existing feature flag.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IFeatureFlagManagementService _featureFlagService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="featureFlagService">The feature flag management service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        IFeatureFlagManagementService featureFlagService,
        ILogger<EditModel> logger)
    {
        _featureFlagService = featureFlagService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for editing a flag.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the created date for display.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the created by user ID for display.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the updated date for display.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the updated by user ID for display.
    /// </summary>
    public string? UpdatedByUserId { get; set; }

    /// <summary>
    /// Input model for editing a feature flag.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the flag ID.
        /// </summary>
        public Guid Id { get; set; }

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
        public FeatureFlagEnvironment Environment { get; set; }

        /// <summary>
        /// Gets or sets the target type.
        /// </summary>
        [Display(Name = "Target Type")]
        public FeatureFlagTargetType TargetType { get; set; }

        /// <summary>
        /// Gets or sets the target value.
        /// </summary>
        [Display(Name = "Target Value")]
        public string? TargetValue { get; set; }

        /// <summary>
        /// Gets or sets whether this flag is enabled.
        /// </summary>
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; }
    }

    /// <summary>
    /// Handles GET requests to the edit page.
    /// </summary>
    /// <param name="id">The flag ID to edit.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _featureFlagService.GetFlagByIdAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        if (result.Flag == null)
        {
            return NotFound();
        }

        var flag = result.Flag;
        Input = new InputModel
        {
            Id = flag.Id,
            Key = flag.Key,
            Name = flag.Name,
            Description = flag.Description,
            Environment = flag.Environment,
            TargetType = flag.TargetType,
            TargetValue = flag.TargetValue,
            IsEnabled = flag.IsEnabled
        };

        CreatedAt = flag.CreatedAt;
        CreatedByUserId = flag.CreatedByUserId;
        UpdatedAt = flag.UpdatedAt;
        UpdatedByUserId = flag.UpdatedByUserId;

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the feature flag.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new UpdateFeatureFlagCommand
        {
            Id = Input.Id,
            Key = Input.Key.ToLowerInvariant(),
            Name = Input.Name,
            Description = Input.Description,
            IsEnabled = Input.IsEnabled,
            Environment = Input.Environment,
            TargetType = Input.TargetType,
            TargetValue = Input.TargetValue,
            UpdatedByUserId = userId,
            UpdatedByUserEmail = userEmail
        };

        var result = await _featureFlagService.UpdateFlagAsync(command);

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
            "Feature flag '{Key}' (ID: {Id}) updated by user {UserId}",
            command.Key,
            command.Id,
            userId);

        TempData["SuccessMessage"] = $"Feature flag '{command.Key}' was updated successfully.";
        return RedirectToPage("Index");
    }

    /// <summary>
    /// Handles POST requests to delete the feature flag.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var result = await _featureFlagService.DeleteFlagAsync(Input.Id, userId, userEmail);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        _logger.LogInformation(
            "Feature flag (ID: {Id}) deleted by user {UserId}",
            Input.Id,
            userId);

        TempData["SuccessMessage"] = "Feature flag was deleted successfully.";
        return RedirectToPage("Index");
    }
}
