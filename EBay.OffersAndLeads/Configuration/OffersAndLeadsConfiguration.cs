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
    private string _classifiedLeadsBaseUrl = "https://apiz.ebay.com/sell/leads/v1";
    private string _graphQLBaseUrl = "https://api.ebay.com/graphql";

    public string ClassifiedLeadsBaseUrl
    {
        get => _classifiedLeadsBaseUrl;
        set => _classifiedLeadsBaseUrl = RequireHttps(value);
    }

    public string GraphQLBaseUrl
    {
        get => _graphQLBaseUrl;
        set => _graphQLBaseUrl = RequireHttps(value);
    }

    /// <summary>Fallback bearer token used when no per-call accessToken is supplied.</summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// When true, logs the HTTP method, URL, status code, and response reason phrase for all
    /// non-success responses to stderr.
    /// <para>
    /// <b>WARNING — PII risk.</b> Response bodies and headers may contain personally identifiable
    /// information. Do not enable in production without appropriate data-handling controls.
    /// </para>
    /// </summary>
    public bool DiagnosticLogging { get; set; }

    /// <summary>Timeout applied to all HTTP requests. Defaults to 30 seconds.</summary>
    public TimeSpan HttpTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Default headers applied to every REST request. Per-call RequestOptions override these.</summary>
    public RequestOptions DefaultRequestOptions { get; set; } = new();

    internal string RestBaseUrl(string apiId) => apiId switch
    {
        "classified-leads" => _classifiedLeadsBaseUrl,
        _ => throw new ArgumentOutOfRangeException(nameof(apiId), apiId, "Unknown REST API.")
    };

    internal static IEnumerable<string> AllowedHosts =>
    [
        "apiz.ebay.com",
        "api.ebay.com"
    ];

    private static string RequireHttps(string value)
    {
        if (!value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"URL must use HTTPS: '{value}'", nameof(value));
        }
        return value;
    }
}
