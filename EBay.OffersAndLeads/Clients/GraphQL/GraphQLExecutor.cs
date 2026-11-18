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
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EBay.OffersAndLeads.Clients.GraphQL;

/// <summary>A GraphQL error returned by the server or raised while reading the response.</summary>
internal sealed class GraphQLError
{
    public GraphQLError(string message, string? code = null, Exception? exception = null)
    {
        Message = message;
        Code = code;
        Exception = exception;
    }

    public string Message { get; }
    public string? Code { get; }
    public Exception? Exception { get; }
}

internal sealed class GraphQLResponse<T> where T : class
{
    public GraphQLResponse(T? data, IReadOnlyList<GraphQLError> errors)
    {
        Data = data;
        Errors = errors;
    }

    public T? Data { get; }
    public IReadOnlyList<GraphQLError> Errors { get; }
}

/// <summary>Non-success HTTP status from the GraphQL endpoint (net472 HttpRequestException has no StatusCode).</summary>
internal sealed class GraphQLHttpException : HttpRequestException
{
    public GraphQLHttpException(HttpStatusCode statusCode, string? reasonPhrase)
        : base($"Response status code does not indicate success: {(int)statusCode} ({reasonPhrase}).")
        => HttpStatus = statusCode;

    public HttpStatusCode HttpStatus { get; }
}

/// <summary>Executes generated GraphQL operations over HTTP using System.Text.Json only.</summary>
internal sealed class GraphQLExecutor
{
    private readonly HttpClient _http;
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;

    internal GraphQLExecutor(HttpClient http, OffersAndLeadsConfiguration config, ITokenContext tokenContext)
    {
        _http = http;
        _config = config;
        _tokenContext = tokenContext;
    }

    internal async Task<GraphQLResponse<T>> ExecuteAsync<T>(
        string operationName,
        string document,
        Action<Utf8JsonWriter>? writeVariables,
        Func<JsonElement, T> parse,
        CancellationToken ct) where T : class
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _http.BaseAddress ?? new Uri(_config.GraphQLBaseUrl));
        var token = _tokenContext.Current ?? _config.AccessToken;
        // Never hand the token to a host outside the trusted list.
        if (token is not null && _config.IsTrustedHost(request.RequestUri))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new ByteArrayContent(RequestBody(operationName, document, writeVariables));
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new GraphQLHttpException(response.StatusCode, response.ReasonPhrase);

#if NETFRAMEWORK
        var body = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#else
        var body = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
#endif
        return ReadResponse(body, parse);
    }

    private static byte[] RequestBody(string operationName, string document, Action<Utf8JsonWriter>? writeVariables)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("query", document);
            writer.WriteString("operationName", operationName);
            // Operations without variables omit the "variables" member.
            if (writeVariables is not null)
            {
                writer.WritePropertyName("variables");
                writeVariables(writer);
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static GraphQLResponse<T> ReadResponse<T>(byte[] body, Func<JsonElement, T> parse) where T : class
    {
        var errors = new List<GraphQLError>();
        T? data = null;
        try
        {
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.TryGetProperty("errors", out var errorArray) && errorArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var error in errorArray.EnumerateArray())
                    errors.Add(ReadError(error));
            }
            if (root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
            {
                try
                {
                    data = parse(dataElement);
                }
                catch (Exception ex) when (ex is GraphQLResultException or InvalidOperationException or FormatException)
                {
                    errors.Insert(0, new GraphQLError(ex.Message, "InvalidResultDataStructure", ex));
                }
            }
        }
        catch (JsonException ex)
        {
            errors.Add(new GraphQLError(ex.Message, "InvalidResultDataStructure", ex));
        }
        return new GraphQLResponse<T>(data, errors);
    }

    private static GraphQLError ReadError(JsonElement error)
    {
        var message = error.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
            ? m.GetString()!
            : "The error format is invalid and was missing the property `message`.";
        string? code = null;
        if (error.TryGetProperty("extensions", out var extensions)
            && extensions.ValueKind == JsonValueKind.Object
            && extensions.TryGetProperty("code", out var c)
            && c.ValueKind == JsonValueKind.String)
        {
            code = c.GetString();
        }
        return new GraphQLError(message, code);
    }
}
