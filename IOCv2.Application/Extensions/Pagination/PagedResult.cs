namespace IOCv2.Application.Extensions.Pagination
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasNext => PageIndex < TotalPages;
        public bool HasPrevious => PageIndex > 1;
        public PagedResult(
            IReadOnlyList<T> items,
            PaginationParams pagination,
            int totalCount
            )
        {
            Items = items;
            TotalCount = totalCount;
            PageIndex = pagination.PageIndex;
            PageSize = pagination.PageSize;
        }
    }
}
