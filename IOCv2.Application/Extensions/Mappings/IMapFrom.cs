using AutoMapper;

namespace IOCv2.Application.Extensions.Mappings
{
    public interface IMapFrom<T>
    {
        // Có một phương thức mặc định để cấu hình mapping. 
        // Nếu cần cấu hình phức tạp (như ForMember), DTO có thể ghi đè phương thức này.
        void Mapping(Profile profile) => profile.CreateMap(typeof(T), GetType()).ReverseMap();
    }
}
