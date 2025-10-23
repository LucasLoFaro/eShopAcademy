using Domain.Basket.Contracts;
using Domain.Common.Events;
using AutoMapper;


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
