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

using System.IO;
using System.Net;
using System.Net.Http;
using EBay.OffersAndLeads.Configuration;
using EBay.OffersAndLeads.Exceptions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;

#nullable enable

namespace EBay.OffersAndLeads.Clients.Rest;

internal static class RestInvocation
{
    internal static async Task<T> CallAsync<T>(
        ITokenContext tokenContext,
        string? accessToken,
        Func<Task<T>> op)
    {
        try
        {
            return await tokenContext.RunAsync(accessToken, op);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException(ex);
        }
    }

    internal static async Task CallAsync(
        ITokenContext tokenContext,
        string? accessToken,
        Func<Task> op)
    {
        try
        {
            await tokenContext.RunAsync(accessToken, op);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException(ex);
        }
    }

    internal static void ApplyHeaders(
        RequestHeaders headers,
        OffersAndLeadsConfiguration config,
        RequestOptions? callOptions)
    {
        foreach (var (k, v) in config.DefaultRequestOptions.Headers)
        {
            headers.TryAdd(k, v);
        }
        if (callOptions is null)
        {
            return;
        }
        foreach (var (k, v) in callOptions.Headers)
        {
            headers[k] = new[] { v };
        }
    }

    internal static async Task<T?> SendMultipartFileAsync<T>(
        IRequestAdapter adapter,
        RequestInformation requestInfo,
        string partName,
        Stream fileContent,
        string fileName,
        ParsableFactory<T> responseFactory,
        CancellationToken ct) where T : IParsable
    {
        await AttachMultipartFileAsync(requestInfo, partName, fileContent, fileName, ct);
        return await adapter.SendAsync(requestInfo, responseFactory, default, ct);
    }

    internal static async Task SendMultipartFileAsync(
        IRequestAdapter adapter,
        RequestInformation requestInfo,
        string partName,
        Stream fileContent,
        string fileName,
        CancellationToken ct)
    {
        await AttachMultipartFileAsync(requestInfo, partName, fileContent, fileName, ct);
        await adapter.SendNoContentAsync(requestInfo, default, ct);
    }

    private static async Task AttachMultipartFileAsync(
        RequestInformation requestInfo,
        string partName,
        Stream fileContent,
        string fileName,
        CancellationToken ct)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StreamContent(fileContent), partName, fileName);
        var memStream = new MemoryStream();
        await multipart.CopyToAsync(memStream, ct);
        memStream.Position = 0;
        requestInfo.SetStreamContent(memStream, multipart.Headers.ContentType!.ToString());
    }
}
