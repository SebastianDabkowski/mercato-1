using Mercato.Web.Pages.Orders;

namespace Mercato.Tests.Orders;

/// <summary>
/// Unit tests for the order details page model.
/// </summary>
public class OrderDetailsModelTests
{
    #region GetCarrierTrackingUrl Tests

    [Fact]
    public void GetCarrierTrackingUrl_UPS_ReturnsCorrectUrl()
    {
        // Arrange
        var carrier = "UPS";
        var trackingNumber = "1Z999AA10123456784";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("ups.com/track", result);
        Assert.Contains("loc=en_US", result);
        Assert.Contains(trackingNumber, result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_FedEx_ReturnsCorrectUrl()
    {
        // Arrange
        var carrier = "FedEx";
        var trackingNumber = "123456789012";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("fedex.com/fedextrack", result);
        Assert.Contains("tracknumbers=", result);
        Assert.Contains(trackingNumber, result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_USPS_ReturnsCorrectUrl()
    {
        // Arrange
        var carrier = "USPS";
        var trackingNumber = "9400111899223456789012";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("usps.com/go/TrackConfirmAction", result);
        Assert.Contains(trackingNumber, result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_DHL_ReturnsCorrectUrl()
    {
        // Arrange
        var carrier = "DHL";
        var trackingNumber = "1234567890";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("dhl.com", result);
        Assert.Contains(trackingNumber, result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_CaseInsensitive_ReturnsCorrectUrl()
    {
        // Arrange
        var carrier = "ups";
        var trackingNumber = "1Z999AA10123456784";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("ups.com/track", result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_UnknownCarrier_ReturnsNull()
    {
        // Arrange
        var carrier = "UnknownCarrier";
        var trackingNumber = "123456";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_NullCarrier_ReturnsNull()
    {
        // Arrange
        string? carrier = null;
        var trackingNumber = "123456";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_EmptyCarrier_ReturnsNull()
    {
        // Arrange
        var carrier = "";
        var trackingNumber = "123456";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_NullTrackingNumber_ReturnsNull()
    {
        // Arrange
        var carrier = "UPS";
        string? trackingNumber = null;

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_EmptyTrackingNumber_ReturnsNull()
    {
        // Arrange
        var carrier = "UPS";
        var trackingNumber = "";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCarrierTrackingUrl_TrackingNumberWithSpecialChars_EncodesUrl()
    {
        // Arrange
        var carrier = "UPS";
        var trackingNumber = "1Z999&AA10123456784";

        // Act
        var result = DetailsModel.GetCarrierTrackingUrl(carrier, trackingNumber);

        // Assert
        Assert.NotNull(result);
        // The & should be URL encoded as %26
        Assert.Contains("%26", result);
    }

    #endregion
}
