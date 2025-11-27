using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Mercato.Seller.Application.Commands;
using Mercato.Seller.Application.Services;
using Mercato.Seller.Domain.Entities;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Seller.Infrastructure;

/// <summary>
/// Implementation of store user management service.
/// </summary>
public class StoreUserService : IStoreUserService
{
    /// <summary>
    /// Duration in days for which an invitation token is valid.
    /// </summary>
    private const int InvitationExpirationDays = 7;

    private readonly IStoreUserRepository _repository;
    private readonly IStoreRepository _storeRepository;
    private readonly ILogger<StoreUserService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreUserService"/> class.
    /// </summary>
    /// <param name="repository">The store user repository.</param>
    /// <param name="storeRepository">The store repository.</param>
    /// <param name="logger">The logger.</param>
    public StoreUserService(
        IStoreUserRepository repository,
        IStoreRepository storeRepository,
        ILogger<StoreUserService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreUser>> GetStoreUsersAsync(Guid storeId)
    {
        return await _repository.GetByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<StoreUser?> GetStoreUserByIdAsync(Guid storeUserId)
    {
        return await _repository.GetByIdAsync(storeUserId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoreUser>> GetUserStoreAccessAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return await _repository.GetByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<bool> HasStoreAccessAsync(Guid storeId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var storeUser = await _repository.GetByStoreAndUserIdAsync(storeId, userId);
        return storeUser != null && storeUser.Status == StoreUserStatus.Active;
    }

    /// <inheritdoc />
    public async Task<StoreRole?> GetUserRoleAsync(Guid storeId, string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var storeUser = await _repository.GetByStoreAndUserIdAsync(storeId, userId);
        
        if (storeUser == null || storeUser.Status != StoreUserStatus.Active)
        {
            return null;
        }

        return storeUser.Role;
    }

    /// <inheritdoc />
    public async Task<InviteStoreUserResult> InviteUserAsync(InviteStoreUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var validationErrors = ValidateInviteCommand(command);
        if (validationErrors.Count > 0)
        {
            return InviteStoreUserResult.Failure(validationErrors);
        }

        // Verify the store exists
        var store = await _storeRepository.GetByIdAsync(command.StoreId);
        if (store == null)
        {
            return InviteStoreUserResult.Failure("Store not found.");
        }

        // Check if user with this email already exists for the store
        var normalizedEmail = command.Email.ToLowerInvariant();
        if (await _repository.EmailExistsForStoreAsync(command.StoreId, normalizedEmail))
        {
            return InviteStoreUserResult.Failure("A user with this email already exists for this store.");
        }

        // Generate invitation token
        var invitationToken = GenerateInvitationToken();

        var storeUser = new StoreUser
        {
            Id = Guid.NewGuid(),
            StoreId = command.StoreId,
            UserId = null, // Will be set when invitation is accepted
            Email = normalizedEmail,
            Role = command.Role,
            Status = StoreUserStatus.Pending,
            InvitationToken = invitationToken,
            InvitationExpiresAt = DateTimeOffset.UtcNow.AddDays(InvitationExpirationDays),
            InvitedAt = DateTimeOffset.UtcNow,
            InvitedBy = command.InvitedBy
        };

        await _repository.CreateAsync(storeUser);
        _logger.LogInformation(
            "Invited user {Email} to store {StoreId} with role {Role}",
            normalizedEmail,
            command.StoreId,
            command.Role);

        return InviteStoreUserResult.Success(storeUser.Id, invitationToken);
    }

    /// <inheritdoc />
    public async Task<AcceptStoreUserInvitationResult> AcceptInvitationAsync(AcceptStoreUserInvitationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return AcceptStoreUserInvitationResult.Failure("Invitation token is required.");
        }

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return AcceptStoreUserInvitationResult.Failure("User ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return AcceptStoreUserInvitationResult.Failure("Email is required.");
        }

        var storeUser = await _repository.GetByInvitationTokenAsync(command.Token);
        
        if (storeUser == null)
        {
            return AcceptStoreUserInvitationResult.Failure("Invalid invitation token.");
        }

        if (storeUser.Status != StoreUserStatus.Pending)
        {
            return AcceptStoreUserInvitationResult.Failure("This invitation has already been used.");
        }

        if (storeUser.InvitationExpiresAt.HasValue && storeUser.InvitationExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return AcceptStoreUserInvitationResult.Failure("This invitation has expired.");
        }

        // Verify the email matches
        if (!string.Equals(storeUser.Email, command.Email.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase))
        {
            return AcceptStoreUserInvitationResult.Failure("Email does not match the invitation.");
        }

        // Accept the invitation
        storeUser.UserId = command.UserId;
        storeUser.Status = StoreUserStatus.Active;
        storeUser.AcceptedAt = DateTimeOffset.UtcNow;
        storeUser.InvitationToken = null; // Clear the token
        storeUser.InvitationExpiresAt = null;

        await _repository.UpdateAsync(storeUser);
        _logger.LogInformation(
            "User {UserId} accepted invitation to store {StoreId}",
            command.UserId,
            storeUser.StoreId);

        return AcceptStoreUserInvitationResult.Success(storeUser.StoreId);
    }

    /// <inheritdoc />
    public async Task<UpdateStoreUserRoleResult> UpdateUserRoleAsync(UpdateStoreUserRoleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.ChangedBy))
        {
            return UpdateStoreUserRoleResult.Failure("Changed by user ID is required.");
        }

        var storeUser = await _repository.GetByIdAsync(command.StoreUserId);
        
        if (storeUser == null)
        {
            return UpdateStoreUserRoleResult.Failure("Store user not found.");
        }

        if (storeUser.StoreId != command.StoreId)
        {
            return UpdateStoreUserRoleResult.Failure("Store user does not belong to the specified store.");
        }

        if (storeUser.Status == StoreUserStatus.Deactivated)
        {
            return UpdateStoreUserRoleResult.Failure("Cannot update role of a deactivated user.");
        }

        // Prevent changing the role of a store owner if they are the only one
        if (storeUser.Role == StoreRole.StoreOwner && command.NewRole != StoreRole.StoreOwner)
        {
            var storeUsers = await _repository.GetByStoreIdAsync(command.StoreId);
            var activeOwners = storeUsers.Count(u => 
                u.Role == StoreRole.StoreOwner && 
                u.Status == StoreUserStatus.Active &&
                u.Id != storeUser.Id);
            
            if (activeOwners == 0)
            {
                return UpdateStoreUserRoleResult.Failure("Cannot change the role of the only store owner.");
            }
        }

        var previousRole = storeUser.Role;
        storeUser.Role = command.NewRole;
        storeUser.RoleChangedAt = DateTimeOffset.UtcNow;
        storeUser.RoleChangedBy = command.ChangedBy;

        await _repository.UpdateAsync(storeUser);
        _logger.LogInformation(
            "Updated role for user {Email} in store {StoreId} from {PreviousRole} to {NewRole}",
            storeUser.Email,
            storeUser.StoreId,
            previousRole,
            command.NewRole);

        return UpdateStoreUserRoleResult.Success();
    }

    /// <inheritdoc />
    public async Task<DeactivateStoreUserResult> DeactivateUserAsync(DeactivateStoreUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.DeactivatedBy))
        {
            return DeactivateStoreUserResult.Failure("Deactivated by user ID is required.");
        }

