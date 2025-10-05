using AutoMapper;
using Core.Domain.Contracts;
using Core.Domain.Events;
using Domain.DTOs;

namespace EventsProcessor
{
    public class EventMapper : Profile
    {
        public EventMapper()
        {
            CreateMap<ProductUpdatedEvent, ProductDTO>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.Product.ID))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product.Price));

            CreateMap<StockUpdatedEvent, AlterStockDTO>()
                .ForMember(dest => dest.ProductGuid, opt => opt.MapFrom(src => src.Stock.ProductGuid))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Stock.Quantity));
        }
    }
}
