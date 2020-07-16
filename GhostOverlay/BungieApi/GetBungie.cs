using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhostSharper.Api;
using Newtonsoft.Json;
using RestSharp;

namespace GhostOverlay
{
    public partial class BungieApi
    {
        public async Task<T> GetBungie<T>(string path, bool requireAuth = false, Method method = Method.GET, object body = default)
        {
            Log.Debug("REQUEST {path}", path);
            var request = new RestRequest(path) { Method = method };

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

            var data = JsonConvert.DeserializeObject<DestinyServerResponse<T>>(response.Content);
            Log.Debug("RESPONSE {Path} {ErrorStatus}, {Message}", path, data.ErrorStatus, data.Message);

            if (data.ErrorStatus.Equals("Success") != true)
            {
                Log.Info("setting exception fields");
                var err = new BungieApiException($"Bungie API Error {data.ErrorStatus}: {data.Message}")
                {
                    Response = DestinyServerResponse<T>.ToSimple(data),
                    RequestPath = path,
                    Request = request
                };

                throw err;
            }

            if (data.Response == null) throw new BungieApiException("API did not return a response body");

            return data.Response;
        }
    }
}
