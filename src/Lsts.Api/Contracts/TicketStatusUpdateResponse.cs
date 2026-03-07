namespace Lsts.Api.Contracts;

public sealed class TicketStatusUpdateResponse
{
    public long TicketId { get; init; }
    public string ServiceNo { get; init; } = default!;
    public string CurrentStatus { get; init; } = default!;
}
