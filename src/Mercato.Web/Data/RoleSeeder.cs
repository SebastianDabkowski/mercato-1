using Microsoft.AspNetCore.Identity;

namespace Mercato.Web.Data;

/// <summary>
/// Seeds the default roles for the Mercato marketplace.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Role name for buyers.
    /// </summary>
    public const string BuyerRole = "Buyer";

    /// <summary>
    /// Role name for sellers.
    /// </summary>
    public const string SellerRole = "Seller";

    /// <summary>
    /// Role name for administrators.
    /// </summary>
    public const string AdminRole = "Admin";

    /// <summary>
    /// Seeds the default roles if they do not exist.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { BuyerRole, SellerRole, AdminRole };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
