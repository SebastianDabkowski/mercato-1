using Mercato.Admin.Application.Commands;
using Mercato.Admin.Application.Queries;
using Mercato.Admin.Application.Services;
using Mercato.Admin.Domain.Entities;
using Mercato.Admin.Domain.Interfaces;
using Mercato.Notifications.Application.Commands;
using Mercato.Notifications.Application.Services;
using Mercato.Notifications.Domain.Entities;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Seller.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Admin.Infrastructure;

/// <summary>
/// Service implementation for admin photo moderation operations.
/// </summary>
public class PhotoModerationService : IPhotoModerationService
{
    private readonly IPhotoModerationRepository _photoModerationRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IStoreRepository _storeRepository;
    private readonly IAdminAuditRepository _adminAuditRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PhotoModerationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PhotoModerationService"/> class.
    /// </summary>
    /// <param name="photoModerationRepository">The photo moderation repository.</param>
    /// <param name="productImageRepository">The product image repository.</param>
    /// <param name="storeRepository">The store repository.</param>
    /// <param name="adminAuditRepository">The admin audit repository.</param>
    /// <param name="notificationService">The notification service.</param>
    /// <param name="logger">The logger.</param>
    public PhotoModerationService(
        IPhotoModerationRepository photoModerationRepository,
        IProductImageRepository productImageRepository,
        IStoreRepository storeRepository,
        IAdminAuditRepository adminAuditRepository,
        INotificationService notificationService,
        ILogger<PhotoModerationService> logger)
    {
        _photoModerationRepository = photoModerationRepository ?? throw new ArgumentNullException(nameof(photoModerationRepository));
        _productImageRepository = productImageRepository ?? throw new ArgumentNullException(nameof(productImageRepository));
        _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
        _adminAuditRepository = adminAuditRepository ?? throw new ArgumentNullException(nameof(adminAuditRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<GetPhotosForModerationResult> GetPhotosForModerationAsync(PhotoModerationFilterQuery query)
    {
        try
        {
            var skip = (query.Page - 1) * query.PageSize;
            var (photos, totalCount) = await _photoModerationRepository.GetPendingPhotosAsync(
                query.StoreId,
                query.FlaggedOnly,
                skip,
                query.PageSize);

            // Get store information for the photos
            var storeIds = photos
                .Where(p => p.Product != null)
                .Select(p => p.Product!.StoreId)
                .Distinct()
                .ToList();
            var stores = await _storeRepository.GetByIdsAsync(storeIds);
            var storeDict = stores.ToDictionary(s => s.Id, s => s.Name);

            var photoSummaries = photos.Select(p => new PhotoModerationSummary
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductTitle = p.Product?.Title ?? "Unknown Product",
                StoreId = p.Product?.StoreId ?? Guid.Empty,
                StoreName = p.Product != null ? storeDict.GetValueOrDefault(p.Product.StoreId, "Unknown Store") : "Unknown Store",
                StoragePath = p.StoragePath,
                ThumbnailPath = p.ThumbnailPath,
                FileName = p.FileName,
                IsFlagged = p.IsFlagged,
                FlagReason = p.FlagReason,
                ModerationStatus = p.ModerationStatus,
                CreatedAt = p.CreatedAt,
                FlaggedAt = p.FlaggedAt
            }).ToList();

            return GetPhotosForModerationResult.Success(photoSummaries, totalCount, query.Page, query.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photos for moderation");
            return GetPhotosForModerationResult.Failure("An error occurred while retrieving photos for moderation.");
        }
    }

    /// <inheritdoc />
    public async Task<GetPhotoModerationDetailsResult> GetPhotoDetailsAsync(Guid imageId)
    {
        try
        {
            var photo = await _photoModerationRepository.GetPhotoByIdAsync(imageId);
            if (photo == null)
            {
                return GetPhotoModerationDetailsResult.Failure("Photo not found.");
            }

            // Get store information
            Seller.Domain.Entities.Store? store = null;
            if (photo.Product != null)
            {
                store = await _storeRepository.GetByIdAsync(photo.Product.StoreId);
            }

            // Get moderation history
            var history = await _photoModerationRepository.GetModerationHistoryAsync(imageId);

            // Get count of other images for this product
            var otherImagesCount = await _productImageRepository.GetImageCountByProductIdAsync(photo.ProductId) - 1;

            var photoDetails = new PhotoModerationDetails
            {
                Id = photo.Id,
                ProductId = photo.ProductId,
                ProductTitle = photo.Product?.Title ?? "Unknown Product",
                StoreId = photo.Product?.StoreId ?? Guid.Empty,
                StoreName = store?.Name ?? "Unknown Store",
                SellerId = store?.SellerId ?? string.Empty,
                StoragePath = photo.StoragePath,
                ThumbnailPath = photo.ThumbnailPath,
                OptimizedPath = photo.OptimizedPath,
                FileName = photo.FileName,
                ContentType = photo.ContentType,
                FileSize = photo.FileSize,
                IsMain = photo.IsMain,
                IsFlagged = photo.IsFlagged,
                FlagReason = photo.FlagReason,
                FlaggedAt = photo.FlaggedAt,
                ModerationStatus = photo.ModerationStatus,
                ModerationReason = photo.ModerationReason,
                ModeratedAt = photo.ModeratedAt,
                ModeratedBy = photo.ModeratedBy,
                CreatedAt = photo.CreatedAt,
                OtherProductImagesCount = Math.Max(0, otherImagesCount),
                ModerationHistory = history.Select(h => new PhotoModerationHistoryEntry
                {
                    Id = h.Id,
                    AdminUserId = h.AdminUserId,
                    Decision = h.Decision,
                    Reason = h.Reason,
                    PreviousStatus = h.PreviousStatus,
                    CreatedAt = h.CreatedAt
                }).ToList()
            };

            return GetPhotoModerationDetailsResult.Success(photoDetails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving photo details for image {ImageId}", imageId);
            return GetPhotoModerationDetailsResult.Failure("An error occurred while retrieving photo details.");
        }
    }

    /// <inheritdoc />
    public async Task<ApprovePhotoResult> ApprovePhotoAsync(ApprovePhotoCommand command)
    {
        try
        {
            var validationErrors = ValidateApprovePhotoCommand(command);
            if (validationErrors.Count > 0)
            {
                return ApprovePhotoResult.Failure(validationErrors);
            }

            var photo = await _photoModerationRepository.GetPhotoByIdAsync(command.ImageId);
            if (photo == null)
            {
                return ApprovePhotoResult.Failure("Photo not found.");
            }

            var previousStatus = photo.ModerationStatus;

            // Update photo moderation status
            photo.ModerationStatus = PhotoModerationStatus.Approved;
            photo.ModerationReason = command.Reason;
            photo.ModeratedAt = DateTimeOffset.UtcNow;
            photo.ModeratedBy = command.AdminUserId;
            photo.IsFlagged = false;

            await _photoModerationRepository.UpdatePhotoModerationStatusAsync(photo);

            // Record moderation decision
            await _photoModerationRepository.AddModerationDecisionAsync(new PhotoModerationDecision
            {
                Id = Guid.NewGuid(),
                ProductImageId = photo.Id,
                ProductId = photo.ProductId,
                StoreId = photo.Product?.StoreId ?? Guid.Empty,
                AdminUserId = command.AdminUserId,
                Decision = PhotoModerationStatus.Approved,
                Reason = command.Reason,
                PreviousStatus = previousStatus,
                CreatedAt = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "ApprovePhoto",
                EntityType = "ProductImage",
                EntityId = photo.Id.ToString(),
                Details = $"Approved photo for product {photo.ProductId}. Previous status: {previousStatus}. Reason: {command.Reason ?? "N/A"}",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            _logger.LogInformation("Photo {ImageId} approved by admin {AdminUserId}", photo.Id, command.AdminUserId);

            return ApprovePhotoResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving photo {ImageId}", command.ImageId);
            return ApprovePhotoResult.Failure("An error occurred while approving the photo.");
        }
    }

    /// <inheritdoc />
    public async Task<RemovePhotoResult> RemovePhotoAsync(RemovePhotoCommand command)
    {
        try
        {
            var validationErrors = ValidateRemovePhotoCommand(command);
            if (validationErrors.Count > 0)
            {
                return RemovePhotoResult.Failure(validationErrors);
            }

            var photo = await _photoModerationRepository.GetPhotoByIdAsync(command.ImageId);
            if (photo == null)
            {
                return RemovePhotoResult.Failure("Photo not found.");
            }

            var previousStatus = photo.ModerationStatus;
            var productTitle = photo.Product?.Title ?? "Unknown Product";
            var storeId = photo.Product?.StoreId ?? Guid.Empty;

            // Update photo moderation status - photo is archived, not hard-deleted
            photo.ModerationStatus = PhotoModerationStatus.Removed;
            photo.ModerationReason = command.Reason;
            photo.ModeratedAt = DateTimeOffset.UtcNow;
            photo.ModeratedBy = command.AdminUserId;

            await _photoModerationRepository.UpdatePhotoModerationStatusAsync(photo);

            // Record moderation decision
            await _photoModerationRepository.AddModerationDecisionAsync(new PhotoModerationDecision
            {
                Id = Guid.NewGuid(),
                ProductImageId = photo.Id,
                ProductId = photo.ProductId,
                StoreId = storeId,
                AdminUserId = command.AdminUserId,
                Decision = PhotoModerationStatus.Removed,
                Reason = command.Reason,
                PreviousStatus = previousStatus,
                CreatedAt = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Create audit log entry
            await _adminAuditRepository.AddAsync(new AdminAuditLog
            {
                Id = Guid.NewGuid(),
                AdminUserId = command.AdminUserId,
                Action = "RemovePhoto",
                EntityType = "ProductImage",
                EntityId = photo.Id.ToString(),
                Details = $"Removed photo for product {photo.ProductId}. Previous status: {previousStatus}. Reason: {command.Reason}",
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress
            });

            // Get seller ID for notification
            var store = await _storeRepository.GetByIdAsync(storeId);
            var sellerId = store?.SellerId ?? string.Empty;

            if (!string.IsNullOrEmpty(sellerId))
            {
                // Send notification to seller
                await _notificationService.CreateNotificationAsync(new CreateNotificationCommand
                {
                    UserId = sellerId,
                    Title = "Product Photo Removed",
                    Message = $"A photo from your product \"{productTitle}\" has been removed by moderation. Reason: {command.Reason}",
                    Type = NotificationType.PhotoImageRemoved,
                    RelatedEntityId = photo.ProductId,
                    RelatedUrl = $"/Seller/Products/Details/{photo.ProductId}"
                });
            }

            _logger.LogInformation("Photo {ImageId} removed by admin {AdminUserId}. Reason: {Reason}",
                photo.Id, command.AdminUserId, command.Reason);

            return RemovePhotoResult.Success(sellerId, productTitle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing photo {ImageId}", command.ImageId);
            return RemovePhotoResult.Failure("An error occurred while removing the photo.");
        }
    }

    /// <inheritdoc />
    public async Task<int> GetPendingPhotoCountAsync()
    {
        try
        {
            return await _photoModerationRepository.GetPendingPhotoCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending photo count");
            return 0;
        }
    }

    private static List<string> ValidateApprovePhotoCommand(ApprovePhotoCommand command)
    {
        var errors = new List<string>();

        if (command.ImageId == Guid.Empty)
        {
            errors.Add("Image ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRemovePhotoCommand(RemovePhotoCommand command)
    {
        var errors = new List<string>();

        if (command.ImageId == Guid.Empty)
        {
            errors.Add("Image ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.AdminUserId))
        {
            errors.Add("Admin user ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            errors.Add("Removal reason is required.");
        }

        return errors;
    }
}
