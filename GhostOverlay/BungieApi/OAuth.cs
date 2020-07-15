using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading.Tasks;
using RestSharp;

namespace GhostOverlay
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class BungieOAuthTokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public int refresh_expires_in { get; set; }
        public string membership_id { get; set; }
    }

    public partial class BungieApi
    {
        public async Task<bool> EnsureTokenDataIsValid()
        {
            if (AppState.Data.TokenData == null)
            {
                Log.Error("ERROR: TokenData is null");
                throw new BungieApiException("TokenData is null when attempted to refresh it");
            }

            if (AppState.Data.TokenData.IsValid() != true)
            {
                Log.Error("ERROR: TokenData is not valid");
                throw new BungieApiException("TokenData is not valid when attempted to refresh it");
            }

            if (
                (AppState.Data.TokenData.BungieMembershipId == null ||
                 AppState.Data.TokenData.BungieMembershipId.Length <= 1) &&
                AppState.Data.TokenData.RefreshTokenIsValid())
            {
                Log.Debug("Token lacks BungieMembershipId");
                await RefreshOAuthAccessToken();
                return true;
            }

            if (AppState.Data.TokenData.AccessTokenIsValid()) return true;

            if (AppState.Data.TokenData.RefreshTokenIsValid())
            {
                Log.Debug("Need to refresh access token!");
                await RefreshOAuthAccessToken();
                return true;
            }

            Log.Error("Unhandled scenario while ensuring TokenData is valid");
            throw new BungieApiException(
                "Unhandled scenario while ensuring TokenData is valid, which shouldn't happen!!");
        }

        public async Task RefreshOAuthAccessToken()
        {
            Log.Info("RefreshOAuthAccessToken");
            if (AppState.Data.TokenData == null || AppState.Data.TokenData.RefreshTokenIsValid() != true)
            {
                // TODO: throw exception?
                Log.Error("ERROR: have no valid refresh token to use to refresh");
                return;
            }

            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=refresh_token&refresh_token={AppState.Data.TokenData.RefreshToken}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            Log.Info("auth membershipID {membershipId}", data.membership_id);

            AppState.Data.TokenData = new OAuthToken(data, AppState.Data.TokenData.Version);
            AppState.Data.TokenData.SaveToSettings();
        }

        public async Task GetOAuthAccessToken(string authCode)
        {
            Log.Debug("GetOAuthAccessToken");
            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=authorization_code&code={authCode}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            Log.Debug("auth membershipID {membershipId}", data.membership_id);

            AppState.Data.TokenData = new OAuthToken(data, OAuthToken.CurrentVersion);
            AppState.Data.TokenData.SaveToSettings();
        }

        public string GetAuthorisationUrl()
        {
            return $"https://www.bungie.net/en/OAuth/Authorize?client_id={clientId}&response_type=code";
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
