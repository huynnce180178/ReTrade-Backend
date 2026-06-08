using AutoMapper;
using RetradeBE.Models;
using RetradeBE.Models.DTOs;

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

            // User -> UserProfileDto
            CreateMap<User, UserProfileDto>();

            // Account -> UserProfileDto
            CreateMap<Account, UserProfileDto>();

            // Category Mappings
            // Category -> CategoryResponseDto
            CreateMap<Category, CategoryResponseDto>()
                .ForMember(dest => dest.Attributes, opt => opt.MapFrom(src => src.Attributes));

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
        }
    }
}
