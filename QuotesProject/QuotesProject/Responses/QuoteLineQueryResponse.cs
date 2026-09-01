using System.Text.Json.Serialization;

namespace QuotesProject.Responses
{
    public class QuoteLineQueryResponse
    {
        public string? Key { get; set; }

        /// <summary>Bare item code, e.g. "20523". The document GET returns the composite "20523--HORMOBAN APM 100ML".</summary>
        [JsonPropertyName("item.id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("item.name")]
        public string? ItemName { get; set; }

        public string? Quantity { get; set; }

        public string? UnitQuantity { get; set; }

        public string? Unit { get; set; }

        public string? Price { get; set; }

        public string? UnitPrice { get; set; }

        public string? DiscountPercent { get; set; }

        [JsonPropertyName("documentHeader.key")]
        public string? QuoteKey { get; set; }

        [JsonPropertyName("documentHeader.documentNumber")]
        public string? QuoteNumber { get; set; }

        [JsonPropertyName("documentHeader.customerPONumber")]
        public string? ExternalOrderNumber { get; set; }

        [JsonPropertyName("documentHeader.customer.id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("documentHeader.customer.name")]
        public string? CustomerName { get; set; }

        [JsonPropertyName("documentHeader.contacts.shipTo.id")]
        public string? Address { get; set; }
    }
}
