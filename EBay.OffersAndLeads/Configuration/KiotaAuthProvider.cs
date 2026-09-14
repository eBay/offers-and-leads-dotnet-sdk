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


using Microsoft.Kiota.Abstractions.Authentication;

namespace EBay.OffersAndLeads.Configuration;

internal sealed class KiotaAuthProvider : IAccessTokenProvider
{
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;

    public AllowedHostsValidator AllowedHostsValidator { get; }

    public KiotaAuthProvider(OffersAndLeadsConfiguration config, ITokenContext tokenContext)
    {
        _config = config;
        _tokenContext = tokenContext;
        AllowedHostsValidator = new AllowedHostsValidator(OffersAndLeadsConfiguration.AllowedHosts);
    }

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        var token = _tokenContext.Current ?? _config.AccessToken;
        if (token is null)
            throw new InvalidOperationException(
                "No access token available. Pass an accessToken per call or set OffersAndLeadsConfiguration.AccessToken.");
        return Task.FromResult(token);
    }
}
