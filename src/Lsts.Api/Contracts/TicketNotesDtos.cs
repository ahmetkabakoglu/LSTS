namespace Lsts.Api.Contracts;

public sealed record TicketNotesUpdateRequest(
    string? InternalNotes,
    string? PublicNote,
    long UpdatedBy
);

public sealed record TicketNotesUpdateResponse(
    long TicketId,
    string? InternalNotes,
    string? PublicNote
);