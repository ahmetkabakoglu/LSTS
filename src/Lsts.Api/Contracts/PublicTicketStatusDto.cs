namespace Lsts.Api.Contracts;

public sealed class PublicTicketStatusDto
{
    public string ServiceNo { get; init; } = default!;
    public string CurrentStatus { get; init; } = default!;
    public DateTimeOffset StatusUpdatedAt { get; init; }

    public DateTime? EstimatedDeliveryDate { get; init; }
    public string? PublicNote { get; init; }
}
