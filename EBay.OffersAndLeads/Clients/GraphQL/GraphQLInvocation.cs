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

using System.Linq;
using System.Net;
using System.Net.Http;
using EBay.OffersAndLeads.Exceptions;

#nullable enable

namespace EBay.OffersAndLeads.Clients.GraphQL;

internal static class GraphQLInvocation
{
    internal static async Task<GraphQLResponse<T>> ExecuteAsync<T>(Func<Task<GraphQLResponse<T>>> call)
        where T : class
    {
        try
        {
            return await call().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not SdkException)
        {
            throw WrapException(ex);
        }
    }

    internal static SdkException WrapException(Exception ex) => ex switch
    {
        GraphQLHttpException { HttpStatus: HttpStatusCode.Unauthorized } => new UnauthorizedException(ex),
        GraphQLHttpException http =>
            new SdkException($"GraphQL request failed with HTTP {(int)http.HttpStatus}: {http.Message}", ex),
        HttpRequestException http =>
            new SdkException($"GraphQL request failed (network error): {http.Message} — check that GraphQLBaseUrl is reachable.", ex),
        TaskCanceledException =>
            new SdkException("GraphQL request timed out — check that GraphQLBaseUrl is reachable and the network is accessible.", ex),
        _ => new SdkException($"GraphQL request failed: {ex.Message}", ex)
    };

    internal static void EnsureNoErrors<T>(GraphQLResponse<T> result)
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
