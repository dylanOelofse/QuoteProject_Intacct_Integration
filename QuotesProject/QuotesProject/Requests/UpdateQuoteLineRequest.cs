using System.Text.Json.Serialization;

namespace QuotesProject.Requests
{
    public class UpdateQuoteLineRequest
    {
        public string? Key { get; set; }

        public DimensionsRequest? Dimensions { get; set; }

        public string? Unit { get; set; }

        public string? UnitQuantity { get; set; }

        public string? DiscountPercent { get; set; }

        [JsonPropertyName("ia::operation")]
        public string? Operation { get; set; }
    }
}
