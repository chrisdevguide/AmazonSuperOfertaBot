using AmazonApi.Models;
using AmazonSuperOfertaBot.Dtos;
using AutoMapper;

namespace AmazonSuperOfertaBot.Models
{
    public class Profiles : Profile
    {
        public Profiles()
        {
            CreateMap<ScrapeCategoriesRequestDto, ScrapeRequestDto>();
            CreateMap<AmazonProduct, AmazonProductTelegram>()
                .ForMember(x => x.LastPrice, opt => opt.MapFrom(x => x.CurrentPrice));
        }
    }
}
