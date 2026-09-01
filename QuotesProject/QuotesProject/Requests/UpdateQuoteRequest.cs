using System.Text.Json.Serialization;

namespace QuotesProject.Requests
{
    public class UpdateQuoteRequest
    {
        public string? TxnDate { get; set; }

        public string? DueDate { get; set; }

        public string? InvoiceDate { get; set; }

        public string? CustomerPONumber { get; set; }

        public string? Status { get; set; }

        [JsonPropertyName("nsp::CUSTOMER_UDFS")]
        public bool CustomerUdfs { get; set; } = true;

        public List<UpdateQuoteLineRequest>? Lines { get; set; }
    }
}
