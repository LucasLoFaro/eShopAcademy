using AutoMapper;

namespace Domain.Product.MappingProfiles;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Entities.Product, Entities.Product>().ReverseMap();
    }
}