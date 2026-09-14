using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Tokens;

namespace FinanceLedger.API.Auth;

/// <summary>
/// Fetches Supabase Auth's raw JWKS document (no full OIDC discovery doc is needed —
/// Supabase only exposes the bare JWKS endpoint) and parses it into a JsonWebKeySet,
/// so it can back a <see cref="ConfigurationManager{T}"/> that caches and
/// auto-refreshes the signing keys used to validate access tokens.
/// </summary>
public class JwksConfigurationRetriever : IConfigurationRetriever<JsonWebKeySet>
{
    public async Task<JsonWebKeySet> GetConfigurationAsync(
        string address, IDocumentRetriever retriever, CancellationToken cancel)
    {
        var document = await retriever.GetDocumentAsync(address, cancel);
        return JsonWebKeySet.Create(document);
    }
}
