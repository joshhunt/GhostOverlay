using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Text;
using GhostSharper.Models;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Validation;

namespace GhostOverlay
{
    public partial class BungieApi
    {
        private readonly Logger Log = new Logger("BungieApi");

        private readonly RestClient client;
        private readonly string clientId;
        private readonly string clientSecret;

        private static readonly long BucketShips = 284967655;
        private static readonly long BucketSparrows = 2025709351;

        public readonly DestinyComponentType[] DefaultProfileComponents =
        {
            DestinyComponentType.Profiles,
            DestinyComponentType.ProfileInventories,
            DestinyComponentType.Characters,
            DestinyComponentType.CharacterInventories,
            DestinyComponentType.CharacterEquipment,
            DestinyComponentType.ItemInstances,
            DestinyComponentType.ItemObjectives,
            DestinyComponentType.Records,
            DestinyComponentType.CharacterActivities,
            DestinyComponentType.PresentationNodes
        };

        public BungieApi()
        {
            var resources = new ResourceLoader("Configuration");
            var apiKey = resources.GetString("BungieApiKey");
            clientId = resources.GetString("BungieClientId");
            clientSecret = resources.GetString("BungieClientSecret");

            client = new RestClient("https://www.bungie.net");
            client.AddDefaultHeader("x-api-key", apiKey);
            client.UserAgent = "GhostOverlay/dev josh@trtr.co";
            client.CookieContainer = new CookieContainer();
        }

    }
}