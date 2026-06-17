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
using EBay.OffersAndLeads.Exceptions;
using EBay.OffersAndLeads.Generated.GraphQL;
using StrawberryShake;
using System.Net;
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

    // ── Offer ─────────────────────────────────────────────────────────────────

    public Task<IAcceptOffer_AcceptOffer?> AcceptOfferAsync(
        AcceptOfferInput input, string? accessToken = null, CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await ExecuteAsync(() => _client!.AcceptOffer.ExecuteAsync(input, ct));
            EnsureNoErrors(result);
            return result.Data?.AcceptOffer;
        });

    public Task<IDeclineOffer_DeclineOffer?> DeclineOfferAsync(
        DeclineOfferInput input, string? accessToken = null, CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await ExecuteAsync(() => _client!.DeclineOffer.ExecuteAsync(input, ct));
            EnsureNoErrors(result);
            return result.Data?.DeclineOffer;
        });

    public Task<IMakeOffer_MakeOffer?> MakeOfferAsync(
        MakeOfferInput input, string? accessToken = null, CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await ExecuteAsync(() => _client!.MakeOffer.ExecuteAsync(input, ct));
            EnsureNoErrors(result);
            return result.Data?.MakeOffer;
        });

    public Task<ICounterOffer_CounterOffer?> CounterOfferAsync(
        CounterOfferInput input, string? accessToken = null, CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await ExecuteAsync(() => _client!.CounterOffer.ExecuteAsync(input, ct));
            EnsureNoErrors(result);
            return result.Data?.CounterOffer;
        });

    // ── Negotiation History ───────────────────────────────────────────────────

    public Task<ISellerNegotiationHistory_SellerNegotiationHistory?> SellerNegotiationHistoryAsync(
        SellerNegotiationHistoryInput input, string? accessToken = null, CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await ExecuteAsync(() => _client!.SellerNegotiationHistory.ExecuteAsync(input, ct));
            EnsureNoErrors(result);
            return result.Data?.SellerNegotiationHistory;
        });

    public Task<IBuyerNegotiationHistory_BuyerNegotiationHistory?> BuyerNegotiationHistoryAsync(
        BuyerNegotiationHistoryInput input, string? accessToken = null, CancellationToken ct = default)
        => _tokenContext.RunAsync(accessToken, async () =>
        {
            EnsureTypedClientAvailable();
            var result = await ExecuteAsync(() => _client!.BuyerNegotiationHistory.ExecuteAsync(input, ct));
            EnsureNoErrors(result);
            return result.Data?.BuyerNegotiationHistory;
        });

    // ── Dynamic escape hatch ──────────────────────────────────────────────────

    /// <inheritdoc cref="DynamicGraphQLClient.ExecuteRawUnsafeAsync"/>
    public Task<JsonDocument> ExecuteRawUnsafeAsync(
        string query, object? variables = null, string? accessToken = null, CancellationToken ct = default)
        => _dynamic.ExecuteRawUnsafeAsync(query, variables, accessToken, ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<IOperationResult<T>> ExecuteAsync<T>(Func<Task<IOperationResult<T>>> call)
        where T : class
    {
        try { return await call(); }
        catch (Exception ex) when (ex is not SdkException) { throw WrapException(ex); }
    }

    private static Exception WrapException(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: HttpStatusCode.Unauthorized } => new UnauthorizedException(ex),
        HttpRequestException { StatusCode: { } code } http =>
            new SdkException($"GraphQL request failed with HTTP {(int)code}: {http.Message}", ex),
        HttpRequestException http =>
            new SdkException($"GraphQL request failed (network error): {http.Message} — check that GraphQLBaseUrl is reachable.", ex),
        TaskCanceledException =>
            new SdkException("GraphQL request timed out — check that GraphQLBaseUrl is reachable and the network is accessible.", ex),
        InvalidOperationException { Message: var msg } when msg.Contains("execution attempt") =>
            new SdkException("GraphQL request returned no data; the endpoint may be unreachable or returned an unexpected response — check GraphQLBaseUrl.", ex),
        _ => new SdkException($"GraphQL request failed: {ex.Message}", ex)
    };

    private static void EnsureNoErrors<T>(IOperationResult<T> result)
        where T : class
    {
        if (result.Errors is { Count: > 0 })
        {
            var messages = result.Errors
                .Select(e => e.Message ?? e.Code ?? "unknown error")
                .ToArray();
            throw new GraphQLException(messages);
        }
    }
}
