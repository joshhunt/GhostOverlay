using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GhostSharper.Api;
using RestSharp;

namespace GhostOverlay
{
    [Serializable]
    public class BungieApiException : Exception
    {
        public DestinyServerResponse<object> Response;

        public BungieApiException()
        {
        }

        public BungieApiException(string message) : base(message)
        {
        }

        public BungieApiException(string message, Exception inner) : base(message, inner)
        {
        }

        // exception propagates from a remoting server to the client. 
        protected BungieApiException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public string RequestPath { get; set; }
        public RestRequest Request { get; set; }
    }
}
