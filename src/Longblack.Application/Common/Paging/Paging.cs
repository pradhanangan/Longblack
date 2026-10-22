namespace Longblack.Application.Common.Paging;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    // Invalid or missing values fall back to defaults; oversized pages are capped rather than rejected.
    public static PageRequest Create(int? page, int? pageSize) =>
        new(
            page is > 0 ? page.Value : 1,
            pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize);

    public int Skip => (int)Math.Min((long)(Page - 1) * PageSize, int.MaxValue);
}
