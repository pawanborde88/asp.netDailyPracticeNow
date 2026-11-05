using AutoMapper;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Models.Domain;

namespace dailypracticeASPDotNet.Mappings
{
    /// <summary>
    /// AutoMapper Profile mapping Domain Models to DTOs and vice-versa.
    /// Demonstrates AutoMapper best practice for clean separation between entities and API DTOs.
    /// </summary>
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            // Map User Domain Model to UserDto
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role != null ? src.Role.Name : string.Empty));

            // Map AddUserRequestDto to User Domain Model
            CreateMap<AddUserRequestDto, User>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore());

            // Map Role Domain Model to RoleDto
            CreateMap<Role, RoleDto>().ReverseMap();
        }
    }
}
