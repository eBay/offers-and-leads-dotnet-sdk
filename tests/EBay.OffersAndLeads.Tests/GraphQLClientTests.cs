using System.Net;
using System.Text.Json;
using EBay.OffersAndLeads.Configuration;
using EBay.OffersAndLeads.Exceptions;
using EBay.OffersAndLeads.Generated.GraphQL;
using Xunit;

namespace EBay.OffersAndLeads.Tests;

public sealed class GraphQLClientTests
{
    private const string NotFoundResponse = """
        {"data":{"declineOffer":{"__typename":"ReferenceArgumentsNotFound","errorCode":"OFFER_NOT_FOUND",
          "argumentNames":["offerId"],"errorMessage":"No offer with this id."}}}
        """;

    [Fact]
    public async Task Typed_operation_sends_document_and_parses_union_result()
    {
        var sdk = new TestSdk { Handler = { ResponseBody = NotFoundResponse } };

        var result = await sdk.Client.GraphQL.DeclineOfferAsync(Decline());

        var notFound = Assert.IsAssignableFrom<IDeclineOffer_DeclineOffer_ReferenceArgumentsNotFound>(result);
        Assert.Equal("OFFER_NOT_FOUND", notFound.ErrorCode);
        Assert.Equal(["offerId"], notFound.ArgumentNames);

        var request = sdk.Handler.Request!.RootElement;
        Assert.Equal("DeclineOffer", request.GetProperty("operationName").GetString());
        Assert.StartsWith("mutation DeclineOffer(", request.GetProperty("query").GetString());
        Assert.Equal("offer-1", Input(sdk).GetProperty("offerId").GetString());
        Assert.Equal("Bearer config-token", sdk.Handler.Authorization);
    }

    [Fact]
    public async Task Per_call_access_token_overrides_configuration()
    {
        var sdk = new TestSdk { Handler = { ResponseBody = NotFoundResponse } };

        await sdk.Client.GraphQL.DeclineOfferAsync(Decline(), accessToken: "call-token");

        Assert.Equal("Bearer call-token", sdk.Handler.Authorization);
    }

    [Fact]
    public async Task Unset_input_fields_are_omitted_and_explicit_null_is_sent()
    {
        var sdk = new TestSdk { Handler = { ResponseBody = NotFoundResponse } };

        await sdk.Client.GraphQL.DeclineOfferAsync(Decline());
        Assert.False(Input(sdk).TryGetProperty("message", out _));

        var withNullMessage = Decline();
        withNullMessage.Message = null;
        await sdk.Client.GraphQL.DeclineOfferAsync(withNullMessage);
        Assert.Equal(JsonValueKind.Null, Input(sdk).GetProperty("message").ValueKind);
    }

    [Fact]
    public async Task GraphQL_errors_throw_GraphQLException_with_every_message()
    {
        var sdk = new TestSdk
        {
            Handler = { ResponseBody = """{"errors":[{"message":"boom"},{"message":"bang"}],"data":null}""" }
        };

        var error = await Assert.ThrowsAsync<GraphQLException>(() => sdk.Client.GraphQL.DeclineOfferAsync(Decline()));

        Assert.Equal(["boom", "bang"], error.Errors);
    }

    [Fact]
    public async Task Http_401_throws_UnauthorizedException()
    {
        var sdk = new TestSdk { Handler = { Status = HttpStatusCode.Unauthorized } };

        await Assert.ThrowsAsync<UnauthorizedException>(() => sdk.Client.GraphQL.DeclineOfferAsync(Decline()));
    }

    [Fact]
    public async Task Http_500_throws_SdkException_with_status()
    {
        var sdk = new TestSdk { Handler = { Status = HttpStatusCode.InternalServerError } };

        var error = await Assert.ThrowsAsync<SdkException>(() => sdk.Client.GraphQL.DeclineOfferAsync(Decline()));

        Assert.Contains("HTTP 500", error.Message);
    }

    [Fact]
    public async Task Raw_queries_map_401_to_UnauthorizedException()
    {
        var sdk = new TestSdk { Handler = { Status = HttpStatusCode.Unauthorized } };

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            sdk.Client.GraphQL.ExecuteRawUnsafeAsync("query { __typename }"));
    }

    [Fact]
    public void Standalone_client_exposes_typed_graphql_operations()
    {
        // Before the in-house codegen, typed operations required DI and threw NotSupportedException.
        using var client = new OffersAndLeadsClient(new OffersAndLeadsConfiguration { AccessToken = "t" });

        Assert.NotNull(client.GraphQL);
    }

    private static DeclineOfferInput Decline() => new() { ItemId = "item-1", OfferId = "offer-1" };

    private static JsonElement Input(TestSdk sdk) =>
        sdk.Handler.Request!.RootElement.GetProperty("variables").GetProperty("input");
}
