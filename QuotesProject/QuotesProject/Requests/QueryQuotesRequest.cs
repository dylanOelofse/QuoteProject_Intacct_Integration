using System.Text.Json.Serialization;

namespace QuotesProject.Requests
{
    public class QueryQuotesRequest
    {
        [JsonPropertyName("object")]
        public string ObjectName { get; set; } = string.Empty;

        public List<string> Fields { get; set; } = new();

        public List<Dictionary<string, Dictionary<string, string>>>? Filters { get; set; }

        public string? FilterExpression { get; set; }

        public List<Dictionary<string, string>>? OrderBy { get; set; }

        public int? Start { get; set; }

        public int? Size { get; set; }
    }
}
