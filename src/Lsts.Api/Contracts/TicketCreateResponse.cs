namespace Lsts.Api.Contracts;

public sealed class TicketCreateResponse
{
    public long TicketId { get; init; }
    public string ServiceNo { get; init; } = default!;
    public string CurrentStatus { get; init; } = default!;
}
