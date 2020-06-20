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
using BungieNetApi.Model;
using OpenAPIDateConverter = BungieNetApi.Client.OpenAPIDateConverter;

namespace GhostOverlay
{
    /// <summary>
    /// InlineResponse20037
    /// </summary>
    [DataContract]
    public partial class BungieApiResponse<T> : IValidatableObject
    {

        /// <summary>
        /// Gets or Sets Response
        /// </summary>
        [DataMember(Name = "Response", EmitDefaultValue = false)]
        public T Response { get; set; }

        /// <summary>
        /// Gets or Sets ErrorCode
        /// </summary>
        [DataMember(Name = "ErrorCode", EmitDefaultValue = false)]
        public int ErrorCode { get; set; }

        /// <summary>
        /// Gets or Sets ThrottleSeconds
        /// </summary>
        [DataMember(Name = "ThrottleSeconds", EmitDefaultValue = false)]
        public int ThrottleSeconds { get; set; }

        /// <summary>
        /// Gets or Sets ErrorStatus
        /// </summary>
        [DataMember(Name = "ErrorStatus", EmitDefaultValue = false)]
        public string ErrorStatus { get; set; }

        /// <summary>
        /// Gets or Sets Message
        /// </summary>
        [DataMember(Name = "Message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// Gets or Sets MessageData
        /// </summary>
        [DataMember(Name = "MessageData", EmitDefaultValue = false)]
        public Dictionary<string, string> MessageData { get; set; }

        /// <summary>
        /// Gets or Sets DetailedErrorTrace
        /// </summary>
        [DataMember(Name = "DetailedErrorTrace", EmitDefaultValue = false)]
        public string DetailedErrorTrace { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class BungieApiResponse {\n");
            sb.Append("  Response: ").Append(Response).Append("\n");
            sb.Append("  ErrorCode: ").Append(ErrorCode).Append("\n");
            sb.Append("  ThrottleSeconds: ").Append(ThrottleSeconds).Append("\n");
            sb.Append("  ErrorStatus: ").Append(ErrorStatus).Append("\n");
            sb.Append("  Message: ").Append(Message).Append("\n");
            sb.Append("  MessageData: ").Append(MessageData).Append("\n");
            sb.Append("  DetailedErrorTrace: ").Append(DetailedErrorTrace).Append("\n");
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
        /// Gets the hash code
        /// </summary>
        /// <returns>Hash code</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hashCode = 41;
                if (this.Response != null)
                    hashCode = hashCode * 59 + this.Response.GetHashCode();
                hashCode = hashCode * 59 + this.ErrorCode.GetHashCode();
                hashCode = hashCode * 59 + this.ThrottleSeconds.GetHashCode();
                if (this.ErrorStatus != null)
                    hashCode = hashCode * 59 + this.ErrorStatus.GetHashCode();
                if (this.Message != null)
                    hashCode = hashCode * 59 + this.Message.GetHashCode();
                if (this.MessageData != null)
                    hashCode = hashCode * 59 + this.MessageData.GetHashCode();
                if (this.DetailedErrorTrace != null)
                    hashCode = hashCode * 59 + this.DetailedErrorTrace.GetHashCode();
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

        public static BungieApiResponse<object> ToSimple<TT>(BungieApiResponse<TT> data)
        {
            var simpleData = new BungieApiResponse<object>()
            {
                Response = data.Response,
                ErrorCode = data.ErrorCode,
                ThrottleSeconds = data.ThrottleSeconds,
                ErrorStatus = data.ErrorStatus,
                Message = data.Message,
                MessageData = data.MessageData,
                DetailedErrorTrace = data.DetailedErrorTrace,

            };

            return simpleData;
        }
    }
}
