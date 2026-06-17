# Offers and Leads .NET SDK

A production-ready .NET SDK for eBay Classified Leads and Offer APIs, providing unified access to buyer leads retrieval (REST) and offer management (GraphQL).

## Features

- **Classified Leads API**: Retrieve buyer leads for classified listings (2 operations)
  - Get all classified leads
  - Get classified leads by item ID
- **Offer API (GraphQL)**: Manage buyer offers and negotiation history (3 operations)
  - Accept offers
  - Decline offers
  - View seller negotiation history
- **OAuth Authentication**: Pass bearer tokens per-call or via configuration
- **Dependency Injection**: First-class support for ASP.NET Core DI
- **Strong Typing**: Internal generated types with public abstraction layer

## Architecture

```
OffersAndLeadsSdk/
├── Generated/                    # Kiota-generated REST client (internal)
│   └── Feed/                     # Feed API (serves Classified Leads endpoints)
├── Clients/
│   ├── Rest/
│   │   └── ClassifiedLeadsClient.cs  # REST client wrapper (public API)
│   └── GraphQL/
│       └── DynamicGraphQLClient.cs   # GraphQL client with offer operations
├── Configuration/                # SDK configuration and auth
├── Exceptions/                   # SDK exceptions
└── OffersAndLeadsClient.cs      # Main SDK facade
```

## Installation

```bash
# From source
dotnet pack OffersAndLeadsSdk/OffersAndLeadsSdk.csproj
dotnet add reference path/to/OffersAndLeadsSdk.1.0.0.nupkg

# Or add project reference
dotnet add reference OffersAndLeadsSdk/OffersAndLeadsSdk.csproj
```

## Quick Start

### 1. Configure and Use the SDK

Token management is the caller's responsibility. Pass a bearer token via configuration (used for all
calls) or per-call (overrides the configured token for that request).

```csharp
using OffersAndLeads;
using OffersAndLeads.Configuration;

// Create configuration
var config = new OffersAndLeadsConfiguration
{
    AccessToken = "your_bearer_token",  // default for all calls
    LeadsBaseUrl = "https://apiz.ebay.com/sell/leads/v1",
    GraphQLBaseUrl = "https://api.ebay.com/graphql"
};

// Create SDK client
using var client = new OffersAndLeadsClient(config);

// Get classified leads
var allLeads = await client.ClassifiedLeads.GetAllClassifiedLeadsAsync();
var itemLeads = await client.ClassifiedLeads.GetClassifiedLeadsByItemIdAsync("item123");

// Manage offers (GraphQL)
var acceptResult = await client.GraphQL.AcceptOfferAsync("offer123");
var declineResult = await client.GraphQL.DeclineOfferAsync("offer456");
var history = await client.GraphQL.GetSellerNegotiationHistoryAsync();

// Execute custom GraphQL queries
var customResult = await client.GraphQL.ExecuteRawUnsafeAsync(@"
    query {
      sellerNegotiationHistory {
        offerId
        itemId
        amount
        status
      }
    }");
```

### 2. Dependency Injection (ASP.NET Core)

```csharp
using OffersAndLeads.Extensions;
using OffersAndLeads.Configuration;

// In Program.cs or Startup.cs
builder.Services.AddOffersAndLeadsSdk(new OffersAndLeadsConfiguration
{
    AccessToken = "your_bearer_token"
});

// Inject the SDK or sub-clients
public class LeadsController : ControllerBase
{
    private readonly OffersAndLeadsClient _sdk;
    private readonly ClassifiedLeadsClient _leads;

    public LeadsController(
        OffersAndLeadsClient sdk,
        ClassifiedLeadsClient leads)
    {
        _sdk = sdk;
        _leads = leads;
    }

    [HttpGet("leads")]
    public async Task<IActionResult> GetAllLeads()
    {
        var leads = await _leads.GetAllClassifiedLeadsAsync();
        return Ok(leads);
    }

    [HttpGet("leads/{itemId}")]
    public async Task<IActionResult> GetItemLeads(string itemId)
    {
        var leads = await _leads.GetClassifiedLeadsByItemIdAsync(itemId);
        return Ok(leads);
    }

    [HttpPost("offers/{offerId}/accept")]
    public async Task<IActionResult> AcceptOffer(string offerId)
    {
        var result = await _sdk.GraphQL.AcceptOfferAsync(offerId);
        return Ok(result);
    }
}
```

## API Operations

### Classified Leads API (REST)

#### Get All Classified Leads
```csharp
var leads = await client.ClassifiedLeads.GetAllClassifiedLeadsAsync();
```
Retrieves all buyer leads for the authenticated seller's classified listings.

#### Get Classified Leads by Item ID
```csharp
var leads = await client.ClassifiedLeads.GetClassifiedLeadsByItemIdAsync(itemId);
```
Retrieves buyer leads for a specific classified listing item.

**Response includes:**
- Lead ID
- Buyer information
- Contact details
- Lead creation date
- Item information

