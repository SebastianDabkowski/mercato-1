using Mercato.Payments.Application.Services;
using Mercato.Payments.Domain.Entities;

namespace Mercato.Tests.Payments;

public class PaymentStatusDisplayTests
{
    #region GetDisplayText Tests

    [Theory]
    [InlineData(PaymentStatus.Pending, "Pending")]
    [InlineData(PaymentStatus.Processing, "Processing")]
    [InlineData(PaymentStatus.Paid, "Paid")]
    [InlineData(PaymentStatus.Failed, "Failed")]
    [InlineData(PaymentStatus.Cancelled, "Cancelled")]
    [InlineData(PaymentStatus.Refunded, "Refunded")]
    public void GetDisplayText_AllStatuses_ReturnsCorrectText(PaymentStatus status, string expectedText)
    {
        // Act
        var result = PaymentStatusDisplay.GetDisplayText(status);

        // Assert
        Assert.Equal(expectedText, result);
    }

    #endregion

    #region GetBadgeClass Tests

    [Fact]
    public void GetBadgeClass_Pending_ReturnsWarningClass()
    {
        // Act
        var result = PaymentStatusDisplay.GetBadgeClass(PaymentStatus.Pending);

        // Assert
        Assert.Contains("warning", result);
    }

    [Fact]
    public void GetBadgeClass_Paid_ReturnsSuccessClass()
    {
        // Act
        var result = PaymentStatusDisplay.GetBadgeClass(PaymentStatus.Paid);

        // Assert
        Assert.Contains("success", result);
    }

    [Fact]
    public void GetBadgeClass_Failed_ReturnsDangerClass()
    {
        // Act
        var result = PaymentStatusDisplay.GetBadgeClass(PaymentStatus.Failed);

        // Assert
        Assert.Contains("danger", result);
    }

    [Fact]
    public void GetBadgeClass_Refunded_ReturnsDarkClass()
    {
        // Act
        var result = PaymentStatusDisplay.GetBadgeClass(PaymentStatus.Refunded);

        // Assert
        Assert.Contains("dark", result);
    }

    #endregion

    #region GetIconClass Tests

    [Fact]
    public void GetIconClass_Pending_ReturnsHourglassIcon()
    {
        // Act
        var result = PaymentStatusDisplay.GetIconClass(PaymentStatus.Pending);

        // Assert
        Assert.Contains("hourglass", result);
    }

    [Fact]
    public void GetIconClass_Paid_ReturnsCheckIcon()
    {
        // Act
        var result = PaymentStatusDisplay.GetIconClass(PaymentStatus.Paid);

        // Assert
        Assert.Contains("check", result);
    }

    [Fact]
    public void GetIconClass_Failed_ReturnsXIcon()
    {
        // Act
        var result = PaymentStatusDisplay.GetIconClass(PaymentStatus.Failed);

        // Assert
        Assert.Contains("x-circle", result);
    }

    [Fact]
    public void GetIconClass_Refunded_ReturnsCounterclockwiseIcon()
    {
        // Act
        var result = PaymentStatusDisplay.GetIconClass(PaymentStatus.Refunded);

        // Assert
        Assert.Contains("counterclockwise", result);
    }

    #endregion

    #region GetBuyerMessage Tests

    [Fact]
    public void GetBuyerMessage_Failed_DoesNotExposeErrors()
    {
        // Act
        var result = PaymentStatusDisplay.GetBuyerMessage(PaymentStatus.Failed);

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains("unable to process", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("error", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exception", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuyerMessage_Refunded_IncludesRefundAmount()
    {
        // Act
        var result = PaymentStatusDisplay.GetBuyerMessage(PaymentStatus.Refunded, 50.00m);

        // Assert
        Assert.Contains("50.00", result);
        Assert.Contains("refunded", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetBuyerMessage_Refunded_WithoutAmount_ShowsGenericMessage()
    {
        // Act
        var result = PaymentStatusDisplay.GetBuyerMessage(PaymentStatus.Refunded, 0);

        // Assert
        Assert.Contains("refunded", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("5-10 business days", result);
    }

    [Fact]
    public void GetBuyerMessage_Paid_ShowsSuccessMessage()
    {
        // Act
        var result = PaymentStatusDisplay.GetBuyerMessage(PaymentStatus.Paid);

        // Assert
        Assert.Contains("successful", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Thank you", result);
    }

    #endregion

    #region FormatRefundDisplay Tests

    [Fact]
    public void FormatRefundDisplay_ZeroAmount_ReturnsEmpty()
    {
        // Act
        var result = PaymentStatusDisplay.FormatRefundDisplay(0, 100);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FormatRefundDisplay_FullRefund_ShowsFullRefund()
    {
        // Act
        var result = PaymentStatusDisplay.FormatRefundDisplay(100, 100);

        // Assert
        Assert.Contains("Full refund", result);
    }

    [Fact]
    public void FormatRefundDisplay_PartialRefund_ShowsPartialRefund()
    {
        // Act
        var result = PaymentStatusDisplay.FormatRefundDisplay(50, 100);

        // Assert
        Assert.Contains("Partial refund", result);
    }

    #endregion
}
