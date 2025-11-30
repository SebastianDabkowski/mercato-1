using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Mercato.Web.Pages.Admin.Integrations;

/// <summary>
/// Page model for editing an existing integration.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly IIntegrationManagementService _integrationService;
    private readonly ILogger<EditModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditModel"/> class.
    /// </summary>
    /// <param name="integrationService">The integration management service.</param>
    /// <param name="logger">The logger.</param>
    public EditModel(
        IIntegrationManagementService integrationService,
        ILogger<EditModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for editing an integration.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// Gets or sets the current masked API key for display.
    /// </summary>
    public string? CurrentApiKeyMasked { get; set; }

    /// <summary>
    /// Gets or sets the current status.
    /// </summary>
    public IntegrationStatus CurrentStatus { get; set; }

    /// <summary>
    /// Gets or sets whether the integration is enabled.
    /// </summary>
    public bool CurrentIsEnabled { get; set; }

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
    /// Gets the list of integration types for the dropdown.
    /// </summary>
    public List<SelectListItem> IntegrationTypes { get; } =
    [
        new SelectListItem("Payment", IntegrationType.Payment.ToString()),
        new SelectListItem("Shipping", IntegrationType.Shipping.ToString()),
        new SelectListItem("ERP", IntegrationType.ERP.ToString()),
        new SelectListItem("Other", IntegrationType.Other.ToString())
    ];

    /// <summary>
    /// Gets the list of environments for the dropdown.
    /// </summary>
    public List<SelectListItem> Environments { get; } =
    [
        new SelectListItem("Sandbox", IntegrationEnvironment.Sandbox.ToString()),
        new SelectListItem("Production", IntegrationEnvironment.Production.ToString())
    ];

    /// <summary>
    /// Input model for editing an integration.
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// Gets or sets the integration ID.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the integration name.
        /// </summary>
        [Required(ErrorMessage = "Integration name is required.")]
        [StringLength(200, ErrorMessage = "Integration name must not exceed 200 characters.")]
        [Display(Name = "Integration Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the integration type.
        /// </summary>
        [Required(ErrorMessage = "Integration type is required.")]
        [Display(Name = "Type")]
        public IntegrationType IntegrationType { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        [Required(ErrorMessage = "Environment is required.")]
        [Display(Name = "Environment")]
        public IntegrationEnvironment Environment { get; set; }

        /// <summary>
        /// Gets or sets the API endpoint.
        /// </summary>
        [StringLength(500, ErrorMessage = "API endpoint must not exceed 500 characters.")]
        [Display(Name = "API Endpoint")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string? ApiEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the new API key. If empty, the existing key is preserved.
        /// </summary>
        [StringLength(500, ErrorMessage = "API key must not exceed 500 characters.")]
        [Display(Name = "API Key")]
        public string? ApiKey { get; set; }

        /// <summary>
        /// Gets or sets the merchant ID.
        /// </summary>
        [StringLength(100, ErrorMessage = "Merchant ID must not exceed 100 characters.")]
        [Display(Name = "Merchant ID")]
        public string? MerchantId { get; set; }

        /// <summary>
        /// Gets or sets the callback URL.
        /// </summary>
        [StringLength(500, ErrorMessage = "Callback URL must not exceed 500 characters.")]
        [Display(Name = "Callback URL")]
        [Url(ErrorMessage = "Please enter a valid URL.")]
        public string? CallbackUrl { get; set; }
    }

    /// <summary>
    /// Handles GET requests to the edit page.
    /// </summary>
    /// <param name="id">The integration ID to edit.</param>
    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var result = await _integrationService.GetIntegrationByIdAsync(id);

        if (!result.Succeeded)
        {
            if (result.IsNotAuthorized)
            {
                return Forbid();
            }

            TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            return RedirectToPage("Index");
        }

        if (result.Integration == null)
        {
            return NotFound();
        }

        var integration = result.Integration;
        Input = new InputModel
        {
            Id = integration.Id,
            Name = integration.Name,
            IntegrationType = integration.IntegrationType,
            Environment = integration.Environment,
            ApiEndpoint = integration.ApiEndpoint,
            MerchantId = integration.MerchantId,
            CallbackUrl = integration.CallbackUrl
        };

        CurrentApiKeyMasked = integration.ApiKeyMasked;
        CurrentStatus = integration.Status;
        CurrentIsEnabled = integration.IsEnabled;
        CreatedAt = integration.CreatedAt;
        CreatedByUserId = integration.CreatedByUserId;
        UpdatedAt = integration.UpdatedAt;
        UpdatedByUserId = integration.UpdatedByUserId;

        return Page();
    }

    /// <summary>
    /// Handles POST requests to update the integration.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new UpdateIntegrationCommand
        {
            Id = Input.Id,
            Name = Input.Name,
            IntegrationType = Input.IntegrationType,
            Environment = Input.Environment,
            ApiEndpoint = Input.ApiEndpoint,
            ApiKey = Input.ApiKey,
            MerchantId = Input.MerchantId,
            CallbackUrl = Input.CallbackUrl,
            UpdatedByUserId = userId,
            UpdatedByUserEmail = userEmail
        };

        var result = await _integrationService.UpdateIntegrationAsync(command);

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
            "Integration '{Name}' (ID: {Id}) updated by user {UserId}",
            result.Integration?.Name,
            Input.Id,
            userId);

        TempData["SuccessMessage"] = $"Integration '{result.Integration?.Name}' was updated successfully.";
        return RedirectToPage("Index");
    }
}
