using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Text;
using BungieNetApi.Model;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Validation;

namespace GhostOverlay
{
    [Serializable]
    public class BungieApiException : Exception
    {
        public BungieApiResponse<object> Response;

        public BungieApiException()
        {
        }

        public BungieApiException(string message) : base(message)
        {
        }

        public BungieApiException(string message, Exception inner) : base(message, inner)
        {
        }

        // exception propagates from a remoting server to the client. 
        protected BungieApiException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public string RequestPath { get; set; }
        public RestRequest Request { get; set; }
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

    [Flags]
    public enum DestinyItemState
    {
        None = 0,
        Locked = 1,
        Tracked = 2,
        Masterwork = 4,
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
        private readonly Logger Log = new Logger("BungieApi");

        // Loaded from Configuration.resw by the constructor
        private readonly string apiKey;

        private readonly RestClient client;
        private readonly string clientId;
        private readonly string clientSecret;

        private static readonly long BucketShips = 284967655;
        private static readonly long BucketSparrows = 2025709351;

        public readonly DestinyComponent[] DefaultProfileComponents =
        {
            DestinyComponent.Profiles, DestinyComponent.ProfileInventories, DestinyComponent.Characters, DestinyComponent.CharacterInventories, DestinyComponent.CharacterEquipment, DestinyComponent.ItemInstances, DestinyComponent.ItemObjectives, DestinyComponent.Records, DestinyComponent.CharacterActivities
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

        public async Task<DestinyResponsesDestinyLinkedProfilesResponse> GetLinkedProfiles()
        {
            // If the user's saved access token doesn't have a BungieMembershipId yet,
            // this will effectively upgrade it to one that does :) :) :) 
            await EnsureTokenDataIsValid();

            if (AppState.Data.TokenData.BungieMembershipId == null)
            {
                throw new Exception("TokenData somehow lacks a BungieMembershipId. This is very bad.");
            }

            return await GetBungie<DestinyResponsesDestinyLinkedProfilesResponse>($"/Platform/Destiny2/254/Profile/{AppState.Data.TokenData.BungieMembershipId}/LinkedProfiles/", true);
        }

        private (string characterId, DestinyEntitiesItemsDestinyItemComponent item) FindItemToLock(
            DestinyResponsesDestinyProfileResponse profile)
        {
            string selectedCharacterId = default;
            DestinyEntitiesItemsDestinyItemComponent selectedItem = default;

            foreach (var (characterId, characterInventory) in profile.CharacterEquipment.Data)
            {
                foreach (var item in characterInventory.Items)
                {
                    if (item.ItemInstanceId != null && item.ItemInstanceId != 0 && (item.BucketHash == BucketShips ||
                                                                                    item.BucketHash == BucketSparrows))
                    {
                        selectedCharacterId = characterId;
                        selectedItem = item;
                        break;
                    }
                }

                if (selectedCharacterId != null)
                {
                    break;
                }
            }

            return (characterId: selectedCharacterId, item: selectedItem);
        }

        private class SetLockStatePayload
        {
            [JsonProperty("state")]
            public bool State { get; set; }

            [JsonProperty("itemId")]
            public long ItemId { get; set; }

            [JsonProperty("characterId")]
            public string CharacterId { get; set; }

            [JsonProperty("membershipType")]
            public int MembershipType { get; set; }
        }

        public async Task CacheBust(DestinyResponsesDestinyProfileResponse profile)
        {
            var (characterId, item) = FindItemToLock(profile);
            var itemState = (DestinyItemState) (item?.State ?? 0);
            var itemIsLocked = itemState == DestinyItemState.Locked;

            if (item != null)
            {
                Log.Info("Found item to toggle lock state, character ID {characterId}, {itemHash}:{itemInstanceId}. locked state:{locked}", characterId, item.ItemHash, item.ItemInstanceId, itemIsLocked);

                try
                {
                    await SetLockState(itemIsLocked, item.ItemInstanceId, characterId,
                        profile.Profile.Data.UserInfo.MembershipType);
                }
                catch (Exception err)
                {
                    Log.Error("Error busting profile cache, silently ignoring {Error}", err);
                }
            }
        }

        private async Task SetLockState(bool itemState, long itemItemInstanceId, string characterId, int membershipType)
        {
            var payload = new SetLockStatePayload() { State = itemState, ItemId = itemItemInstanceId, CharacterId = characterId, MembershipType = membershipType};

            Log.Info("Setting locked state to {locked} on item {itemId}", itemState, itemItemInstanceId);

            await GetBungie<int>("/Platform/Destiny2/Actions/Items/SetLockState/", requireAuth: true,
                method: Method.POST, body: payload);
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfileForCurrentUser(
            DestinyComponent[] components)
        {
            var linkedProfiles = await GetLinkedProfiles();
            var memberships = linkedProfiles.Profiles.OrderByDescending(v => v.DateLastPlayed).ToList();

            Log.Info("Linked memberships:");
            foreach (var ship in memberships)
            {
                Log.Info("  ship {type}:{id}", ship.MembershipType, ship.MembershipId);
            }

            var user = memberships[0];

            Log.Info("Returning primary membership {MembershipType}:{MembershipId}", user.MembershipType, user.MembershipId);

            return await GetProfile(user.MembershipType, user.MembershipId, components, true);
        }

        public Task<DestinyConfigDestinyManifest> GetManifest()
        {
            return GetBungie<DestinyConfigDestinyManifest>("/Platform/Destiny2/Manifest");
        }

        public Task<CommonModelsCoreSettingsConfiguration> GetSettings()
        {
            return GetBungie<CommonModelsCoreSettingsConfiguration>("/Platform/Settings/");
        }

        public async Task<T> GetBungie<T>(string path, bool requireAuth = false, Method method = Method.GET, object body = default)
        {
            Log.Debug("REQUEST {path}", path);
            var request = new RestRequest(path) {Method = method};

            if (body != null)
            {
                request.AddJsonBody(body);
            }

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
            Log.Debug("RESPONSE {Path} {ErrorStatus}, {Message}", path, data.ErrorStatus, data.Message);

            if (data.ErrorStatus.Equals("Success") != true)
            {
                Log.Info("setting exception fields");
                var err = new BungieApiException($"Bungie API Error {data.ErrorStatus}: {data.Message}")
                {
                    Response = BungieApiResponse<T>.ToSimple(data),
                    RequestPath = path,
                    Request = request
                };

                throw err;
            }

            if (data.Response == null) throw new BungieApiException("API did not return a response body");
            
            return data.Response;
        }

        public async Task<bool> EnsureTokenDataIsValid()
        {
            if (AppState.Data.TokenData == null)
            {
                Log.Info("ERROR: TokenData is null");
                throw new BungieApiException("TokenData is null when attempted to refresh it");
            }

            if (AppState.Data.TokenData.IsValid() != true)
            {
                Log.Info("ERROR: TokenData is not valid");
                throw new BungieApiException("TokenData is not valid when attempted to refresh it");
            }

            if (
                (AppState.Data.TokenData.BungieMembershipId == null ||
                 AppState.Data.TokenData.BungieMembershipId.Length <= 1) &&
                AppState.Data.TokenData.RefreshTokenIsValid())
            {
                Log.Info("Token lacks BungieMembershipId");
                await RefreshOAuthAccessToken();
                Log.Info("Successfully refreshed token.");
                return true;
            }

            if (AppState.Data.TokenData.AccessTokenIsValid()) return true;

            if (AppState.Data.TokenData.RefreshTokenIsValid())
            {
                Log.Info("Need to refresh access token!");
                await RefreshOAuthAccessToken();
                Log.Info("Successfully refreshed token.");
                return true;
            }

            throw new BungieApiException(
                "Unhandled scenario while ensuring TokenData is valid, which shouldn't happen!!");
        }

        public async Task RefreshOAuthAccessToken()
        {
            Log.Info("RefreshOAuthAccessToken");
            if (AppState.Data.TokenData == null || AppState.Data.TokenData.RefreshTokenIsValid() != true)
            {
                // TODO: throw exception?
                Log.Info("ERROR: have no valid refresh token to use to refresh");
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
            Log.Info("GetOAuthAccessToken");
            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=authorization_code&code={authCode}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            Log.Info("auth membershipID {membershipId}", data.membership_id);

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