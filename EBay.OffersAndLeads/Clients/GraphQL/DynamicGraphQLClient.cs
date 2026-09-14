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
using System.Text.Json.Serialization;

namespace EBay.OffersAndLeads.Clients.GraphQL;

internal sealed class DynamicGraphQLClient
{
    private readonly HttpClient _http;
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal DynamicGraphQLClient(HttpClient http, OffersAndLeadsConfiguration config, ITokenContext tokenContext)
    {
        _http = http;
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

        using var req = new HttpRequestMessage(HttpMethod.Post, _config.GraphQLBaseUrl);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { query, variables }, JsonOpts),
            Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (_config.DiagnosticLogging && !resp.IsSuccessStatusCode)
        {
            var safeUrl = _config.GraphQLBaseUrl?.ReplaceLineEndings(" ") ?? string.Empty;
            Console.Error.WriteLine($"[SDK] POST {safeUrl} → {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedException();

        if (!resp.IsSuccessStatusCode)
            throw new GraphQLException([$"HTTP {(int)resp.StatusCode}: {body}"]);

        var doc = JsonDocument.Parse(body);

        if (doc.RootElement.TryGetProperty("errors", out var errs)
            && errs.ValueKind == JsonValueKind.Array
            && errs.GetArrayLength() > 0)
        {
            var msgs = errs.EnumerateArray()
                .Select(e => e.TryGetProperty("message", out var m) ? m.GetString() ?? "" : "")
                .ToArray();
            doc.Dispose();
            throw new GraphQLException(msgs);
        }

        return doc;
    }
}
