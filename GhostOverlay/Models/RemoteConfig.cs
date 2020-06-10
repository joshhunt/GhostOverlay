﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace GhostOverlay.Models
{
    public class RemoteConfigValues
    {
        [JsonProperty("autoProfileBust")]
        public bool AutoProfileBust = false;

        [JsonProperty("autoProfileBustIntervalSeconds")]
        public long AutoProfileBustIntervalSeconds = 30;

        [JsonProperty("rightClickForceRefresh")]
        public bool RightClickForceRefresh = true;

        [JsonProperty("ForceRefreshBustsProfile")]
        public bool ForceRefreshBustsProfile = true;

        [JsonProperty("crucibleMapTrackerAutoProfileBust")]
        public bool CrucibleMapTrackerAutoProfileBust = true;

        [JsonProperty("crucibleMapTrackerAutoProfileBustIntervalSeconds")]
        public long CrucibleMapTrackerAutoProfileBustIntervalSeconds = 30;

        public override string ToString()
        {
            return $"RemoteConfigValues(AutoProfileBust: {AutoProfileBust}, AutoProfileBustIntervalSeconds: {AutoProfileBustIntervalSeconds}, RightClickForceRefresh: {RightClickForceRefresh}, ForceRefreshBustsProfile: {ForceRefreshBustsProfile}, CrucibleMapTrackerAutoProfileBust: {CrucibleMapTrackerAutoProfileBust}, CrucibleMapTrackerAutoProfileBustIntervalSeconds: {CrucibleMapTrackerAutoProfileBustIntervalSeconds})";
        }
    }

    public class RemoteConfig
    {
        private readonly Logger Log = new Logger("RemoteConfig");
        private readonly RestClient client;
        public RemoteConfigValues Values = new RemoteConfigValues();

        public RemoteConfig()
        {
            client = new RestClient("https://raw.githubusercontent.com");
        }

        public async void LoadRemoteConfig()
        {
            try
            {
                var request = new RestRequest("/joshhunt/ghost-site/master/generated-data/settings.json");
                var response = await client.ExecuteAsync(request);
                Values = JsonConvert.DeserializeObject<RemoteConfigValues>(response.Content);
                Log.Info("Loading remote config {config}", Values);
            }
            catch (Exception err)
            {
                Log.Error("Error loading remote config", err);
            }
            
        }
    }
}
