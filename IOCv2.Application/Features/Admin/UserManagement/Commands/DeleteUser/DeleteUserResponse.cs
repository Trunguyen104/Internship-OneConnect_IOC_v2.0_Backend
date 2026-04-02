using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser
{
    public class DeleteUserResponse : IMapFrom<User>
    {
        public Guid UserId { get; set; }
        public string UserCode { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<User, DeleteUserResponse>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));
        }
    }
}
