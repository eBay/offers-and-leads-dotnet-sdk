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


using EBay.OffersAndLeads.Clients.GraphQL;
using EBay.OffersAndLeads.Clients.Rest;
using EBay.OffersAndLeads.Configuration;
using EBay.OffersAndLeads.Generated.Feed;
using EBay.OffersAndLeads.Generated.GraphQL;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace EBay.OffersAndLeads;

public sealed class OffersAndLeadsClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public ClassifiedLeadsClient ClassifiedLeads { get; }
    public OfferGraphQLClient GraphQL { get; }

    /// <summary>
    /// DI-friendly constructor: StrawberryShake client injected by the container.
    /// </summary>
    internal OffersAndLeadsClient(
        OffersAndLeadsConfiguration config,
        HttpClient httpClient,
        IOffersAndLeadsGraphQLClient ssGqlClient,
        ITokenContext tokenContext)
    {
        _httpClient = httpClient;
        _ownsHttpClient = false;

        var auth = new BaseBearerTokenAuthenticationProvider(new KiotaAuthProvider(config, tokenContext));

        ClassifiedLeads = new ClassifiedLeadsClient(
            new FeedApiClient(new HttpClientRequestAdapter(auth, httpClient: BuildKiotaClient(config.LeadsBaseUrl, httpClient))),
            config,
            tokenContext);

        GraphQL = new OfferGraphQLClient(
            ssGqlClient,
            new DynamicGraphQLClient(httpClient, config, tokenContext),
            tokenContext);
    }

    /// <summary>
    /// Standalone constructor for use outside DI (tests, scripts).
    /// Creates its own HttpClient and does not use StrawberryShake DI.
    /// Typed GraphQL operations are unavailable; use GraphQL.ExecuteRawUnsafeAsync instead.
    /// </summary>
    public OffersAndLeadsClient(OffersAndLeadsConfiguration config)
        : this(config, BuildStandaloneHttpClient(config), null!, new AsyncLocalTokenContext())
    {
        _ownsHttpClient = true;
    }

    private static HttpClient BuildKiotaClient(string baseUrl, HttpClient httpClient)
    {
        httpClient.BaseAddress = new Uri(baseUrl);
        return httpClient;
    }

    private static HttpClient BuildStandaloneHttpClient(OffersAndLeadsConfiguration config)
    {
        var handler = new DiagnosticLoggingHandler(config)
        {
            InnerHandler = new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) }
        };
        return new HttpClient(handler) { Timeout = config.HttpTimeout };
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
    }
}
