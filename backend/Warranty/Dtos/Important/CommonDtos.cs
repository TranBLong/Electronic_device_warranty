namespace Warranty.Dtos;

/// <summary>Generic paged result for list endpoints.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }

    public int Skip => Math.Max(0, (Math.Max(1, Page) - 1) * Math.Clamp(PageSize, 1, 100));
    public int Take => Math.Clamp(PageSize, 1, 100);
}
