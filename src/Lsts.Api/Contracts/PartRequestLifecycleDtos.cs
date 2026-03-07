namespace Lsts.Api.Contracts;

public sealed record PartRequestStatusUpdateRequest(
    string ToStatus,
    long ChangedBy,
    string? Note
);

public sealed record PartRequestStatusUpdateResponse(
    long RequestId,
    string RequestStatus
);

public sealed record PartRequestCancelRequest(
    long CancelledBy,
    string? Note
);

public sealed record PartRequestCloseRequest(
    long ClosedBy,
    string? Note
);
