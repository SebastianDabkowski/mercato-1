using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Mercato.Product.Application.Commands;
using Mercato.Product.Application.Services;
using Mercato.Product.Domain;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Product.Infrastructure;

/// <summary>
/// Service implementation for product import operations.
/// </summary>
public class ProductImportService : IProductImportService
{
    private readonly IProductImportRepository _importRepository;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductImportService> _logger;

    /// <summary>
    /// Maximum file size in bytes (10 MB).
    /// </summary>
    private const int MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Supported file extensions.
    /// </summary>
    private static readonly string[] SupportedExtensions = [".csv", ".xls", ".xlsx"];

    public ProductImportService(
        IProductImportRepository importRepository,
        IProductRepository productRepository,
        ILogger<ProductImportService> logger)
    {
        _importRepository = importRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UploadProductImportResult> UploadAndValidateAsync(UploadProductImportCommand command)
    {
        var validationErrors = ValidateUploadCommand(command);
        if (validationErrors.Count > 0)
        {
            return UploadProductImportResult.Failure(validationErrors);
        }

        try
        {
            // Validate file extension
            var extension = Path.GetExtension(command.FileName).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                return UploadProductImportResult.Failure($"Unsupported file type. Supported types: {string.Join(", ", SupportedExtensions)}");
            }

            // Parse the file
            List<ProductImportRow> rows;
            try
            {
                rows = extension == ".csv"
                    ? ParseCsvFile(command.FileContent)
                    : ParseExcelFile(command.FileContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing import file {FileName}", command.FileName);
                return UploadProductImportResult.Failure($"Error parsing file: {ex.Message}");
            }

            if (rows.Count == 0)
            {
                return UploadProductImportResult.Failure("The file contains no data rows.");
            }

            // Get existing products by SKU for matching
            var skus = rows.Where(r => !string.IsNullOrWhiteSpace(r.Sku)).Select(r => r.Sku!).Distinct().ToList();
            var existingProducts = await _importRepository.GetProductsBySkusAsync(command.StoreId, skus);

            // Validate rows and determine create vs update
            var rowErrors = new List<ProductImportRowValidationError>();
            var newProductsCount = 0;
            var updatedProductsCount = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 1; // 1-based row number (excluding header)

                var errors = ValidateImportRow(row, rowNumber, existingProducts);
                rowErrors.AddRange(errors);

                if (errors.Count == 0)
                {
                    if (!string.IsNullOrWhiteSpace(row.Sku) && existingProducts.ContainsKey(row.Sku))
                    {
                        updatedProductsCount++;
                    }
                    else
                    {
                        newProductsCount++;
                    }
                }
            }

            // Get only valid rows for storage
            var validRows = new List<ProductImportRow>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 1;
                var hasErrors = rowErrors.Any(e => e.RowNumber == rowNumber);
                if (!hasErrors)
                {
                    validRows.Add(row);
                }
            }

            // Serialize valid rows for later processing
            var importDataJson = rowErrors.Count == 0 ? JsonSerializer.Serialize(validRows) : null;

            // Create import job
            var job = new ProductImportJob
            {
                Id = Guid.NewGuid(),
                StoreId = command.StoreId,
                SellerId = command.SellerId,
                FileName = command.FileName,
                Status = rowErrors.Count > 0 ? ProductImportStatus.ValidationFailed : ProductImportStatus.AwaitingConfirmation,
                TotalRows = rows.Count,
                NewProductsCount = newProductsCount,
                UpdatedProductsCount = updatedProductsCount,
                ErrorCount = rowErrors.Count,
                ImportDataJson = importDataJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _importRepository.AddAsync(job);

            // Save row errors
            if (rowErrors.Count > 0)
            {
                var errorEntities = rowErrors.Select(e => new ProductImportRowError
                {
                    Id = Guid.NewGuid(),
                    ImportJobId = job.Id,
                    RowNumber = e.RowNumber,
                    ColumnName = e.ColumnName,
                    ErrorMessage = e.ErrorMessage,
                    Sku = e.Sku
                }).ToList();

                await _importRepository.AddRowErrorsAsync(errorEntities);
            }

            _logger.LogInformation(
                "Product import file {FileName} validated for store {StoreId}: {TotalRows} rows, {NewProducts} new, {UpdatedProducts} updates, {Errors} errors",
                command.FileName, command.StoreId, rows.Count, newProductsCount, updatedProductsCount, rowErrors.Count);

            return UploadProductImportResult.Success(
                job.Id,
                rows.Count,
                newProductsCount,
                updatedProductsCount,
                rowErrors.Count,
                rowErrors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading product import file for store {StoreId}", command.StoreId);
            return UploadProductImportResult.Failure("An error occurred while processing the import file.");
        }
    }

    /// <inheritdoc />
    public async Task<ConfirmProductImportResult> ConfirmImportAsync(ConfirmProductImportCommand command)
    {
        var validationErrors = ValidateConfirmCommand(command);
        if (validationErrors.Count > 0)
        {
            return ConfirmProductImportResult.Failure(validationErrors);
        }

        try
        {
            var job = await _importRepository.GetByIdAsync(command.ImportJobId);
            if (job == null)
            {
                return ConfirmProductImportResult.Failure("Import job not found.");
            }

            if (job.StoreId != command.StoreId)
            {
                return ConfirmProductImportResult.NotAuthorized("You are not authorized to confirm this import.");
            }

            if (job.Status != ProductImportStatus.AwaitingConfirmation)
            {
                return ConfirmProductImportResult.Failure($"Import job cannot be confirmed. Current status: {job.Status}");
            }

            if (string.IsNullOrEmpty(job.ImportDataJson))
            {
                return ConfirmProductImportResult.Failure("Import data not found. Please upload the file again.");
            }

            // Update job status to processing
            job.Status = ProductImportStatus.Processing;
            job.StartedAt = DateTimeOffset.UtcNow;
            await _importRepository.UpdateAsync(job);

            try
            {
                // Deserialize the stored import data
                var rows = JsonSerializer.Deserialize<List<ProductImportRow>>(job.ImportDataJson) ?? [];

                // Get existing products for update matching
                var skus = rows.Where(r => !string.IsNullOrWhiteSpace(r.Sku)).Select(r => r.Sku!).Distinct().ToList();
                var existingProducts = await _importRepository.GetProductsBySkusAsync(job.StoreId, skus);

                var createdCount = 0;
                var updatedCount = 0;
                var now = DateTimeOffset.UtcNow;

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Sku))
                    {
                        continue;
                    }

                    if (existingProducts.TryGetValue(row.Sku, out var existingProduct))
                    {
                        // Update existing product
                        existingProduct.Title = row.Title ?? existingProduct.Title;
                        existingProduct.Description = row.Description;
                        existingProduct.Price = row.Price;
                        existingProduct.Stock = row.Stock;
                        existingProduct.Category = row.Category ?? existingProduct.Category;
                        existingProduct.Weight = row.Weight;
                        existingProduct.Length = row.Length;
                        existingProduct.Width = row.Width;
                        existingProduct.Height = row.Height;
                        existingProduct.ShippingMethods = row.ShippingMethods;
                        existingProduct.Images = row.Images;
                        existingProduct.LastUpdatedAt = now;
                        existingProduct.LastUpdatedBy = command.SellerId;

                        await _productRepository.UpdateAsync(existingProduct);
                        updatedCount++;
                    }
                    else
                    {
                        // Create new product
                        var newProduct = new Domain.Entities.Product
                        {
                            Id = Guid.NewGuid(),
                            StoreId = job.StoreId,
                            Sku = row.Sku,
                            Title = row.Title ?? string.Empty,
                            Description = row.Description,
                            Price = row.Price,
                            Stock = row.Stock,
                            Category = row.Category ?? string.Empty,
                            Weight = row.Weight,
                            Length = row.Length,
                            Width = row.Width,
                            Height = row.Height,
                            ShippingMethods = row.ShippingMethods,
                            Images = row.Images,
                            Status = ProductStatus.Draft,
                            CreatedAt = now,
                            LastUpdatedAt = now
                        };

                        await _productRepository.AddAsync(newProduct);
                        createdCount++;
                    }
                }

                // Mark job as completed
                job.Status = ProductImportStatus.Completed;
                job.SuccessCount = createdCount + updatedCount;
                job.NewProductsCount = createdCount;
                job.UpdatedProductsCount = updatedCount;
                job.CompletedAt = DateTimeOffset.UtcNow;
                job.ImportDataJson = null; // Clear the stored data
                await _importRepository.UpdateAsync(job);

                _logger.LogInformation(
                    "Product import job {JobId} completed for store {StoreId}: {Created} created, {Updated} updated",
                    job.Id, job.StoreId, createdCount, updatedCount);

                return ConfirmProductImportResult.Success(createdCount, updatedCount, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing import job {JobId}", command.ImportJobId);

                // Mark job as failed
                job.Status = ProductImportStatus.Failed;
                job.ErrorMessage = "An error occurred while processing the import.";
                job.CompletedAt = DateTimeOffset.UtcNow;
                await _importRepository.UpdateAsync(job);

                return ConfirmProductImportResult.Failure("An error occurred while processing the import.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming product import {JobId}", command.ImportJobId);
            return ConfirmProductImportResult.Failure("An error occurred while confirming the import.");
        }
    }

