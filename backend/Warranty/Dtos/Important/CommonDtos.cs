namespace Warranty.Dtos;

// Canonical PagedResult lives in Dtos/Common/QueryParameters.cs (PageNumber, PageSize, ...).
// Legacy helper kept for older query shapes.

public sealed class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }

    public int Skip => Math.Max(0, (Math.Max(1, Page) - 1) * Math.Clamp(PageSize, 1, 100));
    public int Take => Math.Clamp(PageSize, 1, 100);
}
