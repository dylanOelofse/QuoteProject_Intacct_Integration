namespace QuotesProject.Responses
{
    /// <summary>
    /// A full Order Entry document as returned by
    /// GET /objects/order-entry/document::{documentType}/{key}.
    ///
    /// Unlike the query service (flat rows), the GET endpoint returns nested
    /// objects (customer, contacts, lines...), so this class uses real object
    /// properties. Only the fields this app cares about are mapped - System.Text.Json
    /// ignores the rest of the (very large) document payload.
    /// </summary>
    public class QuoteResponse
    {
        public string? Key { get; set; }
        public string? Id { get; set; }
        public string? DocumentNumber { get; set; }
        public string? DocumentType { get; set; }
        public string? State { get; set; }
        public string? Status { get; set; }
        public string? TxnDate { get; set; }
        public string? DueDate { get; set; }
        public string? InvoiceDate { get; set; }

        /// <summary>"External Order #" in the UI. The customer's PO number.</summary>
        public string? CustomerPONumber { get; set; }

        public string? Subtotal { get; set; }
        public string? Total { get; set; }
        public string? TxnCurrency { get; set; }
        public string? BaseCurrency { get; set; }

        public ObjectRefResponse? Customer { get; set; }

        /// <summary>Primary / ship-to / bill-to contacts. ShipTo drives the "Address" column.</summary>
        public ContactsResponse? Contacts { get; set; }

        public List<QuoteLineResponse> Lines { get; set; } = new();
        public string? Href { get; set; }
    }

    public class ContactsResponse
    {
        public ObjectRefResponse? Primary { get; set; }
        public ObjectRefResponse? ShipTo { get; set; }
        public ObjectRefResponse? BillTo { get; set; }
    }
}
