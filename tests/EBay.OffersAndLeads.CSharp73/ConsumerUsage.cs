using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EBay.OffersAndLeads;
using EBay.OffersAndLeads.Configuration;
using EBay.OffersAndLeads.Exceptions;

namespace EBay.OffersAndLeads.CSharp73
{
    /// <summary>Typical consumer code written in C# 7.3; it only has to compile.</summary>
    public static class ConsumerUsage
    {
        public static async Task<string> RunAsync()
        {
            var config = new OffersAndLeadsConfiguration
            {
                AccessToken = "token",
                HttpTimeout = TimeSpan.FromSeconds(60),
                DiagnosticLogging = true,
                ClassifiedLeadsBaseUrl = "https://ebay-proxy.example.com/sell/leads/v1",
                AllowedHosts = { "ebay-proxy.example.com" },
                DefaultRequestOptions = new RequestOptions
                {
                    Headers = new Dictionary<string, string> { { "X-EBAY-C-MARKETPLACE-ID", "EBAY_US" } }
                }
            };

            using (var client = new OffersAndLeadsClient(config))
            {
                try
                {
                    var result = await client.ClassifiedLeads.GetAllClassifiedLeadsAsync();
                    return result != null ? "ok" : "empty";
                }
                catch (UnauthorizedException)
                {
                    return "unauthorized";
                }
            }
        }
    }
}
