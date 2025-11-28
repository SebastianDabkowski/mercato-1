using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for product image management operations.
/// </summary>
public class ProductImageService : IProductImageService
{
    private readonly IProductImageRepository _imageRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductImageService> _logger;
    private readonly string _uploadsBasePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductImageService"/> class.
    /// </summary>
    /// <param name="imageRepository">The product image repository.</param>
    /// <param name="productRepository">The product repository.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="uploadsBasePath">The base path for file uploads (e.g., wwwroot).</param>
    public ProductImageService(
        IProductImageRepository imageRepository,
        IProductRepository productRepository,
        ILogger<ProductImageService> logger,
        string uploadsBasePath)
    {
        _imageRepository = imageRepository;
        _productRepository = productRepository;
        _logger = logger;
        _uploadsBasePath = uploadsBasePath;
    }

    /// <inheritdoc />
    public async Task<UploadProductImageResult> UploadImageAsync(UploadProductImageCommand command)
    {
        var validationErrors = ValidateUploadCommand(command);
        if (validationErrors.Count > 0)
        {
            return UploadProductImageResult.Failure(validationErrors);
        }

        try
        {
            // Verify product exists and belongs to the store
            var product = await _productRepository.GetByIdAsync(command.ProductId);
            if (product == null)
            {
                return UploadProductImageResult.Failure("Product not found.");
            }

            if (product.StoreId != command.StoreId)
            {
                return UploadProductImageResult.NotAuthorized("You are not authorized to upload images for this product.");
            }

            // Check max images limit
            var currentImageCount = await _imageRepository.GetImageCountByProductIdAsync(command.ProductId);
            if (currentImageCount >= ProductImageValidationConstants.MaxImagesPerProduct)
            {
                return UploadProductImageResult.Failure($"Maximum number of images ({ProductImageValidationConstants.MaxImagesPerProduct}) reached for this product.");
            }

            // FileStream validation is already done in ValidateUploadCommand, but ensure it's not null for safety
            if (command.FileStream == null)
            {
                return UploadProductImageResult.Failure("File stream is required.");
            }

            // Generate unique filename and paths
            var imageId = Guid.NewGuid();
            var fileExtension = Path.GetExtension(command.FileName).ToLowerInvariant();
            var uniqueFileName = $"{imageId}{fileExtension}";
            var productFolder = Path.Combine("uploads", "products", command.ProductId.ToString());
            var fullProductFolder = Path.Combine(_uploadsBasePath, productFolder);

            // Ensure directory exists
            Directory.CreateDirectory(fullProductFolder);

            // Save original file
            var originalPath = Path.Combine(productFolder, uniqueFileName);
            var fullOriginalPath = Path.Combine(_uploadsBasePath, originalPath);

            await using (var fileStream = new FileStream(fullOriginalPath, FileMode.Create))
            {
                await command.FileStream.CopyToAsync(fileStream);
            }

            // Generate thumbnail and optimized versions
            string? thumbnailPath = null;
            string? optimizedPath = null;

            try
            {
                thumbnailPath = await GenerateThumbnailAsync(fullOriginalPath, productFolder, imageId);
                optimizedPath = await GenerateOptimizedVersionAsync(fullOriginalPath, productFolder, imageId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate optimized versions for image {ImageId}. Using original.", imageId);
                // Continue without optimized versions - original will be used
            }

            // Determine if this should be the main image
            var isMain = command.SetAsMain || currentImageCount == 0;

            // Create image record
            var productImage = new ProductImage
            {
                Id = imageId,
                ProductId = command.ProductId,
                FileName = command.FileName,
                StoragePath = originalPath,
                ContentType = command.ContentType,
                FileSize = command.FileSize,
                IsMain = isMain,
                DisplayOrder = currentImageCount,
                CreatedAt = DateTimeOffset.UtcNow,
                ThumbnailPath = thumbnailPath,
                OptimizedPath = optimizedPath
            };

            await _imageRepository.AddAsync(productImage);

            // If this is set as main, clear main flag from other images
            if (isMain && currentImageCount > 0)
            {
                await _imageRepository.SetMainImageAsync(command.ProductId, imageId);
            }

            _logger.LogInformation(
                "Image {ImageId} uploaded for product {ProductId} by seller {SellerId}",
                imageId,
                command.ProductId,
                command.SellerId);

            return UploadProductImageResult.Success(
                imageId,
                "/" + originalPath.Replace("\\", "/"),
                thumbnailPath != null ? "/" + thumbnailPath.Replace("\\", "/") : null,
                optimizedPath != null ? "/" + optimizedPath.Replace("\\", "/") : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for product {ProductId}", command.ProductId);
            return UploadProductImageResult.Failure("An error occurred while uploading the image.");
        }
    }

    /// <inheritdoc />
    public async Task<DeleteProductImageResult> DeleteImageAsync(DeleteProductImageCommand command)
    {
        var validationErrors = ValidateDeleteCommand(command);
        if (validationErrors.Count > 0)
        {
            return DeleteProductImageResult.Failure(validationErrors);
        }

        try
        {
            // Verify product exists and belongs to the store
            var product = await _productRepository.GetByIdAsync(command.ProductId);
            if (product == null)
            {
                return DeleteProductImageResult.Failure("Product not found.");
            }

            if (product.StoreId != command.StoreId)
            {
                return DeleteProductImageResult.NotAuthorized("You are not authorized to delete images for this product.");
            }

            // Get the image
            var image = await _imageRepository.GetByIdAsync(command.ImageId);
            if (image == null)
            {
                return DeleteProductImageResult.Failure("Image not found.");
            }

            if (image.ProductId != command.ProductId)
            {
                return DeleteProductImageResult.Failure("Image does not belong to the specified product.");
            }

            // Delete physical files
            DeleteFileIfExists(Path.Combine(_uploadsBasePath, image.StoragePath));
            if (!string.IsNullOrEmpty(image.ThumbnailPath))
            {
                DeleteFileIfExists(Path.Combine(_uploadsBasePath, image.ThumbnailPath));
            }
            if (!string.IsNullOrEmpty(image.OptimizedPath))
            {
                DeleteFileIfExists(Path.Combine(_uploadsBasePath, image.OptimizedPath));
            }

            var wasMain = image.IsMain;

            // Delete the image record
            await _imageRepository.DeleteAsync(command.ImageId);

            // If the deleted image was main, set another image as main
            if (wasMain)
            {
                var remainingImages = await _imageRepository.GetByProductIdAsync(command.ProductId);
                if (remainingImages.Count > 0)
                {
                    await _imageRepository.SetMainImageAsync(command.ProductId, remainingImages[0].Id);
                }
            }

            _logger.LogInformation(
                "Image {ImageId} deleted for product {ProductId} by seller {SellerId}",
                command.ImageId,
                command.ProductId,
                command.SellerId);

            return DeleteProductImageResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId} for product {ProductId}", command.ImageId, command.ProductId);
            return DeleteProductImageResult.Failure("An error occurred while deleting the image.");
        }
    }

    /// <inheritdoc />
    public async Task<SetMainProductImageResult> SetMainImageAsync(SetMainProductImageCommand command)
    {
        var validationErrors = ValidateSetMainCommand(command);
        if (validationErrors.Count > 0)
        {
            return SetMainProductImageResult.Failure(validationErrors);
        }

        try
        {
            // Verify product exists and belongs to the store
            var product = await _productRepository.GetByIdAsync(command.ProductId);
            if (product == null)
            {
                return SetMainProductImageResult.Failure("Product not found.");
            }

            if (product.StoreId != command.StoreId)
            {
                return SetMainProductImageResult.NotAuthorized("You are not authorized to modify images for this product.");
            }

            // Get the image
            var image = await _imageRepository.GetByIdAsync(command.ImageId);
            if (image == null)
            {
                return SetMainProductImageResult.Failure("Image not found.");
            }

            if (image.ProductId != command.ProductId)
            {
                return SetMainProductImageResult.Failure("Image does not belong to the specified product.");
            }

            // Set as main image
            await _imageRepository.SetMainImageAsync(command.ProductId, command.ImageId);

            _logger.LogInformation(
                "Image {ImageId} set as main for product {ProductId} by seller {SellerId}",
                command.ImageId,
                command.ProductId,
                command.SellerId);

            return SetMainProductImageResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting main image {ImageId} for product {ProductId}", command.ImageId, command.ProductId);
            return SetMainProductImageResult.Failure("An error occurred while setting the main image.");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImage>> GetImagesByProductIdAsync(Guid productId)
    {
        return await _imageRepository.GetByProductIdAsync(productId);
    }

    private static List<string> ValidateUploadCommand(UploadProductImageCommand command)
    {
        var errors = new List<string>();

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            errors.Add("File name is required.");
        }
        else
        {
            var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
            if (!ProductImageValidationConstants.AllowedExtensions.Contains(extension))
            {
                errors.Add($"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", ProductImageValidationConstants.AllowedExtensions)}");
            }
        }

        if (string.IsNullOrWhiteSpace(command.ContentType))
        {
            errors.Add("Content type is required.");
        }
        else if (!ProductImageValidationConstants.AllowedContentTypes.Contains(command.ContentType.ToLowerInvariant()))
        {
            errors.Add($"Content type '{command.ContentType}' is not allowed. Allowed types: {string.Join(", ", ProductImageValidationConstants.AllowedContentTypes)}");
        }

        if (command.FileSize <= 0)
        {
            errors.Add("File size must be greater than 0.");
        }
        else if (command.FileSize > ProductImageValidationConstants.MaxFileSizeBytes)
        {
            errors.Add($"File size exceeds maximum allowed size of {ProductImageValidationConstants.MaxFileSizeBytes / (1024 * 1024)}MB.");
        }

        if (command.FileStream == null)
        {
            errors.Add("File stream is required.");
        }

        return errors;
    }

    private static List<string> ValidateDeleteCommand(DeleteProductImageCommand command)
    {
        var errors = new List<string>();

        if (command.ImageId == Guid.Empty)
        {
            errors.Add("Image ID is required.");
        }

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateSetMainCommand(SetMainProductImageCommand command)
    {
        var errors = new List<string>();

        if (command.ImageId == Guid.Empty)
        {
            errors.Add("Image ID is required.");
        }

        if (command.ProductId == Guid.Empty)
        {
            errors.Add("Product ID is required.");
        }

        if (command.StoreId == Guid.Empty)
        {
            errors.Add("Store ID is required.");
        }

        if (string.IsNullOrWhiteSpace(command.SellerId))
        {
            errors.Add("Seller ID is required.");
        }

        return errors;
    }

    private async Task<string?> GenerateThumbnailAsync(string sourcePath, string productFolder, Guid imageId)
    {
        var thumbnailFileName = $"{imageId}_thumb.jpg";
        var thumbnailPath = Path.Combine(productFolder, thumbnailFileName);
        var fullThumbnailPath = Path.Combine(_uploadsBasePath, thumbnailPath);

        await Task.Run(() =>
        {
            using var inputStream = File.OpenRead(sourcePath);
            using var original = SKBitmap.Decode(inputStream);

            if (original == null)
            {
                throw new InvalidOperationException("Failed to decode image.");
            }

            var resized = ResizeImage(
                original,
                ProductImageValidationConstants.ThumbnailWidth,
                ProductImageValidationConstants.ThumbnailHeight,
                true);

            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, ProductImageValidationConstants.JpegQuality);
            using var outputStream = File.OpenWrite(fullThumbnailPath);
            data.SaveTo(outputStream);
        });

        return thumbnailPath;
    }

    private async Task<string?> GenerateOptimizedVersionAsync(string sourcePath, string productFolder, Guid imageId)
    {
        var optimizedFileName = $"{imageId}_opt.jpg";
        var optimizedPath = Path.Combine(productFolder, optimizedFileName);
        var fullOptimizedPath = Path.Combine(_uploadsBasePath, optimizedPath);

        await Task.Run(() =>
        {
            using var inputStream = File.OpenRead(sourcePath);
            using var original = SKBitmap.Decode(inputStream);

            if (original == null)
            {
                throw new InvalidOperationException("Failed to decode image.");
            }

            // Only resize if larger than max dimensions
            if (original.Width <= ProductImageValidationConstants.OptimizedMaxWidth &&
                original.Height <= ProductImageValidationConstants.OptimizedMaxHeight)
            {
                // Just re-encode with compression
                using var image = SKImage.FromBitmap(original);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, ProductImageValidationConstants.JpegQuality);
                using var outputStream = File.OpenWrite(fullOptimizedPath);
                data.SaveTo(outputStream);
            }
            else
            {
                var resized = ResizeImage(
                    original,
                    ProductImageValidationConstants.OptimizedMaxWidth,
                    ProductImageValidationConstants.OptimizedMaxHeight,
                    false);

                using var image = SKImage.FromBitmap(resized);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, ProductImageValidationConstants.JpegQuality);
                using var outputStream = File.OpenWrite(fullOptimizedPath);
                data.SaveTo(outputStream);
            }
        });

