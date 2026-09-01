namespace QuotesProject.Models
{
    public class Quote
    {
        public string? Key { get; set; }

        public string? QuoteNumber { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string? CustomerName { get; set; }

        public string? Address { get; set; }

        public string? ExternalOrderNumber { get; set; }

        public DateTime? TransactionDate { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? InvoiceDate { get; set; }

        public string? State { get; set; }

        public string? Status { get; set; }

        public decimal? Total { get; set; }

        public List<QuoteLine> Lines { get; set; } = new();
    }
}
