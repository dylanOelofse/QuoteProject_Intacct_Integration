# Sage Intacct REST Integration — Quotes Manager (scaffold)

An ASP.NET Core MVC app for managing **Sales Quotations in Sage Intacct** through the Sage Intacct
REST API (v1). This is a copy of the *Direct Intacct integration* project with the two HTTP-facing
engines **emptied out** — the endpoint calls and the OAuth2 token call are yours to build.

Everything else is intact and compiles: controllers, services (validation + mapping), models,
request/response DTOs and all the views.

---

## 1. What's already done vs. what you build

| | State |
|---|---|
| `Api/QuotesApiEngine.cs` | **Stubs.** Eight methods, each `throw new NotImplementedException()`. ← build the endpoint calls here |
| `Api/AuthEngine.cs` | **Stub.** Reads the credentials from config; `GetAccessTokenAsync()` throws. ← build the token call here |
| `Services/QuoteApiService.cs` | Done — validation, `Quote` ↔ `CreateQuoteRequest` mapping, date/decimal formatting |
| `Services/QuoteLineApiService.cs` | Done — same for lines |
| `Controllers/` | Done — routes, model binding, status-code mapping |
| `Models/`, `Requests/`, `Responses/` | Done — the anti-corruption layer around Intacct's JSON |
| `Views/` | Done — quotes list, create page, quote-lines page |

The app builds and runs right now; any page that hits Intacct will throw
`NotImplementedException` until the engines are filled in.

## 2. Layering

The interfaces (`IQuoteApiService` / `IQuoteLineApiService`) are gone — controllers depend on the
concrete service. One service sits between the controller and the engine, nothing more:

```
Browser JS (fetch)  →  QuoteController  →  QuoteApiService  →  QuotesApiEngine  →  Sage Intacct
                       binds JSON to      validates, maps      builds URL,          ↑
                       Quote model        Quote → Request      sends HTTP      AuthEngine supplies
                                          formats dates and                    the bearer token
                                          decimals as strings
```

`QuotesApiEngine` is the only class that talks HTTP to the API; `AuthEngine` is the only class that
knows how to get a token. Neither is registered with a `BaseAddress`, and no endpoint URLs live in
config — **the engines own their URLs**.

## 3. Settings

Credentials only. `QuotesProject/QuotesProject/appsettings.json`:

```jsonc
"ApiSettings": {
  "GrantType": "client_credentials",
  "ClientId": "",
  "ClientSecret": "",
  "Username": ""
}
```

Fill in `ClientId`, `ClientSecret` and `Username`. `AuthEngine` throws at construction if any of the
four values is missing.

For real credentials prefer user secrets over the file, so they never reach source control:

```bash
cd "QuotesProject/QuotesProject"
dotnet user-secrets init
dotnet user-secrets set "ApiSettings:ClientSecret" "..."
```

## 4. The token call to build

From the Postman request:

```http
POST https://api.intacct.com/ia/api/v1/oauth2/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=...&client_secret=...&username=...
```

`FormUrlEncodedContent` produces that body. The response includes the token and its lifetime, so
caching it in `AuthEngine` and refreshing shortly before expiry beats fetching one per API call —
but note `AddHttpClient<T>` registers the engine as *transient*, so a cached field dies with the
instance; see the lifetime note in `Program.cs`.

## 5. Reference: the API this app was written against

The sections below are notes from the original working integration — the endpoints, the payload
shapes and the traps. They are reference material, not a spec you have to follow.

### 5.1 The endpoints

Quotes are **Order Entry documents** whose type is your company's quote *transaction definition* —
`18-Sales Quotation` in this company. The type is part of the URL after `::` and needs
`Uri.EscapeDataString` (it contains a space).

| Purpose | Call | Engine method |
|---|---|---|
| List/search quotes | `POST /services/core/query` — body picks `object`, `fields`, `filters`, `orderBy` | `QueryQuotesAsync` |
| Read one quote + lines | `GET /objects/order-entry/document::18-Sales%20Quotation/{key}` | `GetQuoteByKeyAsync` |
| Create quote | `POST /objects/order-entry/document::18-Sales%20Quotation` | `CreateQuoteAsync` |
| Update quote header | `PATCH /objects/order-entry/document::18-Sales%20Quotation/{key}` | `UpdateQuoteAsync` |
| Delete quote | `DELETE /objects/order-entry/document::18-Sales%20Quotation/{key}` | `DeleteQuoteAsync` |
| Add line to existing quote | `POST /objects/order-entry/document-line::18-Sales%20Quotation` — body includes `documentHeader.key` | `CreateQuoteLineAsync` |
| Update one line | `PATCH /objects/order-entry/document-line::18-Sales%20Quotation/{lineKey}` | `UpdateQuoteLineAsync` |
| Delete one line | `DELETE /objects/order-entry/document-line::18-Sales%20Quotation/{lineKey}` | `DeleteQuoteLineAsync` |

