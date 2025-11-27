using System.ComponentModel.DataAnnotations;
using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for saving verification data in the onboarding wizard.
/// </summary>
public class SaveVerificationDataCommand
{
    /// <summary>
    /// Gets or sets the seller's user ID.
    /// </summary>
    public string SellerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the seller type (Individual or Company).
    /// </summary>
    public SellerType SellerType { get; set; }

    // Company-specific fields

    /// <summary>
    /// Gets or sets the business name (required for Company sellers).
    /// </summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Business name must be between 2 and 200 characters.")]
    public string? BusinessName { get; set; }

    /// <summary>
    /// Gets or sets the address. Nullable but validated as required for both seller types via service-level validation.
    /// </summary>
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 500 characters.")]
    public string? BusinessAddress { get; set; }

    /// <summary>
    /// Gets or sets the tax identification number. Nullable but validated as required for both seller types via service-level validation.
    /// </summary>
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tax ID must be between 5 and 50 characters.")]
    public string? TaxId { get; set; }

    /// <summary>
    /// Gets or sets the business registration number (required for Company sellers).
    /// </summary>
    [StringLength(50, ErrorMessage = "Business registration number must be at most 50 characters.")]
    public string? BusinessRegistrationNumber { get; set; }

    /// <summary>
    /// Gets or sets the contact person name (required for Company sellers).
    /// </summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Contact person name must be between 2 and 200 characters.")]
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Gets or sets the contact person email (required for Company sellers).
    /// </summary>
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(254, ErrorMessage = "Contact person email must be at most 254 characters.")]
    public string? ContactPersonEmail { get; set; }

    /// <summary>
    /// Gets or sets the contact person phone (required for Company sellers).
    /// </summary>
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [StringLength(20, ErrorMessage = "Contact person phone must be at most 20 characters.")]
    public string? ContactPersonPhone { get; set; }

    // Individual-specific fields

    /// <summary>
    /// Gets or sets the full name (required for Individual sellers).
    /// </summary>
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
    public string? FullName { get; set; }

    /// <summary>
    /// Gets or sets the personal ID number (required for Individual sellers).
    /// </summary>
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Personal ID number must be between 5 and 50 characters.")]
    public string? PersonalIdNumber { get; set; }
}
