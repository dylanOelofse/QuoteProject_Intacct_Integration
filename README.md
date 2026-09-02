# Sage Intacct REST Integration — Quotes Manager

An ASP.NET Core MVC app (.NET 10) for managing **Sales Quotations in Sage Intacct** through the
Sage Intacct REST API. View, create, update and delete quotes and their lines.

Quotes are Order Entry documents whose type is the company's quote *transaction definition* —
`18-Sales Quotation` here. The type is part of the URL after `::` and needs `Uri.EscapeDataString`
because it contains a space.

---

## 1. Layering

```
Browser JS (fetch)  →  QuoteController  →  QuoteApiService  →  QuotesApiEngine  →  Sage Intacct
                       binds JSON to      validates, maps      builds URL,          ↑
                       Quote model        Quote → Request      sends HTTP      AuthEngine supplies
                                          formats dates and                    the bearer token
                                          decimals as strings
```

`QuotesApiEngine` is the only class that talks HTTP to the API; `AuthEngine` is the only class that
knows how to get a token. Neither is registered with a `BaseAddress` and no endpoint URLs live in
config — **the engines own their URLs**.

| | Role |
|---|---|
| `Api/AuthEngine.cs` | OAuth2 token: fetch, cache, refresh |
| `Api/QuotesApiEngine.cs` | Every HTTP call to Intacct (6 methods) |
| `Services/` | Validation, `Quote` ↔ request/response mapping, date and decimal formatting |
| `Services/LookupStore.cs` | Customers and items, loaded once at startup |
| `Controllers/` | Routes, model binding, status-code mapping |
| `Models/`, `Requests/`, `Responses/` | The anti-corruption layer around Intacct's JSON |
| `Views/` | Quotes list, create page, quote-lines page |

## 2. Settings

`appsettings.json` is **gitignored** — it holds live credentials and is not in the repo. Create it
at `QuotesProject/QuotesProject/appsettings.json`:

```jsonc
{
  "ApiSettings": {
    "GrantType":    "client_credentials",
    "ClientId":     "",
    "ClientSecret": "",
    "Username":     "",

    // Sent as the X-IA-API-Param-Entity header on every data call.
    "Entity":    "18. EfektoCare_SA",

    // Required on every quote line by the create schema, not collected by the UI.
    "Location":  "18. EfektoCare_SA",
    "Warehouse": "11ISADistStorage"
  },

  // Display only. Subtotal/Total are worked out in the browser; Sage calculates the real tax.
  "VatRate": 15
}
```

`AuthEngine` throws at construction if any credential is missing. For real credentials prefer user
secrets over the file, so they can never reach source control even by accident:

```bash
cd "QuotesProject/QuotesProject"
dotnet user-secrets init
dotnet user-secrets set "ApiSettings:ClientSecret" "..."
```

## 3. Auth

```http
POST https://api.intacct.com/ia/api/v1/oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=...&client_secret=...&username=...
```

Form-encoded, not JSON — `Dictionary<string,string>` into `FormUrlEncodedContent`, sent with
`PostAsync`. `AuthEngine` is registered as a **singleton** so the cached token survives between
requests, and takes its `HttpClient` from a named `IHttpClientFactory` client rather than
`AddHttpClient<T>` (which would register it transient and throw the cache away every call).

`GetValidTokenAsync()` is the only method callers need. It returns the cached token, and on expiry
refreshes under a `SemaphoreSlim` with a double-check so concurrent requests fetch one token, not
five. Expiry is stored 300s early. A `refresh_token` grant is tried first and falls back to a full
`client_credentials` fetch.

## 4. Endpoints used

| Purpose | Call |
|---|---|
| Quotes list | `POST /services/core/query` on `order-entry/document` |
| Quote lines | `POST /services/core/query` on `order-entry/document-line` |
| Customers | `POST /services/core/query` on `accounts-receivable/customer` |
| Items + stock | `POST /services/core/query` on `inventory-control/item-warehouse-inventory` |
| Create quote | `POST /objects/order-entry/document::18-Sales%20Quotation` |
| Everything else | `PATCH /objects/order-entry/document::18-Sales%20Quotation/{key}` |

**One PATCH does four jobs.** Updating the header, updating a line, deleting a line and deleting a
quote all go through the same document PATCH. There is no `DELETE` and no `document-line` write
anywhere in the engine — see §5 for why.

Create body essentials:

