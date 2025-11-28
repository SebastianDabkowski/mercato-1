namespace Mercato.Cart.Application.Commands;

/// <summary>
/// Command for merging a guest cart with a user's cart after login.
/// </summary>
public class MergeGuestCartCommand
{
    /// <summary>
    /// Gets or sets the authenticated buyer ID.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the guest cart ID from the cookie.
    /// </summary>
    public string GuestCartId { get; set; } = string.Empty;
}
