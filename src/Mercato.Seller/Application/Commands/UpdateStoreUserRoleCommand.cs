using Mercato.Seller.Domain.Entities;

namespace Mercato.Seller.Application.Commands;

/// <summary>
/// Command for updating an internal user's role within a store.
/// </summary>
public class UpdateStoreUserRoleCommand
{
    /// <summary>
    /// Gets or sets the store user ID to update.
    /// </summary>
    public Guid StoreUserId { get; set; }

    /// <summary>
    /// Gets or sets the store ID for authorization validation.
    /// </summary>
    public Guid StoreId { get; set; }

    /// <summary>
    /// Gets or sets the new role to assign.
    /// </summary>
    public StoreRole NewRole { get; set; }

    /// <summary>
    /// Gets or sets the user ID of the person making the change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;
}
