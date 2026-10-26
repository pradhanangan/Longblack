using Longblack.Application.Common.Paging;
using Microsoft.EntityFrameworkCore;

namespace Longblack.Infrastructure.Persistence;

internal static class PagingExtensions
{
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IOrderedQueryable<TSource> query,
        PageRequest? page,
        Func<TSource, TResult> map,
        CancellationToken ct)
    {
        if (page is null)
        {
            var all = await query.ToListAsync(ct);
            return new(all.Select(map).ToList(), all.Count, 1, Math.Max(1, all.Count));
        }

        var total = await query.CountAsync(ct);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(ct);
        return new(items.Select(map).ToList(), total, page.Page, page.PageSize);
    }
}
