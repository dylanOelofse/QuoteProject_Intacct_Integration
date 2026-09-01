using System.Text.Json.Serialization;

namespace QuotesProject.Responses
{
    public class IntacctResponse<T>
    {
        [JsonPropertyName("ia::result")]
        public T? Result { get; set; }

        [JsonPropertyName("ia::meta")]
        public IntacctMeta? Meta { get; set; }
    }

    public class IntacctMeta
    {
        [JsonPropertyName("totalCount")]
        public int? TotalCount { get; set; }

        [JsonPropertyName("start")]
        public int? Start { get; set; }

        [JsonPropertyName("pageSize")]
        public int? PageSize { get; set; }

        [JsonPropertyName("next")]
        public int? Next { get; set; }

        [JsonPropertyName("previous")]
        public int? Previous { get; set; }

        [JsonPropertyName("totalSuccess")]
        public int? TotalSuccess { get; set; }

        [JsonPropertyName("totalError")]
        public int? TotalError { get; set; }
    }
}
