using Microsoft.EntityFrameworkCore;

namespace ERP.StockService.Infrastructure.Persistence
{
    internal static class PaginationHelper
    {
        internal static async Task<(List<T> Items, int TotalCount)> ToPagedResultAsync<T>(
            IQueryable<T> query,
            int pageNumber,
            int pageSize,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy)
        {
            int totalCount = await query.CountAsync();
            List<T> items = await orderBy(query)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}