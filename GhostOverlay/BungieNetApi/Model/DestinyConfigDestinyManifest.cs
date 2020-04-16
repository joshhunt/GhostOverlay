/* 
 * Bungie.Net API
 *
 * These endpoints constitute the functionality exposed by Bungie.net, both for more traditional website functionality and for connectivity to Bungie video games and their related functionality.
 *
 * The version of the OpenAPI document: 2.8.0
 * Contact: support@bungie.com
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using OpenAPIDateConverter = BungieNetApi.Client.OpenAPIDateConverter;

namespace BungieNetApi.Model
{
    /// <summary>
    /// DestinyManifest is the external-facing contract for just the properties needed by those calling the Destiny Platform.
    /// </summary>
    [DataContract]
    public partial class DestinyConfigDestinyManifest :  IEquatable<DestinyConfigDestinyManifest>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DestinyConfigDestinyManifest" /> class.
        /// </summary>
        /// <param name="version">version.</param>
        /// <param name="mobileAssetContentPath">mobileAssetContentPath.</param>
        /// <param name="mobileGearAssetDataBases">mobileGearAssetDataBases.</param>
        /// <param name="mobileWorldContentPaths">mobileWorldContentPaths.</param>
        /// <param name="jsonWorldContentPaths">This points to the generated JSON that contains all the Definitions. Each key is a locale. The value is a path to the aggregated world definitions (warning: large file!).</param>
        /// <param name="jsonWorldComponentContentPaths">This points to the generated JSON that contains all the Definitions. Each key is a locale. The value is a dictionary, where the key is a definition type by name, and the value is the path to the file for that definition. WARNING: This is unsafe and subject to change - do not depend on data in these files staying around long-term..</param>
        /// <param name="mobileClanBannerDatabasePath">mobileClanBannerDatabasePath.</param>
        /// <param name="mobileGearCDN">mobileGearCDN.</param>
        /// <param name="iconImagePyramidInfo">Information about the \&quot;Image Pyramid\&quot; for Destiny icons. Where possible, we create smaller versions of Destiny icons. These are found as subfolders under the location of the \&quot;original/full size\&quot; Destiny images, with the same file name and extension as the original image itself. (this lets us avoid sending largely redundant path info with every entity, at the expense of the smaller versions of the image being less discoverable).</param>
        public DestinyConfigDestinyManifest(string version = default(string), string mobileAssetContentPath = default(string), List<DestinyConfigGearAssetDataBaseDefinition> mobileGearAssetDataBases = default(List<DestinyConfigGearAssetDataBaseDefinition>), Dictionary<string, string> mobileWorldContentPaths = default(Dictionary<string, string>), Dictionary<string, string> jsonWorldContentPaths = default(Dictionary<string, string>), Dictionary<string, Dictionary<string, string>> jsonWorldComponentContentPaths = default(Dictionary<string, Dictionary<string, string>>), string mobileClanBannerDatabasePath = default(string), Dictionary<string, string> mobileGearCDN = default(Dictionary<string, string>), List<DestinyConfigImagePyramidEntry> iconImagePyramidInfo = default(List<DestinyConfigImagePyramidEntry>))
        {
            this.Version = version;
            this.MobileAssetContentPath = mobileAssetContentPath;
            this.MobileGearAssetDataBases = mobileGearAssetDataBases;
            this.MobileWorldContentPaths = mobileWorldContentPaths;
            this.JsonWorldContentPaths = jsonWorldContentPaths;
            this.JsonWorldComponentContentPaths = jsonWorldComponentContentPaths;
            this.MobileClanBannerDatabasePath = mobileClanBannerDatabasePath;
            this.MobileGearCDN = mobileGearCDN;
            this.IconImagePyramidInfo = iconImagePyramidInfo;
        }
        
        /// <summary>
        /// Gets or Sets Version
        /// </summary>
        [DataMember(Name="version", EmitDefaultValue=false)]
        public string Version { get; set; }

        /// <summary>
        /// Gets or Sets MobileAssetContentPath
        /// </summary>
        [DataMember(Name="mobileAssetContentPath", EmitDefaultValue=false)]
        public string MobileAssetContentPath { get; set; }

        /// <summary>
        /// Gets or Sets MobileGearAssetDataBases
        /// </summary>
        [DataMember(Name="mobileGearAssetDataBases", EmitDefaultValue=false)]
        public List<DestinyConfigGearAssetDataBaseDefinition> MobileGearAssetDataBases { get; set; }

        /// <summary>
        /// Gets or Sets MobileWorldContentPaths
        /// </summary>
        [DataMember(Name="mobileWorldContentPaths", EmitDefaultValue=false)]
        public Dictionary<string, string> MobileWorldContentPaths { get; set; }

        /// <summary>
        /// This points to the generated JSON that contains all the Definitions. Each key is a locale. The value is a path to the aggregated world definitions (warning: large file!)
        /// </summary>
        /// <value>This points to the generated JSON that contains all the Definitions. Each key is a locale. The value is a path to the aggregated world definitions (warning: large file!)</value>
        [DataMember(Name="jsonWorldContentPaths", EmitDefaultValue=false)]
        public Dictionary<string, string> JsonWorldContentPaths { get; set; }

        /// <summary>
        /// This points to the generated JSON that contains all the Definitions. Each key is a locale. The value is a dictionary, where the key is a definition type by name, and the value is the path to the file for that definition. WARNING: This is unsafe and subject to change - do not depend on data in these files staying around long-term.
        /// </summary>
        /// <value>This points to the generated JSON that contains all the Definitions. Each key is a locale. The value is a dictionary, where the key is a definition type by name, and the value is the path to the file for that definition. WARNING: This is unsafe and subject to change - do not depend on data in these files staying around long-term.</value>
        [DataMember(Name="jsonWorldComponentContentPaths", EmitDefaultValue=false)]
        public Dictionary<string, Dictionary<string, string>> JsonWorldComponentContentPaths { get; set; }

        /// <summary>
        /// Gets or Sets MobileClanBannerDatabasePath
        /// </summary>
        [DataMember(Name="mobileClanBannerDatabasePath", EmitDefaultValue=false)]
        public string MobileClanBannerDatabasePath { get; set; }

        /// <summary>
        /// Gets or Sets MobileGearCDN
        /// </summary>
        [DataMember(Name="mobileGearCDN", EmitDefaultValue=false)]
        public Dictionary<string, string> MobileGearCDN { get; set; }

        /// <summary>
        /// Information about the \&quot;Image Pyramid\&quot; for Destiny icons. Where possible, we create smaller versions of Destiny icons. These are found as subfolders under the location of the \&quot;original/full size\&quot; Destiny images, with the same file name and extension as the original image itself. (this lets us avoid sending largely redundant path info with every entity, at the expense of the smaller versions of the image being less discoverable)
        /// </summary>
        /// <value>Information about the \&quot;Image Pyramid\&quot; for Destiny icons. Where possible, we create smaller versions of Destiny icons. These are found as subfolders under the location of the \&quot;original/full size\&quot; Destiny images, with the same file name and extension as the original image itself. (this lets us avoid sending largely redundant path info with every entity, at the expense of the smaller versions of the image being less discoverable)</value>
        [DataMember(Name="iconImagePyramidInfo", EmitDefaultValue=false)]
        public List<DestinyConfigImagePyramidEntry> IconImagePyramidInfo { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DestinyConfigDestinyManifest {\n");
            sb.Append("  Version: ").Append(Version).Append("\n");
            sb.Append("  MobileAssetContentPath: ").Append(MobileAssetContentPath).Append("\n");
            sb.Append("  MobileGearAssetDataBases: ").Append(MobileGearAssetDataBases).Append("\n");
            sb.Append("  MobileWorldContentPaths: ").Append(MobileWorldContentPaths).Append("\n");
            sb.Append("  JsonWorldContentPaths: ").Append(JsonWorldContentPaths).Append("\n");
            sb.Append("  JsonWorldComponentContentPaths: ").Append(JsonWorldComponentContentPaths).Append("\n");
            sb.Append("  MobileClanBannerDatabasePath: ").Append(MobileClanBannerDatabasePath).Append("\n");
            sb.Append("  MobileGearCDN: ").Append(MobileGearCDN).Append("\n");
            sb.Append("  IconImagePyramidInfo: ").Append(IconImagePyramidInfo).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }
  
        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Returns true if objects are equal
        /// </summary>
        /// <param name="input">Object to be compared</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object input)
        {
            return this.Equals(input as DestinyConfigDestinyManifest);
        }

        /// <summary>
        /// Returns true if DestinyConfigDestinyManifest instances are equal
        /// </summary>
        /// <param name="input">Instance of DestinyConfigDestinyManifest to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DestinyConfigDestinyManifest input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.Version == input.Version ||
                    (this.Version != null &&
                    this.Version.Equals(input.Version))
                ) && 
                (
                    this.MobileAssetContentPath == input.MobileAssetContentPath ||
                    (this.MobileAssetContentPath != null &&
                    this.MobileAssetContentPath.Equals(input.MobileAssetContentPath))
                ) && 
                (
                    this.MobileGearAssetDataBases == input.MobileGearAssetDataBases ||
                    this.MobileGearAssetDataBases != null &&
                    input.MobileGearAssetDataBases != null &&
                    this.MobileGearAssetDataBases.SequenceEqual(input.MobileGearAssetDataBases)
                ) && 
                (
                    this.MobileWorldContentPaths == input.MobileWorldContentPaths ||
                    this.MobileWorldContentPaths != null &&
                    input.MobileWorldContentPaths != null &&
                    this.MobileWorldContentPaths.SequenceEqual(input.MobileWorldContentPaths)
                ) && 
                (
                    this.JsonWorldContentPaths == input.JsonWorldContentPaths ||
                    this.JsonWorldContentPaths != null &&
                    input.JsonWorldContentPaths != null &&
                    this.JsonWorldContentPaths.SequenceEqual(input.JsonWorldContentPaths)
                ) && 
                (
                    this.JsonWorldComponentContentPaths == input.JsonWorldComponentContentPaths ||
                    this.JsonWorldComponentContentPaths != null &&
                    input.JsonWorldComponentContentPaths != null &&
                    this.JsonWorldComponentContentPaths.SequenceEqual(input.JsonWorldComponentContentPaths)
                ) && 
                (
                    this.MobileClanBannerDatabasePath == input.MobileClanBannerDatabasePath ||
                    (this.MobileClanBannerDatabasePath != null &&
                    this.MobileClanBannerDatabasePath.Equals(input.MobileClanBannerDatabasePath))
                ) && 
                (
                    this.MobileGearCDN == input.MobileGearCDN ||
                    this.MobileGearCDN != null &&
                    input.MobileGearCDN != null &&
                    this.MobileGearCDN.SequenceEqual(input.MobileGearCDN)
                ) && 
                (
                    this.IconImagePyramidInfo == input.IconImagePyramidInfo ||
                    this.IconImagePyramidInfo != null &&
                    input.IconImagePyramidInfo != null &&
                    this.IconImagePyramidInfo.SequenceEqual(input.IconImagePyramidInfo)
                );
        }

        /// <summary>
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Version != null)
                    hashCode = hashCode * 59 + this.Version.GetHashCode();
                if (this.MobileAssetContentPath != null)
                    hashCode = hashCode * 59 + this.MobileAssetContentPath.GetHashCode();
                if (this.MobileGearAssetDataBases != null)
                    hashCode = hashCode * 59 + this.MobileGearAssetDataBases.GetHashCode();
                if (this.MobileWorldContentPaths != null)
                    hashCode = hashCode * 59 + this.MobileWorldContentPaths.GetHashCode();
                if (this.JsonWorldContentPaths != null)
                    hashCode = hashCode * 59 + this.JsonWorldContentPaths.GetHashCode();
                if (this.JsonWorldComponentContentPaths != null)
                    hashCode = hashCode * 59 + this.JsonWorldComponentContentPaths.GetHashCode();
                if (this.MobileClanBannerDatabasePath != null)
                    hashCode = hashCode * 59 + this.MobileClanBannerDatabasePath.GetHashCode();
                if (this.MobileGearCDN != null)
                    hashCode = hashCode * 59 + this.MobileGearCDN.GetHashCode();
                if (this.IconImagePyramidInfo != null)
                    hashCode = hashCode * 59 + this.IconImagePyramidInfo.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }

}
