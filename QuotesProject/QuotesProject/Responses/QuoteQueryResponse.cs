using System.Text.Json.Serialization;

namespace QuotesProject.Responses
{
    public class QuoteQueryResponse
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("documentNumber")]
        public string? DocumentNumber { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("txnDate")]
        public string? TxnDate { get; set; }

        [JsonPropertyName("dueDate")]
        public string? DueDate { get; set; }

        [JsonPropertyName("invoiceDate")]
        public string? InvoiceDate { get; set; }

        [JsonPropertyName("customerPONumber")]          //External Order No
        public string? CustomerPONumber { get; set; }

        [JsonPropertyName("total")]
        public string? Total { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("customer.id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("customer.name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("contacts.shipTo.id")]           //Address
        public string? ShipToContactId { get; set; }
    }
}
