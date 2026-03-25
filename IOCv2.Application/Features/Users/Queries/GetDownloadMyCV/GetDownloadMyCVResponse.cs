using System.IO;

namespace IOCv2.Application.Features.Users.Queries.GetDownloadMyCV
{
    public class GetDownloadMyCVResponse
    {
        public Stream Content { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public string FileName { get; set; } = null!;
    }
}
