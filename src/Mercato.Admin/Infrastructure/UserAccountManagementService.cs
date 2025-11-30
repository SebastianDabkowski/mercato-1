using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for managing and querying user accounts.
/// </summary>
public class UserAccountManagementService : IUserAccountManagementService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IAuthenticationEventRepository _authEventRepository;
    private readonly ILogger<UserAccountManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountManagementService"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="authEventRepository">The authentication event repository.</param>
    /// <param name="logger">The logger.</param>
    public UserAccountManagementService(
        UserManager<IdentityUser> userManager,
        IAuthenticationEventRepository authEventRepository,
        ILogger<UserAccountManagementService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authEventRepository = authEventRepository ?? throw new ArgumentNullException(nameof(authEventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<PagedResult<UserAccountInfo>> GetUsersAsync(
        UserAccountFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Note: Loading users into memory is required because UserManager.GetRolesAsync
        // doesn't support IQueryable. For large user bases, consider implementing
        // a repository with direct database queries joining user and role tables.
        var allUsers = _userManager.Users.ToList();
        var filteredUsers = new List<(IdentityUser User, IList<string> Roles)>();

        foreach (var user in allUsers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var roles = await _userManager.GetRolesAsync(user);
            var userStatus = DetermineUserStatus(user);

            // Apply role filter
            if (!string.IsNullOrEmpty(query.Role) && !roles.Contains(query.Role, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Apply status filter
            if (query.Status.HasValue && userStatus != query.Status.Value)
            {
                continue;
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.Trim();
                var matchesSearch =
                    (user.Email?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (user.UserName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    user.Id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase);

                if (!matchesSearch)
                {
                    continue;
                }
            }

            filteredUsers.Add((user, roles));
        }

        var totalCount = filteredUsers.Count;

        // Apply pagination
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, Math.Min(100, query.PageSize));
        var skip = (page - 1) * pageSize;

        var pagedUsers = filteredUsers
            .OrderBy(x => x.User.Email)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new UserAccountInfo
            {
                UserId = x.User.Id,
                Email = x.User.Email ?? string.Empty,
                Roles = x.Roles.ToList().AsReadOnly(),
                Status = DetermineUserStatus(x.User),
                CreatedAt = DateTimeOffset.MinValue // ASP.NET Identity doesn't track creation date by default
            })
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} users (page {Page} of {TotalPages}) with filters: Role={Role}, Status={Status}, Search={Search}",
            pagedUsers.Count,
            page,
            (int)Math.Ceiling((double)totalCount / pageSize),
            query.Role ?? "All",
            query.Status?.ToString() ?? "All",
            query.SearchTerm ?? "None");

        return new PagedResult<UserAccountInfo>
        {
            Items = pagedUsers.AsReadOnly(),
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc/>
    public async Task<UserDetailInfo?> GetUserDetailAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} not found.", userId);
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Get recent authentication events for this user
        var endDate = DateTimeOffset.UtcNow;
        var startDate = endDate.AddDays(-30);
        var authEvents = await _authEventRepository.GetFilteredAsync(
            startDate,
            endDate,
            eventType: null,
            userRole: null,
            ipAddressHash: null,
            isSuccessful: null,
            maxResults: 10,
            cancellationToken);

        // Filter events for this user by email
        var userEvents = authEvents
            .Where(e => e.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase) ||
                        e.UserId == userId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(10)
            .Select(e => new LoginActivityInfo
            {
                Timestamp = e.OccurredAt,
                IsSuccessful = e.IsSuccessful,
                EventType = e.EventType.ToString()
            })
            .ToList();

        // Find the most recent successful login
        var lastLogin = userEvents
            .Where(e => e.IsSuccessful && e.EventType == "Login")
            .OrderByDescending(e => e.Timestamp)
            .FirstOrDefault();

        var userDetail = new UserDetailInfo
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList().AsReadOnly(),
            Status = DetermineUserStatus(user),
            CreatedAt = DateTimeOffset.MinValue, // ASP.NET Identity doesn't track creation date by default
            LastLoginAt = lastLogin?.Timestamp,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            AccessFailedCount = user.AccessFailedCount,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            RecentLoginActivity = userEvents.AsReadOnly(),
            AdminNotes = [] // Notes could be extended in a future implementation
        };

        _logger.LogInformation("Retrieved detailed information for user {UserId} ({Email}).", userId, user.Email);

        return userDetail;
    }

    /// <summary>
    /// Determines the account status based on user properties.
    /// </summary>
    private static UserAccountStatus DetermineUserStatus(IdentityUser user)
    {
        // Check if user is currently locked out
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            return UserAccountStatus.Blocked;
        }

        // Check if email is not confirmed (pending verification)
        if (!user.EmailConfirmed)
        {
            return UserAccountStatus.PendingVerification;
        }

        return UserAccountStatus.Active;
    }
}
