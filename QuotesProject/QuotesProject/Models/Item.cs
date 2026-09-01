namespace QuotesProject.Models
{
    public class Item
    {
        public string? Key { get; set; }

        public string Id { get; set; } = string.Empty;

        public string? Name { get; set; }

        public decimal QuantityOnHand { get; set; }
    }
}
