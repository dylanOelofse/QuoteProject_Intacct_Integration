using Newtonsoft.Json.Linq;
using QuotesProject.Requests;
using QuotesProject.Responses;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuotesProject.Api
{
    public class QuotesApiEngine
    {
        private readonly HttpClient _http;
        private readonly AuthEngine _auth;
        private readonly string _warehouse;

        public const string QuoteDocumentType = "18-Sales Quotation";

        public QuotesApiEngine(HttpClient http, AuthEngine auth, IConfiguration configuration)
        {
            _http = http;
            _auth = auth;

            _http.DefaultRequestHeaders.Add("Accept", "application/json");
            _http.DefaultRequestHeaders.Add("X-IA-API-Param-Entity", configuration["ApiSettings:Entity"] ?? throw new InvalidOperationException("ApiSettings:Entity is not configured."));

            _warehouse = configuration["ApiSettings:Warehouse"] ?? throw new InvalidOperationException("ApiSettings:Warehouse is not configured.");
        }

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        //Alternative approach to QueryQuotesAsync, using a request object.

        //public async Task<List<QuoteQueryResponse>> QueryQuotesAsync()
        //{
        //    List<QuoteQueryResponse> allQuotes = new();

        //    int start = 1;

        //    while (true)
        //    {
        //        var request = new QueryQuotesRequest
        //        {
        //            ObjectName = "order-entry/document",
        //            Fields = new List<string>
        //            {
        //                "key",
        //                "id",
        //                "documentNumber",
        //                "state",
        //                "status",
        //                "txnDate",
        //                "dueDate",
        //                "invoiceDate",
        //                "customerPONumber",                     // The customer's PO, shown as "External Order #". NOT referenceNumber,
        //                "contacts.shipTo.id",                   // Ship-to contact, shown as "Address".
        //                "customer.id",
        //                "customer.name",
        //                "total",
        //                "href"
        //            },
        //            Filters = new List<Dictionary<string, Dictionary<string, string>>>
        //            {
        //                new() { ["$eq"] = new() { ["documentType"] = QuoteDocumentType } },
        //                new() { ["$eq"] = new() { ["state"] = "pending" } },
        //                new() { ["$eq"] = new() { ["status"] = "active" } }
        //            },
        //            OrderBy = new List<Dictionary<string, string>>
        //            {
        //                new() { ["key"] = "desc" }
        //            },
        //            Size = 500,
        //            Start = start
        //        };

        //        //string token = await _auth.GetValidTokenAsync();    Leave these comments here
        //        //_http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        //        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _auth.GetValidTokenAsync());
        //        HttpResponseMessage response = await _http.PostAsJsonAsync("https://api.intacct.com/ia/api/v1/services/core/query", request, JsonOptions);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            string error = await response.Content.ReadAsStringAsync();
        //            throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
        //        }

        //        var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<List<QuoteQueryResponse>>>(JsonOptions);

        //        if (envelope?.Result != null)
        //        {
        //            allQuotes.AddRange(envelope.Result);
        //        }

        //        int? next = envelope?.Meta?.Next;

        //        if (next == null || next.Value <= start)
        //        {
        //            break;
        //        }

        //        start = next.Value;
        //    }

        //    return allQuotes;
        //}


        public async Task<List<QuoteQueryResponse>> QueryQuotesAsync()
        {
            List<QuoteQueryResponse> allQuotes = new();

            string token = await _auth.GetValidTokenAsync();
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            int start = 1;

            while (true)
            {
                string jsonBody = $$"""
                 {
                   "object": "order-entry/document",
                   "fields": [
                     "key",
                     "id",
                     "documentNumber",
                     "state",
                     "status",
                     "txnDate",
                     "dueDate",
                     "invoiceDate",
                     "customerPONumber",
                     "contacts.shipTo.id",
                     "customer.id",
                     "customer.name",
                     "total",
                     "href"
                   ],
                   "filters": [
                     { "$eq": { "documentType": "18-Sales Quotation" } },
                     { "$eq": { "state": "pending" } },
                     { "$eq": { "status": "active" } }
                   ],
                   "orderBy": [ { "key": "desc" } ],
                   "size": 500,
                   "start": {{start}}
                 }
                 """;

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _http.PostAsync("https://api.intacct.com/ia/api/v1/services/core/query", content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
                }

                var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<List<QuoteQueryResponse>>>(JsonOptions);

                if (envelope?.Result != null)
                {
                    allQuotes.AddRange(envelope.Result);
                }

                int? next = envelope?.Meta?.Next;

                if (next == null || next.Value <= start)
                {
                    break;
                }

                start = next.Value;
            }

            return allQuotes;
        }

        public async Task<List<CustomerQueryResponse>> QueryCustomersAsync()
        {
            List<CustomerQueryResponse> allCustomers = new();

            int start = 1;

            string token = await _auth.GetValidTokenAsync();
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            while (true)
            {
                string jsonBody = $$"""
                 {
                   "object": "accounts-receivable/customer",
                   "fields": [
                     "key",
                     "id",
                     "name",
                     "contacts.default.id",
                     "contacts.shipTo.id"
                   ],
                   "filters": [
                     { "$eq": { "status": "active" } }
                   ],
                   "filterParameters": {
                     "caseSensitiveComparison": true,
                     "includePrivate": true
                   },
                   "orderBy": [ { "key": "desc" } ],
                   "size": 4000,
                   "start": {{start}}
                 }
                 """;

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _http.PostAsync("https://api.intacct.com/ia/api/v1/services/core/query", content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
                }

                var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<List<CustomerQueryResponse>>>(JsonOptions);

                if (envelope?.Result != null)
                {
                    allCustomers.AddRange(envelope.Result);
                }

                int? next = envelope?.Meta?.Next;

                if (next == null || next.Value <= start)
                {
                    break;
                }

                start = next.Value;
            }

            return allCustomers;
        }

        public async Task<List<ItemQueryResponse>> QueryItemsByWarehouseAsync()
        {
            List<ItemQueryResponse> allItems = new();

            int start = 1;

            string token = await _auth.GetValidTokenAsync();
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            while (true)
            {
                string jsonBody = $$"""
                 {
                   "object": "inventory-control/item-warehouse-inventory",
                   "fields": [
                     "key",
                     "id",
                     "item.key",
                     "item.id",
                     "item.name",
                     "item.status",
                     "warehouse.id",
                     "onHand"
                   ],
                   "filters": [
                     { "$eq": { "warehouse.id": "{{_warehouse}}" } }
                   ],
                   "filterParameters": {
                     "caseSensitiveComparison": true,
                     "includePrivate": true
                   },
                   "orderBy": [ { "key": "desc" } ],
                   "size": 4000,
                   "start": {{start}}
                 }
                 """;

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _http.PostAsync("https://api.intacct.com/ia/api/v1/services/core/query", content);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
                }

                var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<List<ItemQueryResponse>>>(JsonOptions);

                if (envelope?.Result != null)
                {
                    allItems.AddRange(envelope.Result);
                }

                int? next = envelope?.Meta?.Next;

                if (next == null || next.Value <= start)
                {
                    break;
                }

                start = next.Value;
            }

            return allItems;
        }

        //Object request appraoch

        public async Task<List<QuoteLineQueryResponse>> QueryQuoteLinesAsync(string quoteKey)
        {
            List<QuoteLineQueryResponse> allLines = new();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _auth.GetValidTokenAsync());

            int start = 1;

            while (true)
            {
                var request = new QueryQuotesRequest
                {
                    ObjectName = "order-entry/document-line",
                    Fields = new List<string>
                    {
                        "key",
                        "item.id",
                        "item.name",
                        "quantity",
                        "unitQuantity",
                        "unit",
                        "price",
                        "unitPrice",
                        "discountPercent",
                        "documentHeader.key",
                        "documentHeader.documentNumber",
                        "documentHeader.customerPONumber",
                        "documentHeader.customer.id",
                        "documentHeader.customer.name",
                        "documentHeader.contacts.shipTo.id"
                    },
                    Filters = new List<Dictionary<string, Dictionary<string, string>>>
                    {
                        new() { ["$eq"] = new() { ["documentHeader.key"] = quoteKey } },
                        new() { ["$eq"] = new() { ["status"] = "active" } }
                    },
                    Size = 500,
                    Start = start
                };

                HttpResponseMessage response = await _http.PostAsJsonAsync("https://api.intacct.com/ia/api/v1/services/core/query", request, JsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
                }

                var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<List<QuoteLineQueryResponse>>>(JsonOptions);

                if (envelope?.Result != null)
                {
                    allLines.AddRange(envelope.Result);
                }

                int? next = envelope?.Meta?.Next;

                if (next == null || next.Value <= start)
                {
                    break;
                }

                start = next.Value;
            }

            return allLines;
        }

        public async Task<CreateQuoteResponse> CreateQuoteAsync(CreateQuoteRequest request)
        {
            string url = $"https://api.intacct.com/ia/api/v1/objects/order-entry/document::{Uri.EscapeDataString(QuoteDocumentType)}";

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await _auth.GetValidTokenAsync());
            HttpResponseMessage response = await _http.PostAsJsonAsync(url, request, JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
            }

            var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<CreateQuoteResponse>>(JsonOptions);
            return envelope?.Result ?? throw new InvalidOperationException("Sage Intacct returned no result for the created quote.");
        }

        public async Task<CreateQuoteResponse> UpdateQuoteAsync(string key, UpdateQuoteRequest request)
        {
            string url = $"https://api.intacct.com/ia/api/v1/objects/order-entry/document::{Uri.EscapeDataString(QuoteDocumentType)}/{Uri.EscapeDataString(key)}";

            string token = await _auth.GetValidTokenAsync();
            _http.DefaultRequestHeaders.Remove("Authorization");
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            HttpResponseMessage response = await _http.PatchAsJsonAsync(url, request, JsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Sage Intacct returned {(int)response.StatusCode}: {error}", null, response.StatusCode);
            }

            var envelope = await response.Content.ReadFromJsonAsync<IntacctResponse<CreateQuoteResponse>>(JsonOptions);
            return envelope?.Result ?? throw new InvalidOperationException("Sage Intacct returned no result for the updated quote.");
        }
    }
}
