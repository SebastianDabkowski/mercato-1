using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercato.Admin.Migrations
{
    /// <inheritdoc />
    public partial class InitialAdminSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthenticationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IpAddressHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    IsBaseCurrency = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ExchangeRateToBase = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ExchangeRateSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExchangeRateUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurrencyHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencyHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProcessingActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LegalBasis = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DataCategories = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DataSubjectCategories = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Recipients = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ThirdCountryTransfers = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetentionPeriod = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TechnicalMeasures = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OrganizationalMeasures = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProcessorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProcessorContact = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProcessingActivities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProcessingActivityHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataProcessingActivityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProcessingActivityHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlagHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FeatureFlagId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlagHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Integrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IntegrationType = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApiEndpoint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApiKeyMasked = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MerchantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CallbackUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastHealthCheckAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastHealthCheckStatus = table.Column<bool>(type: "bit", nullable: true),
                    LastHealthCheckMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Integrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalConsents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LegalDocumentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IpAddressHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ConsentContext = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalConsents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalDocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    ChangeSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalDocumentVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleChangeAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OldRole = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NewRole = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PerformedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleChangeAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DetectionRule = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AlertsSent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityIncidentStatusChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SecurityIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityIncidentStatusChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FirstResponseDeadlineHours = table.Column<int>(type: "int", nullable: false),
                    ResolutionDeadlineHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SlaTrackingRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CaseCreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstResponseDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolutionDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FirstResponseAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsFirstResponseBreached = table.Column<bool>(type: "bit", nullable: false),
                    IsResolutionBreached = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaTrackingRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserBlockInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BlockedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    BlockedByAdminEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    ReasonDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BlockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnblockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UnblockedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UnblockedByAdminEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlockInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatRuleHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VatRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PreviousValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedByUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatRuleHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VatRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_EntityType_EntityId",
                table: "AdminAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_IsSuccess",
                table: "AdminAuditLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Timestamp",
                table: "AdminAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Timestamp_IsSuccess",
                table: "AdminAuditLogs",
                columns: new[] { "Timestamp", "IsSuccess" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_Email",
                table: "AuthenticationEvents",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_EventType",
                table: "AuthenticationEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_IpAddressHash",
                table: "AuthenticationEvents",
                column: "IpAddressHash");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_IsSuccessful",
                table: "AuthenticationEvents",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationEvents_OccurredAt",
                table: "AuthenticationEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_Code",
                table: "Currencies",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsBaseCurrency",
                table: "Currencies",
                column: "IsBaseCurrency");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsEnabled",
                table: "Currencies",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Currencies_IsEnabled_IsBaseCurrency",
                table: "Currencies",
                columns: new[] { "IsEnabled", "IsBaseCurrency" });

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyHistories_ChangedAt",
                table: "CurrencyHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyHistories_CurrencyId",
                table: "CurrencyHistories",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencyHistories_CurrencyId_ChangedAt",
                table: "CurrencyHistories",
                columns: new[] { "CurrencyId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivities_CreatedAt",
                table: "DataProcessingActivities",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivities_IsActive",
                table: "DataProcessingActivities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivities_IsActive_CreatedAt",
                table: "DataProcessingActivities",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivities_Name",
                table: "DataProcessingActivities",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivityHistories_ActivityId_ChangedAt",
                table: "DataProcessingActivityHistories",
                columns: new[] { "DataProcessingActivityId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivityHistories_ChangedAt",
                table: "DataProcessingActivityHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DataProcessingActivityHistories_DataProcessingActivityId",
                table: "DataProcessingActivityHistories",
                column: "DataProcessingActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagHistories_ChangedAt",
                table: "FeatureFlagHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagHistories_FeatureFlagId",
                table: "FeatureFlagHistories",
                column: "FeatureFlagId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlagHistories_FeatureFlagId_ChangedAt",
                table: "FeatureFlagHistories",
                columns: new[] { "FeatureFlagId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Environment",
                table: "FeatureFlags",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Environment_IsEnabled",
                table: "FeatureFlags",
                columns: new[] { "Environment", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_IsEnabled",
                table: "FeatureFlags",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Key",
                table: "FeatureFlags",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Key_Environment",
                table: "FeatureFlags",
                columns: new[] { "Key", "Environment" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Environment",
                table: "Integrations",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_IntegrationType",
                table: "Integrations",
                column: "IntegrationType");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_IntegrationType_Environment",
                table: "Integrations",
                columns: new[] { "IntegrationType", "Environment" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_IsEnabled",
                table: "Integrations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_IsEnabled_Status",
                table: "Integrations",
                columns: new[] { "IsEnabled", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Name",
                table: "Integrations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Integrations_Status",
                table: "Integrations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LegalConsents_ConsentedAt",
                table: "LegalConsents",
                column: "ConsentedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LegalConsents_DocumentType",
                table: "LegalConsents",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_LegalConsents_LegalDocumentVersionId",
                table: "LegalConsents",
                column: "LegalDocumentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalConsents_UserId",
                table: "LegalConsents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LegalConsents_UserId_DocumentType",
                table: "LegalConsents",
                columns: new[] { "UserId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalConsents_UserId_VersionId",
                table: "LegalConsents",
                columns: new[] { "UserId", "LegalDocumentVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_DocumentType",
                table: "LegalDocuments",
                column: "DocumentType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocuments_IsActive",
                table: "LegalDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_DocumentId_Published_EffectiveDate",
                table: "LegalDocumentVersions",
                columns: new[] { "LegalDocumentId", "IsPublished", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_DocumentId_VersionNumber",
                table: "LegalDocumentVersions",
                columns: new[] { "LegalDocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_EffectiveDate",
                table: "LegalDocumentVersions",
                column: "EffectiveDate");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_IsPublished",
                table: "LegalDocumentVersions",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_LegalDocumentVersions_LegalDocumentId",
                table: "LegalDocumentVersions",
                column: "LegalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleChangeAuditLogs_PerformedAt",
                table: "RoleChangeAuditLogs",
                column: "PerformedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoleChangeAuditLogs_UserId",
                table: "RoleChangeAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_DetectedAt",
                table: "SecurityIncidents",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_DetectedAt_Severity",
                table: "SecurityIncidents",
                columns: new[] { "DetectedAt", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_DetectionRule",
                table: "SecurityIncidents",
                column: "DetectionRule");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_Severity",
                table: "SecurityIncidents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_Severity_Status",
                table: "SecurityIncidents",
                columns: new[] { "Severity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidents_Status",
                table: "SecurityIncidents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidentStatusChanges_ChangedAt",
                table: "SecurityIncidentStatusChanges",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidentStatusChanges_IncidentId_ChangedAt",
                table: "SecurityIncidentStatusChanges",
                columns: new[] { "SecurityIncidentId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIncidentStatusChanges_SecurityIncidentId",
                table: "SecurityIncidentStatusChanges",
                column: "SecurityIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaConfigurations_CaseType_Category",
                table: "SlaConfigurations",
                columns: new[] { "CaseType", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaConfigurations_IsActive",
                table: "SlaConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SlaConfigurations_Name",
                table: "SlaConfigurations",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SlaConfigurations_Priority",
                table: "SlaConfigurations",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_CaseCreatedAt",
                table: "SlaTrackingRecords",
                column: "CaseCreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_CaseId",
                table: "SlaTrackingRecords",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_CaseNumber",
                table: "SlaTrackingRecords",
                column: "CaseNumber");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_IsFirstResponseBreached",
                table: "SlaTrackingRecords",
                column: "IsFirstResponseBreached");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_IsResolutionBreached",
                table: "SlaTrackingRecords",
                column: "IsResolutionBreached");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_Status",
                table: "SlaTrackingRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_Status_Breaches",
                table: "SlaTrackingRecords",
                columns: new[] { "Status", "IsFirstResponseBreached", "IsResolutionBreached" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_StoreId",
                table: "SlaTrackingRecords",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaTrackingRecords_StoreId_CaseCreatedAt",
                table: "SlaTrackingRecords",
                columns: new[] { "StoreId", "CaseCreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlockInfos_IsActive",
                table: "UserBlockInfos",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlockInfos_UserId",
                table: "UserBlockInfos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlockInfos_UserId_IsActive",
                table: "UserBlockInfos",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VatRuleHistories_ChangedAt",
                table: "VatRuleHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VatRuleHistories_VatRuleId",
                table: "VatRuleHistories",
                column: "VatRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_VatRuleHistories_VatRuleId_ChangedAt",
                table: "VatRuleHistories",
                columns: new[] { "VatRuleId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VatRules_CountryCode",
                table: "VatRules",
                column: "CountryCode");

            migrationBuilder.CreateIndex(
                name: "IX_VatRules_CountryCode_CategoryId_IsActive",
                table: "VatRules",
                columns: new[] { "CountryCode", "CategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_VatRules_CountryCode_IsActive_EffectiveDates",
                table: "VatRules",
                columns: new[] { "CountryCode", "IsActive", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_VatRules_EffectiveFrom",
                table: "VatRules",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "IX_VatRules_IsActive",
                table: "VatRules",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AuthenticationEvents");

            migrationBuilder.DropTable(
                name: "Currencies");

            migrationBuilder.DropTable(
                name: "CurrencyHistories");

            migrationBuilder.DropTable(
                name: "DataProcessingActivities");

            migrationBuilder.DropTable(
                name: "DataProcessingActivityHistories");

            migrationBuilder.DropTable(
                name: "FeatureFlagHistories");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "Integrations");

            migrationBuilder.DropTable(
                name: "LegalConsents");

            migrationBuilder.DropTable(
                name: "LegalDocuments");

            migrationBuilder.DropTable(
                name: "LegalDocumentVersions");

            migrationBuilder.DropTable(
                name: "RoleChangeAuditLogs");

            migrationBuilder.DropTable(
                name: "SecurityIncidents");

            migrationBuilder.DropTable(
                name: "SecurityIncidentStatusChanges");

            migrationBuilder.DropTable(
                name: "SlaConfigurations");

            migrationBuilder.DropTable(
                name: "SlaTrackingRecords");

            migrationBuilder.DropTable(
                name: "UserBlockInfos");

            migrationBuilder.DropTable(
                name: "VatRuleHistories");

            migrationBuilder.DropTable(
                name: "VatRules");
        }
    }
}
