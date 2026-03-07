namespace Lsts.Api.Contracts;

public sealed class PartRequestCreateRequest
{
    public long TicketId { get; init; }
    public string? Note { get; init; }

    public long RequestedBy { get; init; }
}
