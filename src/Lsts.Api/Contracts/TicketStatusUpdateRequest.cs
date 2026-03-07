namespace Lsts.Api.Contracts;

public sealed class TicketStatusUpdateRequest
{
    public string ToStatus { get; init; } = default!;

    public long ChangedBy { get; init; }
}