```json
POST /objects/order-entry/document::18-Sales Quotation
{
  "customer": { "id": "SAC2059" },
  "txnDate":  "2026-07-16",
  "state":    "pending",
  "lines": [
    {
      "dimensions":   { "item":      { "id": "35139" },
                        "warehouse": { "id": "11ISADistStorage" },
                        "location":  { "id": "18. EfektoCare_SA" } },
      "unit":         "06Pk",
      "unitQuantity": "10"
    }
  ]
}
```

`201 Created` returns **only a reference** — `{ "key", "id", "documentType", "href" }` inside
`ia::result`. The quote number is generated inside Sage, so the list is re-queried to pick it up.

## 5. Things learned the hard way

- **Lines cannot be written through `order-entry/document-line`.** They are edited as a `lines`
  array inside a document PATCH. Deleting one is `{"key": "...", "ia::operation": "delete"}`.
- **A smart rule blocks every PATCH** (`CUSTOMER_UDF`, `BL04002055`) unless the body carries
  `"nsp::CUSTOMER_UDFS": true`. It is on `UpdateQuoteRequest` for exactly this reason.
- **A Delete Policy on the transaction definition blocks `DELETE`** (`INV-1372`). Deleting a quote
  is therefore a soft delete — PATCH `state` to `closed`, which drops it out of the list's
  `state = pending` filter.
- **`status` is not writable on a document.** PATCHing `"status": "inactive"` returns `200` with
  `"totalSuccess": 1` and changes nothing. Note what that means generally: a 2xx and a
  `totalSuccess` count confirm Sage *processed* the request, not that it *applied* your field.
  Verify writes by re-querying, not by trusting the status code.
- **`item` is read-only on a line update** (`REST-1050`). Change the item via `dimensions.item`,
  which needs only `item` — no warehouse or location, unlike create.
- **Dot notation traverses many-to-one only.** `lines.item.id` and `warehouseInfo.warehouse.id`
  both fail with `REST-1107` because they cross an array. Query the child object directly instead.
- **Filters are stricter than fields.** A field can be valid in `"fields"` and still be rejected in
  `"filters"` — `documentHeader.documentType` is the one that bites.
- **Quantity comes in two flavours**: `unitQuantity` is packs (what the user types),
  `quantity` is base units. Sending 10 × `06Pk` stores `unitQuantity: 10`, `quantity: 60`.
- **So does price**: `unitPrice` is per unit-of-measure and matches Intacct's Price column;
  `price` is per base unit. Both arrive **already net of the discount**, so applying the
  discount again double-counts it.
- **`ia::meta.next` is a number, not a string** — typing it as `string?` throws on page 2 of any
  paged result.
- **`Authorization` is single-value.** `DefaultRequestHeaders.Add` appends rather than replaces, so
  a paging loop that adds per page throws a `FormatException` on the second iteration. Set it once
  above the loop.
- **Item stock is warehouse-scoped.** Querying `inventory-control/item-warehouse-inventory` filtered
  on `warehouse.id` is what makes on-hand figures match what Intacct shows on the document.

## 6. Startup lookups

Customers and items change rarely and are needed on every page, so `LookupLoaderService`
(a `BackgroundService`) loads them once into the singleton `LookupStore` at startup. Pages read
them from `/Lookup`; the quotes list has a **Refresh** button that re-runs the load. This keeps
the app to a handful of API calls a day rather than several per page view.

## 7. Run it

```bash
cd "QuotesProject/QuotesProject"
dotnet run --launch-profile https
```

Use the **https** profile — `UseHttpsRedirection` 307s to the https port, so the http profile
lands on a port with nothing bound.

- **Quotes list** (`/Quote`) — edit dates and External Order # inline → **Save**, **Delete**,
  **View**. **Refresh** re-pulls customers and items.
- **+ New Quote** (`/Quote/Create`) — header fields and a line grid.
- **Quote lines** (`/QuoteLine?quoteKey=...`) — editable item, unit, quantity and discount, with
  read-only price, stock and totals.

## 8. Gotchas

- **401 / `GW-0031 Invalid token`** → expired or never fetched.
- **`REST-1030 malformed JSON`** → usually a trailing comma left behind after editing one of the
  hardcoded query bodies.
- **`INV-1359 Missing unit`** → the unit is not in that item's unit-of-measure group. Units are not
  interchangeable between items; `Each` is not universally valid.
- **Only pending/active documents** can be edited; a converted quote refuses with a 400.
- If a query 400s on a field name, drop that field — names can be verified in Postman by adding
  them to `"fields"` one at a time.
