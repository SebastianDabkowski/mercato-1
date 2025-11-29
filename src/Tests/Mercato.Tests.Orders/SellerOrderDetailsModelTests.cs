using Mercato.Orders.Domain.Entities;
using Mercato.Web.Pages.Seller.Orders;

namespace Mercato.Tests.Orders;

/// <summary>
/// Unit tests for the seller order details page model helper methods.
/// </summary>
public class SellerOrderDetailsModelTests
{
    #region GetStatusBadgeClass Tests

    [Theory]
    [InlineData(SellerSubOrderStatus.New, "bg-warning text-dark")]
    [InlineData(SellerSubOrderStatus.Paid, "bg-info")]
    [InlineData(SellerSubOrderStatus.Preparing, "bg-primary")]
    [InlineData(SellerSubOrderStatus.Shipped, "bg-secondary")]
    [InlineData(SellerSubOrderStatus.Delivered, "bg-success")]
    [InlineData(SellerSubOrderStatus.Cancelled, "bg-danger")]
    [InlineData(SellerSubOrderStatus.Refunded, "bg-dark")]
    public void GetStatusBadgeClass_ReturnsCorrectClass(SellerSubOrderStatus status, string expectedClass)
    {
        // Act
        var result = DetailsModel.GetStatusBadgeClass(status);

        // Assert
        Assert.Equal(expectedClass, result);
    }

    #endregion

    #region GetStatusDisplayText Tests

    [Theory]
    [InlineData(SellerSubOrderStatus.New, "New")]
    [InlineData(SellerSubOrderStatus.Paid, "Paid")]
    [InlineData(SellerSubOrderStatus.Preparing, "Preparing")]
    [InlineData(SellerSubOrderStatus.Shipped, "Shipped")]
    [InlineData(SellerSubOrderStatus.Delivered, "Delivered")]
    [InlineData(SellerSubOrderStatus.Cancelled, "Cancelled")]
    [InlineData(SellerSubOrderStatus.Refunded, "Refunded")]
    public void GetStatusDisplayText_ReturnsCorrectText(SellerSubOrderStatus status, string expectedText)
    {
        // Act
        var result = DetailsModel.GetStatusDisplayText(status);

        // Assert
        Assert.Equal(expectedText, result);
    }

    #endregion

    #region GetPaymentStatusBadgeClass Tests

    [Fact]
    public void GetPaymentStatusBadgeClass_New_ReturnsWarningClass()
    {
        // Act
        var result = DetailsModel.GetPaymentStatusBadgeClass(SellerSubOrderStatus.New);

        // Assert
        Assert.Equal("bg-warning text-dark", result);
    }

    [Fact]
    public void GetPaymentStatusBadgeClass_Cancelled_ReturnsDangerClass()
    {
        // Act
        var result = DetailsModel.GetPaymentStatusBadgeClass(SellerSubOrderStatus.Cancelled);

        // Assert
        Assert.Equal("bg-danger", result);
    }

    [Fact]
    public void GetPaymentStatusBadgeClass_Refunded_ReturnsDarkClass()
    {
        // Act
        var result = DetailsModel.GetPaymentStatusBadgeClass(SellerSubOrderStatus.Refunded);

        // Assert
        Assert.Equal("bg-dark", result);
    }

    [Theory]
    [InlineData(SellerSubOrderStatus.Paid)]
    [InlineData(SellerSubOrderStatus.Preparing)]
    [InlineData(SellerSubOrderStatus.Shipped)]
    [InlineData(SellerSubOrderStatus.Delivered)]
    public void GetPaymentStatusBadgeClass_PaidStates_ReturnsSuccessClass(SellerSubOrderStatus status)
    {
        // Act
        var result = DetailsModel.GetPaymentStatusBadgeClass(status);

        // Assert
        Assert.Equal("bg-success", result);
    }

    #endregion

    #region GetPaymentStatusDisplayText Tests

    [Fact]
    public void GetPaymentStatusDisplayText_New_ReturnsPaymentPending()
    {
        // Act
        var result = DetailsModel.GetPaymentStatusDisplayText(SellerSubOrderStatus.New);

        // Assert
        Assert.Equal("Payment Pending", result);
    }

    [Fact]
    public void GetPaymentStatusDisplayText_Cancelled_ReturnsPaymentCancelled()
    {
        // Act
        var result = DetailsModel.GetPaymentStatusDisplayText(SellerSubOrderStatus.Cancelled);

        // Assert
        Assert.Equal("Payment Cancelled", result);
    }

    [Fact]
    public void GetPaymentStatusDisplayText_Refunded_ReturnsRefunded()
    {
        // Act
        var result = DetailsModel.GetPaymentStatusDisplayText(SellerSubOrderStatus.Refunded);

        // Assert
        Assert.Equal("Refunded", result);
    }

    [Theory]
    [InlineData(SellerSubOrderStatus.Paid)]
    [InlineData(SellerSubOrderStatus.Preparing)]
    [InlineData(SellerSubOrderStatus.Shipped)]
    [InlineData(SellerSubOrderStatus.Delivered)]
    public void GetPaymentStatusDisplayText_PaidStates_ReturnsPaid(SellerSubOrderStatus status)
    {
        // Act
        var result = DetailsModel.GetPaymentStatusDisplayText(status);

        // Assert
        Assert.Equal("Paid", result);
    }

    #endregion

    #region Payment Status Abstraction Tests

    [Fact]
    public void PaymentStatus_DoesNotExposeFinancialDetails()
    {
        // This test verifies that payment status is abstracted and doesn't expose sensitive data
        // The payment status should only show "Paid", "Payment Pending", "Payment Cancelled", or "Refunded"

        var allStatuses = Enum.GetValues<SellerSubOrderStatus>();
        var validPaymentStatusTexts = new[] { "Paid", "Payment Pending", "Payment Cancelled", "Refunded" };

        foreach (var status in allStatuses)
        {
            var paymentText = DetailsModel.GetPaymentStatusDisplayText(status);
            Assert.Contains(paymentText, validPaymentStatusTexts);
        }
    }

    #endregion
}
