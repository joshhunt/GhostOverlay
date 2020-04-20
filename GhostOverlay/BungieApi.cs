using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BungieNetApi.Client;
using Newtonsoft.Json;
using RestSharp;
using BungieNetApi.Model;

namespace GhostOverlay
{

    [Serializable()]
    public class BungieApiException : System.Exception
    {
        public BungieApiException() : base() { }
        public BungieApiException(string message) : base(message) { }
        public BungieApiException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected BungieApiException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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

    public class BungieApi
    {
        public readonly int[] DefaultProfileComponents = new[] { 100, 102, 200, 201, 300, 301, 900, 800 };

        // Loaded from Configuration.resw by the constructor
        private readonly string apiKey; 
        private readonly string clientId;
        private readonly string clientSecret; 

        private readonly RestClient client;

        public BungieApi()
        {
            var resources = new Windows.ApplicationModel.Resources.ResourceLoader("Configuration");
            apiKey = resources.GetString("BungieApiKey");
            clientId = resources.GetString("BungieClientId");
            clientSecret = resources.GetString("BungieClientSecret");

            client = new RestClient("https://www.bungie.net");
            client.AddDefaultHeader("x-api-key", apiKey);
            client.UserAgent = "GhostOverlay/dev josh@trtr.co";
            client.CookieContainer = new System.Net.CookieContainer();
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfile(int membershipType, long membershipId , int[] components, bool requireAuth = false)
        {   
            var componemtsSt = string.Join(",", components);
            return await GetBungie<DestinyResponsesDestinyProfileResponse>(
                    $"Platform/Destiny2/{membershipType}/Profile/{membershipId}/?components={componemtsSt}", requireAuth: requireAuth);
        }

        public async Task<UserUserMembershipData> GetMembershipsForCurrentUser()
        {
            return await GetBungie<UserUserMembershipData>("/Platform/User/GetMembershipsForCurrentUser/", true);
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfileForCurrentUser(int[] components)
        {
            var membershipData = await GetMembershipsForCurrentUser();
            var user = membershipData.DestinyMemberships.Find(p =>
                p.MembershipId == membershipData.PrimaryMembershipId);
            
            if (user == null)
            {   
                Debug.WriteLine("TODO: Unable to find primary membership, so just returning the 0th one");
                user = membershipData.DestinyMemberships[0];
            }

            return await GetProfile(user.MembershipType, user.MembershipId, components, requireAuth: true);
        }

        public async Task<DestinyConfigDestinyManifest> GetManifest()
        {
            return await GetBungie<DestinyConfigDestinyManifest>("/Platform/Destiny2/Manifest");
        }

        public async Task<T> GetBungie<T>(string path, bool requireAuth = false)
        {   
            Debug.WriteLine($"GetBungie request for {path}");
            var request = new RestRequest(path);

            if (AppState.TokenData != null && AppState.TokenData.RefreshTokenIsValid())
            {
                await EnsureTokenDataIsValid();
            }
            
            if (requireAuth && (AppState.TokenData == null || !AppState.TokenData.AccessTokenIsValid()))
            {
                throw new BungieApiException("Auth was required but the access token was not valid");
            }

            if (AppState.TokenData != null && AppState.TokenData.AccessTokenIsValid())
            {
                var headerValue = $"Bearer {AppState.TokenData.AccessToken}";
                Debug.WriteLine("  Adding access token");
                
                request.AddHeader("authorization", headerValue);
            }

            var response = await client.ExecuteAsync(request);

            if (response.ContentType.Contains("application/json") != true)
            {
                throw new BungieApiException("API did not return JSON");
            }

            var data = JsonConvert.DeserializeObject<BungieApiResponse<T>>(response.Content);

            if (data.ErrorStatus.Equals("Success") != true)
            {
                throw new BungieApiException($"Bungie API Error {data.ErrorStatus}: {data.Message}");
            }

            if (data.Response == null)
            {
                throw new BungieApiException("API did not return JSON");
            }

            return data.Response;
        }

        public async Task<bool> EnsureTokenDataIsValid()
        {
            if (AppState.TokenData == null)
            {
                Debug.WriteLine("ERROR: TokenData is null");
                throw new BungieApiException("TokenData is null when attempted to refresh it");
            }
            
            if (AppState.TokenData.IsValid() != true)
            {
                Debug.WriteLine("ERROR: TokenData is not valid");
                throw new BungieApiException("TokenData is not valid when attempted to refresh it");
            }
            
            if (AppState.TokenData.AccessTokenIsValid())
            {
                Debug.WriteLine("Access token is still valid, everything's all good!");
                return true;
            }

            if (AppState.TokenData.RefreshTokenIsValid())
            {
                Debug.WriteLine("Need to refresh access token!");
                await RefreshOAuthAccessToken();
                Debug.WriteLine("Successfully refreshed token. New TokenData:");
                Debug.WriteLine(AppState.TokenData);
                return true;
            }

            throw new BungieApiException("Unhandled scenario while ensuring TokenData is valid, which shouldn't happen!!");
        }

        public async Task RefreshOAuthAccessToken()
        {
            if (AppState.TokenData == null || AppState.TokenData.RefreshTokenIsValid() != true)
            {
                // TODO: throw exception?
                Debug.WriteLine("ERROR: have no valid refresh token to use to refresh");
                return;
            }

            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=refresh_token&refresh_token={AppState.TokenData.RefreshToken}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            AppState.TokenData = new OAuthToken(data.access_token, data.refresh_token,
                data.expires_in, data.refresh_expires_in);
            AppState.TokenData.SaveToSettings();
        }

        public async Task GetOAuthAccessToken(string authCode)
        {
            var request = new RestRequest("/Platform/App/OAuth/Token/", DataFormat.Json);
            request.AddParameter("application/x-www-form-urlencoded; charset=utf-8",
                $"grant_type=authorization_code&code={authCode}", ParameterType.RequestBody);

            var auth = Base64Encode($"{clientId}:{clientSecret}");
            request.AddHeader("Authorization", $"Basic {auth}");
            var data = await client.PostAsync<BungieOAuthTokenResponse>(request);

            AppState.TokenData = new OAuthToken(data.access_token, data.refresh_token,
                data.expires_in, data.refresh_expires_in);
            AppState.TokenData.SaveToSettings();
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