using AutoMapper;

namespace Domain.Products.MappingProfiles;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Entities.Product, Entities.Product>().ReverseMap();
    }
}