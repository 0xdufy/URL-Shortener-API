using AutoMapper;
using UrlShortener.Application.Dtos;
using UrlShortener.Domain.Entities;

namespace UrlShortener.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ShortUrl, ShortUrlResponse>();
        CreateMap<ShortUrl, ShortUrlDetailsResponse>();
    }
}
