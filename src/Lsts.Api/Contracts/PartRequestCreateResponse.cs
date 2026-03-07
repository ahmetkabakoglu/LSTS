namespace Lsts.Api.Contracts;

public sealed class PartRequestCreateResponse
{
    public long RequestId { get; init; }
    public string RequestStatus { get; init; } = default!;
}
