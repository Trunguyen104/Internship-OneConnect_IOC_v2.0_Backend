namespace IOCv2.Application.Common.Models
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; private set; }
        public int PageNumber { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PaginatedResult(List<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            TotalCount = count;
            Items = items;
        }

        public static PaginatedResult<T> Create(List<T> items, int count, int pageNumber, int pageSize)
        {
            return new PaginatedResult<T>(items, count, pageNumber, pageSize);
        }
    }
}
