namespace QuotesProject.Models
{
    public class QuoteLine
    {
        public string? Key { get; set; }

        public string? QuoteKey { get; set; }

        public string ItemId { get; set; } = string.Empty;

        public string? ItemName { get; set; }

        public string? Description { get; set; }

        public string Unit { get; set; } = "Each";

        public decimal Quantity { get; set; }

        public decimal UnitQuantity { get; set; }
        public decimal UnitPrice { get; set; }

        public decimal DiscountPercent { get; set; }
    }
}
