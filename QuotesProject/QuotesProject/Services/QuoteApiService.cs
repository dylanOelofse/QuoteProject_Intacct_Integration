using System.Globalization;
using QuotesProject.Api;
using QuotesProject.Models;
using QuotesProject.Requests;
using QuotesProject.Responses;

namespace QuotesProject.Services
{
    public class QuoteApiService
    {
        private readonly QuotesApiEngine _engine;
        private const string IntacctDateFormat = "yyyy-MM-dd";

        private readonly string _location;
        private readonly string _warehouse;

        public QuoteApiService(QuotesApiEngine engine, IConfiguration configuration)
        {
            _engine = engine;

            _location = configuration["ApiSettings:Location"] ?? throw new InvalidOperationException("ApiSettings:Location is not configured.");
            _warehouse = configuration["ApiSettings:Warehouse"] ?? throw new InvalidOperationException("ApiSettings:Warehouse is not configured.");
        }

        public async Task<List<Quote>> GetQuotesAsync()
        {
            List<QuoteQueryResponse> rows = await _engine.QueryQuotesAsync();

            List<Quote> quotes = rows.Select(row => new Quote
            {
                Key = row.Key,
                QuoteNumber = row.DocumentNumber,
                CustomerId = row.CustomerId ?? string.Empty,
                CustomerName = row.CustomerName,
                Address = row.ShipToContactId,
                ExternalOrderNumber = row.CustomerPONumber,
                TransactionDate = DateTime.TryParse(row.TxnDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime txnDate) ? txnDate : null,
                DueDate = DateTime.TryParse(row.DueDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dueDate) ? dueDate : null,
                InvoiceDate = DateTime.TryParse(row.InvoiceDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime invoiceDate) ? invoiceDate : null,
                State = row.State,
                Status = row.Status,
                Total = decimal.TryParse(row.Total, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal total) ? total : null
            }).ToList();

            return quotes;
        }

        public async Task<Quote> CreateQuoteAsync(Quote quote)
        {
            if (quote == null)
                throw new ArgumentNullException(nameof(quote));

            if (string.IsNullOrWhiteSpace(quote.CustomerId))
                throw new ArgumentException("Customer ID is required.");

            if (quote.TransactionDate == null)
                throw new ArgumentException("Transaction date is required.");

            if (quote.Lines.Count == 0)
                throw new ArgumentException("At least one quote line is required.");

            foreach (var line in quote.Lines)
            {
                if (string.IsNullOrWhiteSpace(line.ItemId))
                    throw new ArgumentException("Every line needs an Item ID.");

                if (string.IsNullOrWhiteSpace(line.Unit))
                    throw new ArgumentException($"Line for item '{line.ItemId}' needs a unit, e.g. 06Pk.");

                if (line.Quantity <= 0)
                    throw new ArgumentException($"Line for item '{line.ItemId}' needs a quantity greater than 0.");
            }

            var request = new CreateQuoteRequest
            {
                Customer = new ObjectRefRequest { Id = quote.CustomerId },
                TxnDate = quote.TransactionDate.Value.ToString(IntacctDateFormat, CultureInfo.InvariantCulture),
                DueDate = quote.DueDate?.ToString(IntacctDateFormat, CultureInfo.InvariantCulture),
                InvoiceDate = quote.InvoiceDate?.ToString(IntacctDateFormat, CultureInfo.InvariantCulture),
                CustomerPONumber = string.IsNullOrWhiteSpace(quote.ExternalOrderNumber) ? null : quote.ExternalOrderNumber,
                State = string.IsNullOrWhiteSpace(quote.State) ? "draft" : quote.State,
                Lines = quote.Lines.Select(line => new CreateQuoteLineRequest
                {
                    Dimensions = new DimensionsRequest
                    {
                        Item = new ObjectRefRequest { Id = line.ItemId },
                        Warehouse = new ObjectRefRequest { Id = _warehouse },
                        Location = new ObjectRefRequest { Id = _location }
                    },
                    Unit = line.Unit,
                    UnitQuantity = line.Quantity.ToString(CultureInfo.InvariantCulture),
                    DiscountPercent = line.DiscountPercent.ToString(CultureInfo.InvariantCulture)
                }).ToList()
            };

            CreateQuoteResponse created = await _engine.CreateQuoteAsync(request);

            if (string.IsNullOrWhiteSpace(created.Key))
                throw new InvalidOperationException("Sage Intacct did not return a key for the created quote.");

            quote.Key = created.Key;

            return quote;
        }

        public async Task UpdateQuoteAsync(Quote quote)
        {
            if (quote == null)
                throw new ArgumentNullException(nameof(quote));

            if (string.IsNullOrWhiteSpace(quote.Key))
                throw new ArgumentException("A valid quote key is required.");

            var request = new UpdateQuoteRequest
            {
                TxnDate = quote.TransactionDate?.ToString(IntacctDateFormat, CultureInfo.InvariantCulture),
                DueDate = quote.DueDate?.ToString(IntacctDateFormat, CultureInfo.InvariantCulture),
                InvoiceDate = quote.InvoiceDate?.ToString(IntacctDateFormat, CultureInfo.InvariantCulture),
                CustomerPONumber = string.IsNullOrWhiteSpace(quote.ExternalOrderNumber) ? null : quote.ExternalOrderNumber
            };

            await _engine.UpdateQuoteAsync(quote.Key, request);
        }

        public async Task DeleteQuoteAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A valid quote key is required.");

            var request = new UpdateQuoteRequest { State = "closed" };

            await _engine.UpdateQuoteAsync(key, request);
        }
    }
}
