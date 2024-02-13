using AutoMapper;
using Domain.DTOs;
using Domain.Events;

namespace EventsProcessor
{
    public class EventMapper : Profile
    {
        public EventMapper()
        {
            // Assuming you want to map the Product property of ProductUpdatedEvent to ProductDTO
            CreateMap<ProductUpdatedEvent, ProductDTO>()
                .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.Product.ID))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Product.Price));
        }
    }
}
