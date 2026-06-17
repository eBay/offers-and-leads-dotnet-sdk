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
using EBay.OffersAndLeads.Generated.GraphQL;
using Microsoft.Extensions.DependencyInjection;

namespace EBay.OffersAndLeads.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOffersAndLeadsSdk(
        this IServiceCollection services,
        OffersAndLeadsConfiguration config)
    {
        services.AddSingleton(config);
        services.AddSingleton<ITokenContext, AsyncLocalTokenContext>();

        // StrawberryShake: register generated client
        services.AddOffersAndLeadsGraphQLClient();

        // Wire auth into the named HTTP client that StrawberryShake uses
        services.AddTransient<BearerTokenHandler>();
        services
            .AddHttpClient("OffersAndLeadsGraphQLClient", client =>
                client.BaseAddress = new Uri(config.GraphQLBaseUrl))
            .AddHttpMessageHandler<BearerTokenHandler>()
            .ConfigureHttpClient(c => c.Timeout = config.HttpTimeout);

        // Dynamic escape hatch
        services.AddScoped<DynamicGraphQLClient>(sp =>
            new DynamicGraphQLClient(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("OffersAndLeadsGraphQLClient"),
                config,
                sp.GetRequiredService<ITokenContext>()));

        // Named HTTP client for the REST (Kiota) path — wires DiagnosticLoggingHandler into the DI pipeline
        services.AddHttpClient(nameof(OffersAndLeadsClient))
            .AddHttpMessageHandler(sp => new DiagnosticLoggingHandler(sp.GetRequiredService<OffersAndLeadsConfiguration>()))
            .ConfigureHttpClient(c => c.Timeout = config.HttpTimeout);

        // Register facade
        services.AddScoped<OffersAndLeadsClient>(sp =>
            new OffersAndLeadsClient(
                sp.GetRequiredService<OffersAndLeadsConfiguration>(),
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OffersAndLeadsClient)),
                sp.GetRequiredService<IOffersAndLeadsGraphQLClient>(),
                sp.GetRequiredService<ITokenContext>()));

        // Register sub-clients for direct injection
        services.AddScoped(sp => sp.GetRequiredService<OffersAndLeadsClient>().ClassifiedLeads);
        services.AddScoped(sp => sp.GetRequiredService<OffersAndLeadsClient>().GraphQL);

        return services;
    }
}
