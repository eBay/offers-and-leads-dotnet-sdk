using EBay.OffersAndLeads.Configuration;
using Xunit;

namespace EBay.OffersAndLeads.Tests;

public sealed class TrustedHostsTests
{
    private const string Sandbox = "https://apiz.sandbox.ebay.com/sell/leads/v1";
    private const string Proxy = "https://ebay-proxy.example.com/sell/leads/v1";

    [Fact]
    public void Untrusted_base_url_is_rejected_by_both_constructors()
    {
        var config = new OffersAndLeadsConfiguration { AccessToken = "t", ClassifiedLeadsBaseUrl = Proxy };

        var standalone = Assert.Throws<InvalidOperationException>(() => new OffersAndLeadsClient(config));
        Assert.Contains("AllowedHosts", standalone.Message);
        Assert.Throws<InvalidOperationException>(() => new TestSdk(configure: c => c.ClassifiedLeadsBaseUrl = Proxy));
    }

    [Fact]
    public async Task Production_requests_carry_the_token()
    {
        var sdk = new TestSdk();

        await sdk.Client.ClassifiedLeads.GetAllClassifiedLeadsAsync();

        Assert.StartsWith("https://apiz.ebay.com/sell/leads/v1/", sdk.Handler.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer config-token", sdk.Handler.Authorization);
    }

    [Fact]
    public async Task Sandbox_hosts_are_trusted_by_default()
    {
        var sdk = new TestSdk(configure: c => c.ClassifiedLeadsBaseUrl = Sandbox);

        await sdk.Client.ClassifiedLeads.GetAllClassifiedLeadsAsync();

        Assert.Equal("apiz.sandbox.ebay.com", sdk.Handler.RequestUri!.Host);
        Assert.Equal("Bearer config-token", sdk.Handler.Authorization);
    }

    [Fact]
    public async Task Additional_allowed_host_receives_requests_with_token()
    {
        var sdk = new TestSdk(configure: c =>
        {
            c.ClassifiedLeadsBaseUrl = Proxy;
            c.AllowedHosts.Add("ebay-proxy.example.com");
        });

        await sdk.Client.ClassifiedLeads.GetAllClassifiedLeadsAsync();

        Assert.Equal("ebay-proxy.example.com", sdk.Handler.RequestUri!.Host);
        Assert.Equal("Bearer config-token", sdk.Handler.Authorization);
    }

    [Fact]
    public async Task Token_is_never_sent_to_a_host_configured_after_construction()
    {
        // Regression: the token used to be attached for any HTTPS host.
        var sdk = new TestSdk();
        sdk.Config.GraphQLBaseUrl = "https://attacker.example.com/graphql";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sdk.Client.GraphQL.ExecuteRawUnsafeAsync("query { __typename }"));

        Assert.Equal(0, sdk.Handler.Calls);
    }

    [Fact]
    public void Default_hosts_are_read_only()
    {
        Assert.Contains("apiz.ebay.com", OffersAndLeadsConfiguration.DefaultAllowedHosts);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<string>)OffersAndLeadsConfiguration.DefaultAllowedHosts).Add("evil.example.com"));
    }
}
