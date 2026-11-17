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
using EBay.OffersAndLeads.Generated.Negotiation.Models;
using EBay.OffersAndLeads.Generated.Negotiation;
using Microsoft.Kiota.Abstractions;

namespace EBay.OffersAndLeads.Clients.Rest;

public sealed class NegotiationClient
{
    private readonly NegotiationApiClient _kiota;
    private readonly OffersAndLeadsConfiguration _config;
    private readonly ITokenContext _tokenContext;
    private readonly IRequestAdapter _adapter;

    internal NegotiationClient(NegotiationApiClient kiota, OffersAndLeadsConfiguration config, ITokenContext tokenContext, IRequestAdapter adapter)
    {
        _kiota = kiota;
        _config = config;
        _tokenContext = tokenContext;
        _adapter = adapter;
    }

    /// <summary>
    /// This method evaluates a seller's current listings and returns the set of IDs that are eligible for a seller-initiated discount offer to a buyer.
    /// A listing ID is returned only when one or more buyers have shown an "interest" in the listing.
    /// If any buyers have shown interest in a listing, the seller can initiate a "negotiation" with them by calling <see href="https://developer.ebay.com/api-docs/sell/negotiation/resources/offer/methods/sendOfferToInterestedBuyers">sendOfferToInterestedBuyers</see>, which sends all interested buyers a message that offers the listing at a discount.
    /// For details about how to create seller offers to buyers, see <see href="https://developer.ebay.com/api-docs/sell/static/marketing/offers-to-buyers.html">Sending offers to buyers</see>
    /// </summary>
    public Task<PagedEligibleItemCollection?> FindEligibleItemsAsync(
        string xEBAYCMARKETPLACEID,
        int? limit = null,
        int? offset = null,
        string? accessToken = null,
        RequestOptions? requestOptions = null,
        CancellationToken ct = default)
        => RestInvocation.CallAsync(
            _tokenContext,
            accessToken,
            () => _kiota.Find_eligible_items.GetAsync(cfg =>
        {
            if (limit is not null)
            {
                cfg.QueryParameters.Limit = limit;
            }
            if (offset is not null)
            {
                cfg.QueryParameters.Offset = offset;
            }
            cfg.Headers.TryAdd("X-EBAY-C-MARKETPLACE-ID", xEBAYCMARKETPLACEID);
            RestInvocation.ApplyHeaders(cfg.Headers, _config, requestOptions);
        }, ct));

    /// <summary>
    /// This method sends eligible buyers offers to purchase items in a listing at a discount.
    /// When a buyer has shown <i>interest</i> in a listing, they become "eligible" to receive a seller-initiated offer to purchase the item(s).
    /// Sellers use <see href="https://developer.ebay.com/api-docs/sell/negotiation/resources/offer/methods/findEligibleItems">findEligibleItems</see> to get the set of listings that have interested buyers. If a listing has interested buyers, sellers can use this method (<b>sendOfferToInterestedBuyers</b>) to send an offer to the buyers who are interested in the listing. The offer gives buyers the ability to purchase the associated listings at a discounted price.
    /// For details about how to create seller offers to buyers, see <see href="https://developer.ebay.com/api-docs/sell/static/marketing/offers-to-buyers.html">Sending offers to buyers</see>.
    /// </summary>
    public Task<SendOfferToInterestedBuyersCollectionResponse?> SendOfferToInterestedBuyersAsync(
        string xEBAYCMARKETPLACEID,
        CreateOffersRequest body,
        string? accessToken = null,
        RequestOptions? requestOptions = null,
        CancellationToken ct = default)
        => RestInvocation.CallAsync(
            _tokenContext,
            accessToken,
            () => _kiota.Send_offer_to_interested_buyers.PostAsync(body, cfg =>
        {
            cfg.Headers.TryAdd("X-EBAY-C-MARKETPLACE-ID", xEBAYCMARKETPLACEID);
            RestInvocation.ApplyHeaders(cfg.Headers, _config, requestOptions);
        }, ct));
}
