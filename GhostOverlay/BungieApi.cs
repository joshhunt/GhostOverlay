using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using BungieNetApi.Model;
using RestSharp;

namespace GhostOverlay
{
    public class BungieApi
    {
        private readonly RestClient client;
        public readonly int[] DefaultProfileComponents = new[] { 100, 102, 200, 201, 300, 301, 900, 800 };

        public BungieApi()
        {
            client = new RestClient("https://www.bungie.net");
            client.AddDefaultHeader("x-api-key", "c3e5fac733944b058c558a0a0ef15a34");
            client.UserAgent = "GhostOverlay/dev josh@trtr.co";
            client.CookieContainer = new System.Net.CookieContainer();
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfile(int membershipType, long membershipId , int[] components, bool requireAuth = false)
        {
            var storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await storageFolder.GetFileAsync("Assets\\profileApiResponse.json");
            var text = await FileIO.ReadTextAsync(file);
            var data = JsonConvert.DeserializeObject<BungieApiResponse<DestinyResponsesDestinyProfileResponse>>(text);
            return data.Response;
        }

        public async Task<DestinyResponsesDestinyProfileResponse> GetProfileForCurrentUser(int[] components)
        {
            return await GetProfile(0, 0, components, requireAuth: true);
        }

        public async Task<DestinyConfigDestinyManifest> GetManifest()
        {
            return await GetBungie<DestinyConfigDestinyManifest>("/Platform/Destiny2/Manifest");
        }

        public async Task<T> GetBungie<T>(string path, bool requireAuth = false)
        {
            var request = new RestRequest(path);
            var response = await client.ExecuteAsync(request);
            var data = JsonConvert.DeserializeObject<BungieApiResponse<T>>(response.Content);
            return data.Response;
        }
    }
}