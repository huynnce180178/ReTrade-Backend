using AutoMapper;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;
using RetradeBE.Models.DTOs.Admin;
using System.Linq;

namespace RetradeBE.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // RegisterDto -> User
            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => "https://res.cloudinary.com/dx0hrokek/image/upload/v1780673207/avt-emty_wwnzba.jpg"))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

            // RegisterDto -> Account
            CreateMap<RegisterDto, Account>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => RetradeBE.Models.Enums.AccountStatusEnum.Pending.ToString()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()); // Will be hashed manually in service


            // Category Mappings
            // Category -> CategoryResponseDto
            CreateMap<Category, CategoryResponseDto>()
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.Attributes.Where(a => a.IsDeleted != true)))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.CategoryImage.OrderBy(ci => ci.CreatedAt).Select(ci => ci.Image.ImageUrl).FirstOrDefault()));

            // Account -> UserListDto (admin user list)
            CreateMap<Account, UserListDto>()
                .ForMember(dest => dest.PrimaryRole, opt => opt.MapFrom(src =>
                    src.AccountRole
                        .OrderBy(ar => ar.CreatedAt)
                        .Select(ar => ar.Role != null ? ar.Role.Name : null)
                        .FirstOrDefault()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User != null ? src.User.Email : null))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User != null ? src.User.FirstName : null))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User != null ? src.User.LastName : null))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.User != null ? src.User.Phone : null))
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null));

            // Attributes -> AttributeDto
            CreateMap<Attributes, AttributeDto>();

            // AttributeCreateDto -> Attributes
            CreateMap<AttributeCreateDto, Attributes>()
                .ForMember(dest => dest.AttributeId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

            // AttributeUpdateDto -> Attributes
            CreateMap<AttributeUpdateDto, Attributes>()
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
            //Attribute -> AttributeDTO
            CreateMap<Role, RoleDto>();
        }
    }
}