### Offer API (GraphQL)

#### Accept Offer
```csharp
var result = await client.GraphQL.AcceptOfferAsync(offerId);
```
Accepts a buyer's offer on an item. The mutation returns:
- `success`: Boolean indicating if the offer was accepted
- `message`: Descriptive message about the operation result

#### Decline Offer
```csharp
var result = await client.GraphQL.DeclineOfferAsync(offerId);
```
Declines a buyer's offer on an item. The mutation returns:
- `success`: Boolean indicating if the offer was declined
- `message`: Descriptive message about the operation result

#### Get Seller Negotiation History
```csharp
var history = await client.GraphQL.GetSellerNegotiationHistoryAsync();
```
Retrieves the seller's complete negotiation history, including:
- Offer ID
- Item ID
- Offer amount
- Status (pending, accepted, declined, etc.)
- Creation date

### GraphQL - Custom Queries

#### Execute Arbitrary Queries
```csharp
var result = await client.GraphQL.ExecuteRawUnsafeAsync(@"
    query GetOfferDetails {
      sellerNegotiationHistory {
        offerId
        itemId
        amount
        status
        createdDate
        buyer {
          username
          feedbackScore
        }
      }
    }");

// Parse the response
var data = result.RootElement.GetProperty("data");
var negotiations = data.GetProperty("sellerNegotiationHistory");

foreach (var negotiation in negotiations.EnumerateArray())
{
    var offerId = negotiation.GetProperty("offerId").GetString();
    var status = negotiation.GetProperty("status").GetString();
    Console.WriteLine($"Offer {offerId}: {status}");
}
```

#### Execute Custom Mutations
```csharp
var mutation = @"
    mutation CounterOffer($offerId: ID!, $amount: Float!) {
      counterOffer(input: { offerId: $offerId, amount: $amount }) {
        success
        message
        newOfferId
      }
    }";

var variables = new { offerId = "offer123", amount = 99.99 };
var result = await client.GraphQL.ExecuteRawUnsafeAsync(mutation, variables);
```

## Error Handling

```csharp
using OffersAndLeads.Exceptions;

try
{
    var leads = await client.ClassifiedLeads.GetAllClassifiedLeadsAsync();
}
catch (UnauthorizedException)
{
    // Token is invalid or expired — obtain a new token and retry
}
catch (GraphQLException ex)
{
    foreach (var error in ex.Errors)
        Console.WriteLine($"GraphQL Error: {error}");
}
catch (SdkException ex)
{
    Console.WriteLine($"SDK Error: {ex.Message}");
}
```

`UnauthorizedException` is thrown by both REST and GraphQL operations when the API returns 401.
Token refresh is the caller's responsibility.

## Regenerating the SDK

When OpenAPI specs or GraphQL schemas change:

```bash
# Run the regeneration script
./generate.sh

# Or manually:
dotnet tool restore
dotnet kiota generate -l csharp -d specs/sell_feed_v1_oas3.yaml ...
dotnet build OffersAndLeadsSdk/OffersAndLeadsSdk.csproj
```

## Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `LeadsBaseUrl` | `https://apiz.ebay.com/sell/leads/v1` | Base URL for the Classified Leads REST API |
| `GraphQLBaseUrl` | `https://api.ebay.com/graphql` | GraphQL API endpoint for Offer operations |
| `AccessToken` | `null` | Default bearer token used when no per-call token is provided |

## Project Structure

- **Generated/Feed/**: Kiota-generated REST client (internal, committed to git)
- **Clients/Rest/**: Hand-crafted wrapper exposing public Classified Leads API
- **Clients/GraphQL/**: GraphQL client for Offer operations with typed helpers
- **Configuration/**: SDK config, auth providers
- **Exceptions/**: SDK exceptions
- **Extensions/**: DI extension methods

## Use Cases

### Classified Leads Management
Perfect for sellers with classified listings who need to:
- Track buyer inquiries and leads
- Analyze lead conversion rates
- Follow up with interested buyers
- Monitor lead generation by item

### Offer Negotiation Workflow
Ideal for sellers who want to:
- Programmatically accept or decline offers
- Track negotiation history across all items
- Automate offer responses based on business rules
- Integrate offer management into existing CRM systems

## Development

### Prerequisites

- .NET 10.0 SDK
- Kiota CLI tool (auto-installed via dotnet tools)

### Building

```bash
dotnet build OffersAndLeadsSdk/OffersAndLeadsSdk.csproj
```

### Packaging

```bash
dotnet pack OffersAndLeadsSdk/OffersAndLeadsSdk.csproj
```

## Dependencies

- **Microsoft.Kiota.Bundle**: REST client generation and runtime
- **Microsoft.Extensions.DependencyInjection.Abstractions**: DI support
- **Microsoft.Extensions.Http**: HttpClient factory integration

## License

[Apache 2.0](LICENSE) © 2026 eBay Inc.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run `./generate.sh` if specs changed
5. Ensure `dotnet build` succeeds
6. Submit a pull request
