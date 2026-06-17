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


namespace EBay.OffersAndLeads.Configuration;

public sealed class OffersAndLeadsConfiguration
{
    private string _leadsBaseUrl = "https://apiz.ebay.com/sell/leads/v1";
    private string _graphQLBaseUrl = "https://api.ebay.com/graphql";

    public string LeadsBaseUrl
    {
        get => _leadsBaseUrl;
        set => _leadsBaseUrl = RequireHttps(value, nameof(LeadsBaseUrl));
    }

    public string GraphQLBaseUrl
    {
        get => _graphQLBaseUrl;
        set => _graphQLBaseUrl = RequireHttps(value, nameof(GraphQLBaseUrl));
    }

    /// <summary>Fallback bearer token used when no per-call accessToken is supplied.</summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// When true, logs the HTTP method, URL, and status code of non-success responses to stderr.
    /// Response bodies are intentionally excluded to avoid leaking PII.
    /// <para>
    /// <b>WARNING — PII risk.</b> URLs may contain item IDs and query parameters that are
    /// sensitive in context. Never enable this in production. Ensure logs are access-controlled
    /// and not persisted to any external system when enabled in non-production environments.
    /// </para>
    /// </summary>
    public bool DiagnosticLogging { get; set; }

    /// <summary>Timeout applied to all HTTP requests. Defaults to 30 seconds.</summary>
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Headers applied to every REST request. Per-call RequestOptions override these.</summary>
    public RequestOptions DefaultRequestOptions { get; set; } = new();

    internal IEnumerable<string> AllowedHosts => ["api.ebay.com", "apim.ebay.com", "apiz.ebay.com"];

    private static string RequireHttps(string value, string paramName)
    {
        if (!value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only HTTPS endpoints are allowed.", paramName);
        return value;
    }
}
