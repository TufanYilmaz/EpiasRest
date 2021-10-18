using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epias
{
    namespace Recive
    {
        public class Failed
        {

            [JsonProperty("message")]
            public string Message;

            [JsonProperty("code")]
            public string Code;

            [JsonProperty("eic")]
            public string Eic;

            [JsonProperty("meteringTime")]
            public string MeteringTime;
        }

        public class Body
        {

            [JsonProperty("successCount")]
            public int SuccessCount;

            [JsonProperty("failed")]
            public Failed[] Failed;
        }

        public class EpiasReciveAnswer
        {

            [JsonProperty("resultCode")]
            public string ResultCode;

            [JsonProperty("resultDescription")]
            public string ResultDescription;

            [JsonProperty("body")]
            public Body Body;
        }
    }
}
