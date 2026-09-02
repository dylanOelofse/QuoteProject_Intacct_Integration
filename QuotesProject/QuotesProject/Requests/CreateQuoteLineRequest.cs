namespace QuotesProject.Requests
{
    public class CreateQuoteLineRequest
    {
        public ObjectRefRequest? DocumentHeader { get; set; }

        public DimensionsRequest Dimensions { get; set; } = new();

        public string Unit { get; set; } = string.Empty;

        public string UnitQuantity { get; set; } = string.Empty;

        public string? DiscountPercent { get; set; }
    }
}
