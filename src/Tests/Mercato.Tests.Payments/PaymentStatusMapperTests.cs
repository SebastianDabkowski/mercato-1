using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;
using Mercato.Payments.Infrastructure;

namespace Mercato.Tests.Payments;

public class PaymentStatusMapperTests
{
    private readonly PaymentStatusMapper _mapper;

    public PaymentStatusMapperTests()
    {
        _mapper = new PaymentStatusMapper();
    }

    #region MapProviderStatus Tests

    [Theory]
    [InlineData("succeeded", PaymentStatus.Paid)]
    [InlineData("success", PaymentStatus.Paid)]
    [InlineData("paid", PaymentStatus.Paid)]
    [InlineData("completed", PaymentStatus.Paid)]
    [InlineData("approved", PaymentStatus.Paid)]
    [InlineData("captured", PaymentStatus.Paid)]
    [InlineData("SUCCEEDED", PaymentStatus.Paid)] // Case insensitive
    [InlineData("  success  ", PaymentStatus.Paid)] // Trimmed
    public void MapProviderStatus_SuccessStatuses_ReturnsPaid(string providerCode, PaymentStatus expectedStatus)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.IsErrorStatus);
    }

    [Theory]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("pending_capture", PaymentStatus.Pending)]
    [InlineData("awaiting_payment", PaymentStatus.Pending)]
    [InlineData("created", PaymentStatus.Pending)]
    [InlineData("initiated", PaymentStatus.Pending)]
    [InlineData("authorized", PaymentStatus.Pending)]
    public void MapProviderStatus_PendingStatuses_ReturnsPending(string providerCode, PaymentStatus expectedStatus)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.IsErrorStatus);
    }

    [Theory]
    [InlineData("processing", PaymentStatus.Processing)]
    [InlineData("in_progress", PaymentStatus.Processing)]
    [InlineData("requires_action", PaymentStatus.Processing)]
    public void MapProviderStatus_ProcessingStatuses_ReturnsProcessing(string providerCode, PaymentStatus expectedStatus)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.IsErrorStatus);
    }

    [Theory]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("failure", PaymentStatus.Failed)]
    [InlineData("declined", PaymentStatus.Failed)]
    [InlineData("rejected", PaymentStatus.Failed)]
    [InlineData("expired", PaymentStatus.Failed)]
    [InlineData("insufficient_funds", PaymentStatus.Failed)]
    [InlineData("card_declined", PaymentStatus.Failed)]
    public void MapProviderStatus_FailedStatuses_ReturnsFailed(string providerCode, PaymentStatus expectedStatus)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.IsErrorStatus);
    }

    [Theory]
    [InlineData("cancelled", PaymentStatus.Cancelled)]
    [InlineData("canceled", PaymentStatus.Cancelled)]
    [InlineData("voided", PaymentStatus.Cancelled)]
    [InlineData("abandoned", PaymentStatus.Cancelled)]
    public void MapProviderStatus_CancelledStatuses_ReturnsCancelled(string providerCode, PaymentStatus expectedStatus)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.IsErrorStatus);
    }

    [Theory]
    [InlineData("refunded", PaymentStatus.Refunded)]
    [InlineData("refund", PaymentStatus.Refunded)]
    [InlineData("partially_refunded", PaymentStatus.Refunded)]
    [InlineData("chargeback", PaymentStatus.Refunded)]
    public void MapProviderStatus_RefundedStatuses_ReturnsRefunded(string providerCode, PaymentStatus expectedStatus)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedStatus, result.Status);
        Assert.False(result.IsErrorStatus);
    }

    [Theory]
    [InlineData("unknown_status")]
    [InlineData("random_code")]
    [InlineData("xyz123")]
    public void MapProviderStatus_UnknownStatus_ReturnsFailed(string providerCode)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains(providerCode, result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void MapProviderStatus_EmptyOrNull_ReturnsFailed(string? providerCode)
    {
        // Act
        var result = _mapper.MapProviderStatus(providerCode!);

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.ErrorMessage);
    }

    #endregion

    #region GetBuyerFriendlyMessage Tests

    [Theory]
    [InlineData(PaymentStatus.Pending, "processed")]
    [InlineData(PaymentStatus.Processing, "being processed")]
    [InlineData(PaymentStatus.Paid, "successful")]
    [InlineData(PaymentStatus.Failed, "could not be processed")]
    [InlineData(PaymentStatus.Cancelled, "cancelled")]
    [InlineData(PaymentStatus.Refunded, "refunded")]
    public void GetBuyerFriendlyMessage_AllStatuses_ReturnsRelevantMessage(PaymentStatus status, string expectedContains)
    {
        // Act
        var message = _mapper.GetBuyerFriendlyMessage(status);

        // Assert
        Assert.NotEmpty(message);
        Assert.Contains(expectedContains, message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetBuyerFriendlyErrorMessage Tests

    [Fact]
    public void GetBuyerFriendlyErrorMessage_Failed_DoesNotExposeInternalError()
    {
        // Arrange
        var internalError = "Transaction declined: Card verification failed with code 4002";

        // Act
        var message = _mapper.GetBuyerFriendlyErrorMessage(PaymentStatus.Failed, internalError);

        // Assert
        Assert.NotEmpty(message);
        Assert.DoesNotContain("4002", message);
        Assert.DoesNotContain("Card verification", message);
        Assert.Contains("unable to process", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuyerFriendlyErrorMessage_Cancelled_ReturnsGenericMessage()
    {
        // Act
        var message = _mapper.GetBuyerFriendlyErrorMessage(PaymentStatus.Cancelled);

        // Assert
        Assert.NotEmpty(message);
        Assert.Contains("cancelled", message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
