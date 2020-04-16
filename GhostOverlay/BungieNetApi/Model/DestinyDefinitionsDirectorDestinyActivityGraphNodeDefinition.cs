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
    /// This is the position and other data related to nodes in the activity graph that you can click to launch activities. An Activity Graph node will only have one active Activity at a time, which will determine the activity to be launched (and, unless overrideDisplay information is provided, will also determine the tooltip and other UI related to the node)
    /// </summary>
    [DataContract]
    public partial class DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition :  IEquatable<DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition" /> class.
        /// </summary>
        /// <param name="nodeId">An identifier for the Activity Graph Node, only guaranteed to be unique within its parent Activity Graph..</param>
        /// <param name="overrideDisplay">The node *may* have display properties that override the active Activity&#39;s display properties..</param>
        /// <param name="position">The position on the map for this node..</param>
        /// <param name="featuringStates">The node may have various visual accents placed on it, or styles applied. These are the list of possible styles that the Node can have. The game iterates through each, looking for the first one that passes a check of the required game/character/account state in order to show that style, and then renders the node in that style..</param>
        /// <param name="activities">The node may have various possible activities that could be active for it, however only one may be active at a time. See the DestinyActivityGraphNodeActivityDefinition for details..</param>
        /// <param name="states">Represents possible states that the graph node can be in. These are combined with some checking that happens in the game client and server to determine which state is actually active at any given time..</param>
        public DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition(long nodeId = default(long), DestinyDefinitionsCommonDestinyDisplayPropertiesDefinition overrideDisplay = default(DestinyDefinitionsCommonDestinyDisplayPropertiesDefinition), DestinyDefinitionsCommonDestinyPositionDefinition position = default(DestinyDefinitionsCommonDestinyPositionDefinition), List<DestinyDefinitionsDirectorDestinyActivityGraphNodeFeaturingStateDefinition> featuringStates = default(List<DestinyDefinitionsDirectorDestinyActivityGraphNodeFeaturingStateDefinition>), List<DestinyDefinitionsDirectorDestinyActivityGraphNodeActivityDefinition> activities = default(List<DestinyDefinitionsDirectorDestinyActivityGraphNodeActivityDefinition>), List<DestinyDefinitionsDirectorDestinyActivityGraphNodeStateEntry> states = default(List<DestinyDefinitionsDirectorDestinyActivityGraphNodeStateEntry>))
        {
            this.NodeId = nodeId;
            this.OverrideDisplay = overrideDisplay;
            this.Position = position;
            this.FeaturingStates = featuringStates;
            this.Activities = activities;
            this.States = states;
        }
        
        /// <summary>
        /// An identifier for the Activity Graph Node, only guaranteed to be unique within its parent Activity Graph.
        /// </summary>
        /// <value>An identifier for the Activity Graph Node, only guaranteed to be unique within its parent Activity Graph.</value>
        [DataMember(Name="nodeId", EmitDefaultValue=false)]
        public long NodeId { get; set; }

        /// <summary>
        /// The node *may* have display properties that override the active Activity&#39;s display properties.
        /// </summary>
        /// <value>The node *may* have display properties that override the active Activity&#39;s display properties.</value>
        [DataMember(Name="overrideDisplay", EmitDefaultValue=false)]
        public DestinyDefinitionsCommonDestinyDisplayPropertiesDefinition OverrideDisplay { get; set; }

        /// <summary>
        /// The position on the map for this node.
        /// </summary>
        /// <value>The position on the map for this node.</value>
        [DataMember(Name="position", EmitDefaultValue=false)]
        public DestinyDefinitionsCommonDestinyPositionDefinition Position { get; set; }

        /// <summary>
        /// The node may have various visual accents placed on it, or styles applied. These are the list of possible styles that the Node can have. The game iterates through each, looking for the first one that passes a check of the required game/character/account state in order to show that style, and then renders the node in that style.
        /// </summary>
        /// <value>The node may have various visual accents placed on it, or styles applied. These are the list of possible styles that the Node can have. The game iterates through each, looking for the first one that passes a check of the required game/character/account state in order to show that style, and then renders the node in that style.</value>
        [DataMember(Name="featuringStates", EmitDefaultValue=false)]
        public List<DestinyDefinitionsDirectorDestinyActivityGraphNodeFeaturingStateDefinition> FeaturingStates { get; set; }

        /// <summary>
        /// The node may have various possible activities that could be active for it, however only one may be active at a time. See the DestinyActivityGraphNodeActivityDefinition for details.
        /// </summary>
        /// <value>The node may have various possible activities that could be active for it, however only one may be active at a time. See the DestinyActivityGraphNodeActivityDefinition for details.</value>
        [DataMember(Name="activities", EmitDefaultValue=false)]
        public List<DestinyDefinitionsDirectorDestinyActivityGraphNodeActivityDefinition> Activities { get; set; }

        /// <summary>
        /// Represents possible states that the graph node can be in. These are combined with some checking that happens in the game client and server to determine which state is actually active at any given time.
        /// </summary>
        /// <value>Represents possible states that the graph node can be in. These are combined with some checking that happens in the game client and server to determine which state is actually active at any given time.</value>
        [DataMember(Name="states", EmitDefaultValue=false)]
        public List<DestinyDefinitionsDirectorDestinyActivityGraphNodeStateEntry> States { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition {\n");
            sb.Append("  NodeId: ").Append(NodeId).Append("\n");
            sb.Append("  OverrideDisplay: ").Append(OverrideDisplay).Append("\n");
            sb.Append("  Position: ").Append(Position).Append("\n");
            sb.Append("  FeaturingStates: ").Append(FeaturingStates).Append("\n");
            sb.Append("  Activities: ").Append(Activities).Append("\n");
            sb.Append("  States: ").Append(States).Append("\n");
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
            return this.Equals(input as DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition);
        }

        /// <summary>
        /// Returns true if DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition instances are equal
        /// </summary>
        /// <param name="input">Instance of DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DestinyDefinitionsDirectorDestinyActivityGraphNodeDefinition input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.NodeId == input.NodeId ||
                    this.NodeId.Equals(input.NodeId)
                ) && 
                (
                    this.OverrideDisplay == input.OverrideDisplay ||
                    (this.OverrideDisplay != null &&
                    this.OverrideDisplay.Equals(input.OverrideDisplay))
                ) && 
                (
                    this.Position == input.Position ||
                    (this.Position != null &&
                    this.Position.Equals(input.Position))
                ) && 
                (
                    this.FeaturingStates == input.FeaturingStates ||
                    this.FeaturingStates != null &&
                    input.FeaturingStates != null &&
                    this.FeaturingStates.SequenceEqual(input.FeaturingStates)
                ) && 
                (
                    this.Activities == input.Activities ||
                    this.Activities != null &&
                    input.Activities != null &&
                    this.Activities.SequenceEqual(input.Activities)
                ) && 
                (
                    this.States == input.States ||
                    this.States != null &&
                    input.States != null &&
                    this.States.SequenceEqual(input.States)
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
                hashCode = hashCode * 59 + this.NodeId.GetHashCode();
                if (this.OverrideDisplay != null)
                    hashCode = hashCode * 59 + this.OverrideDisplay.GetHashCode();
                if (this.Position != null)
                    hashCode = hashCode * 59 + this.Position.GetHashCode();
                if (this.FeaturingStates != null)
                    hashCode = hashCode * 59 + this.FeaturingStates.GetHashCode();
                if (this.Activities != null)
                    hashCode = hashCode * 59 + this.Activities.GetHashCode();
                if (this.States != null)
                    hashCode = hashCode * 59 + this.States.GetHashCode();
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
