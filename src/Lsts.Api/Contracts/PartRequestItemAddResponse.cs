namespace Lsts.Api.Contracts;

public sealed class PartRequestItemAddResponse
{
    public long RequestId { get; init; }
    public long ItemId { get; init; }
    public string PartCode { get; init; } = default!;
    public int Qty { get; init; }
}