    /// <inheritdoc />
    public async Task<ProductImportJob?> GetImportJobByIdAsync(Guid id)
    {
        return await _importRepository.GetByIdAsync(id);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImportJob>> GetImportJobsByStoreIdAsync(Guid storeId)
    {
        return await _importRepository.GetByStoreIdAsync(storeId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductImportRowError>> GetImportJobErrorsAsync(Guid jobId)
    {
        return await _importRepository.GetRowErrorsByJobIdAsync(jobId);
    }

    private static List<string> ValidateUploadCommand(UploadProductImportCommand command)
    {
        var errors = new List<string>();

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

        if (command.FileContent == null || command.FileContent == Stream.Null || !command.FileContent.CanRead)
        {
            errors.Add("File content is required.");
        }

        return errors;
    }

    private static List<string> ValidateConfirmCommand(ConfirmProductImportCommand command)
    {
        var errors = new List<string>();

        if (command.ImportJobId == Guid.Empty)
        {
            errors.Add("Import job ID is required.");
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

    private List<ProductImportRow> ParseCsvFile(Stream fileContent)
    {
        using var reader = new StreamReader(fileContent);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<ProductImportRowMap>();

        var records = csv.GetRecords<ProductImportRow>().ToList();
        return records;
    }

    private List<ProductImportRow> ParseExcelFile(Stream fileContent)
    {
        var rows = new List<ProductImportRow>();

        using var workbook = new XLWorkbook(fileContent);
        var worksheet = workbook.Worksheets.First();

        // Get header row
        var headerRow = worksheet.Row(1);
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumnUsed = worksheet.LastColumnUsed();
        var lastColumnNumber = lastColumnUsed?.ColumnNumber() ?? 0;
        for (var col = 1; col <= lastColumnNumber; col++)
        {
            var headerValue = headerRow.Cell(col).GetString()?.Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                headers[headerValue] = col;
            }
        }

        // Parse data rows
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);

            // Skip empty rows
            if (row.IsEmpty())
            {
                continue;
            }

            var importRow = new ProductImportRow
            {
                Sku = GetCellValue(row, headers, "SKU"),
                Title = GetCellValue(row, headers, "Title"),
                Description = GetCellValue(row, headers, "Description"),
                Category = GetCellValue(row, headers, "Category"),
                ShippingMethods = GetCellValue(row, headers, "ShippingMethods"),
                Images = GetCellValue(row, headers, "Images")
            };

            // Parse numeric values
            if (decimal.TryParse(GetCellValue(row, headers, "Price"), NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                importRow.Price = price;
            }

            if (int.TryParse(GetCellValue(row, headers, "Stock"), NumberStyles.Any, CultureInfo.InvariantCulture, out var stock))
            {
                importRow.Stock = stock;
            }

            if (decimal.TryParse(GetCellValue(row, headers, "Weight"), NumberStyles.Any, CultureInfo.InvariantCulture, out var weight))
            {
                importRow.Weight = weight;
            }

            if (decimal.TryParse(GetCellValue(row, headers, "Length"), NumberStyles.Any, CultureInfo.InvariantCulture, out var length))
            {
                importRow.Length = length;
            }

            if (decimal.TryParse(GetCellValue(row, headers, "Width"), NumberStyles.Any, CultureInfo.InvariantCulture, out var width))
            {
                importRow.Width = width;
            }

            if (decimal.TryParse(GetCellValue(row, headers, "Height"), NumberStyles.Any, CultureInfo.InvariantCulture, out var height))
            {
                importRow.Height = height;
            }

            rows.Add(importRow);
        }

        return rows;
    }

    private static string? GetCellValue(IXLRow row, Dictionary<string, int> headers, string columnName)
    {
        if (!headers.TryGetValue(columnName, out var col))
        {
            return null;
        }

        var value = row.Cell(col).GetString()?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static List<ProductImportRowValidationError> ValidateImportRow(
        ProductImportRow row,
        int rowNumber,
        IDictionary<string, Domain.Entities.Product> existingProducts)
    {
        var errors = new List<ProductImportRowValidationError>();

        // SKU is required for updates, but optional for new products
        // However, without SKU we can't match existing products, so let's require it
        if (string.IsNullOrWhiteSpace(row.Sku))
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "SKU",
                ErrorMessage = "SKU is required."
            });
        }
        else if (row.Sku.Length > 100)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "SKU",
                ErrorMessage = "SKU must be at most 100 characters.",
                Sku = row.Sku
            });
        }

        // Title validation
        if (string.IsNullOrWhiteSpace(row.Title))
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Title",
                ErrorMessage = "Title is required.",
                Sku = row.Sku
            });
        }
        else if (row.Title.Length < ProductValidationConstants.TitleMinLength ||
                 row.Title.Length > ProductValidationConstants.TitleMaxLength)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Title",
                ErrorMessage = $"Title must be between {ProductValidationConstants.TitleMinLength} and {ProductValidationConstants.TitleMaxLength} characters.",
                Sku = row.Sku
            });
        }

        // Price validation
        if (row.Price <= 0)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Price",
                ErrorMessage = "Price must be greater than 0.",
                Sku = row.Sku
            });
        }

        // Stock validation
        if (row.Stock < 0)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Stock",
                ErrorMessage = "Stock cannot be negative.",
                Sku = row.Sku
            });
        }

        // Category validation
        if (string.IsNullOrWhiteSpace(row.Category))
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Category",
                ErrorMessage = "Category is required.",
                Sku = row.Sku
            });
        }
        else if (row.Category.Length < ProductValidationConstants.CategoryMinLength ||
                 row.Category.Length > ProductValidationConstants.CategoryMaxLength)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Category",
                ErrorMessage = $"Category must be between {ProductValidationConstants.CategoryMinLength} and {ProductValidationConstants.CategoryMaxLength} characters.",
                Sku = row.Sku
            });
        }

        // Description validation
        if (row.Description != null && row.Description.Length > ProductValidationConstants.DescriptionMaxLength)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Description",
                ErrorMessage = $"Description must be at most {ProductValidationConstants.DescriptionMaxLength} characters.",
                Sku = row.Sku
            });
        }

        // Weight validation
        if (row.Weight.HasValue)
        {
            if (row.Weight < 0)
            {
                errors.Add(new ProductImportRowValidationError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Weight",
                    ErrorMessage = "Weight cannot be negative.",
                    Sku = row.Sku
                });
            }
            else if (row.Weight > ProductValidationConstants.WeightMaxKg)
            {
                errors.Add(new ProductImportRowValidationError
                {
                    RowNumber = rowNumber,
                    ColumnName = "Weight",
                    ErrorMessage = $"Weight must be at most {ProductValidationConstants.WeightMaxKg} kg.",
                    Sku = row.Sku
                });
            }
        }

        // Dimension validations
        ValidateDimension(row.Length, "Length", rowNumber, row.Sku, errors);
        ValidateDimension(row.Width, "Width", rowNumber, row.Sku, errors);
        ValidateDimension(row.Height, "Height", rowNumber, row.Sku, errors);

        // Shipping methods validation
        if (row.ShippingMethods != null && row.ShippingMethods.Length > ProductValidationConstants.ShippingMethodsMaxLength)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "ShippingMethods",
                ErrorMessage = $"Shipping methods must be at most {ProductValidationConstants.ShippingMethodsMaxLength} characters.",
                Sku = row.Sku
            });
        }

        // Images validation
        if (row.Images != null && row.Images.Length > ProductValidationConstants.ImagesMaxLength)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = "Images",
                ErrorMessage = $"Images must be at most {ProductValidationConstants.ImagesMaxLength} characters.",
                Sku = row.Sku
            });
        }

        return errors;
    }

    private static void ValidateDimension(decimal? value, string columnName, int rowNumber, string? sku, List<ProductImportRowValidationError> errors)
    {
        if (!value.HasValue)
        {
            return;
        }

        if (value < 0)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = columnName,
                ErrorMessage = $"{columnName} cannot be negative.",
                Sku = sku
            });
        }
        else if (value > ProductValidationConstants.DimensionMaxCm)
        {
            errors.Add(new ProductImportRowValidationError
            {
                RowNumber = rowNumber,
                ColumnName = columnName,
                ErrorMessage = $"{columnName} must be at most {ProductValidationConstants.DimensionMaxCm} cm.",
                Sku = sku
            });
        }
    }

    /// <summary>
    /// Internal class representing a row in the import file.
    /// </summary>
    internal class ProductImportRow
    {
        public string? Sku { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? ShippingMethods { get; set; }
        public string? Images { get; set; }
    }

    /// <summary>
    /// CSV mapping configuration for ProductImportRow.
    /// </summary>
    private sealed class ProductImportRowMap : ClassMap<ProductImportRow>
    {
        public ProductImportRowMap()
        {
            Map(m => m.Sku).Name("SKU", "sku", "Sku");
            Map(m => m.Title).Name("Title", "title", "TITLE", "ProductTitle", "Product Title");
            Map(m => m.Description).Name("Description", "description", "DESCRIPTION");
            Map(m => m.Price).Name("Price", "price", "PRICE");
            Map(m => m.Stock).Name("Stock", "stock", "STOCK", "Quantity", "quantity", "QUANTITY");
            Map(m => m.Category).Name("Category", "category", "CATEGORY");
            Map(m => m.Weight).Name("Weight", "weight", "WEIGHT").Optional();
            Map(m => m.Length).Name("Length", "length", "LENGTH").Optional();
            Map(m => m.Width).Name("Width", "width", "WIDTH").Optional();
            Map(m => m.Height).Name("Height", "height", "HEIGHT").Optional();
            Map(m => m.ShippingMethods).Name("ShippingMethods", "shippingmethods", "Shipping Methods", "shipping_methods").Optional();
            Map(m => m.Images).Name("Images", "images", "IMAGES", "Image URLs", "image_urls").Optional();
        }
    }
}
