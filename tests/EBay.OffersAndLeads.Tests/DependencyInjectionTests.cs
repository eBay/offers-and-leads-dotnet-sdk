using EBay.OffersAndLeads.Clients.GraphQL;
using EBay.OffersAndLeads.Clients.Rest;
using EBay.OffersAndLeads.Configuration;
using EBay.OffersAndLeads.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EBay.OffersAndLeads.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Registers_one_shared_entry_client_and_no_individual_api_clients()
    {
        var services = new ServiceCollection();
        services.AddOffersAndLeadsSdk(new OffersAndLeadsConfiguration { AccessToken = "t" });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<OffersAndLeadsClient>();

        Assert.Same(client, provider.CreateScope().ServiceProvider.GetRequiredService<OffersAndLeadsClient>());
        Assert.NotNull(client.ClassifiedLeads);
        Assert.NotNull(client.GraphQL);
        Assert.Null(provider.GetService<ClassifiedLeadsClient>());
        Assert.Null(provider.GetService<OfferGraphQLClient>());
    }

    [Fact]
    public void Rejects_a_null_primary_handler()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OffersAndLeadsClient(new OffersAndLeadsConfiguration(), (HttpMessageHandler)null!));
    }
}