        var storeUser = await _repository.GetByIdAsync(command.StoreUserId);
        
        if (storeUser == null)
        {
            return DeactivateStoreUserResult.Failure("Store user not found.");
        }

        if (storeUser.StoreId != command.StoreId)
        {
            return DeactivateStoreUserResult.Failure("Store user does not belong to the specified store.");
        }

        if (storeUser.Status == StoreUserStatus.Deactivated)
        {
            return DeactivateStoreUserResult.Failure("User is already deactivated.");
        }

        // Prevent deactivating the only store owner
        if (storeUser.Role == StoreRole.StoreOwner)
        {
            var storeUsers = await _repository.GetByStoreIdAsync(command.StoreId);
            var activeOwners = storeUsers.Count(u => 
                u.Role == StoreRole.StoreOwner && 
                u.Status == StoreUserStatus.Active &&
                u.Id != storeUser.Id);
            
            if (activeOwners == 0)
            {
                return DeactivateStoreUserResult.Failure("Cannot deactivate the only store owner.");
            }
        }

        storeUser.Status = StoreUserStatus.Deactivated;
        storeUser.DeactivatedAt = DateTimeOffset.UtcNow;
        storeUser.DeactivatedBy = command.DeactivatedBy;

        await _repository.UpdateAsync(storeUser);
        _logger.LogInformation(
            "Deactivated user {Email} from store {StoreId}",
            storeUser.Email,
            storeUser.StoreId);

        return DeactivateStoreUserResult.Success();
    }

    /// <inheritdoc />
    public async Task<StoreUser?> ValidateInvitationTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var storeUser = await _repository.GetByInvitationTokenAsync(token);
        
        if (storeUser == null)
        {
            return null;
        }

        if (storeUser.Status != StoreUserStatus.Pending)
        {
            return null;
        }

        if (storeUser.InvitationExpiresAt.HasValue && storeUser.InvitationExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return null;
        }

        return storeUser;
    }

    /// <summary>
    /// Validates the invite store user command.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>A list of validation error messages.</returns>
    private static List<string> ValidateInviteCommand(InviteStoreUserCommand command)
    {
        var errors = new List<string>();

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add("Email address is required.");
        }
        else if (!IsValidEmail(command.Email))
        {
            errors.Add("Please enter a valid email address.");
        }
        else if (command.Email.Length > 254)
        {
            errors.Add("Email address must be at most 254 characters.");
        }

        if (string.IsNullOrWhiteSpace(command.InvitedBy))
        {
            errors.Add("Invited by user ID is required.");
        }

        return errors;
    }

    /// <summary>
    /// Validates if the given string is a valid email address.
    /// </summary>
    /// <param name="email">The email string to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    private static bool IsValidEmail(string email)
    {
        var emailAttribute = new EmailAddressAttribute();
        return emailAttribute.IsValid(email);
    }

    /// <summary>
    /// Generates a cryptographically secure invitation token.
    /// </summary>
    /// <returns>A unique invitation token.</returns>
    private static string GenerateInvitationToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
