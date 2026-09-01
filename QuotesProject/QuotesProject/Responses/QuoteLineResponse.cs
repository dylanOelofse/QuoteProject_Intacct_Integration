namespace QuotesProject.Responses
{
    /// <summary>
    /// One document line as returned by GET /objects/order-entry/document::{documentType}/{key}.
    /// Decimal values (quantity, prices) arrive as JSON strings, so they are kept
    /// as strings here and parsed in the service layer.
    ///
    /// WATCH THE PRICE FIELDS. On a line with unit "06Pk", quantity 6, Intacct returns
    /// price "106.00" and unitPrice "636.00" - so Price is the per-unit price and
    /// UnitPrice is the extended amount (quantity x price), the opposite of what the
    /// names suggest. They only agree when the unit of measure is "Each".
    /// </summary>
    public class QuoteLineResponse
    {
        public string? Key { get; set; }
        public string? Id { get; set; }
        public int? LineNumber { get; set; }

        public ObjectRefResponse? Item { get; set; }
        public DimensionsResponse? Dimensions { get; set; }

        /// <summary>"Description" in the UI, e.g. "HORMOBAN APM 100ML".</summary>
        public string? LineDescription { get; set; }

        public string? Unit { get; set; }

        /// <summary>Quantity in base units, e.g. "6.0000000000" for one 06Pk.</summary>
        public string? Quantity { get; set; }

        /// <summary>Quantity in units of measure, e.g. "1.0000000000" for one 06Pk.</summary>
        public string? UnitQuantity { get; set; }

        /// <summary>Per-unit price. This is the one the UI's "Price" column shows.</summary>
        public string? Price { get; set; }

        /// <summary>Extended amount for the line (quantity x price), despite the name.</summary>
        public string? UnitPrice { get; set; }

        public string? DiscountPercent { get; set; }
        public string? Href { get; set; }
    }

    public class DimensionsResponse
    {
        public ObjectRefResponse? Item { get; set; }
        public ObjectRefResponse? Location { get; set; }
        public ObjectRefResponse? Warehouse { get; set; }
    }
}
