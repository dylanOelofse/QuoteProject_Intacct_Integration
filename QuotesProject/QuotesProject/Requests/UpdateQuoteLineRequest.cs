using System.Text.Json.Serialization;

namespace QuotesProject.Requests
{
    public class UpdateQuoteLineRequest
    {
        // Null on a NEW line. Presence of a key is what tells Intacct to update or
        // delete an existing line rather than add one.
        public string? Key { get; set; }

        public DimensionsRequest? Dimensions { get; set; }

        public string? Unit { get; set; }

        public string? UnitQuantity { get; set; }

        public string? DiscountPercent { get; set; }

        [JsonPropertyName("ia::operation")]
        public string? Operation { get; set; }
    }
}
