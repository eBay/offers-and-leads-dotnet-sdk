using Xunit;

namespace EBay.OffersAndLeads.Tests;

public sealed class NegotiationTests
{
    [Fact]
    public async Task Negotiation_api_is_part_of_this_sdk()
    {
        // The Negotiation API moved here from the Communication SDK.
        var sdk = new TestSdk { Handler = { ResponseBody = """{"eligibleItems":[{"listingId":"110000000001"}],"total":1}""" } };

        var page = await sdk.Client.Negotiation.FindEligibleItemsAsync("EBAY_US", limit: 10);

        Assert.Equal("110000000001", Assert.Single(page!.EligibleItems!).ListingId);
        Assert.StartsWith("https://api.ebay.com/sell/negotiation/v1/find_eligible_items", sdk.Handler.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer config-token", sdk.Handler.Authorization);
    }
}
