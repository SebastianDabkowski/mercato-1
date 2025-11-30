using Mercato.Admin.Application.Commands;
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
    private readonly IUserBlockRepository _userBlockRepository;
    private readonly ILogger<UserAccountManagementService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserAccountManagementService"/> class.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="authEventRepository">The authentication event repository.</param>
    /// <param name="userBlockRepository">The user block repository.</param>
    /// <param name="logger">The logger.</param>
    public UserAccountManagementService(
        UserManager<IdentityUser> userManager,
        IAuthenticationEventRepository authEventRepository,
        IUserBlockRepository userBlockRepository,
        ILogger<UserAccountManagementService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _authEventRepository = authEventRepository ?? throw new ArgumentNullException(nameof(authEventRepository));
        _userBlockRepository = userBlockRepository ?? throw new ArgumentNullException(nameof(userBlockRepository));
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

        // Check for active block
        var activeBlock = await _userBlockRepository.GetActiveBlockAsync(userId);

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
            AdminNotes = [], // Notes could be extended in a future implementation
            IsBlocked = activeBlock != null,
            BlockedByAdminEmail = activeBlock?.BlockedByAdminEmail,
            BlockedAt = activeBlock?.BlockedAt,
            BlockReason = activeBlock?.Reason.ToString(),
            BlockReasonDetails = activeBlock?.ReasonDetails
        };

        _logger.LogInformation("Retrieved detailed information for user {UserId} ({Email}).", userId, user.Email);

        return userDetail;
    }

    /// <inheritdoc/>
    public async Task<BlockUserResult> BlockUserAsync(
        BlockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateBlockUserCommand(command);
        if (validationErrors.Count > 0)
        {
            return BlockUserResult.Failure(validationErrors);
        }

        // Verify user exists
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null)
        {
            return BlockUserResult.Failure("User not found.");
        }

        // Check if user is already blocked
        var existingBlock = await _userBlockRepository.GetActiveBlockAsync(command.UserId);
        if (existingBlock != null)
        {
            return BlockUserResult.Failure("User is already blocked.");
        }

        // Set lockout on user first to prevent login
        // This is done before creating the block record to ensure the user is locked out
        // even if the block record creation fails (fail-safe approach)
        var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        if (!lockoutResult.Succeeded)
        {
            var errorMessages = lockoutResult.Errors.Select(e => e.Description).ToList();
            _logger.LogError(
                "Failed to set lockout for user {UserId}: {Errors}",
                command.UserId,
                string.Join(", ", errorMessages));
            return BlockUserResult.Failure("Failed to block user account. Please try again.");
        }

        // Create block record
        var blockInfo = new UserBlockInfo
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            BlockedByAdminId = command.AdminUserId,
            BlockedByAdminEmail = command.AdminEmail,
            Reason = command.Reason,
            ReasonDetails = command.ReasonDetails,
            BlockedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        try
        {
            await _userBlockRepository.AddAsync(blockInfo);
        }
        catch (Exception ex)
        {
            // If block record creation fails, attempt to remove the lockout to restore consistency
            _logger.LogError(ex, "Failed to create block record for user {UserId}. Attempting to remove lockout.", command.UserId);
            await _userManager.SetLockoutEndDateAsync(user, null);
            return BlockUserResult.Failure("Failed to create block record. Please try again.");
        }

        _logger.LogInformation(
            "Admin {AdminId} blocked user {UserId} for reason {Reason}.",
            command.AdminUserId,
            command.UserId,
            command.Reason);

        return BlockUserResult.Success();
    }

    /// <inheritdoc/>
    public async Task<UnblockUserResult> UnblockUserAsync(
        UnblockUserCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateUnblockUserCommand(command);
        if (validationErrors.Count > 0)
        {
            return UnblockUserResult.Failure(validationErrors);
        }

        // Verify user exists
        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null)
        {
            return UnblockUserResult.Failure("User not found.");
        }

        // Check if user is blocked
        var existingBlock = await _userBlockRepository.GetActiveBlockAsync(command.UserId);
        if (existingBlock == null)
        {
            return UnblockUserResult.Failure("User is not currently blocked.");
        }

        // Remove lockout from user first
        var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        if (!lockoutResult.Succeeded)
        {
            var errorMessages = lockoutResult.Errors.Select(e => e.Description).ToList();
            _logger.LogError(
                "Failed to remove lockout for user {UserId}: {Errors}",
                command.UserId,
                string.Join(", ", errorMessages));
            return UnblockUserResult.Failure("Failed to unblock user account. Please try again.");
        }

        // Reset access failed count
        await _userManager.ResetAccessFailedCountAsync(user);

        // Update block record
        existingBlock.IsActive = false;
        existingBlock.UnblockedAt = DateTimeOffset.UtcNow;
        existingBlock.UnblockedByAdminId = command.AdminUserId;

        try
        {
            await _userBlockRepository.UpdateAsync(existingBlock);
        }
        catch (Exception ex)
        {
            // If block record update fails, log but don't fail the operation
            // The user is already unblocked in ASP.NET Identity which is the primary concern
            _logger.LogError(ex, "Failed to update block record for user {UserId}. User has been unblocked but block record may be inconsistent.", command.UserId);
        }

        _logger.LogInformation(
            "Admin {AdminId} unblocked user {UserId}.",
            command.AdminUserId,
            command.UserId);

        return UnblockUserResult.Success();
    }

    /// <inheritdoc/>
    public async Task<UserBlockInfo?> GetActiveBlockAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await _userBlockRepository.GetActiveBlockAsync(userId);
    }

    /// <summary>
    /// Validates the block user command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateBlockUserCommand(BlockUserCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminEmail))
        {
            errors.Add("Admin email is required.");
        }

        return errors;
    }

    /// <summary>
    /// Validates the unblock user command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateUnblockUserCommand(UnblockUserCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        return errors;
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
