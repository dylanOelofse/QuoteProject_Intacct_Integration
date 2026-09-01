using System.Globalization;
using System.Net;
using QuotesProject.Api;
using QuotesProject.Models;
using QuotesProject.Requests;
using QuotesProject.Responses;

namespace QuotesProject.Services
{
    public class QuoteLineApiService
    {
        private readonly QuotesApiEngine _engine;

        public QuoteLineApiService(QuotesApiEngine engine)
        {
            _engine = engine;
        }

        public async Task<Quote> GetQuoteWithLinesAsync(string quoteKey)
        {
            if (string.IsNullOrWhiteSpace(quoteKey))
                throw new ArgumentException("A valid quote key is required.");

            List<QuoteLineQueryResponse> rows = await _engine.QueryQuoteLinesAsync(quoteKey);

            if (rows.Count == 0)
                throw new HttpRequestException("Quote not found, or it has no lines.", null, HttpStatusCode.NotFound);

            QuoteLineQueryResponse first = rows[0];

            var quote = new Quote
            {
                Key = first.QuoteKey,
                QuoteNumber = first.QuoteNumber,
                CustomerId = first.CustomerId ?? string.Empty,
                CustomerName = first.CustomerName,
                Address = first.Address,
                ExternalOrderNumber = first.ExternalOrderNumber
            };

            quote.Lines = rows.Select(line => new QuoteLine
            {
                Key = line.Key,
                QuoteKey = first.QuoteKey,
                ItemId = line.ItemId ?? string.Empty,
                Description = line.ItemName,
                Quantity = decimal.TryParse(line.Quantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal quantity) ? quantity : 0,
                UnitQuantity = decimal.TryParse(line.UnitQuantity, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitQuantity) ? unitQuantity : 0,
                Unit = line.Unit ?? "Each",
                UnitPrice = decimal.TryParse(line.UnitPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal unitPrice) ? unitPrice : 0,
                DiscountPercent = decimal.TryParse(line.DiscountPercent, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal discount) ? discount : 0
            }).ToList();

            return quote;
        }

        // Lines are edited through the DOCUMENT endpoint, not the line endpoint. A PATCH
        // on order-entry/document-line rewrites the parent anyway and trips the
        // CUSTOMER_UDF smart rule, which can only be satisfied from the document body.
        public async Task UpdateQuoteLineAsync(QuoteLine line)
        {
            if (line == null)
                throw new ArgumentNullException(nameof(line));

            if (string.IsNullOrWhiteSpace(line.QuoteKey))
                throw new ArgumentException("A valid quote key is required.");

            if (string.IsNullOrWhiteSpace(line.Key))
                throw new ArgumentException("A valid line key is required.");

            if (string.IsNullOrWhiteSpace(line.ItemId))
                throw new ArgumentException("Item ID is required.");

            if (line.UnitQuantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0.");

            var request = new UpdateQuoteRequest
            {
                Lines = new List<UpdateQuoteLineRequest>
                {
                    new UpdateQuoteLineRequest
                    {
                        Key = line.Key,
                        // dimensions.item, not item - the line-level item is read-only
                        // on update (REST-1050). Warehouse and location are not required
                        // here, unlike on create.
                        Dimensions = new DimensionsRequest
                        {
                            Item = new ObjectRefRequest { Id = line.ItemId }
                        },
                        Unit = line.Unit,
                        UnitQuantity = line.UnitQuantity.ToString(CultureInfo.InvariantCulture),
                        DiscountPercent = line.DiscountPercent.ToString(CultureInfo.InvariantCulture)
                    }
                }
            };

            await _engine.UpdateQuoteAsync(line.QuoteKey, request);
        }

        public async Task DeleteQuoteLineAsync(string quoteKey, string lineKey)
        {
            if (string.IsNullOrWhiteSpace(quoteKey))
                throw new ArgumentException("A valid quote key is required.");

            if (string.IsNullOrWhiteSpace(lineKey))
                throw new ArgumentException("A valid line key is required.");

            var request = new UpdateQuoteRequest
            {
                Lines = new List<UpdateQuoteLineRequest>
                {
                    new UpdateQuoteLineRequest
                    {
                        Key = lineKey,
                        Operation = "delete"
                    }
                }
            };

            await _engine.UpdateQuoteAsync(quoteKey, request);
        }
    }
}
