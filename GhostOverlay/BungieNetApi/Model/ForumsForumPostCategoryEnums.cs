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
    /// Defines Forums.ForumPostCategoryEnums
    /// </summary>
    
    [JsonConverter(typeof(StringEnumConverter))]
    
    public enum ForumsForumPostCategoryEnums
    {
        /// <summary>
        /// Enum NUMBER_0 for value: 0
        /// </summary>
        [EnumMember(Value = "0")]
        NUMBER_0 = 1,

        /// <summary>
        /// Enum NUMBER_1 for value: 1
        /// </summary>
        [EnumMember(Value = "1")]
        NUMBER_1 = 2,

        /// <summary>
        /// Enum NUMBER_2 for value: 2
        /// </summary>
        [EnumMember(Value = "2")]
        NUMBER_2 = 3,

        /// <summary>
        /// Enum NUMBER_4 for value: 4
        /// </summary>
        [EnumMember(Value = "4")]
        NUMBER_4 = 4,

        /// <summary>
        /// Enum NUMBER_8 for value: 8
        /// </summary>
        [EnumMember(Value = "8")]
        NUMBER_8 = 5,

        /// <summary>
        /// Enum NUMBER_16 for value: 16
        /// </summary>
        [EnumMember(Value = "16")]
        NUMBER_16 = 6,

        /// <summary>
        /// Enum NUMBER_32 for value: 32
        /// </summary>
        [EnumMember(Value = "32")]
        NUMBER_32 = 7,

        /// <summary>
        /// Enum NUMBER_64 for value: 64
        /// </summary>
        [EnumMember(Value = "64")]
        NUMBER_64 = 8,

        /// <summary>
        /// Enum NUMBER_128 for value: 128
        /// </summary>
        [EnumMember(Value = "128")]
        NUMBER_128 = 9,

        /// <summary>
        /// Enum NUMBER_256 for value: 256
        /// </summary>
        [EnumMember(Value = "256")]
        NUMBER_256 = 10,

        /// <summary>
        /// Enum NUMBER_512 for value: 512
        /// </summary>
        [EnumMember(Value = "512")]
        NUMBER_512 = 11,

        /// <summary>
        /// Enum NUMBER_1024 for value: 1024
        /// </summary>
        [EnumMember(Value = "1024")]
        NUMBER_1024 = 12

    }

}
