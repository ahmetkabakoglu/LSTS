public sealed record PartListItemDto(
    long PartId,
    string PartCode,
    string PartName,
    int OnHandQty,
    decimal UnitPrice
);

public sealed record PartGetDto(
    long PartId,
    string PartCode,
    string PartName,
    int OnHandQty,
    decimal UnitPrice
);