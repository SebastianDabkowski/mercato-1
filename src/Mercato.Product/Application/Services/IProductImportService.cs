using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;

namespace Mercato.Product.Application.Services;

/// <summary>
/// Service interface for product import operations.
/// </summary>
public interface IProductImportService
{
    /// <summary>
    /// Uploads and validates a product import file.
    /// </summary>
    /// <param name="command">The upload command.</param>
    /// <returns>The result of the upload and validation.</returns>
    Task<UploadProductImportResult> UploadAndValidateAsync(UploadProductImportCommand command);

    /// <summary>
    /// Confirms and executes a product import.
    /// </summary>
    /// <param name="command">The confirm command.</param>
    /// <returns>The result of the import execution.</returns>
    Task<ConfirmProductImportResult> ConfirmImportAsync(ConfirmProductImportCommand command);

    /// <summary>
    /// Gets an import job by its unique identifier.
    /// </summary>
    /// <param name="id">The import job ID.</param>
    /// <returns>The import job if found; otherwise, null.</returns>
    Task<ProductImportJob?> GetImportJobByIdAsync(Guid id);

    /// <summary>
    /// Gets all import jobs for a specific store.
    /// </summary>
    /// <param name="storeId">The store ID.</param>
    /// <returns>A list of import jobs belonging to the store.</returns>
    Task<IReadOnlyList<ProductImportJob>> GetImportJobsByStoreIdAsync(Guid storeId);

    /// <summary>
    /// Gets row errors for an import job.
    /// </summary>
    /// <param name="jobId">The import job ID.</param>
    /// <returns>A list of row errors for the job.</returns>
    Task<IReadOnlyList<ProductImportRowError>> GetImportJobErrorsAsync(Guid jobId);
}
