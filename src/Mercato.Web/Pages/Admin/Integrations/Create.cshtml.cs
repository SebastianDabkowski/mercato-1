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
/// Page model for creating a new integration.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly IIntegrationManagementService _integrationService;
    private readonly ILogger<CreateModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateModel"/> class.
    /// </summary>
    /// <param name="integrationService">The integration management service.</param>
    /// <param name="logger">The logger.</param>
    public CreateModel(
        IIntegrationManagementService integrationService,
        ILogger<CreateModel> logger)
    {
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the input model for creating an integration.
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

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
    /// Input model for creating an integration.
    /// </summary>
    public class InputModel
    {
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
        /// Gets or sets the API key.
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

        /// <summary>
        /// Gets or sets whether this integration is enabled.
        /// </summary>
        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// Handles GET requests to the create page.
    /// </summary>
    public void OnGet()
    {
        // Initialize with default values
    }

    /// <summary>
    /// Handles POST requests to create a new integration.
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
        var userEmail = User.FindFirstValue(ClaimTypes.Email);

        var command = new CreateIntegrationCommand
        {
            Name = Input.Name,
            IntegrationType = Input.IntegrationType,
            Environment = Input.Environment,
            ApiEndpoint = Input.ApiEndpoint,
            ApiKey = Input.ApiKey,
            MerchantId = Input.MerchantId,
            CallbackUrl = Input.CallbackUrl,
            IsEnabled = Input.IsEnabled,
            CreatedByUserId = userId,
            CreatedByUserEmail = userEmail
        };

        var result = await _integrationService.CreateIntegrationAsync(command);

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
            "Integration '{Name}' of type {Type} created by user {UserId}",
            command.Name,
            command.IntegrationType,
            userId);

        TempData["SuccessMessage"] = $"Integration '{command.Name}' was created successfully.";
        return RedirectToPage("Index");
    }
}
