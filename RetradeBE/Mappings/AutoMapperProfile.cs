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
        }
    }
}
