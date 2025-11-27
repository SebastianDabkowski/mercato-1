namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for deactivating an internal user from a store.
/// </summary>
public class DeactivateStoreUserCommand
{
    /// <summary>
    /// Gets or sets the store user ID to deactivate.
    /// </summary>
    public Guid StoreUserId { get; set; }

    /// <summary>
    /// Gets or sets the store ID for authorization validation.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person performing the deactivation.
    /// </summary>
    public string DeactivatedBy { get; set; } = string.Empty;
}
