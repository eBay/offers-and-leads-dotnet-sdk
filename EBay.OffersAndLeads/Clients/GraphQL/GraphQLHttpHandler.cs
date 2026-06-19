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
using System.Text;

namespace EBay.OffersAndLeads.Clients.GraphQL;

/// <summary>
/// Delegating handler for the StrawberryShake GraphQL HTTP pipeline.
/// Ensures non-2xx responses throw <see cref="HttpRequestException"/> (with status code) instead of
/// silently yielding zero results, and logs requests/responses when DiagnosticLogging is enabled.
/// </summary>
internal sealed class GraphQLHttpHandler(OffersAndLeadsConfiguration config) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        if (config.DiagnosticLogging)
            Console.Error.WriteLine($"[SDK GraphQL] {request.Method} {request.RequestUri}");

        var response = await base.SendAsync(request, ct).ConfigureAwait(false);

        if (config.DiagnosticLogging)
            Console.Error.WriteLine($"[SDK GraphQL] → {(int)response.StatusCode} {response.ReasonPhrase}");

        if (config.DiagnosticLogging)
        {
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            Console.Error.WriteLine($"[SDK GraphQL] Body: {body}");
            var bytes = Encoding.UTF8.GetBytes(body);
            var replaced = new ByteArrayContent(bytes);
            if (response.Content.Headers.ContentType is { } ct2)
                replaced.Headers.ContentType = ct2;
            response.Content = replaced;
        }

        if (!response.IsSuccessStatusCode)
            response.EnsureSuccessStatusCode();

        return response;
    }
}
