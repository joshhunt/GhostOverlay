using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GhostSharp.BungieNetApi.Models;
using Newtonsoft.Json;
using RestSharp;

namespace GhostOverlay
{
    public partial class BungieApi
    {

        // Set this to an alternate URL to get the profile from, useful for debugging 
        #pragma warning disable 649
        private static readonly string DebugProfile;
        #pragma warning restore 649

        public async Task<DestinyProfileResponse> GetProfile(BungieMembershipType membershipType, long membershipId,
            DestinyComponent[] components, bool requireAuth = false)
        {
            if (DebugProfile != null)
            {
                return await GetDebugProfile();
            }

            var componentsStr = string.Join(",", components);
            return await GetBungie<DestinyProfileResponse>(
                $"Platform/Destiny2/{(int)membershipType}/Profile/{membershipId}/?components={componentsStr}", requireAuth);
        }

        public async Task<DestinyLinkedProfilesResponse> GetLinkedProfiles()
        {
            // If the user's saved access token doesn't have a BungieMembershipId yet,
            // this will effectively upgrade it to one that does :) :) :) 
            await EnsureTokenDataIsValid();

            if (AppState.Data.TokenData.BungieMembershipId == null)
            {
                throw new Exception("TokenData somehow lacks a BungieMembershipId. This is very bad.");
            }

            return await GetBungie<DestinyLinkedProfilesResponse>($"/Platform/Destiny2/254/Profile/{AppState.Data.TokenData.BungieMembershipId}/LinkedProfiles/", true);
        }

        private static (string characterId, DestinyItemComponent item) FindItemToLock(
            DestinyProfileResponse profile)
        {
            string selectedCharacterId = default;
            DestinyItemComponent selectedItem = default;

            foreach (var (characterId, characterInventory) in profile.CharacterEquipment.Data)
            {
                foreach (var item in characterInventory.Items)
                {
                    if (item.ItemInstanceId == 0 ||
                        (item.BucketHash != BucketShips && item.BucketHash != BucketSparrows)) continue;

                    selectedCharacterId = characterId;
                    selectedItem = item;
                    break;
                }

                if (selectedCharacterId != null)
                {
                    break;
                }
            }

            return (characterId: selectedCharacterId, item: selectedItem);
        }

        public async Task CacheBust(DestinyProfileResponse profile)
        {
            var (characterId, item) = FindItemToLock(profile);
            var itemState = (DestinyItemState)(item?.State ?? 0);
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

        private async Task SetLockState(bool itemState, long itemItemInstanceId, string characterId, BungieMembershipType membershipType)
        {   
            // TODO: Evaluate whether we still need this, or we can grab it from GhostSharp
            var payload = new SetLockStatePayload()
            {
                State = itemState,
                ItemId = itemItemInstanceId,
                CharacterId = characterId,
                MembershipType = (int)membershipType
            };

            Log.Info("Setting locked state to {locked} on item {itemId}", itemState, itemItemInstanceId);

            await GetBungie<int>("/Platform/Destiny2/Actions/Items/SetLockState/", requireAuth: true,
                method: Method.POST, body: payload);
        }

        public async Task<DestinyProfileResponse> GetProfileForCurrentUser(
            DestinyComponent[] components)
        {
            var linkedProfiles = await GetLinkedProfiles();
            var memberships = linkedProfiles.Profiles.OrderByDescending(v => v.DateLastPlayed).ToList();

            Log.Info("Linked memberships:");
            foreach (var ship in memberships) Log.Info("  membership {type}:{id}", ship.MembershipType, ship.MembershipId);

            var user = memberships[0];
            Log.Info("Returning primary membership {MembershipType}:{MembershipId}", user.MembershipType, user.MembershipId);

            return await GetProfile(user.MembershipType, user.MembershipId, components, true);
        }

        public Task<DestinyManifest> GetManifest()
        {
            return GetBungie<DestinyManifest>("/Platform/Destiny2/Manifest");
        }

        public Task<CoreSettingsConfiguration> GetSettings()
        {
            return GetBungie<CoreSettingsConfiguration>("/Platform/Settings/");
        }

        private async Task<DestinyProfileResponse> GetDebugProfile()
        {
            Log.Error("DebugProfile is defined!!! Returning fixed debug profile data, instead of user's live profile!!!");
            var debugClient = new RestClient();
            var request = new RestRequest(DebugProfile);

            var response = await debugClient.ExecuteAsync(request);

            var data = JsonConvert.DeserializeObject<BungieApiResponse<DestinyProfileResponse>>(response.Content);

            return data.Response;
        }
    }
}
