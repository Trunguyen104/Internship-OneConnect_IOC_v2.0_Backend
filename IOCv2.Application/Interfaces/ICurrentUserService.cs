namespace IOCv2.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserCode { get; }
        string? Role { get; }
        string? IpAddress { get; }
    }
}
