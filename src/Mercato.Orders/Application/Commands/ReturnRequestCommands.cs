using Mercato.Orders.Domain.Entities;

namespace Mercato.Orders.Application.Commands;

/// <summary>
/// Command for creating a new return request.
/// </summary>
public class CreateReturnRequestCommand
{
    /// <summary>
    /// Gets or sets the seller sub-order ID to request a return for.
    /// </summary>
    public Guid SellerSubOrderId { get; set; }

    /// <summary>
    /// Gets or sets the buyer ID who is initiating the return.
    /// </summary>
    public string BuyerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the reason for the return request.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of creating a return request.
/// </summary>
public class CreateReturnRequestResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the ID of the created return request.
    /// </summary>
    public Guid? ReturnRequestId { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="returnRequestId">The ID of the created return request.</param>
    /// <returns>A successful result.</returns>
    public static CreateReturnRequestResult Success(Guid returnRequestId) => new()
    {
        Succeeded = true,
        Errors = [],
        ReturnRequestId = returnRequestId
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CreateReturnRequestResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CreateReturnRequestResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CreateReturnRequestResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to create a return request for this sub-order."]
    };
}

/// <summary>
/// Result of getting a return request.
/// </summary>
public class GetReturnRequestResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets the return request if found.
    /// </summary>
    public ReturnRequest? ReturnRequest { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="returnRequest">The return request.</param>
    /// <returns>A successful result.</returns>
    public static GetReturnRequestResult Success(ReturnRequest returnRequest) => new()
    {
        Succeeded = true,
        Errors = [],
        ReturnRequest = returnRequest
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetReturnRequestResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetReturnRequestResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static GetReturnRequestResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to access this return request."]
    };
}

/// <summary>
/// Command for updating the status of a return request.
/// </summary>
public class UpdateReturnRequestStatusCommand
{
    /// <summary>
    /// Gets or sets the new status for the return request.
    /// </summary>
    public ReturnStatus NewStatus { get; set; }

    /// <summary>
    /// Gets or sets optional notes from the seller.
    /// </summary>
    public string? SellerNotes { get; set; }
}

/// <summary>
/// Result of updating a return request status.
/// </summary>
public class UpdateReturnRequestStatusResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static UpdateReturnRequestStatusResult Success() => new()
    {
        Succeeded = true,
        Errors = []
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static UpdateReturnRequestStatusResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static UpdateReturnRequestStatusResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static UpdateReturnRequestStatusResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to update this return request."]
    };
}

/// <summary>
/// Result of getting return requests for a buyer.
/// </summary>
public class GetReturnRequestsResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets the list of return requests.
    /// </summary>
    public IReadOnlyList<ReturnRequest> ReturnRequests { get; private init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="returnRequests">The return requests.</param>
    /// <returns>A successful result.</returns>
    public static GetReturnRequestsResult Success(IReadOnlyList<ReturnRequest> returnRequests) => new()
    {
        Succeeded = true,
        Errors = [],
        ReturnRequests = returnRequests
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static GetReturnRequestsResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static GetReturnRequestsResult Failure(string error) => Failure([error]);
}

/// <summary>
/// Result of checking if a return can be initiated.
/// </summary>
public class CanInitiateReturnResult
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; private init; }

    /// <summary>
    /// Gets the list of errors if the operation failed.
    /// </summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>
    /// Gets a value indicating whether the user is not authorized.
    /// </summary>
    public bool IsNotAuthorized { get; private init; }

    /// <summary>
    /// Gets a value indicating whether a return can be initiated.
    /// </summary>
    public bool CanInitiate { get; private init; }

    /// <summary>
    /// Gets the reason why a return cannot be initiated (if applicable).
    /// </summary>
    public string? BlockedReason { get; private init; }

    /// <summary>
    /// Creates a successful result indicating return can be initiated.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static CanInitiateReturnResult Yes() => new()
    {
        Succeeded = true,
        Errors = [],
        CanInitiate = true
    };

    /// <summary>
    /// Creates a successful result indicating return cannot be initiated.
    /// </summary>
    /// <param name="reason">The reason why return cannot be initiated.</param>
    /// <returns>A successful result.</returns>
    public static CanInitiateReturnResult No(string reason) => new()
    {
        Succeeded = true,
        Errors = [],
        CanInitiate = false,
        BlockedReason = reason
    };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The list of error messages.</param>
    /// <returns>A failed result.</returns>
    public static CanInitiateReturnResult Failure(IReadOnlyList<string> errors) => new()
    {
        Succeeded = false,
        Errors = errors
    };

    /// <summary>
    /// Creates a failed result with a single error message.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failed result.</returns>
    public static CanInitiateReturnResult Failure(string error) => Failure([error]);

    /// <summary>
    /// Creates a not authorized result.
    /// </summary>
    /// <returns>A not authorized result.</returns>
    public static CanInitiateReturnResult NotAuthorized() => new()
    {
        Succeeded = false,
        IsNotAuthorized = true,
        Errors = ["Not authorized to check return eligibility for this sub-order."]
    };
}
