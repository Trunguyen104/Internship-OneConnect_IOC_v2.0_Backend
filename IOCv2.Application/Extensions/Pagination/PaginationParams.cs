namespace IOCv2.Application.Extensions.Pagination
{
    public class PaginationParams
    {
        private const int MaxPageSize = 50;
        public int PageIndex { get; set; } = 1;
        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }
        public string? Search { get; set; }
        public string? OrderBy { get; set; }
        public List<string>? Filters { get; set; }
    }
}
