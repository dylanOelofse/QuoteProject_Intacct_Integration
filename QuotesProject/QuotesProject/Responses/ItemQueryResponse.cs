using System.Text.Json.Serialization;

namespace QuotesProject.Responses
{
    public class ItemQueryResponse
    {
        public string? Key { get; set; }

        public string? Id { get; set; }

        [JsonPropertyName("item.key")]
        public string? ItemKey { get; set; }

        [JsonPropertyName("item.id")]
        public string? ItemId { get; set; }

        [JsonPropertyName("item.name")]
        public string? ItemName { get; set; }

        [JsonPropertyName("item.status")]
        public string? ItemStatus { get; set; }

        [JsonPropertyName("warehouse.id")]
        public string? WarehouseId { get; set; }

        public decimal? OnHand { get; set; }
    }
}
