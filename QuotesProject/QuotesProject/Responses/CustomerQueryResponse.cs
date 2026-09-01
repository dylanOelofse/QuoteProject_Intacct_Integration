using System.Text.Json.Serialization;

namespace QuotesProject.Responses
{
    public class CustomerQueryResponse
    {
        public string? Key { get; set; }

        public string? Id { get; set; }

        public string? Name { get; set; }

        [JsonPropertyName("contacts.default.id")]
        public string? DefaultContactId { get; set; }

        [JsonPropertyName("contacts.shipTo.id")]
        public string? ShipToContactId { get; set; }
    }
}
