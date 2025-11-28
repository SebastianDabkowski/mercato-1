using Mercato.Product.Application.Commands;
using Mercato.Product.Domain.Entities;
using Mercato.Product.Domain.Interfaces;
using Mercato.Product.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mercato.Tests.Product;

public class ProductImportServiceTests
{
    private static readonly Guid TestStoreId = Guid.NewGuid();
    private static readonly string TestSellerId = "test-seller-id";

    private readonly Mock<IProductImportRepository> _mockImportRepository;
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ILogger<ProductImportService>> _mockLogger;
    private readonly ProductImportService _service;

    public ProductImportServiceTests()
    {
        _mockImportRepository = new Mock<IProductImportRepository>(MockBehavior.Strict);
        _mockProductRepository = new Mock<IProductRepository>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<ProductImportService>>();
        _service = new ProductImportService(
            _mockImportRepository.Object,
            _mockProductRepository.Object,
            _mockLogger.Object);
    }

    #region UploadAndValidateAsync Tests

    [Fact]
    public async Task UploadAndValidateAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.StoreId = Guid.Empty;

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task UploadAndValidateAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.SellerId = string.Empty;

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task UploadAndValidateAsync_EmptyFileName_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileName = string.Empty;

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("File name is required.", result.Errors);
    }

    [Fact]
    public async Task UploadAndValidateAsync_NullFileContent_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileContent = Stream.Null;

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("File content is required.", result.Errors);
    }

    [Fact]
    public async Task UploadAndValidateAsync_UnsupportedFileType_ReturnsFailure()
    {
        // Arrange
        var command = CreateValidUploadCommand();
        command.FileName = "products.pdf";

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Unsupported file type", result.Errors[0]);
    }

    [Fact]
    public async Task UploadAndValidateAsync_ValidCsvFile_ReturnsSuccess()
    {
        // Arrange
        var csvContent = "SKU,Title,Price,Stock,Category\nSKU001,Test Product,19.99,100,Electronics";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.NewProductsCount);
        Assert.Equal(0, result.UpdatedProductsCount);
        Assert.Equal(0, result.ErrorCount);
        Assert.NotNull(result.ImportJobId);

        _mockImportRepository.Verify(r => r.AddAsync(It.Is<ProductImportJob>(j =>
            j.StoreId == TestStoreId &&
            j.SellerId == TestSellerId &&
            j.FileName == "products.csv" &&
            j.TotalRows == 1 &&
            j.Status == ProductImportStatus.AwaitingConfirmation
        )), Times.Once);
    }

    [Fact]
    public async Task UploadAndValidateAsync_CsvWithExistingSku_CountsAsUpdate()
    {
        // Arrange
        var csvContent = "SKU,Title,Price,Stock,Category\nSKU001,Test Product,19.99,100,Electronics";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var existingProduct = new Mercato.Product.Domain.Entities.Product
        {
            Id = Guid.NewGuid(),
            StoreId = TestStoreId,
            Sku = "SKU001",
            Title = "Old Product"
        };

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product> { { "SKU001", existingProduct } });

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.NewProductsCount);
        Assert.Equal(1, result.UpdatedProductsCount);
    }

    [Fact]
    public async Task UploadAndValidateAsync_CsvWithValidationErrors_ReturnsFailedValidation()
    {
        // Arrange - missing required fields (no Title, Price is 0)
        var csvContent = "SKU,Title,Price,Stock,Category\nSKU001,,0,100,";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        _mockImportRepository.Setup(r => r.AddRowErrorsAsync(It.IsAny<IEnumerable<ProductImportRowError>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.ErrorCount > 0);
        Assert.True(result.RowErrors.Count > 0);
        Assert.Contains(result.RowErrors, e => e.ColumnName == "Title");
        Assert.Contains(result.RowErrors, e => e.ColumnName == "Price");
        Assert.Contains(result.RowErrors, e => e.ColumnName == "Category");

        _mockImportRepository.Verify(r => r.AddAsync(It.Is<ProductImportJob>(j =>
            j.Status == ProductImportStatus.ValidationFailed
        )), Times.Once);
    }

    [Fact]
    public async Task UploadAndValidateAsync_EmptyCsvFile_ReturnsFailure()
    {
        // Arrange - only header, no data rows
        var csvContent = "SKU,Title,Price,Stock,Category";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("no data rows", result.Errors[0]);
    }

    [Fact]
    public async Task UploadAndValidateAsync_ValidatesSkuLength()
    {
        // Arrange - SKU too long (>100 characters)
        var longSku = new string('A', 101);
        var csvContent = $"SKU,Title,Price,Stock,Category\n{longSku},Test Product,19.99,100,Electronics";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        _mockImportRepository.Setup(r => r.AddRowErrorsAsync(It.IsAny<IEnumerable<ProductImportRowError>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.RowErrors, e => e.ColumnName == "SKU" && e.ErrorMessage.Contains("100 characters"));
    }

    [Fact]
    public async Task UploadAndValidateAsync_ValidatesNegativePrice()
    {
        // Arrange
        var csvContent = "SKU,Title,Price,Stock,Category\nSKU001,Test Product,-10.00,100,Electronics";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        _mockImportRepository.Setup(r => r.AddRowErrorsAsync(It.IsAny<IEnumerable<ProductImportRowError>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.RowErrors, e => e.ColumnName == "Price" && e.ErrorMessage.Contains("greater than 0"));
    }

    [Fact]
    public async Task UploadAndValidateAsync_ValidatesNegativeStock()
    {
        // Arrange
        var csvContent = "SKU,Title,Price,Stock,Category\nSKU001,Test Product,19.99,-5,Electronics";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        _mockImportRepository.Setup(r => r.AddRowErrorsAsync(It.IsAny<IEnumerable<ProductImportRowError>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.RowErrors, e => e.ColumnName == "Stock" && e.ErrorMessage.Contains("negative"));
    }

    [Fact]
    public async Task UploadAndValidateAsync_ValidatesMissingSku()
    {
        // Arrange
        var csvContent = "SKU,Title,Price,Stock,Category\n,Test Product,19.99,100,Electronics";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        _mockImportRepository.Setup(r => r.AddRowErrorsAsync(It.IsAny<IEnumerable<ProductImportRowError>>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains(result.RowErrors, e => e.ColumnName == "SKU" && e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public async Task UploadAndValidateAsync_MultipleRows_ValidatesAll()
    {
        // Arrange
        var csvContent = "SKU,Title,Price,Stock,Category\n" +
                        "SKU001,Product 1,10.99,50,Electronics\n" +
                        "SKU002,Product 2,20.99,100,Clothing\n" +
                        "SKU003,Product 3,30.99,25,Books";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        var command = new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };

        _mockImportRepository.Setup(r => r.GetProductsBySkusAsync(TestStoreId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new Dictionary<string, Mercato.Product.Domain.Entities.Product>());

        _mockImportRepository.Setup(r => r.AddAsync(It.IsAny<ProductImportJob>()))
            .ReturnsAsync((ProductImportJob j) => j);

        // Act
        var result = await _service.UploadAndValidateAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, result.TotalRows);
        Assert.Equal(3, result.NewProductsCount);
        Assert.Equal(0, result.ErrorCount);
    }

    #endregion

    #region ConfirmImportAsync Tests

    [Fact]
    public async Task ConfirmImportAsync_EmptyImportJobId_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = Guid.Empty,
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Import job ID is required.", result.Errors);
    }

    [Fact]
    public async Task ConfirmImportAsync_EmptyStoreId_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = Guid.NewGuid(),
            StoreId = Guid.Empty,
            SellerId = TestSellerId
        };

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Store ID is required.", result.Errors);
    }

    [Fact]
    public async Task ConfirmImportAsync_EmptySellerId_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = Guid.NewGuid(),
            StoreId = TestStoreId,
            SellerId = string.Empty
        };

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Seller ID is required.", result.Errors);
    }

    [Fact]
    public async Task ConfirmImportAsync_JobNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = Guid.NewGuid(),
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };

        _mockImportRepository.Setup(r => r.GetByIdAsync(command.ImportJobId))
            .ReturnsAsync((ProductImportJob?)null);

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Import job not found.", result.Errors);
    }

    [Fact]
    public async Task ConfirmImportAsync_WrongStoreId_ReturnsNotAuthorized()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = jobId,
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };

        var job = new ProductImportJob
        {
            Id = jobId,
            StoreId = Guid.NewGuid(), // Different store
            SellerId = TestSellerId,
            Status = ProductImportStatus.AwaitingConfirmation
        };

        _mockImportRepository.Setup(r => r.GetByIdAsync(command.ImportJobId))
            .ReturnsAsync(job);

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.True(result.IsNotAuthorized);
        Assert.Contains("not authorized", result.Errors[0]);
    }

    [Fact]
    public async Task ConfirmImportAsync_WrongStatus_ReturnsFailure()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = jobId,
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };

        var job = new ProductImportJob
        {
            Id = jobId,
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            Status = ProductImportStatus.Completed // Already completed
        };

        _mockImportRepository.Setup(r => r.GetByIdAsync(command.ImportJobId))
            .ReturnsAsync(job);

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("cannot be confirmed", result.Errors[0]);
    }

    [Fact]
    public async Task ConfirmImportAsync_ValidJob_ReturnsSuccess()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var command = new ConfirmProductImportCommand
        {
            ImportJobId = jobId,
            StoreId = TestStoreId,
            SellerId = TestSellerId
        };

        var job = new ProductImportJob
        {
            Id = jobId,
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            Status = ProductImportStatus.AwaitingConfirmation,
            NewProductsCount = 5,
            UpdatedProductsCount = 3
        };

        _mockImportRepository.Setup(r => r.GetByIdAsync(command.ImportJobId))
            .ReturnsAsync(job);

        _mockImportRepository.Setup(r => r.UpdateAsync(It.IsAny<ProductImportJob>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ConfirmImportAsync(command);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(5, result.CreatedCount);
        Assert.Equal(3, result.UpdatedCount);
        Assert.Equal(0, result.FailedCount);

        _mockImportRepository.Verify(r => r.UpdateAsync(It.Is<ProductImportJob>(j =>
            j.Status == ProductImportStatus.Completed
        )), Times.AtLeastOnce);
    }

    #endregion

    #region GetImportJobByIdAsync Tests

    [Fact]
    public async Task GetImportJobByIdAsync_JobExists_ReturnsJob()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new ProductImportJob
        {
            Id = jobId,
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv"
        };

        _mockImportRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync(job);

        // Act
        var result = await _service.GetImportJobByIdAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jobId, result.Id);
    }

    [Fact]
    public async Task GetImportJobByIdAsync_JobNotExists_ReturnsNull()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _mockImportRepository.Setup(r => r.GetByIdAsync(jobId))
            .ReturnsAsync((ProductImportJob?)null);

        // Act
        var result = await _service.GetImportJobByIdAsync(jobId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetImportJobsByStoreIdAsync Tests

    [Fact]
    public async Task GetImportJobsByStoreIdAsync_JobsExist_ReturnsJobs()
    {
        // Arrange
        var jobs = new List<ProductImportJob>
        {
            new() { Id = Guid.NewGuid(), StoreId = TestStoreId, FileName = "products1.csv" },
            new() { Id = Guid.NewGuid(), StoreId = TestStoreId, FileName = "products2.csv" }
        };

        _mockImportRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(jobs);

        // Act
        var result = await _service.GetImportJobsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetImportJobsByStoreIdAsync_NoJobs_ReturnsEmptyList()
    {
        // Arrange
        _mockImportRepository.Setup(r => r.GetByStoreIdAsync(TestStoreId))
            .ReturnsAsync(new List<ProductImportJob>());

        // Act
        var result = await _service.GetImportJobsByStoreIdAsync(TestStoreId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetImportJobErrorsAsync Tests

    [Fact]
    public async Task GetImportJobErrorsAsync_ErrorsExist_ReturnsErrors()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var errors = new List<ProductImportRowError>
        {
            new() { Id = Guid.NewGuid(), ImportJobId = jobId, RowNumber = 1, ErrorMessage = "Error 1" },
            new() { Id = Guid.NewGuid(), ImportJobId = jobId, RowNumber = 2, ErrorMessage = "Error 2" }
        };

        _mockImportRepository.Setup(r => r.GetRowErrorsByJobIdAsync(jobId))
            .ReturnsAsync(errors);

        // Act
        var result = await _service.GetImportJobErrorsAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetImportJobErrorsAsync_NoErrors_ReturnsEmptyList()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _mockImportRepository.Setup(r => r.GetRowErrorsByJobIdAsync(jobId))
            .ReturnsAsync(new List<ProductImportRowError>());

        // Act
        var result = await _service.GetImportJobErrorsAsync(jobId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Helper Methods

    private static UploadProductImportCommand CreateValidUploadCommand()
    {
        var csvContent = "SKU,Title,Price,Stock,Category\nSKU001,Test,10.00,100,Test";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        return new UploadProductImportCommand
        {
            StoreId = TestStoreId,
            SellerId = TestSellerId,
            FileName = "products.csv",
            FileContent = stream
        };
    }

    #endregion
}
