using AutoMapper;
using EFSecondLevelCache.Core.AspNetCoreSample.DataLayer.Entities;
using EFSecondLevelCache.Core.AspNetCoreSample.Models;

namespace EFSecondLevelCache.Core.AspNetCoreSample.Profiles
{
    public class PostProfile : Profile
    {
        public PostProfile()
        {
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => $"{src.User.Name}"))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => $"{src.Title}"));
        }
    }
}