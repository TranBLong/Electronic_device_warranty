using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Warranty.Dtos;

public class QueryParameters
{
    private const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 20;

    /// <summary>Primary search field.</summary>
    public string? Keyword { get; set; }

    /// <summary>Alias for Keyword (compat with older frontend sending ?search=).</summary>
    public string? Search
    {
        get => Keyword;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(Keyword))
                Keyword = value;
        }
    }

    public string? SortBy { get; set; }
    public bool IsDescending { get; set; }

    /// <summary>Alias: sortDir=desc → IsDescending.</summary>
    public string? SortDir
    {
        get => IsDescending ? "desc" : "asc";
        set => IsDescending = string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase);
    }

    public int Skip => Math.Max(0, (Math.Max(1, PageNumber) - 1) * Take);
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);

    public string? NormalizedKeyword =>
        string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim().ToLowerInvariant();
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        var totalPages = pageSize <= 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, pageNumber, pageSize, totalCount, totalPages);
    }
}
