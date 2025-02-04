namespace Core.Domain.Entities
{
    public class BasketWithDetails
    {
        public Guid ClientID { get; set; }
        public List<ItemWithDetails> Items { get; set; } = new();
    }
}
