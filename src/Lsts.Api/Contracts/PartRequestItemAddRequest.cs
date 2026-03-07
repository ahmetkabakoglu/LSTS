namespace Lsts.Api.Contracts;

public sealed class PartRequestItemAddRequest
{
    public string PartCode { get; init; } = default!;
    public int Qty { get; init; }

    public long RequestedBy { get; init; }
}
