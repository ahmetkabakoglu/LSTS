namespace Lsts.Api.Contracts;

public sealed class TicketCreateRequest
{
    public long DeviceId { get; init; }

    public string FaultDesc { get; init; } = default!;
    public string? InternalNotes { get; init; }
    public string? PublicNote { get; init; }

    public DateTime? EstimatedDeliveryDate { get; init; }

    public long CreatedBy { get; init; }
}
