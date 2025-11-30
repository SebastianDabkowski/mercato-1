using Mercato.Buyer.Application.Commands;
using Mercato.Buyer.Application.Queries;
using Mercato.Buyer.Application.Services;
using Mercato.Buyer.Domain.Entities;
using Mercato.Buyer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Mercato.Buyer.Infrastructure;

/// <summary>
/// Service implementation for consent management operations.
/// </summary>
public class ConsentService : IConsentService
{
    private readonly IConsentRepository _consentRepository;
    private readonly ILogger<ConsentService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsentService"/> class.
    /// </summary>
    /// <param name="consentRepository">The consent repository.</param>
    /// <param name="logger">The logger.</param>
    public ConsentService(IConsentRepository consentRepository, ILogger<ConsentService> logger)
    {
        _consentRepository = consentRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GetConsentTypesResult> GetConsentTypesAsync(GetConsentTypesQuery query)
    {
        var consentTypes = await _consentRepository.GetActiveConsentTypesAsync();

        if (query.MandatoryOnly)
        {
            consentTypes = consentTypes.Where(ct => ct.IsMandatory).ToList();
        }

        var dtos = consentTypes
            .Where(ct => ct.Versions.Count != 0)
            .Select(ct =>
            {
                var currentVersion = ct.Versions
                    .Where(v => v.EffectiveTo == null)
                    .OrderByDescending(v => v.VersionNumber)
                    .First();

                return new ConsentTypeDto
                {
                    Id = ct.Id,
                    Code = ct.Code,
                    Name = ct.Name,
                    Description = ct.Description,
                    IsMandatory = ct.IsMandatory,
                    DisplayOrder = ct.DisplayOrder,
                    CurrentVersionId = currentVersion.Id,
                    CurrentConsentText = currentVersion.ConsentText,
                    CurrentVersionNumber = currentVersion.VersionNumber
                };
            })
            .OrderBy(ct => ct.DisplayOrder)
            .ToList();

        return GetConsentTypesResult.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<GetUserConsentsResult> GetUserConsentsAsync(GetUserConsentsQuery query)
    {
        var errors = ValidateGetUserConsentsQuery(query);
        if (errors.Count > 0)
        {
            return GetUserConsentsResult.Failure(errors);
        }

        var userConsents = await _consentRepository.GetUserConsentsAsync(query.UserId);
        var activeConsentTypes = await _consentRepository.GetActiveConsentTypesAsync();

        // Group user consents by consent type and get the latest for each
        var latestConsentsByType = userConsents
            .GroupBy(uc => uc.ConsentVersion!.ConsentTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(uc => uc.ConsentedAt).First()
            );

        var dtos = new List<UserConsentDto>();

        foreach (var consentType in activeConsentTypes.Where(ct => ct.Versions.Count != 0))
        {
            var currentVersion = consentType.Versions
                .Where(v => v.EffectiveTo == null)
                .OrderByDescending(v => v.VersionNumber)
                .First();

            if (latestConsentsByType.TryGetValue(consentType.Id, out var userConsent))
            {
                dtos.Add(new UserConsentDto
                {
                    ConsentTypeCode = consentType.Code,
                    ConsentTypeName = consentType.Name,
                    ConsentTypeDescription = consentType.Description,
                    IsGranted = userConsent.IsGranted,
                    ConsentedAt = userConsent.ConsentedAt,
                    ConsentVersionNumber = userConsent.ConsentVersion!.VersionNumber,
                    ConsentText = userConsent.ConsentVersion.ConsentText,
                    CurrentVersionId = currentVersion.Id,
                    HasNewerVersion = userConsent.ConsentVersionId != currentVersion.Id,
                    IsMandatory = consentType.IsMandatory
                });
            }
            else
            {
                // No consent recorded yet for this type
                dtos.Add(new UserConsentDto
                {
                    ConsentTypeCode = consentType.Code,
                    ConsentTypeName = consentType.Name,
                    ConsentTypeDescription = consentType.Description,
                    IsGranted = false,
                    ConsentedAt = DateTimeOffset.MinValue,
                    ConsentVersionNumber = 0,
                    ConsentText = currentVersion.ConsentText,
                    CurrentVersionId = currentVersion.Id,
                    HasNewerVersion = false,
                    IsMandatory = consentType.IsMandatory
                });
            }
        }

        return GetUserConsentsResult.Success(dtos.OrderBy(d => 
            activeConsentTypes.FirstOrDefault(ct => ct.Code == d.ConsentTypeCode)?.DisplayOrder ?? 0).ToList());
    }

    /// <inheritdoc />
    public async Task<RecordConsentResult> RecordConsentAsync(RecordConsentCommand command)
    {
        var errors = ValidateRecordConsentCommand(command);
        if (errors.Count > 0)
        {
            return RecordConsentResult.Failure(errors);
        }

        var consentVersion = await _consentRepository.GetVersionByIdAsync(command.ConsentVersionId);
        if (consentVersion == null)
        {
            return RecordConsentResult.Failure("Consent version not found.");
        }

        var userConsent = new UserConsent
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            ConsentVersionId = command.ConsentVersionId,
            IsGranted = command.IsGranted,
            ConsentedAt = DateTimeOffset.UtcNow,
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent
        };

        await _consentRepository.AddUserConsentAsync(userConsent);

        _logger.LogInformation(
            "Recorded consent for user {UserId}: {ConsentType} = {IsGranted} (version {Version})",
            command.UserId,
            consentVersion.ConsentType?.Code,
            command.IsGranted,
            consentVersion.VersionNumber);

        return RecordConsentResult.Success(userConsent.Id);
    }

    /// <inheritdoc />
    public async Task<RecordMultipleConsentsResult> RecordMultipleConsentsAsync(RecordMultipleConsentsCommand command)
    {
        var errors = ValidateRecordMultipleConsentsCommand(command);
        if (errors.Count > 0)
        {
            return RecordMultipleConsentsResult.Failure(errors);
        }

        var consentsRecorded = 0;

        foreach (var consent in command.Consents)
        {
            var consentType = await _consentRepository.GetConsentTypeByCodeAsync(consent.ConsentTypeCode);
            if (consentType == null)
            {
                _logger.LogWarning("Consent type {Code} not found, skipping", consent.ConsentTypeCode);
                continue;
            }

            var currentVersion = await _consentRepository.GetCurrentVersionAsync(consentType.Id);
            if (currentVersion == null)
            {
                _logger.LogWarning("No current version found for consent type {Code}, skipping", consent.ConsentTypeCode);
                continue;
            }

            var userConsent = new UserConsent
            {
                Id = Guid.NewGuid(),
                UserId = command.UserId,
                ConsentVersionId = currentVersion.Id,
                IsGranted = consent.IsGranted,
                ConsentedAt = DateTimeOffset.UtcNow,
                IpAddress = command.IpAddress,
                UserAgent = command.UserAgent
            };

            await _consentRepository.AddUserConsentAsync(userConsent);
            consentsRecorded++;

            _logger.LogInformation(
                "Recorded consent for user {UserId}: {ConsentType} = {IsGranted} (version {Version})",
                command.UserId,
                consent.ConsentTypeCode,
                consent.IsGranted,
                currentVersion.VersionNumber);
        }

        return RecordMultipleConsentsResult.Success(consentsRecorded);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveConsentAsync(string userId, string consentTypeCode)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(consentTypeCode))
        {
            return false;
        }

        return await _consentRepository.HasActiveConsentAsync(userId, consentTypeCode);
    }

    private static List<string> ValidateGetUserConsentsQuery(GetUserConsentsQuery query)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(query.UserId))
        {
            errors.Add("User ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRecordConsentCommand(RecordConsentCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (command.ConsentVersionId == Guid.Empty)
        {
            errors.Add("Consent version ID is required.");
        }

        return errors;
    }

    private static List<string> ValidateRecordMultipleConsentsCommand(RecordMultipleConsentsCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            errors.Add("User ID is required.");
        }

        if (command.Consents.Count == 0)
        {
            errors.Add("At least one consent decision is required.");
        }

        return errors;
    }
}
