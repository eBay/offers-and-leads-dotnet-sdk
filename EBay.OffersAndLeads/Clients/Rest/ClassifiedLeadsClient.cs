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
using EBay.OffersAndLeads.Generated.Feed;
using EBay.OffersAndLeads.Generated.Feed.Models;
using Microsoft.Kiota.Abstractions;

namespace EBay.OffersAndLeads.Clients.Rest;

public sealed class ClassifiedLeadsClient
{
    private readonly FeedApiClient _kiota;
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;

    internal ClassifiedLeadsClient(FeedApiClient kiota, OffersAndLeadsConfiguration config, ITokenContext tokenContext)
    {
        _kiota = kiota;
        _config = config;
        _tokenContext = tokenContext;
    }

    public async Task<ClassifiedLeadsListResponse?> GetAllClassifiedLeadsAsync(
        ClassifiedLeadsFilter? filter = null,
        string? accessToken = null,
        RequestOptions? requestOptions = null,
        CancellationToken ct = default)
    {
        try
        {
            return await _tokenContext.RunAsync(accessToken, () =>
                _kiota.Classified_lead.GetAsync(
                    requestConfiguration: cfg =>
                    {
                        ApplyHeaders(cfg.Headers, requestOptions);
                        if (filter is null) return;
                        cfg.QueryParameters.StartTime = filter.StartTime;
                        cfg.QueryParameters.EndTime = filter.EndTime;
                        cfg.QueryParameters.Status = filter.Status;
                        cfg.QueryParameters.IncludeMessages = filter.IncludeMessages;
                    },
                    cancellationToken: ct));
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException(ex);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
        catch (ApiException ex)
        {
            throw new SdkException($"REST API error ({ex.ResponseStatusCode})", ex);
        }
    }

    public async Task<ClassifiedLead?> GetClassifiedLeadsByItemIdAsync(
        string itemId,
        ClassifiedLeadsFilter? filter = null,
        string? accessToken = null,
        RequestOptions? requestOptions = null,
        CancellationToken ct = default)
    {
        try
        {
            return await _tokenContext.RunAsync(accessToken, () =>
                _kiota.Classified_lead[itemId].GetAsync(
                    requestConfiguration: cfg =>
                    {
                        ApplyHeaders(cfg.Headers, requestOptions);
                        if (filter is null) return;
                        cfg.QueryParameters.StartTime = filter.StartTime;
                        cfg.QueryParameters.EndTime = filter.EndTime;
                        cfg.QueryParameters.Status = filter.Status;
                        cfg.QueryParameters.IncludeMessages = filter.IncludeMessages;
                    },
                    cancellationToken: ct));
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == (int)HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedException(ex);
        }
        catch (ApiException ex) when (ex.ResponseStatusCode == 404)
        {
            return null;
        }
        catch (ApiException ex)
        {
            throw new SdkException($"REST API error ({ex.ResponseStatusCode})", ex);
        }
    }

    private void ApplyHeaders(RequestHeaders headers, RequestOptions? callOptions)
    {
        foreach (var (k, v) in _config.DefaultRequestOptions.Headers)
            headers.TryAdd(k, v);
        if (callOptions is null) return;
        foreach (var (k, v) in callOptions.Headers)
            headers[k] = [v];
    }
}
