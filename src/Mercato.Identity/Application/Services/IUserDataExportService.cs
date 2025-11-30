using Mercato.Identity.Application.Commands;

namespace Mercato.Identity.Application.Services;

/// <summary>
/// Service interface for exporting user personal data for GDPR compliance.
/// </summary>
public interface IUserDataExportService
{
    /// <summary>
    /// Exports all personal data for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user to export data for.</param>
    /// <returns>The result containing the exported data in JSON format.</returns>
    Task<UserDataExportResult> ExportUserDataAsync(string userId);
}