        return optimizedPath;
    }

    private static SKBitmap ResizeImage(SKBitmap original, int maxWidth, int maxHeight, bool crop)
    {
        int newWidth, newHeight;

        if (crop)
        {
            // Crop to exact dimensions (center crop)
            var aspectRatio = (float)original.Width / original.Height;
            var targetAspectRatio = (float)maxWidth / maxHeight;

            int sourceX = 0, sourceY = 0, sourceWidth = original.Width, sourceHeight = original.Height;

            if (aspectRatio > targetAspectRatio)
            {
                // Image is wider, crop horizontally
                sourceWidth = (int)(original.Height * targetAspectRatio);
                sourceX = (original.Width - sourceWidth) / 2;
            }
            else if (aspectRatio < targetAspectRatio)
            {
                // Image is taller, crop vertically
                sourceHeight = (int)(original.Width / targetAspectRatio);
                sourceY = (original.Height - sourceHeight) / 2;
            }

            newWidth = maxWidth;
            newHeight = maxHeight;

            var resized = new SKBitmap(newWidth, newHeight);
            using var canvas = new SKCanvas(resized);

            var srcRect = new SKRect(sourceX, sourceY, sourceX + sourceWidth, sourceY + sourceHeight);
            var destRect = new SKRect(0, 0, newWidth, newHeight);
            using var paint = new SKPaint { IsAntialias = true };
            canvas.DrawBitmap(original, srcRect, destRect, paint);

            return resized;
        }
        else
        {
            // Maintain aspect ratio, fit within dimensions
            var ratioX = (float)maxWidth / original.Width;
            var ratioY = (float)maxHeight / original.Height;
            var ratio = Math.Min(ratioX, ratioY);

            newWidth = (int)(original.Width * ratio);
            newHeight = (int)(original.Height * ratio);

            var destInfo = new SKImageInfo(newWidth, newHeight);
            var resized = new SKBitmap(destInfo);
            original.ScalePixels(resized, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            return resized;
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Ignore file deletion errors - file may be in use
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore permission errors - logging already happened at higher level
        }
    }
}
