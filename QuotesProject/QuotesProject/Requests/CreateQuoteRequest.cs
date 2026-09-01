namespace QuotesProject.Requests
{
    /// <summary>
    /// Body for POST /objects/order-entry/document::{documentType}
    /// (creates a new Order Entry document - our quote).
    ///
    /// Fields are limited to what the Quotes Manager UI actually collects:
    /// customer, order date, due date and the external order number, plus the
    /// state and lines Intacct needs to accept the document.
    ///
    ///  - txnDate is REQUIRED (format "yyyy-MM-dd").
    ///  - documentNumber is only needed when the transaction definition has NO numbering
    ///    sequence. Our quotes are auto-numbered by Sage, so we leave it out and read the
    ///    generated number back after creation.
    ///  - state must be "draft", "pending" or "submitted" on create (defaults to "pending").
    ///  - Sage will not accept a document without lines.
    /// </summary>
    public class CreateQuoteRequest
    {
        public ObjectRefRequest Customer { get; set; } = new();

        /// <summary>Order Date in the UI. "yyyy-MM-dd".</summary>
        public string TxnDate { get; set; } = string.Empty;

        /// <summary>Due date, "yyyy-MM-dd". Optional.</summary>
        public string? DueDate { get; set; }

        public string? InvoiceDate { get; set; }

        /// <summary>"External Order #" in the UI - the customer's PO number.</summary>
        public string? CustomerPONumber { get; set; }

        /// <summary>"draft" | "pending" | "submitted".</summary>
        public string? State { get; set; }

        public List<CreateQuoteLineRequest> Lines { get; set; } = new();
    }
}
