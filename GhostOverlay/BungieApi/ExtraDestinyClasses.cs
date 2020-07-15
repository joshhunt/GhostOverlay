using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GhostOverlay
{
    // TODO: Delete this, use GhostSharp instead
    [Flags]
    public enum DestinyItemState
    {
        None = 0,
        Locked = 1,
        Tracked = 2,
        Masterwork = 4,
    }

    // TODO: Delete this, use GhostSharp instead
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
    
    // TODO: Evaluate whether we still need this or not
    public class SetLockStatePayload
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
}
