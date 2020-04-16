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
    /// DestinyComponentsPresentationDestinyPresentationNodeComponent
    /// </summary>
    [DataContract]
    public partial class DestinyComponentsPresentationDestinyPresentationNodeComponent :  IEquatable<DestinyComponentsPresentationDestinyPresentationNodeComponent>, IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DestinyComponentsPresentationDestinyPresentationNodeComponent" /> class.
        /// </summary>
        /// <param name="state">state.</param>
        /// <param name="objective">An optional property: presentation nodes MAY have objectives, which can be used to infer more human readable data about the progress. However, progressValue and completionValue ought to be considered the canonical values for progress on Progression Nodes..</param>
        /// <param name="progressValue">How much of the presentation node is considered to be completed so far by the given character/profile..</param>
        /// <param name="completionValue">The value at which the presentation node is considered to be completed..</param>
        public DestinyComponentsPresentationDestinyPresentationNodeComponent(int state = default(int), DestinyQuestsDestinyObjectiveProgress objective = default(DestinyQuestsDestinyObjectiveProgress), int progressValue = default(int), int completionValue = default(int))
        {
            this.State = state;
            this.Objective = objective;
            this.ProgressValue = progressValue;
            this.CompletionValue = completionValue;
        }
        
        /// <summary>
        /// Gets or Sets State
        /// </summary>
        [DataMember(Name="state", EmitDefaultValue=false)]
        public int State { get; set; }

        /// <summary>
        /// An optional property: presentation nodes MAY have objectives, which can be used to infer more human readable data about the progress. However, progressValue and completionValue ought to be considered the canonical values for progress on Progression Nodes.
        /// </summary>
        /// <value>An optional property: presentation nodes MAY have objectives, which can be used to infer more human readable data about the progress. However, progressValue and completionValue ought to be considered the canonical values for progress on Progression Nodes.</value>
        [DataMember(Name="objective", EmitDefaultValue=false)]
        public DestinyQuestsDestinyObjectiveProgress Objective { get; set; }

        /// <summary>
        /// How much of the presentation node is considered to be completed so far by the given character/profile.
        /// </summary>
        /// <value>How much of the presentation node is considered to be completed so far by the given character/profile.</value>
        [DataMember(Name="progressValue", EmitDefaultValue=false)]
        public int ProgressValue { get; set; }

        /// <summary>
        /// The value at which the presentation node is considered to be completed.
        /// </summary>
        /// <value>The value at which the presentation node is considered to be completed.</value>
        [DataMember(Name="completionValue", EmitDefaultValue=false)]
        public int CompletionValue { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class DestinyComponentsPresentationDestinyPresentationNodeComponent {\n");
            sb.Append("  State: ").Append(State).Append("\n");
            sb.Append("  Objective: ").Append(Objective).Append("\n");
            sb.Append("  ProgressValue: ").Append(ProgressValue).Append("\n");
            sb.Append("  CompletionValue: ").Append(CompletionValue).Append("\n");
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
            return this.Equals(input as DestinyComponentsPresentationDestinyPresentationNodeComponent);
        }

        /// <summary>
        /// Returns true if DestinyComponentsPresentationDestinyPresentationNodeComponent instances are equal
        /// </summary>
        /// <param name="input">Instance of DestinyComponentsPresentationDestinyPresentationNodeComponent to be compared</param>
        /// <returns>Boolean</returns>
        public bool Equals(DestinyComponentsPresentationDestinyPresentationNodeComponent input)
        {
            if (input == null)
                return false;

            return 
                (
                    this.State == input.State ||
                    this.State.Equals(input.State)
                ) && 
                (
                    this.Objective == input.Objective ||
                    (this.Objective != null &&
                    this.Objective.Equals(input.Objective))
                ) && 
                (
                    this.ProgressValue == input.ProgressValue ||
                    this.ProgressValue.Equals(input.ProgressValue)
                ) && 
                (
                    this.CompletionValue == input.CompletionValue ||
                    this.CompletionValue.Equals(input.CompletionValue)
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
                hashCode = hashCode * 59 + this.State.GetHashCode();
                if (this.Objective != null)
                    hashCode = hashCode * 59 + this.Objective.GetHashCode();
                hashCode = hashCode * 59 + this.ProgressValue.GetHashCode();
                hashCode = hashCode * 59 + this.CompletionValue.GetHashCode();
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
