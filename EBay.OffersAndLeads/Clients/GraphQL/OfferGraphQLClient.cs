// Copyright 2026 eBay Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using EBay.OffersAndLeads.Configuration;
using EBay.OffersAndLeads.Generated.GraphQL;
using System.Text.Json;

namespace EBay.OffersAndLeads.Clients.GraphQL;

public sealed class OfferGraphQLClient
{
    private readonly IOffersAndLeadsGraphQLClient? _client;
    private readonly DynamicGraphQLClient _dynamic;
    private readonly ITokenContext _tokenContext;

    internal OfferGraphQLClient(
        IOffersAndLeadsGraphQLClient? client,
        DynamicGraphQLClient dynamic,
        ITokenContext tokenContext)
        => (_client, _dynamic, _tokenContext) = (client, dynamic, tokenContext);

    private void EnsureTypedClientAvailable()
    {
        if (_client is null)
            throw new NotSupportedException(
                "Typed GraphQL operations require DI. Use ExecuteRawUnsafeAsync for standalone mode.");
    }

    public Task<IAcceptOffer_AcceptOffer?> AcceptOfferAsync(
        AcceptOfferInput input,
        string? accessToken = null,
        CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await GraphQLInvocation.ExecuteAsync(() => _client!.AcceptOffer.ExecuteAsync(input, ct));
            GraphQLInvocation.EnsureNoErrors(result);
            return result.Data?.AcceptOffer;
        });

    public Task<IBuyerNegotiationHistory_BuyerNegotiationHistory?> BuyerNegotiationHistoryAsync(
        BuyerNegotiationHistoryInput input,
        string? accessToken = null,
        CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await GraphQLInvocation.ExecuteAsync(() => _client!.BuyerNegotiationHistory.ExecuteAsync(input, ct));
            GraphQLInvocation.EnsureNoErrors(result);
            return result.Data?.BuyerNegotiationHistory;
        });

    public Task<ICounterOffer_CounterOffer?> CounterOfferAsync(
        CounterOfferInput input,
        string? accessToken = null,
        CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await GraphQLInvocation.ExecuteAsync(() => _client!.CounterOffer.ExecuteAsync(input, ct));
            GraphQLInvocation.EnsureNoErrors(result);
            return result.Data?.CounterOffer;
        });

    public Task<IDeclineOffer_DeclineOffer?> DeclineOfferAsync(
        DeclineOfferInput input,
        string? accessToken = null,
        CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await GraphQLInvocation.ExecuteAsync(() => _client!.DeclineOffer.ExecuteAsync(input, ct));
            GraphQLInvocation.EnsureNoErrors(result);
            return result.Data?.DeclineOffer;
        });

    public Task<IMakeOffer_MakeOffer?> MakeOfferAsync(
        MakeOfferInput input,
        string? accessToken = null,
        CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await GraphQLInvocation.ExecuteAsync(() => _client!.MakeOffer.ExecuteAsync(input, ct));
            GraphQLInvocation.EnsureNoErrors(result);
            return result.Data?.MakeOffer;
        });

    public Task<ISellerNegotiationHistory_SellerNegotiationHistory?> SellerNegotiationHistoryAsync(
        SellerNegotiationHistoryInput input,
        string? accessToken = null,
        CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await GraphQLInvocation.ExecuteAsync(() => _client!.SellerNegotiationHistory.ExecuteAsync(input, ct));
            GraphQLInvocation.EnsureNoErrors(result);
            return result.Data?.SellerNegotiationHistory;
        });

    public Task<JsonDocument> ExecuteRawUnsafeAsync(
        string query, object? variables = null, string? accessToken = null, CancellationToken ct = default)
        => _dynamic.ExecuteRawUnsafeAsync(query, variables, accessToken, ct);
}
