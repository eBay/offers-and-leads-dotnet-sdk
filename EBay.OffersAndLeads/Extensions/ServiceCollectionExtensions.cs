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

using EBay.OffersAndLeads;
using EBay.OffersAndLeads.Clients.GraphQL;
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

        services.AddOffersAndLeadsGraphQLClient();
        services.AddTransient<BearerTokenHandler>();
        services
            .AddHttpClient(OffersAndLeadsClient.GraphQLClientName, client =>
                client.BaseAddress = new Uri(config.GraphQLBaseUrl))
            .ConfigureHttpClient(c => c.Timeout = config.HttpTimeout)
            .AddHttpMessageHandler<BearerTokenHandler>()
            .AddHttpMessageHandler(_ => new GraphQLHttpHandler(config));
        services.AddScoped<DynamicGraphQLClient>(sp =>
            new DynamicGraphQLClient(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(OffersAndLeadsClient.GraphQLClientName),
                sp.GetRequiredService<OffersAndLeadsConfiguration>(),
                sp.GetRequiredService<ITokenContext>()));

        foreach (var endpoint in OffersAndLeadsClient.RestEndpointNames)
        {
            var id = endpoint.ApiId;
            services.AddHttpClient(endpoint.HttpClientName, (sp, c) =>
                    c.BaseAddress = new Uri(sp.GetRequiredService<OffersAndLeadsConfiguration>().RestBaseUrl(id)))
                .ConfigureHttpClient((sp, c) =>
                    c.Timeout = sp.GetRequiredService<OffersAndLeadsConfiguration>().HttpTimeout)
                .AddHttpMessageHandler(sp =>
                    new DiagnosticLoggingHandler(sp.GetRequiredService<OffersAndLeadsConfiguration>()));
        }

        services.AddScoped<OffersAndLeadsClient>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new OffersAndLeadsClient(
                sp.GetRequiredService<OffersAndLeadsConfiguration>(),
                factory,
                sp.GetRequiredService<IOffersAndLeadsGraphQLClient>(),
                sp.GetRequiredService<ITokenContext>());
        });

        services.AddScoped(sp => sp.GetRequiredService<OffersAndLeadsClient>().ClassifiedLeads);
        services.AddScoped(sp => sp.GetRequiredService<OffersAndLeadsClient>().GraphQL);
        return services;
    }
}
