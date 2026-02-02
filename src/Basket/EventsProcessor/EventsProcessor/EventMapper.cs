using AutoMapper;
using Common.Domain.Events.Products;
using Common.Domain.Events.Stock;
using Domain.Basket.Contracts;


namespace EventsProcessor;

public class EventMapper : Profile
{
    public EventMapper()
    {
        CreateMap<ProductUpdatedEvent, ProductDTO>()
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price));

        CreateMap<StockUpdatedEvent, AlterStockDTO>()
            .ForMember(dest => dest.ProductGuid, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));
    }
}
