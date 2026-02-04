using IOCv2.Application.Extensions.Pagination;
using Microsoft.EntityFrameworkCore;

using System.Linq.Expressions;

namespace IOCv2.Application.Extensions.Query
{
    public static class QueryableExtension
    {
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            PaginationParams pagination)
        {
            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                .Take(pagination.PageSize)
                .ToListAsync();

            return new PagedResult<T>(items, pagination, totalCount);
        }

        public static IQueryable<T> ApplyGlobalSearch<T>(
            this IQueryable<T> query,
            string? search,
            List<Expression<Func<T, string?>>> searchableFields)
        {
            if (string.IsNullOrWhiteSpace(search))
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? combinedExpression = null;

            var searchLower = search.ToLower();
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });

            foreach (var field in searchableFields)
            {
                var memberAccess = Expression.Invoke(field, parameter);
                var toLowerCall = Expression.Call(memberAccess, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                var containsCall = Expression.Call(toLowerCall, containsMethod!, Expression.Constant(searchLower));

                combinedExpression = combinedExpression == null
                    ? containsCall
                    : Expression.OrElse(combinedExpression, containsCall);
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        public static IQueryable<T> ApplyFilters<T>(
            this IQueryable<T> query,
            List<string>? filters,
            Dictionary<string, Expression<Func<T, object?>>> filterMapping)
        {
            if (filters == null || !filters.Any())
                return query;

            foreach (var filter in filters)
            {
                var parts = filter.Split(':', 2);
                if (parts.Length != 2) continue;

                var key = parts[0].ToLower();
                var value = parts[1];

                if (filterMapping.TryGetValue(key, out var propertySelector))
                {
                    var parameter = Expression.Parameter(typeof(T), "x");
                    var memberAccess = Expression.Invoke(propertySelector, parameter);
                    var constant = Expression.Constant(value);

                    var body = Expression.Equal(Expression.Convert(memberAccess, typeof(string)), constant);
                    var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);
                    query = query.Where(lambda);
                }
            }

            return query;
        }

        public static IQueryable<T> ApplySorting<T>(
            this IQueryable<T> query,
            string? orderBy,
            Dictionary<string, Expression<Func<T, object?>>> mapping,
            Expression<Func<T, object?>> defaultSort)
        {
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return query.OrderBy(defaultSort);
            }

            var sortItems = orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var item in sortItems)
            {
                var trimmedItem = item.Trim();
                var isDescending = trimmedItem.StartsWith("-");
                var propertyName = isDescending ? trimmedItem.Substring(1).ToLower() : trimmedItem.ToLower();

                if (mapping.TryGetValue(propertyName, out var selectedSort))
                {
                    if (orderedQuery == null)
                    {
                        orderedQuery = isDescending
                            ? query.OrderByDescending(selectedSort)
                            : query.OrderBy(selectedSort);
                    }
                    else
                    {
                        orderedQuery = isDescending
                            ? orderedQuery.ThenByDescending(selectedSort)
                            : orderedQuery.ThenBy(selectedSort);
                    }
                }
            }

            return orderedQuery ?? query.OrderBy(defaultSort);
        }
    }
}
