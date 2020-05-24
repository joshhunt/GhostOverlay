using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using BungieNetApi.Model;
using Newtonsoft.Json;
using RestSharp;

namespace GhostOverlay
{
    [Serializable]
    public class BungieApiException : Exception
    {
        public BungieApiException()
        {
        }

        public BungieApiException(string message) : base(message)
        {
        }

        public BungieApiException(string message, Exception inner) : base(message, inner)
        {
        }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected BungieApiException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    public class BungieOAuthTokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public int refresh_expires_in { get; set; }
        public string membership_id { get; set; }
    }

    public enum DestinyComponent
    {
        Profiles = 100,
        VendorReceipts = 101,
        ProfileInventories = 102,
        ProfileCurrencies = 103,
        ProfileProgression = 104,
        PlatformSilver = 105,
        Characters = 200,
        CharacterInventories = 201,
        CharacterProgressions = 202,
        CharacterRenderData = 203,
        CharacterActivities = 204,
        CharacterEquipment = 205,
        ItemInstances = 300,
        ItemObjectives = 301,
        ItemPerks = 302,
        ItemRenderData = 303,
        ItemStats = 304,
        ItemSockets = 305,
        ItemTalentGrids = 306,
        ItemCommonData = 307,
        ItemPlugStates = 308,
        ItemPlugObjectives = 309,
        ItemReusablePlugs = 310,
        Vendors = 400,
        VendorCategories = 401,
        VendorSales = 402,
        Kiosks = 500,
        CurrencyLookups = 600,
        PresentationNodes = 700,
        Collectibles = 800,
        Records = 900,
        Transitory = 1000,
        Metrics = 1100
    }

    public class BungieApi
    {
        // Loaded from Configuration.resw by the constructor
        private readonly string apiKey;

        private readonly RestClient client;
        private readonly string clientId;
        private readonly string clientSecret;

        public readonly DestinyComponent[] DefaultProfileComponents =
        {
            DestinyComponent.Profiles, DestinyComponent.ProfileInventories, DestinyComponent.Characters,
            DestinyComponent.CharacterInventories, DestinyComponent.ItemInstances, DestinyComponent.ItemObjectives,
            DestinyComponent.Records
        };

        public BungieApi()
        {
            var resources = new ResourceLoader("Configuration");
            apiKey = resources.GetString("BungieApiKey");
            clientId = resources.GetString("BungieClientId");
            clientSecret = resources.GetString("BungieClientSecret");

            client = new RestClient("https://www.bungie.net");
            client.AddDefaultHeader("x-api-key", apiKey);
            client.UserAgent = "GhostOverlay/dev josh@trtr.co";
            client.CookieContainer = new CookieContainer();
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfile(int membershipType, long membershipId,
            DestinyComponent[] components, bool requireAuth = false)
        {
            var componentsStr = string.Join(",", components);
            return await GetBungie<DestinyResponsesDestinyProfileResponse>(
                $"Platform/Destiny2/{membershipType}/Profile/{membershipId}/?components={componentsStr}", requireAuth);
        }

        public async Task<UserUserMembershipData> GetMembershipsForCurrentUser()
        {
            return await GetBungie<UserUserMembershipData>("/Platform/User/GetMembershipsForCurrentUser/", true);
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfileForCurrentUser(
            DestinyComponent[] components)
        {
            var membershipData = await GetMembershipsForCurrentUser();
            var user = membershipData.DestinyMemberships.Find(p =>
                p.MembershipId == membershipData.PrimaryMembershipId);

            if (user == null)
            {
                Debug.WriteLine("TODO: Unable to find primary membership, so just returning the 0th one");
                user = membershipData.DestinyMemberships[0];
            }

            return await GetProfile(user.MembershipType, user.MembershipId, components, true);
        }

        public Task<DestinyConfigDestinyManifest> GetManifest()
        {
            return GetBungie<DestinyConfigDestinyManifest>("/Platform/Destiny2/Manifest");
        }

        public async Task<T> GetBungie<T>(string path, bool requireAuth = false)
        {
            Debug.WriteLine($"REQUEST {path}");
            var request = new RestRequest(path);

            if (AppState.Data.TokenData != null && AppState.Data.TokenData.RefreshTokenIsValid()) await EnsureTokenDataIsValid();

            if (requireAuth && (AppState.Data.TokenData == null || !AppState.Data.TokenData.AccessTokenIsValid()))
                throw new BungieApiException("Auth was required but the access token was not valid");

            if (AppState.Data.TokenData != null && AppState.Data.TokenData.AccessTokenIsValid())
            {
                var headerValue = $"Bearer {AppState.Data.TokenData.AccessToken}";

                request.AddHeader("authorization", headerValue);
            }

            var response = await client.ExecuteAsync(request);

            // TODO: handle errors when there's just no response at all

            if (response.ContentType.Contains("application/json") != true)
                throw new BungieApiException("API did not return JSON");

            var data = JsonConvert.DeserializeObject<BungieApiResponse<T>>(response.Content);

            if (data.ErrorStatus.Equals("Success") != true)
                throw new BungieApiException($"Bungie API Error {data.ErrorStatus}: {data.Message}");

            if (data.Response == null) throw new BungieApiException("API did not return JSON");

            return data.Response;
        }

        public async Task<bool> EnsureTokenDataIsValid()
        {
            if (AppState.Data.TokenData == null)
            {
                Debug.WriteLine("ERROR: TokenData is null");
                throw new BungieApiException("TokenData is null when attempted to refresh it");
            }

            if (AppState.Data.TokenData.IsValid() != true)
            {
                Debug.WriteLine("ERROR: TokenData is not valid");
                throw new BungieApiException("TokenData is not valid when attempted to refresh it");
            }

            if (AppState.Data.TokenData.AccessTokenIsValid()) return true;

            if (AppState.Data.TokenData.RefreshTokenIsValid())
            {
                Debug.WriteLine("Need to refresh access token!");
                await RefreshOAuthAccessToken();
                Debug.WriteLine("Successfully refreshed token. New TokenData:");
                Debug.WriteLine(AppState.Data.TokenData);
                return true;
            }

            throw new BungieApiException(
                "Unhandled scenario while ensuring TokenData is valid, which shouldn't happen!!");
        }

        public async Task RefreshOAuthAccessToken()
        {
            if (AppState.Data.TokenData == null || AppState.Data.TokenData.RefreshTokenIsValid() != true)
            {
                // TODO: throw exception?
                Debug.WriteLine("ERROR: have no valid refresh token to use to refresh");
                return;
            }

            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=refresh_token&refresh_token={AppState.Data.TokenData.RefreshToken}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            AppState.Data.TokenData = new OAuthToken(data.access_token, data.refresh_token,
                data.expires_in, data.refresh_expires_in, AppState.Data.TokenData.Version);
            AppState.Data.TokenData.SaveToSettings();
        }

        public async Task GetOAuthAccessToken(string authCode)
        {
            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=authorization_code&code={authCode}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            AppState.Data.TokenData = new OAuthToken(data.access_token, data.refresh_token,
                data.expires_in, data.refresh_expires_in, OAuthToken.CurrentVersion);

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