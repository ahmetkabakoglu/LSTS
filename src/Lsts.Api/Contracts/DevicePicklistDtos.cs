namespace Lsts.Api.Contracts;

public sealed record DevicePicklistItemDto(
    long DeviceId,
    string SerialNo,
    long CustomerId,
    string DeviceType,
    long ModelId,
    string ModelCode,
    string ModelName,
    string ModelNo,
    string? Cpu,
    string? Gpu,
    string? RamSummary,
    string? ScreenSummary,
    long BrandId,
    string BrandCode
);