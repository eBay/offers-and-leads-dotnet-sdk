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
using System.Net.Http.Headers;

namespace EBay.OffersAndLeads.Clients.GraphQL;

internal sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;

    public BearerTokenHandler(OffersAndLeadsConfiguration config, ITokenContext tokenContext)
    {
        _config = config;
        _tokenContext = tokenContext;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = _tokenContext.Current ?? _config.AccessToken;
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, ct);
    }
}
