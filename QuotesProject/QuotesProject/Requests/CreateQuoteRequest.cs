namespace QuotesProject.Requests
{
    public class CreateQuoteRequest
    {
        public ObjectRefRequest Customer { get; set; } = new();

        public string TxnDate { get; set; } = string.Empty;

        public string? DueDate { get; set; }

        public string? InvoiceDate { get; set; }

        public string? CustomerPONumber { get; set; }

        public string? State { get; set; }

        public List<CreateQuoteLineRequest> Lines { get; set; } = new();
    }
}