**Lines have their own endpoints.** Document lines are *owned objects* of the document, but Intacct
still exposes them as a standalone resource (`order-entry/document-line`) with the same
`::{documentType}` URL pattern — which is what lets the quote-lines page add/save/delete one line
per request. A new line attaches to its parent via `"documentHeader": { "key": "<quote key>" }`.
Reading lines needs no extra call: the GET on the document already embeds the full `lines` array.

**Why the query service for the list?** There is a plain list endpoint
(`GET /objects/order-entry/document`), but it returns only `key`/`id`/`href` per row — one extra GET
per quote to fill the table (N+1). The query service returns exactly the columns you ask for in one
call and lets you filter to just quotes (`documentType = 18-Sales Quotation`), which matters because
`order-entry/document` holds *all* document types (orders, invoices, shippers...).

Create body essentials:

```json
POST /objects/order-entry/document::18-Sales Quotation
{
  "customer":  { "id": "CUST-00042" },
  "txnDate":   "2026-07-16",              // REQUIRED
  "state":     "draft",                   // draft | pending | submitted (default pending)
  "lines": [                              // at least one line
    {
      "dimensions":   { "item": { "id": "ITEM-001" }, "location": { "id": "1" } },
      "unit":         "Each",             // REQUIRED
      "unitQuantity": "5",                // REQUIRED (string!)
      "unitPrice":    "650.00"            // REQUIRED (string!)
    }
  ]
}
```

`201 Created` returns **only a reference** — `{ "key", "id", "documentType", "href" }` inside
`ia::result`. That is why `QuoteApiService.CreateQuoteAsync` GETs the document straight back: the
quote number is generated inside Sage.

### 5.2 Things the engine has to get right

- **Every response is wrapped in an envelope**: `{ "ia::result": ..., "ia::meta": ... }` →
  deserialize into `IntacctResponse<T>` (`ia::result` isn't a legal C# name, hence
  `[JsonPropertyName]`).
- **JSON options**: camelCase when writing, case-insensitive when reading, and
  `DefaultIgnoreCondition.WhenWritingNull`. PATCH *depends* on that last one — a null property is
  omitted from the body and Intacct leaves the field untouched.
- **Numbers and dates travel as strings**: `"unitPrice": "650.00"`, `"txnDate": "2024-11-01"`.
  The services already parse and format these in invariant culture — `"1250.50"`, never `1250,5`.
- **Query rows are flat**: asking for `customer.name` returns a JSON key literally called
  `"customer.name"` → see `QuoteQueryResponse`.
- **Surface the error body.** On a non-success status, read the response content into the exception
  message and throw `HttpRequestException` with the `StatusCode` set — the controllers switch on
  `HttpStatusCode.NotFound` / `BadRequest`, and the views show the message in an alert.

## 6. Run it

```bash
cd "QuotesProject/QuotesProject"
dotnet run
```

Browse to the URL it prints (e.g. `https://localhost:7216`) → you land on the quotes list.

- **Quotes list** (`/Quote`) — edit Reference/Dates inline → **Save** (PATCH), **Delete**,
  **View** → the quote-lines page.
- **+ Create New Quote** → `/Quote/Create` — header fields + line grid → success popup shows the
  Sage-generated quote number → back to the list.
- **Quote lines** (`/QuoteLine?quoteKey=...`) — quote details card, "Create Quote Line" card, and an
  editable lines table with per-row **Save** and **Delete**.

## 7. Gotchas

- **401 / `GW-0031 Invalid token`** → the token is expired or was never fetched.
- **Only draft/pending documents** can be edited or deleted; a converted quote refuses with a 400.
- **Location/Warehouse** on lines are optional here, but some company configurations (multi-entity,
  inventory-tracked items) require them — if Intacct rejects a create, its error message names the
  missing field.
- If a query 400s on a field name, drop that field from `QueryQuotesAsync` — field names can be
  verified in Postman by adding them to `"fields"` one at a time.
