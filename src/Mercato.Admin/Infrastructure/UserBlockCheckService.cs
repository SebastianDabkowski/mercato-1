using Mercato.Admin.Domain.Interfaces;
using Mercato.Identity.Application.Services;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Implementation of user block check service for the Identity module.
/// </summary>
public class UserBlockCheckService : IUserBlockCheckService
{
    private readonly IUserBlockRepository _userBlockRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserBlockCheckService"/> class.
    /// </summary>
    /// <param name="userBlockRepository">The user block repository.</param>
    public UserBlockCheckService(IUserBlockRepository userBlockRepository)
    {
        _userBlockRepository = userBlockRepository ?? throw new ArgumentNullException(nameof(userBlockRepository));
    }

    /// <inheritdoc />
    public async Task<bool> IsUserBlockedAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var activeBlock = await _userBlockRepository.GetActiveBlockAsync(userId);
        return activeBlock != null;
    }
}
