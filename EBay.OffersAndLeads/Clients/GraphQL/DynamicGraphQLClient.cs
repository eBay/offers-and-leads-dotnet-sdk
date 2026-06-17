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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EBay.OffersAndLeads.Clients.GraphQL;

internal sealed class DynamicGraphQLClient
{
    private readonly HttpClient _httpClient;
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;

    internal DynamicGraphQLClient(HttpClient httpClient, OffersAndLeadsConfiguration config, ITokenContext tokenContext)
    {
        _httpClient = httpClient;
        _config = config;
        _tokenContext = tokenContext;
    }

    /// <summary>
    /// Executes an arbitrary GraphQL query string against the API.
    /// <para>
    /// <b>WARNING — injection risk.</b> Never interpolate user-supplied input directly into
    /// <paramref name="query"/>. Always pass dynamic values via <paramref name="variables"/>
    /// so the server handles escaping. Treat this method like a raw SQL string: safe only
    /// with static, developer-controlled query text.
    /// </para>
    /// </summary>
    public async Task<JsonDocument> ExecuteRawUnsafeAsync(
        string query, object? variables = null, string? accessToken = null, CancellationToken ct = default)
    {
        var token = accessToken ?? _tokenContext.Current ?? _config.AccessToken
            ?? throw new InvalidOperationException(
                "No access token available. Pass an accessToken per call or set OffersAndLeadsConfiguration.AccessToken.");

        var body = JsonSerializer.Serialize(new { query, variables });
        var req = new HttpRequestMessage(HttpMethod.Post, _config.GraphQLBaseUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _httpClient.SendAsync(req, ct);
        var responseBody = await resp.Content.ReadAsStringAsync(ct);

        if (_config.DiagnosticLogging && !resp.IsSuccessStatusCode)
        {
            var safeUrl = _config.GraphQLBaseUrl?.ReplaceLineEndings(" ") ?? string.Empty;
            Console.Error.WriteLine($"[SDK] POST {safeUrl} → {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedException();

        if (!resp.IsSuccessStatusCode)
            throw new GraphQLException([$"HTTP {(int)resp.StatusCode}: {responseBody}"]);

        var doc = JsonDocument.Parse(responseBody);
        if (doc.RootElement.TryGetProperty("errors", out var errs) && errs.GetArrayLength() > 0)
        {
            var msgs = errs.EnumerateArray()
                .Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "unknown" : "unknown")
                .ToArray();
            throw new GraphQLException(msgs);
        }
        return doc;
    }
}
