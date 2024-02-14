using Core.Domain.DTOs;

namespace Core.Domain.Events
{
    public class ProductUpdatedEvent
    {
        public Guid EventId = new Guid();
        public ProductDTO Product { get; set; }
        public DateTime TriggeredAt = DateTime.Now;
        public ProductEventType EventType { get; set; }
    }
    public enum ProductEventType
    {
        Created,
        Updated,
        Deleted
    }
}
