using Core.Domain.Entities;
using AutoMapper;

namespace Domain.MappingProfiles;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, Product>().ReverseMap();
    }
}