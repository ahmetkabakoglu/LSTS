namespace Lsts.Api.Contracts;

public sealed class TicketDashboardDto
{
    public required TicketDto Ticket { get; set; }
    public required List<PartRequestDto> PartRequests { get; set; }
    public required List<UsageSummaryDto> UsageSummary { get; set; }
}

public sealed class TicketDto
{
    public long TicketId { get; set; }
    public string ServiceNo { get; set; } = "";
    public long DeviceId { get; set; }

    public string CurrentStatus { get; set; } = "";
    public DateTimeOffset? StatusUpdatedAt { get; set; }

    public string FaultDesc { get; set; } = "";
    public string? InternalNotes { get; set; }
    public string? PublicNote { get; set; }

    public DateTime? EstimatedDeliveryDate { get; set; }
}

public sealed class PartRequestDto
{
    public long RequestId { get; set; }
    public string RequestStatus { get; set; } = "";
    public string? Note { get; set; }

    public long RequestedBy { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }

    public long CreatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }

    public List<PartRequestItemDto> Items { get; set; } = new();
}

public sealed class PartRequestItemDto
{
    public long ItemId { get; set; }
    public long RequestId { get; set; }

    public long PartId { get; set; }
    public string PartCode { get; set; } = "";

    public int QtyRequested { get; set; }
    public int QtyIssued { get; set; }

    public string? Note { get; set; }

    public long RequestedBy { get; set; }
    public DateTimeOffset? RequestedAt { get; set; }

    public long CreatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

public sealed class UsageSummaryDto
{
    public long PartId { get; set; }
    public string PartCode { get; set; } = "";

    public int TotalQty { get; set; }
    public decimal TotalAmount { get; set; }
}
