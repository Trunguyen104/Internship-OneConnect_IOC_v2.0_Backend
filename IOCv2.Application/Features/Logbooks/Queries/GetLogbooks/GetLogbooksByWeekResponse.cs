namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public class GetLogbooksByWeekResponse
    {
        public Guid InternshipId { get; set; }
            public DateTime? RangeStartDate { get; set; }
            public DateTime? RangeEndDate { get; set; }
            public List<int> SelectedWeeks { get; set; } = new();
            public int SubmittedCount { get; set; }
            public int TotalCount { get; set; }
            public string Overview { get; set; } = string.Empty;
        public List<LogbookWeekGroupResponse> Weeks { get; set; } = new();
    }

    public class LogbookWeekGroupResponse
    {
            public int WeekNumber { get; set; }
            public string WeekLabel { get; set; } = string.Empty;
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
            public string WeekRangeText { get; set; } = string.Empty;
            public int SubmittedCount { get; set; }
            public int TotalCount { get; set; }
            public string CompletionText { get; set; } = string.Empty;
        public List<GetLogbooksResponse> Items { get; set; } = new();
    }
}




